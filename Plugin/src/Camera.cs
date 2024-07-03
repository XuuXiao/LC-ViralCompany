using System;
using System.Collections;
using System.Diagnostics;
using Dissonance;
using Dissonance.Audio.Capture;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using ViralCompany;
using ViralCompany.Recording;
using ViralCompany.Recording.Audio;
using ViralCompany.Recording.Encoding;
using ViralCompany.Recording.Video;
using ViralCompany.src.Recording;
using ViralCompany.Util;

namespace ViralCompany.CameraScrap;
public class CameraItem : GrabbableObject {
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
    void LogIfDebugBuild(string text) {
      #if DEBUG
      Plugin.Logger.LogInfo(text);
      #endif
    }
    public enum RecordState {
        Off,
        On,
        Finished,
    }

    public enum CameraState {
        Front,
        Back
    }
    public NetworkVariable<RecordState> recordState = new NetworkVariable<RecordState>(RecordState.Off, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public CameraState cameraState = CameraState.Front;
    public override void ItemActivate(bool used, bool buttonDown = true) { }

    // MAKE SURE TO RESET THIS WHEN EXTRACED.
    internal VideoRecorder Recorder { get; private set; }

    float timeSinceLastSavedFrame = 0;
    Camera recordingCamera;

    void StartNewVideo() {
        Recorder = new VideoRecorder();

        CreateRecorderServerRPC(Recorder.Video.VideoID);
    }

    public override void Start() {
        base.Start();

        // silly patch fix because for some reason the AudioRecorder just doesn't like to get inited sometimes??
        if (AudioRecorder.Instance == null) {
            GameNetworkManager.Instance.localPlayerController.GetComponentInChildren<AudioListener>().gameObject.AddComponent<AudioRecorder>();
        }

        Material newMaterial = new Material(Shader.Find("HDRP/Lit"));
        newMaterial.color = Color.white;

        renderTexture = new RenderTexture(RecordingSettings.RESOLUTION, RecordingSettings.RESOLUTION, 0);

        recordingCamera = GetComponentInChildren<Camera>();
        recordingCamera.targetTexture = renderTexture;
        recordingCamera.cullingMask = GameNetworkManager.Instance.localPlayerController.gameplayCamera.cullingMask;
        
        newMaterial.mainTexture = renderTexture;

        screenTransform = this.transform.Find("Armature").Find("Bone").Find("Bone.001").Find("Bone.002").Find("Bone.003").Find("Bone.004").Find("Bone.004_end").Find("Screen");
        if (screenTransform != null) {
            MeshRenderer screenMeshRenderer = screenTransform.GetComponent<MeshRenderer>();
            screenMaterial = screenTransform.GetComponent<MeshRenderer>().material;
            if (screenMeshRenderer != null) {
                screenMeshRenderer.material = newMaterial;
            } else {
                LogIfDebugBuild("MeshRenderer not found on 'Screen' GameObject.");
            }
        } else {
            LogIfDebugBuild("'Screen' GameObject not found in the hierarchy.");
        }
        screenTransform.GetComponent<MeshRenderer>().material.color = Color.black;
        
        // update recording led
        recordState.OnValueChanged += (value, newValue) => {
            if (newValue == RecordState.Off) {
                ledRenderer.material = ledOffMaterial;
            } else {
                ledRenderer.material = ledOnMaterial;
            }
        };
    }

    public override void DiscardItem() {
        base.DiscardItem();
        DoAnimation("closeCamera");
        StartCoroutine(PowerDownCamera());
        cameraOpen = false;
        if (recordState.Value == RecordState.On) {
            StopRecording();
        }
    }

    public override void GrabItem() {
        base.GrabItem();
        DoAnimation("openCamera");
        StartCoroutine(StartUpCamera());
        cameraOpen = true;
    }

    // Update Tooltips
    public override void EquipItem() {
        itemProperties.toolTips = [
            $"Start/Pause Recording : [{Plugin.InputActionsInstance.ToggleRecordingKey.GetBindingDisplayString().Split(' ')[0]}]",
            $"Zoom Camera Out/In : [{Plugin.InputActionsInstance.ZoomOutLevelKey.GetBindingDisplayString().Split(' ')[0]}/{Plugin.InputActionsInstance.ZoomInLevelKey.GetBindingDisplayString().Split(' ')[0]}]",
            $"Front/Back Camera : [{Plugin.InputActionsInstance.FlipCameraKey.GetBindingDisplayString().Split(' ')[0]}]",
        ];
        base.EquipItem();
    }

    public override void Update() {
        base.Update();
        if (recordState.Value == RecordState.Off || recordState.Value == RecordState.Finished) {
            isBeingUsed = false;
        }
        if(!IsOwner || !isHeld) return;
        if (isPocketed) {
            if(Recorder == null || recordState.Value != RecordState.On) return;
            StopRecording();
            LogIfDebugBuild("Recording Stopped");
            return;
        }
        if (insertedBattery.charge <= 0) {
            StopRecording();
        }
        DetectToggleRecording();
        DetectFlipCamera();
        DetectZoomChange();

        if (Keyboard.current.f3Key.wasPressedThisFrame) {
            FFmpegEncoder.CompileClipsToVideo(Recorder.Video);
            StartNewVideo();

            Process.Start(VideoRecorder.TempRecordingPath);
        }
        
        if(isBeingUsed) {
            timeSinceLastSavedFrame -= Time.deltaTime;

            if(timeSinceLastSavedFrame <= 0) {
                timeSinceLastSavedFrame += (float)1 / RecordingSettings.FRAMERATE;
                Recorder.CurrentClip.AddFrame(renderTexture.GetTexture2D());
                AudioRecorder.Instance.Flush();
            }
        }
    }

    public void DetectZoomChange() {
        float fov = recordingCamera.fieldOfView;
        if (Plugin.InputActionsInstance.ZoomOutLevelKey.IsPressed()) {
            fov += Time.deltaTime * zoomSpeed;
        }
        if (Plugin.InputActionsInstance.ZoomInLevelKey.IsPressed()) {
            fov -= Time.deltaTime * zoomSpeed;
        }
        
        recordingCamera.fieldOfView = Mathf.Clamp(fov, minFOV, maxFOV);
    }
    
    public void DetectFlipCamera() {
        if(Plugin.InputActionsInstance.FlipCameraKey.triggered) {
            // handles flipping the camera
            cameraState = cameraState == CameraState.Front ? CameraState.Back : CameraState.Front;
            recordingCamera.transform.parent = cameraState == CameraState.Front ? frontCameraPosition : backCameraPosition;
            recordingCamera.transform.localEulerAngles = Vector3.zero;
            recordingCamera.transform.localPosition = Vector3.zero;
        }
    }
    
    public void DetectToggleRecording() {
        if (Plugin.InputActionsInstance.ToggleRecordingKey.triggered && cameraOpen) {
            if (recordState.Value == RecordState.On) {
                StopRecording();
                LogIfDebugBuild("Recording Stopped");
            } else {
                StartRecording(); 
                LogIfDebugBuild("Recording started");
            }
        }
    }
    
    public void StartRecording() {
        recordState.Value = RecordState.On;
        PlaySoundByID("startRecord");
        isBeingUsed = true;
        //Play on sound

        if(Recorder == null) {
            StartNewVideo();
        }

        Recorder.StartClip();
    }

    public void StopRecording() {
        isBeingUsed = false;
        Recorder.EndClip();
        
        if (insertedBattery.charge <= 0) {
            recordState.Value = RecordState.Finished;
            PlaySoundByID("recordingFinished");
            LogIfDebugBuild("Recording finished");
            return;
        }
        recordState.Value = RecordState.Off;
        PlaySoundByID("stopRecord");
    }

    public void PlaySoundByID(string soundID) {
        if (IsHost) {
            PlaySoundClientRpc(soundID);
        } else {
            PlaySoundServerRpc(soundID);
        }
    }

    // merging of clips will be handled by the extraction machine, because otherwise we don't know if they will record more.

    [ServerRpc]
    void CreateRecorderServerRPC(string videoID) {
        CreateRecorderClientRPC(videoID);
    }

    [ClientRpc]
    void CreateRecorderClientRPC(string videoID) {
        if(Recorder == null || Recorder.Video.VideoID != videoID)
            Recorder = new VideoRecorder(videoID);
    }

    IEnumerator StartUpCamera() {
        yield return new WaitForSeconds(openCameraAnimation.length/3);
        screenTransform.GetComponent<MeshRenderer>().material.color = Color.white;
    }
    IEnumerator PowerDownCamera() {
        yield return new WaitForSeconds(openCameraAnimation.length/3);
        screenTransform.GetComponent<MeshRenderer>().material.color = Color.black;
    }
    IEnumerator CooldownPassing() {
        yield return new WaitForSeconds(2f);
        cooldownPassed = true;
    }
    public override void ChargeBatteries()
    {
        base.ChargeBatteries();
    }
    [ServerRpc]
    public void PlaySoundServerRpc(string sound) {
        PlaySoundClientRpc(sound);
    }

    [ClientRpc]
    public void PlaySoundClientRpc(string sound) {
        PlaySound(sound);
    }

    public void PlaySound(string soundID) {
        LogIfDebugBuild("Playing target switch sound");

        AudioClip sound;
        switch (soundID)
        {
            case "startRecord":
                sound = startRecordSound;
                break;
            case "endRecord":
                sound = endRecordSound;
                break;
            case "recordingFinished":
                sound = recordingFinishedSound;
                break;
            default:
                sound = errorSound;
                return;
        }

        CameraSFX.PlayOneShot(sound, 1);

        WalkieTalkie.TransmitOneShotAudio(CameraSFX, sound, 1);
        RoundManager.Instance.PlayAudibleNoise(transform.position, 10, 1, 0, isInShipRoom && StartOfRound.Instance.hangarDoorsClosed);
    }
    public void DoAnimation(string animationName) {
        LogIfDebugBuild($"doing animation locally: {animationName}");
        cameraAnimator.SetTrigger(animationName);
        if(IsHost) {
            DoAnimationClientRpc(animationName);
        } else {
            DoAnimationServerRpc(animationName);
        }
    }

    [ServerRpc]
    public void DoAnimationServerRpc(string animationName) {
        DoAnimationClientRpc(animationName);
    }

    [ClientRpc]
    public void DoAnimationClientRpc(string animationName) {
        if(IsOwner) return;
        LogIfDebugBuild($"Animation: {animationName}");
        cameraAnimator.SetTrigger(animationName);
    }
}