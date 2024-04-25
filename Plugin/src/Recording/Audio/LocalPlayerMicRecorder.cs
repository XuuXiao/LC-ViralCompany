using Dissonance.Audio.Capture;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using ViralCompany.Recording.Encoding;
using ViralCompany.Recording.Video;

namespace ViralCompany.Recording.Audio;
internal class LocalPlayerMicRecorder : IMicrophoneSubscriber {
    WavWriter wavWriter;

    internal void StartRecording(RecordedClip clip) {
        wavWriter = new(Path.Combine(clip.Video.FolderPath, $"{clip.ClipID}.localMic.wav")) {
            Channels = 1
        };
    }
    internal void StopRecording() {
        wavWriter.Close();
        wavWriter = null;
    }

    public void ReceiveMicrophoneData(ArraySegment<float> buffer, WaveFormat format) {
        if(format.SampleRate != AudioSettings.outputSampleRate) Plugin.Logger.LogWarning("Microphone sample rate is not the same as AudioSettings!");
        //if(format.Channels != 2) Plugin.Logger.LogWarning("Microphone channels: " + format.Channels);

        if(wavWriter != null) wavWriter.WriteMonoAudio(buffer.Array);
    }

    public void Reset() {

    }
}
