using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements.UIR;

namespace ViralCompany;
public class Camera : GrabbableObject {
    public AudioSource CameraSFX;
    public float maxLoudness;
    public float minLoudness;
    public float minPitch;
    public float maxPitch;
    public AudioClip turnOnSound;
    public AudioClip turnOffSound;
    public Animator cameraAnimator;
    public AudioClip recordingFinishedSound;
    public Material screenMaterial;
    public AnimationClip openCameraAnimation;
    [NonSerialized]
    public bool cameraOpen;
    [NonSerialized]
    public RenderTexture renderTexture;
    [NonSerialized]
    public bool isRecording = false;
    [NonSerialized]
    public int recordingTime;
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
    public override void ItemActivate(bool used, bool buttonDown = true) {
        if (cameraOpen) {
            DoAnimationClientRpc("closeCamera");
            screenMaterial.color = Color.black;
            cameraOpen = false;
        } else {
            DoAnimationClientRpc("openCamera");
            cameraOpen = true;
            StartCoroutine(StartUpCamera());
        }
        
        // check if its a specific button, if so, start recording/stop recording/hold camera up to face.
    }
    public void StartRecording() {
        recordState.Value = RecordState.On;
        isRecording = true;
        //Play on sound
    }

    public void StopRecording() {
        if (insertedBattery.empty) {
            recordState.Value = RecordState.Finished;
            return;
        }
        recordState.Value = RecordState.On;
        isRecording = false;
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