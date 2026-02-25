using Dissonance.Audio.Capture;
using NAudio.Wave;
using System;
using System.IO;
using UnityEngine;
using ViralCompany.Recording.Encoding;
using ViralCompany.Recording.Video;

namespace ViralCompany.Recording.Audio;
internal class LocalPlayerMicRecorder : IMicrophoneSubscriber
{
    private WavWriter wavWriter;

    internal void StartRecording(RecordedClip clip)
    {
        wavWriter = new(Path.Combine(clip.Video.FolderPath, $"{clip.ClipID}.localMic.wav"));
    }

    internal void StopRecording()
    {
        Plugin.Logger.LogInfo($"Finished recording local mic for clip, closing wavWriter");
        wavWriter.Close();
        wavWriter = null;
    }

    internal void Flush()
    {
        wavWriter.Flush();
    }

    public void ReceiveMicrophoneData(ArraySegment<float> buffer, WaveFormat format)
    {
        if (format.SampleRate != AudioSettings.outputSampleRate)
        {
            Plugin.Logger.LogWarning("Microphone sample rate is not the same as AudioSettings!");
        }
        //if(format.Channels != 2) Plugin.Logger.LogWarning("Microphone channels: " + format.Channels);

        if (wavWriter != null)
        {
            wavWriter.WriteMonoAudio(buffer.Array);
        }
    }

    public void Reset() { }
}
