using HarmonyLib;

namespace Fleeted.patches;

[HarmonyPatch(typeof(ShipController), "Update")]
public static class DisableMovementOnOnlinePause
{
    static bool Prefix()
    {
        return true;
    }
}