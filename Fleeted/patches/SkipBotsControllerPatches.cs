using HarmonyLib;

namespace Fleeted.patches;

[HarmonyPatch(typeof(SkipBotsController), "Update")]
public class DisableTimeWarpSkipBotsPatch
{
    static bool Prefix()
    {
        return !ApplyPlayOnlinePatch.IsOnlineOptionSelected;
    }
}