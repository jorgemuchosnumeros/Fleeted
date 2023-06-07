using HarmonyLib;

namespace Fleeted.patches;

[HarmonyPatch(typeof(StageMenuController), "HideStageMenu")]
public static class HideStageMenuPatch
{
    static void Postfix()
    {
        if (ApplyPlayOnlinePatch.IsOnlineOptionSelected)
            CustomLobbyMenu.Instance.ShowPlayMenuButtons();
    }
}