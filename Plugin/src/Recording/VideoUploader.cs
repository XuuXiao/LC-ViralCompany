using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using ViralCompany.Recording.Video;
using ViralCompany.src.Recording;
using YoutubeDLSharp.Metadata;

namespace ViralCompany.Recording;
internal class VideoUploader : NetworkBehaviour {
    const float DELAY_BETWEEN_PACKETS = 0.5f;

    internal static VideoUploader Instance { get; private set; }

    internal Dictionary<string, RecordedClip> downloadingClips = [];
    internal List<string> uploadingClips = [];

    void Awake() {
        Instance = this;
        RecordedClip.OnFinishEncoding += HandleClipEncoded;
    }

    void OnDisable() {
        Instance = null;
        RecordedClip.OnFinishEncoding -= HandleClipEncoded;
    }

    internal void HandleClipEncoded(RecordedClip clip) {
        if(uploadingClips.Contains(clip.ClipID)) {
            Plugin.Logger.LogWarning("Trying to upload a clip when we're already uploading it!");
            return;
        }
        uploadingClips.Add(clip.ClipID);
        Plugin.Logger.LogInfo($"Beginning upload of clip: {clip.ClipID}");
        StartCoroutine(UploadClip(clip));
    }

    internal IEnumerator UploadClip(RecordedClip clip) {
        List<byte[]> chunkData = clip.BreakIntoChunks();

        if(IsHost) {
            StartSendingClipClientRpc(clip.Video.VideoID, clip.ClipID, chunkData.Count);
        } else {
            StartSendingClipServerRpc(clip.Video.VideoID, clip.ClipID, chunkData.Count);
        }
        if(Plugin.ModConfig.ExtendedLogging.Value)
            Plugin.Logger.LogInfo($"Sending {chunkData.Count} chunks for clip. Will take about {chunkData.Count * DELAY_BETWEEN_PACKETS} seconds.");

        for(int chunkID = 0; chunkID < chunkData.Count; chunkID++) {
            yield return new WaitForSeconds(DELAY_BETWEEN_PACKETS);

            byte[] chunk = chunkData[chunkID];
            if(IsHost) {
                SendChunkClientRpc(clip.ClipID, chunkID, chunk);
            } else {
                SendChunkServerRpc(clip.ClipID, chunkID, chunk);
            }

        }

        uploadingClips.Remove(clip.ClipID);
    }

    [ServerRpc]
    internal void StartSendingClipServerRpc(string videoID, string clipID, int chunkCount) {
        StartSendingClipClientRpc(videoID, clipID, chunkCount);
    }

    [ClientRpc]
    internal void StartSendingClipClientRpc(string videoID, string clipId, int chunkCount) {
        if(downloadingClips.ContainsKey(clipId)) {
            Plugin.Logger.LogWarning($"WOAH! Tried to initalise sending chunk data for '{clipId}' when we're already recieving that!");
        }
        if(IsOwner) return;
        if(Plugin.ModConfig.ExtendedLogging.Value)
            Plugin.Logger.LogInfo($"About to recieve clip chunk data, videoID: {videoID}, clipID: {clipId}, {chunkCount} chunks");
        RecordedVideo video = VideoDatabase.videos[videoID];

        video.RegisterClip(clipId);
        RecordedClip clip = new(video, clipId) {
            DownloadedChunkData = [],
            ChunkCountToDownload = chunkCount
        };
        downloadingClips.Add(clipId, clip);
    }


    [ServerRpc]
    internal void SendChunkServerRpc(string clipId, int chunkID, byte[] data) {
        SendChunkClientRpc(clipId, chunkID, data);
    }

    [ClientRpc]
    internal void SendChunkClientRpc(string clipId, int chunkID, byte[] data) {
        Plugin.Logger.LogDebug($"IsOwner of VideoUploader? {IsOwner}");
        if(IsOwner) return;
        if(Plugin.ModConfig.ExtendedLogging.Value)
            Plugin.Logger.LogInfo($"Recieved chunk {chunkID} data for '{clipId}'! data.Length: {data.Length}");

        RecordedClip clip = downloadingClips[clipId];
        clip.DownloadedChunkData.Add(chunkID, data);
        if(clip.DownloadedChunkData.Count == clip.ChunkCountToDownload) {
            Plugin.Logger.LogInfo($"Finished downloading clip: {clipId}");

            List<byte> bytes = [];
            for(int i = 0; i < clip.ChunkCountToDownload; i++) { // make sure chunks are in order
                bytes.AddRange(clip.DownloadedChunkData[i]);
            }

            File.WriteAllBytes(clip.FilePath, [.. bytes]); // TODO: Change to clip.FilePath
            
            clip.DownloadedChunkData = null; // clear out of memory.
            downloadingClips.Remove(clipId);
            clip.Video.StoreClip(clipId, clip); // register as downloaded
        }
    }
}
