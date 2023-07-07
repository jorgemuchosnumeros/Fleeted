using HarmonyLib;

namespace Fleeted.patches;

[HarmonyPatch(typeof(BotController), "Update")]
public class RemoveAIIfNotHostPatch0
{
    static bool Prefix()
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return true;
        return LobbyManager.Instance.isHost;
    }
}

[HarmonyPatch(typeof(BotController1), "Update")]
public class RemoveAIIfNotHostPatch1
{
    static bool Prefix()
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return true;
        return LobbyManager.Instance.isHost;
    }
}