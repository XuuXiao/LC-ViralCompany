using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using ViralCompany.Recording.Audio;

namespace ViralCompany.Patches;
[HarmonyPatch(typeof(PlayerControllerB))]
internal static class PlayerControllerPatch {
    [HarmonyPostfix, HarmonyPatch(nameof(PlayerControllerB.ConnectClientToPlayerObject))]
    static void AddPlayerScriptToLocalPlayer(PlayerControllerB __instance)
    {
        if (GameNetworkManager.Instance.localPlayerController != __instance)
        {
            return;
        }
        __instance.GetComponentInChildren<AudioListener>().gameObject.AddComponent<AudioRecorder>();
    }
}
