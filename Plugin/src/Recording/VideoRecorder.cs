using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Netcode;
using UnityEngine.Animations.Rigging;
using ViralCompany.Behaviours;
using ViralCompany.Util;

namespace ViralCompany.Recording;
internal class VideoRecorder {
    internal static string TempRecordingPath { get {
        return Path.Combine(Path.GetTempPath(), "Lethal Company", "Viral Company", "recordings");
    } }

    internal static string VideoExtension { get { return ".webm"; } }
    internal static int Framerate { get { return 24; } }

    internal static string GenerateRandomID() {
        return new Random().NextString("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz", 10);
    }

    internal RecordedVideo Video { get; private set; }
    internal RecordedClip CurrentClip { get; private set; }

    public VideoRecorder() : this(GenerateRandomID()) { }

    public VideoRecorder(string videoID) {
        Plugin.Logger.LogDebug("new VideoRecorder! videoid: " + videoID);
        Video = new RecordedVideo(videoID);
    }

    public void StartClip() {
        CurrentClip = new RecordedClip(Video, GenerateRandomID());
        foreach(AudioRecorder audioRecorder in AudioRecorder.audioRecorders) {
            audioRecorder.StartRecording(CurrentClip);
        }
    }

    public void EndClip() {
        foreach(AudioRecorder audioRecorder in AudioRecorder.audioRecorders) {
            audioRecorder.StopRecording();
        }
            Video.RegisterClip(CurrentClip.ClipID);
        Video.SetClip(CurrentClip.ClipID, CurrentClip);
        CurrentClip.ClipFinished();
    }
}
