using HarmonyLib;

namespace Fleeted.patches;

[HarmonyPatch(typeof(InputController), "Update")]
public static class ApplyNetInputsPatch
{
    static void Postfix(InputController __instance)
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return;
        if (InGameNetManager.Instance == null) return;
        // ShipController patches apply authoritative inputs per-ship.
    }
}
