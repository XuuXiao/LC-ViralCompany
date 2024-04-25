using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using ViralCompany.Recording.Audio;

namespace ViralCompany.src.Patches;
[HarmonyPatch(typeof(PlayerControllerB))]
internal static class PlayerControllerPatch {
    [HarmonyPostfix, HarmonyPatch(nameof(PlayerControllerB.ConnectClientToPlayerObject))]
    static void AddPlayerScriptToLocalPlayer(PlayerControllerB __instance) {
        if(GameNetworkManager.Instance.localPlayerController != __instance) {
            Plugin.Logger.LogInfo("Not local player.");
            return;
        }
        Plugin.Logger.LogInfo("Adding recorder helper.");
        __instance.GetComponentInChildren<AudioListener>().gameObject.AddComponent<AudioRecorder>();
    }
}
