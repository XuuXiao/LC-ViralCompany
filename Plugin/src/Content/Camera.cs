using System;
using System.Collections;
using System.Diagnostics;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using ViralCompany.Recording;
using ViralCompany.Recording.Audio;
using ViralCompany.Recording.Encoding;
using ViralCompany.Recording.Video;
using ViralCompany.Util;

namespace ViralCompany;
public class CameraItem : GrabbableObject
{
    public AudioSource CameraSFX;
    public float maxLoudness;
    public float minLoudness;
    public float minPitch;
    public float maxPitch;
    public Material screenRecordingOverlay;
    public AudioClip startRecordSound;
    public AudioClip endRecordSound;
    public AudioClip errorSound;
    public Animator cameraAnimator;
    public AudioClip recordingFinishedSound;
    
    // LED
    [SerializeField]
    Renderer ledRenderer;

    [SerializeField]
    Material ledOffMaterial, ledOnMaterial;

    [SerializeField]
    Transform backCameraPosition, frontCameraPosition;

    [SerializeField]
    float minFOV = 25;

    [SerializeField]
    float maxFOV = 60;

    [SerializeField]
    float zoomSpeed = 40;
    
    [NonSerialized]
    public Material screenMaterial;
    [NonSerialized]
    public Transform screenTransform;
    public AnimationClip openCameraAnimation;
    [NonSerialized]
    public bool cooldownPassed = true;
    [NonSerialized]
    public bool cameraOpen;
    public RenderTexture renderTexture;
    [NonSerialized]
    public float recordingTime = 0;

    public enum RecordState
    {
        Off,
        On,
        Finished,
    }

    public enum CameraState
    {
        Front,
        Back
    }

    private NetworkVariable<RecordState> recordState = new(RecordState.Off, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public CameraState cameraState = CameraState.Front;

    // MAKE SURE TO RESET THIS WHEN EXTRACED.
    internal VideoRecorder Recorder { get; private set; }

    float timeSinceLastSavedFrame = 0;
    Camera recordingCamera;

    private void StartNewVideo()
    {
        Recorder = new VideoRecorder();
        CreateRecorderServerRPC(Recorder.Video.VideoID);
    }

    public override void Start()
    {
        base.Start();
        // silly patch fix because for some reason the AudioRecorder just doesn't like to get inited sometimes??
        if (AudioRecorder.Instance == null)
        {
            GameNetworkManager.Instance.localPlayerController.GetComponentInChildren<AudioListener>().gameObject.AddComponent<AudioRecorder>();
        }

        Material newMaterial = new(Shader.Find("HDRP/Lit"))
        {
            color = Color.white
        };

        renderTexture = new RenderTexture(RecordingSettings.RESOLUTION, RecordingSettings.RESOLUTION, 0);

        recordingCamera = GetComponentInChildren<Camera>();
        recordingCamera.targetTexture = renderTexture;
        recordingCamera.cullingMask = GameNetworkManager.Instance.localPlayerController.gameplayCamera.cullingMask;
        
        newMaterial.mainTexture = renderTexture;

        screenTransform = this.transform.Find("Armature/Bone/Bone.001/Bone.002/Bone.003/Bone.004/Bone.004_end/Screen");
        MeshRenderer screenMeshRenderer = screenTransform.GetComponent<MeshRenderer>();
        screenMaterial = screenMeshRenderer.material;
        screenMeshRenderer.material = newMaterial;
        screenMeshRenderer.material.color = Color.black;
        
        // update recording led
        recordState.OnValueChanged += (value, newValue) =>
        {
            if (newValue == RecordState.Off || newValue == RecordState.Finished)
            {
                ledRenderer.material = ledOffMaterial;
            }
            else
            {
                ledRenderer.material = ledOnMaterial;
            }
        };
    }

    public override void DiscardItem()
    {
        base.DiscardItem();
        cameraAnimator.SetTrigger("closeCamera");
        StartCoroutine(PowerDownCamera());
        cameraOpen = false;
        if (IsOwner && recordState.Value == RecordState.On)
        {
            Plugin.Logger.LogDebug($"Recording stopped from discarding");
            StopRecording();
        }
    }

    public override void GrabItem()
    {
        base.GrabItem();
        cameraAnimator.SetTrigger("openCamera");
        StartCoroutine(StartUpCamera());
        cameraOpen = true;
    }

    // Update Tooltips
    public override void EquipItem()
    {
        itemProperties.toolTips = [
            $"Start/Pause Recording : [{Plugin.InputActionsInstance.ToggleRecordingKey.GetBindingDisplayString().Split(' ')[0]}]",
            $"Zoom Camera Out/In : [{Plugin.InputActionsInstance.ZoomOutLevelKey.GetBindingDisplayString().Split(' ')[0]}/{Plugin.InputActionsInstance.ZoomInLevelKey.GetBindingDisplayString().Split(' ')[0]}]",
            $"Front/Back Camera : [{Plugin.InputActionsInstance.FlipCameraKey.GetBindingDisplayString().Split(' ')[0]}]",
        ];
        base.EquipItem();
    }

    public override void Update()
    {
        base.Update();
        if (recordState.Value == RecordState.Off || recordState.Value == RecordState.Finished)
        {
            isBeingUsed = false;
        }

        if (!IsOwner || !isHeld)
        {
            return;
        }

        if (isPocketed)
        {
            if (Recorder == null || recordState.Value != RecordState.On)
            {
                return;
            }

            Plugin.Logger.LogDebug("Recording Stopped from pocketing");
            StopRecording();
            return;
        }

        if (isBeingUsed && insertedBattery.charge <= 0)
        {
            Plugin.Logger.LogDebug("Recording Stopped - Battery Dead");
            StopRecording();
            return;
        }

        DetectToggleRecording();
        DetectFlipCamera();
        DetectZoomChange();
        DetectRefreshingNewVideo();
        
        if (isBeingUsed)
        {
            if (Recorder == null || Recorder.CurrentClip == null)
            {
                Plugin.Logger.LogWarning("Camera is marked as recording but no active clip exists.");
                return;
            }

            timeSinceLastSavedFrame -= Time.deltaTime;

            if (timeSinceLastSavedFrame <= 0)
            {
                timeSinceLastSavedFrame += 1f / RecordingSettings.FRAMERATE;
                Recorder.CurrentClip.AddFrame(renderTexture.GetTexture2D());
                AudioRecorder.Instance.Flush();
            }
        }
    }

    private bool _refreshing;

    private void DetectRefreshingNewVideo()
    {
        if (Recorder == null) return;

        if (Plugin.InputActionsInstance.NewVideoKey.triggered && !_refreshing)
            _ = RefreshVideoAsync();
    }

    private async Task RefreshVideoAsync()
    {
        _refreshing = true;

        try
        {
            bool wasRecording = isBeingUsed && recordState.Value == RecordState.On;

            if (wasRecording)
            {
                StopRecording(); // this calls Recorder.EndClip(), which starts encoding
            }

            await FFmpegEncoder.CompileClipsToVideoAsync(Recorder.Video);

            StartNewVideo();

            // Optional: continue recording immediately
            if (wasRecording)
            {
                StartRecording();
            }
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"Refresh failed: {e}");
            PlaySoundByID("error");
        }
        finally
        {
            _refreshing = false;
        }
    }

