using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using ViralCompany.Recording;
using ViralCompany.Recording.Audio;
using ViralCompany.Recording.Encoding;
using ViralCompany.Recording.Video;

namespace ViralCompany.src.Patches;
[HarmonyPatch(typeof(PlayerControllerB))]
internal static class PlayerControllerPatch {
    [HarmonyPostfix, HarmonyPatch(nameof(PlayerControllerB.ConnectClientToPlayerObject))]
    static void AddPlayerScriptToLocalPlayer(PlayerControllerB __instance) {
        if(GameNetworkManager.Instance.localPlayerController != __instance) {
            return;
        }
        __instance.GetComponentInChildren<AudioListener>().gameObject.AddComponent<AudioRecorder>();
    }
}
