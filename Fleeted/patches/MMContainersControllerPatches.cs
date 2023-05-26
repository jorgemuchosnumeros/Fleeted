using HarmonyLib;

namespace Fleeted.patches;

[HarmonyPatch(typeof(MMContainersController), "ShowOptions")]
public static class ShowPlayOnlineMenu
{
    public static bool IsCalledFromApplyOptions;

    static void Postfix(MMContainersController __instance)
    {
        if (ApplyPlayOnlinePatch.IsOnlineOptionSelected)
        {
            if (!IsCalledFromApplyOptions)
                return;
            CustomMenuManager.Instance.ShowPlayOnlineMenu(__instance);
            IsCalledFromApplyOptions = false;
        }
    }
}

[HarmonyPatch(typeof(MMContainersController), "HideOptions")]
public static class HidePlayOnlineMenu
{
    static void Postfix(MMContainersController __instance)
    {
        if (ApplyPlayOnlinePatch.IsOnlineOptionSelected)
        {
            CustomMenuManager.Instance.HidePlayOnlineMenu(__instance);
        }
    }
}