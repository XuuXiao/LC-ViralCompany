using BepInEx;
using Dissonance;
using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Pipes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ViralCompany.Recording;
using ViralCompany.Recording.Encoding;
using ViralCompany.Recording.Video;
using ViralCompany.Util;

namespace ViralCompany.Recording.Encoding;
internal static class FFmpegEncoder {
    internal static string FFmpegInstallPath {
        get {
            return Path.Combine(Paths.GameRootPath, "ffmpeg");
        }
    }

    public static async Task CreateClip(List<Texture2DVideoFrame> frames, RecordedClip clip) {
        if(Plugin.ModConfig.ExtendedLogging.Value)
            Plugin.Logger.LogInfo("About to start encoding!");
        await FFMpegArguments
            .FromPipeInput(new RawVideoPipeSource(frames) {
                FrameRate = RecordingSettings.FRAMERATE
            })
            .OutputToFile(Path.Combine(clip.Video.FolderPath, $"{clip.ClipID}.temp.webm"))
            .LogArguments()
            .ProcessAsynchronously();

        if(Plugin.ModConfig.ExtendedLogging.Value)
            Plugin.Logger.LogInfo("Frames to video done! Adding audio...");

        await FFMpegArguments
            .FromFileInput(Path.Combine(clip.Video.FolderPath, $"{clip.ClipID}.temp.webm"))
            .AddFileInput(Path.Combine(clip.Video.FolderPath, $"{clip.ClipID}.localMic.wav"))
            .AddFileInput(Path.Combine(clip.Video.FolderPath, $"{clip.ClipID}.gameAudio.wav"))
            .OutputToFile(clip.FilePath,
                addArguments: args => args
                    .WithCustomArgument("-filter_complex \"[1:a][2:a]amerge=inputs=2[a]\"")
                    .WithCustomArgument("-map 0:v:0")
                    .WithCustomArgument("-map [a]")
                    .WithCustomArgument("-ac 2")
                    .WithCustomArgument("-c:v libvpx")
                    .WithCustomArgument("-c:a libvorbis")
                    .WithVideoBitrate(RecordingSettings.BITRATE)
            )
            .LogArguments()
            .ProcessAsynchronously();

        Plugin.Logger.LogInfo($"Finished encoding clip: {clip.ClipID}. Cleaning up other files now.");

        File.Delete(Path.Combine(clip.Video.FolderPath, $"{clip.ClipID}.temp.webm"));
        File.Delete(Path.Combine(clip.Video.FolderPath, $"{clip.ClipID}.localMic.wav"));
        File.Delete(Path.Combine(clip.Video.FolderPath, $"{clip.ClipID}.gameAudio.wav"));
        
        VideoUploader.Instance.HandleClipEncoded(clip);
    }

    public static async void CompileClipsToVideo(RecordedVideo video) {
        if (video.GetAllClips().Contains(null)) throw new NullReferenceException("Not all clips have been sent!");
        await FFMpegArguments.FromDemuxConcatInput(video.GetAllClips().Select(clip => clip.FilePath).ToArray()).OutputToFile(video.FinalVideoPath).ProcessAsynchronously();
        Plugin.Logger.LogInfo("Finished compiling clips to video!!");
    }
}
