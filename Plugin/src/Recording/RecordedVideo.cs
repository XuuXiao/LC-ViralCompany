using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine.Timeline;
using ViralCompany.Recording;

namespace ViralCompany.Recording;
internal class RecordedVideo {
    Dictionary<string, RecordedClip> Clips = [];

    public string VideoID { get; private set; }
    public string FolderPath { get {
        return Path.Combine(VideoRecorder.TempRecordingPath, VideoID);
    } }

    public string FinalVideoPath { get {
            return Path.Combine(VideoRecorder.TempRecordingPath, VideoID + VideoRecorder.VideoExtension);
        } }

    public bool IsValid { get {
        List<RecordedClip> clips = GetAllClips();
        foreach(RecordedClip clip in clips) {
            if(clip == null) return false;
            if(!clip.IsValid) return false;
        }

        return true;
    } }

    public RecordedVideo(string VideoID) { 
        this.VideoID = VideoID;
        
        // make sure that the directory exists lol
        Directory.CreateDirectory(FolderPath);
    }

    /// <summary>
    /// Registers a clip, the clip has not been shared to everybody yet and so call GetClip with the clipID may result in null.
    /// </summary>
    public void RegisterClip(string clipID) {
        Clips.Add(clipID, null);
    }

    /// <summary>
    /// Gets all clips that have been registered, if a clip has not been fully sent it will be null
    /// </summary>
    /// <returns>All Clips registered, null values for clips not sent</returns>
    public List<RecordedClip> GetAllClips() {
        return [.. Clips.Values];
    }
    /// <summary>
    /// Gets all clips that have been registered AND sent.
    /// </summary>
    /// <returns>All clips sent, excludes null values for ones that have not been sent</returns>
    public List<RecordedClip> GetAllSentClips() {
        return GetAllClips().Where(clip => clip != null).ToList();
    }

    public void SetClip(string clipID, RecordedClip clip) {
        if(!Clips.ContainsKey(clipID)) throw new ArgumentOutOfRangeException($"ClipID: '{clipID}' has not been registered and yet it was downloaded?!?!");
        if(Clips[clipID] != null) throw new ArgumentException($"ClipID: '{clipID}' has already been sent and added to the recorded video!");

        Clips[clipID] = clip;
        Plugin.Logger.LogDebug($"Sucessfully added clip: '{clipID}' to recorded video.");
    }
}
