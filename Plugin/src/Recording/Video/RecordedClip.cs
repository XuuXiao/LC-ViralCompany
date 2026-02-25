using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using ViralCompany.Recording.Encoding;

namespace ViralCompany.Recording.Video;
internal class RecordedClip(RecordedVideo video, string clipID)
{
    const int CHUNK_SIZE = 50_000;

    public string FilePath
    {
        get
        {
            return Path.Combine(Video.FolderPath, ClipID + VideoRecorder.VideoExtension);
        }
    }

    public bool IsValid { get; private set; }

    public string ClipID { get; private set; } = clipID;

    public RecordedVideo Video { get; private set; } = video;
    private List<Texture2DVideoFrame> frames;

    // maybe move this into a seperate class/struct?
    internal Dictionary<int, byte[]> DownloadedChunkData;
    internal int ChunkCountToDownload;

    public void AddFrame(Texture2D frame)
    {
        if (frames == null)
        {
            Plugin.Logger.LogDebug("Inited RecordedClip for recording.");
            frames = [];
        }

        frames.Add(new Texture2DVideoFrame(frame));
    }

    public Task EncodeTask { get; private set; } = Task.CompletedTask;

    public void StartEncoding()
    {
        // avoid double-start
        if (EncodeTask is not null && !EncodeTask.IsCompleted)
            return;

        EncodeTask = EncodeInternal();
    }

    private async Task EncodeInternal()
    {
        if (frames == null || frames.Count == 0)
        {
            throw new InvalidOperationException($"Clip {ClipID} has no frames to encode.");
        }

        await FFmpegEncoder.CreateClip(frames, this);

        IsValid = true;

        frames.Clear();     // memory savings
        frames = null;      // optional: allow GC / catch accidental reuse
    }

    internal List<byte[]> BreakIntoChunks()
    {
        List<byte[]> chunks = [];
        byte[] data = File.ReadAllBytes(FilePath);
        Plugin.Logger.LogDebug($"Reading back the file, we've got {data.Length} bytes to chunkify. That's about {Mathf.CeilToInt((float)data.Length/ (float)CHUNK_SIZE)} chunks.");

        int i = 0;
        while (i < data.Length)
        {
            int chunkSize = Mathf.Min(CHUNK_SIZE, data.Length - i);
            byte[] chunk = new byte[chunkSize];
            Array.Copy(data, i, chunk, 0, chunk.Length);
            Plugin.Logger.LogDebug($"Created chunk {chunks.Count} with a size of {data.Length}");
            chunks.Add(chunk);

            i += CHUNK_SIZE;
        }

        return chunks;
    }
}
