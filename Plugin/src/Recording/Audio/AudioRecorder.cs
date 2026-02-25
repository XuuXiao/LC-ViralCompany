using Dissonance;
using System.IO;
using UnityEngine;
using ViralCompany.Recording.Encoding;
using ViralCompany.Recording.Video;

namespace ViralCompany.Recording.Audio;
internal class AudioRecorder : MonoBehaviour
{
    internal static AudioRecorder Instance;
    private WavWriter wavWriter;
    private LocalPlayerMicRecorder micRecorder;

    private void Awake()
    {
        Instance = this;
        micRecorder = new LocalPlayerMicRecorder();
        FindObjectOfType<DissonanceComms>().SubscribeToRecordedAudio(micRecorder);
    }

    private void OnDestroy()
    {
        Instance = null;
        FindObjectOfType<DissonanceComms>().UnsubscribeFromRecordedAudio(micRecorder); // TODO: this errors, unsubscribe before leaving ig?
    }

    internal void StopRecording()
    {
        micRecorder.StopRecording();
        wavWriter.Close();
        wavWriter = null;
    }

    internal void Flush()
    {
        wavWriter.Flush();
        micRecorder.Flush();
    }

    internal void StartRecording(RecordedClip clip)
    {
        wavWriter = new(Path.Combine(clip.Video.FolderPath, $"{clip.ClipID}.gameAudio.wav"));
        micRecorder.StartRecording(clip);
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (wavWriter != null)
        {
            wavWriter.WriteStereoAudio(data); // audio data is interlaced
        }
    }
}