    public void DetectZoomChange()
    {
        float fov = recordingCamera.fieldOfView;
        if (Plugin.InputActionsInstance.ZoomOutLevelKey.IsPressed())
        {
            fov += Time.deltaTime * zoomSpeed;
        }

        if (Plugin.InputActionsInstance.ZoomInLevelKey.IsPressed())
        {
            fov -= Time.deltaTime * zoomSpeed;
        }
        
        recordingCamera.fieldOfView = Mathf.Clamp(fov, minFOV, maxFOV);
    }
    
    public void DetectFlipCamera()
    {
        if (Plugin.InputActionsInstance.FlipCameraKey.triggered)
        {
            // handles flipping the camera
            cameraState = cameraState == CameraState.Front ? CameraState.Back : CameraState.Front;
            recordingCamera.transform.parent = cameraState == CameraState.Front ? frontCameraPosition : backCameraPosition;
            recordingCamera.transform.localEulerAngles = Vector3.zero;
            recordingCamera.transform.localPosition = Vector3.zero;
        }
    }
    
    public void DetectToggleRecording()
    {
        if (Plugin.InputActionsInstance.ToggleRecordingKey.triggered && cameraOpen)
        {
            if (recordState.Value == RecordState.On)
            {
                Plugin.Logger.LogDebug("Recording Stopped from triggering recording key");
                StopRecording();
            }
            else
            {
                StartRecording(); 
                Plugin.Logger.LogDebug("Recording started");
            }
        }
    }
    
    public void StartRecording()
    {
        recordState.Value = RecordState.On;
        PlaySoundByID("startRecord");
        isBeingUsed = true;
        //Play on sound

        if (Recorder == null)
        {
            StartNewVideo();
        }

        Recorder.StartClip();
    }

    public void StopRecording()
    {
        isBeingUsed = false;
        Recorder.EndClip();
        
        if (insertedBattery.charge <= 0)
        {
            recordState.Value = RecordState.Finished;
            PlaySoundByID("recordingFinished");
            Plugin.Logger.LogDebug("Recording finished");
            return;
        }

        recordState.Value = RecordState.Off;
        PlaySoundByID("stopRecord");
    }

    public void PlaySoundByID(string soundID)
    {
        if (IsHost)
        {
            PlaySoundClientRpc(soundID);
        }
        else
        {
            PlaySoundServerRpc(soundID);
        }
    }

    // merging of clips will be handled by the extraction machine, because otherwise we don't know if they will record more.

    [ServerRpc]
    private void CreateRecorderServerRPC(string videoID)
    {
        CreateRecorderClientRPC(videoID);
    }

    [ClientRpc]
    private void CreateRecorderClientRPC(string videoID)
    {
        if (Recorder == null || Recorder.Video.VideoID != videoID)
        {
            Recorder = new VideoRecorder(videoID);
        }
    }

    private IEnumerator StartUpCamera()
    {
        yield return new WaitForSeconds(openCameraAnimation.length/3);
        screenTransform.GetComponent<MeshRenderer>().material.color = Color.white;
    }

    private IEnumerator PowerDownCamera()
    {
        yield return new WaitForSeconds(openCameraAnimation.length/3);
        screenTransform.GetComponent<MeshRenderer>().material.color = Color.black;
    }

    [ServerRpc]
    public void PlaySoundServerRpc(string sound)
    {
        PlaySoundClientRpc(sound);
    }

    [ClientRpc]
    public void PlaySoundClientRpc(string sound)
    {
        PlaySound(sound);
    }

    public void PlaySound(string soundID)
    {
        Plugin.Logger.LogDebug("Playing sound: " + soundID);
        AudioClip sound = soundID switch
        {
            "startRecord" => startRecordSound,
            "endRecord" => endRecordSound,
            "recordingFinished" => recordingFinishedSound,
            _ => errorSound,
        };
        CameraSFX.PlayOneShot(sound, 1);

        WalkieTalkie.TransmitOneShotAudio(CameraSFX, sound, 1);
        RoundManager.Instance.PlayAudibleNoise(transform.position, 10, 1, 0, isInShipRoom && StartOfRound.Instance.hangarDoorsClosed);
    }
}