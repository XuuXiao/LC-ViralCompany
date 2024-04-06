using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using ViralCompany;

namespace ViralCompany.Camera;
public class Camera : GrabbableObject {
    public AudioSource CameraSFX;
    public float maxLoudness;
    public float minLoudness;
    public float minPitch;
    public float maxPitch;
    public Material screenRecordingOverlay;
    public AudioClip turnOnSound;
    public AudioClip turnOffSound;
    public Animator cameraAnimator;
    public AudioClip recordingFinishedSound;
    public Material screenMaterial;
    public TextMesh recordingTimeText;
    public AnimationClip openCameraAnimation;
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
    public NetworkVariable<RecordState> recordState = new NetworkVariable<RecordState>(RecordState.Off, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public override void Start() {
        base.Start();
        screenMaterial.color = Color.black;
    }
    public override void Update() {
        base.Update();
        if (!isHeld && cameraOpen) {
            DoAnimationClientRpc("closeCamera");
            screenMaterial.color = Color.black;
            cameraOpen = false;
            if (recordState.Value == RecordState.On) {
                StopRecording();

            }
        }
        if (recordState.Value == RecordState.On) {
            recordingTime += Time.deltaTime;
            TimeSpan time = TimeSpan.FromSeconds(recordingTime);
            recordingTimeText.text = time.ToString(@"mm\:ss\:fff");
        }
        DetectOffRecordButton();
        DetectOnRecordButton();
    }
    public override void ItemActivate(bool used, bool buttonDown = true) {
        base.ItemActivate(used, buttonDown);
        if (cameraOpen) {
            DoAnimationClientRpc("closeCamera");
            screenMaterial.color = Color.black;
            cameraOpen = false;
            StopRecording();
            return;
        } else {
            DoAnimationClientRpc("openCamera");
            cameraOpen = true;
            StartCoroutine(StartUpCamera());
            return;
        }
        
        // check if its a specific button, if so, start recording/stop recording/hold camera up to face.
    }
    public void DetectOnRecordButton() {
        if (!isHeld) return;
        if (Plugin.InputActionsInstance.StartRecordKey.triggered && recordState.Value == RecordState.Off && cameraOpen) {
            StartRecording();
            LogIfDebugBuild("Recording started");
            return;
        }
        return;
    }
    public void DetectOffRecordButton() {
        if (!isHeld) return;
        if (Plugin.InputActionsInstance.StopRecordKey.triggered && recordState.Value != RecordState.Off && cameraOpen) {
            StopRecording();
            LogIfDebugBuild("Recording Stopped");
            return;
        }
        return;
    }
    public void StartRecording() {
        recordState.Value = RecordState.On;
        //Play on sound
    }

    public void StopRecording() {
        /*if (insertedBattery.empty) {
            recordState.Value = RecordState.Finished;
            LogIfDebugBuild("Recording finished");
            return;
        }*/
        recordState.Value = RecordState.Off;
        //Play off sound
    }

    public void PlaySoundByID(string soundID) {
        if (IsHost) {
            PlaySoundClientRpc(soundID);
        } else {
            PlaySoundServerRpc(soundID);
        }
    }
    IEnumerator StartUpCamera() {
        yield return new WaitForSeconds(openCameraAnimation.length/3);
        screenMaterial.color = Color.white;
        StopCoroutine(StartUpCamera());
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

        AudioClip sound = null;

        switch (soundID)
        {
            case "turnOn":
                sound = turnOnSound;
                break;
            case "turnOff":
                sound = turnOffSound;
                break;
            case "recordingFinished":
                sound = recordingFinishedSound;
                break;
            default:
                return;
        }

        CameraSFX.PlayOneShot(sound, 1);

        WalkieTalkie.TransmitOneShotAudio(CameraSFX, sound, 1);
        RoundManager.Instance.PlayAudibleNoise(transform.position, 10, 1, 0, isInShipRoom && StartOfRound.Instance.hangarDoorsClosed);
    }
    public void SwitchRecordState(RecordState state) {

    }
    [ClientRpc]
    public void DoAnimationClientRpc(string animationName) {
        LogIfDebugBuild($"Animation: {animationName}");
        cameraAnimator.SetTrigger(animationName);
    }
}