using Dissonance;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using ViralCompany.Recording.Encoding;
using ViralCompany.Recording.Video;

namespace ViralCompany.Recording.Audio;
internal class AudioRecorder : MonoBehaviour
{


    internal static AudioRecorder Instance;


    WavWriter wavWriter;
    LocalPlayerMicRecorder micRecorder;

    void Awake()
    {
        Instance = this;
        micRecorder = new LocalPlayerMicRecorder();
        FindObjectOfType<DissonanceComms>().SubscribeToRecordedAudio(micRecorder);
    }

    void OnDisable()
    {
        Instance = null;
        FindObjectOfType<DissonanceComms>().UnsubscribeFromRecordedAudio(micRecorder);
    }

    internal void StopRecording()
    {
        micRecorder.StopRecording();
        wavWriter.Close();
        wavWriter = null;
    }
    internal void Flush() {
        wavWriter.Flush();
        micRecorder.Flush();
    }

    internal void StartRecording(RecordedClip clip)
    {
        wavWriter = new(Path.Combine(clip.Video.FolderPath, $"{clip.ClipID}.gameAudio.wav"));
        micRecorder.StartRecording(clip);
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (wavWriter != null)
        {
            wavWriter.WriteStereoAudio(data); // audio data is interlaced
        }
    }
}
