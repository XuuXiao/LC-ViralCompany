using Dissonance;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using ViralCompany.Recording;
using ViralCompany.src.Recording;

namespace ViralCompany.Behaviours;
internal class AudioRecorder : MonoBehaviour {


    internal static List<AudioRecorder> audioRecorders = [];
    internal string fileName = "gameAudio.wav";

    WavWriter wavWriter;
    LocalPlayerMicRecorder micRecorder;

    void Awake() {
        audioRecorders.Add(this);
        micRecorder = new LocalPlayerMicRecorder();
        FindObjectOfType<DissonanceComms>().SubscribeToRecordedAudio(micRecorder);
    }

    void OnDisable() {
        audioRecorders.Remove(this);
        FindObjectOfType<DissonanceComms>().UnsubscribeFromRecordedAudio(micRecorder);
    }

    internal void StopRecording() {
        micRecorder.StopRecording();
        wavWriter.Close();
        wavWriter = null;
    }

    internal void StartRecording(RecordedClip clip) {
        wavWriter = new(Path.Combine(clip.Video.FolderPath, $"{clip.ClipID}.{fileName}"));
        micRecorder.StartRecording(clip);
    }

    void OnAudioFilterRead(float[] data, int channels) {
        if(wavWriter != null) {
            wavWriter.WriteStereoAudio(data); // audio data is interlaced
        }
    }
}
