﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Netcode;
using UnityEngine.Animations.Rigging;
using ViralCompany.Recording.Audio;
using ViralCompany.Util;

namespace ViralCompany.Recording.Video;
internal class VideoRecorder
{
    internal static string TempRecordingPath
    {
        get
        {
            return Path.Combine(Path.GetTempPath(), "Lethal Company", "Viral Company", "recordings");
        }
    }

    internal const string VideoExtension = ".webm";

    internal static string GenerateRandomID()
    {
        return new Random().NextString("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz", 10);
    }

    internal RecordedVideo Video { get; private set; }
    internal RecordedClip CurrentClip { get; private set; }

    public VideoRecorder() : this(GenerateRandomID()) { }

    public VideoRecorder(string videoID)
    {
        if(Plugin.ModConfig.ExtendedLogging.Value)
            Plugin.Logger.LogDebug("new VideoRecorder! videoid: " + videoID);
        Video = new RecordedVideo(videoID);
    }

    public void StartClip()
    {
        CurrentClip = new RecordedClip(Video, GenerateRandomID());
        AudioRecorder.Instance.StartRecording(CurrentClip);
    }

    public void EndClip()
    {
        AudioRecorder.Instance.StopRecording();
        Video.RegisterClip(CurrentClip.ClipID);
        Video.StoreClip(CurrentClip.ClipID, CurrentClip);
        CurrentClip.ClipFinished();
    }
}
