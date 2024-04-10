using BepInEx;
using FFMpegCore;
using FFMpegCore.Pipes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ViralCompany.Recording;

namespace ViralCompany.Recording;
internal static class FFmpegEncoder {
    internal static string FFmpegInstallPath { get {
            return Path.Combine(Paths.GameRootPath, "ffmpeg");
        } }

    public static async Task ConvertFramesToVideo(List<Texture2DVideoFrame> frames, string outputFile) {
        await FFMpegArguments
            .FromPipeInput(new RawVideoPipeSource(frames))
            .OutputToFile(outputFile)
            .ProcessAsynchronously();
    }
}
