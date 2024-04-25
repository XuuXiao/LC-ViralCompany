using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace ViralCompany.src.Patches;
[HarmonyPatch(typeof(ItemDropship))]
internal static class ItemDropshipPatch {
    [HarmonyPrefix, HarmonyPatch(nameof(ItemDropship.Update))]
    static void InstantDropship(ItemDropship __instance) {
        if(!__instance.deliveringOrder)
            __instance.shipTimer = 50f;
    }
}
