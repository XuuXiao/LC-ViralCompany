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
            return;
        }
        __instance.GetComponentInChildren<AudioListener>().gameObject.AddComponent<AudioRecorder>();
    }
}
