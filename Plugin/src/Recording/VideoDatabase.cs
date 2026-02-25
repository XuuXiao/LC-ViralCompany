using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ViralCompany.Recording.Video;

namespace ViralCompany.Recording;
internal class VideoDatabase : MonoBehaviour
{
    internal static Dictionary<string, RecordedVideo> videos = [];

    private void OnApplicationQuit()
    {
        // clear out temp folder
        if (Directory.Exists(VideoRecorder.TempRecordingPath))
        {
            Plugin.Logger.LogInfo("Deleting temprecordingpath folder.");
            Directory.Delete(VideoRecorder.TempRecordingPath, true);
        }
    }
}
