using BepInEx;
using FFMpegCore;
using FFMpegCore.Pipes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ViralCompany.Recording.Video;
using ViralCompany.Util;

namespace ViralCompany.Recording.Encoding;
internal static class FFmpegEncoder
{
    internal static string FFmpegInstallPath
    {
        get
        {
            return Path.Combine(Paths.GameRootPath, "ffmpeg");
        }
    }

    public static async Task CreateClip(List<Texture2DVideoFrame> frames, RecordedClip clip)
    {
        Plugin.Logger.LogDebug("About to start encoding!");

        await FFMpegArguments
            .FromPipeInput(new RawVideoPipeSource(frames)
            {
                FrameRate = RecordingSettings.FRAMERATE
            })
            .OutputToFile(Path.Combine(clip.Video.FolderPath, $"{clip.ClipID}.temp.webm"))
            .LogArguments()
            .ProcessAsynchronously();

        Plugin.Logger.LogDebug("Frames to video done! Adding audio...");

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

    public static async Task CompileClipsToVideoAsync(RecordedVideo video)
    {
        var clips = video.GetAllSentClips();
        if (clips.Count == 0)
            throw new InvalidOperationException("No clips to compile.");

        // Wait for all encodes
        await Task.WhenAll(clips.Select(c => c.EncodeTask));

        // Safety: ensure files exist
        var missing = clips.Where(c => !File.Exists(c.FilePath)).Select(c => c.FilePath).ToArray();
        if (missing.Length > 0)
            throw new FileNotFoundException("Some clip files are missing:\n" + string.Join("\n", missing));

        await FFMpegArguments
            .FromDemuxConcatInput(clips.Select(c => c.FilePath).ToArray())
            .OutputToFile(video.FinalVideoPath)
            .ProcessAsynchronously();
    }
}
