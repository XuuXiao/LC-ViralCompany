using BepInEx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using ViralCompany.Recording.Encoding;

namespace ViralCompany.Recording.Video;
internal class RecordedClip
{
    const int CHUNK_SIZE = 30_000;

    public string FilePath
    {
        get
        {
            return Path.Combine(Video.FolderPath, ClipID + VideoRecorder.VideoExtension);
        }
    }

    public bool IsValid { get; private set; }

    public string ClipID { get; private set; }

    public RecordedVideo Video { get; private set; }
    List<Texture2DVideoFrame> frames;

    internal static Action<RecordedClip> OnFinishEncoding = delegate { };

    // maybe move this into a seperate class/struct?
    internal Dictionary<int, byte[]> DownloadedChunkData;
    internal int ChunkCountToDownload;

    public RecordedClip(RecordedVideo video, string clipID)
    {
        Video = video;
        ClipID = clipID;
    }

    public void AddFrame(Texture2D frame)
    {
        if (frames == null)
        {
            Plugin.Logger.LogDebug("Inited RecordedClip for recording.");
            frames = [];
        }

        frames.Add(new Texture2DVideoFrame(frame));
    }

    public async void ClipFinished()
    {
        
        await FFmpegEncoder.CreateClip(frames, this);

        IsValid = true;
        frames.Clear(); // clear frames for memory savings
    }

    internal List<byte[]> BreakIntoChunks()
    {
        List<byte[]> chunks = [];
        byte[] data = File.ReadAllBytes(FilePath);
        Plugin.Logger.LogDebug($"Reading back the file, we've got {data.Length} bytes to chunkify. That's about {Mathf.Ceil(data.Length/CHUNK_SIZE)} chunks.");

        int i = 0;
        while(i < data.Length) {
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
