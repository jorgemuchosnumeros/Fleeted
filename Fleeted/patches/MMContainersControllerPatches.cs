using HarmonyLib;

namespace Fleeted.patches;

[HarmonyPatch(typeof(MMContainersController), "ShowOptions")]
public static class ShowPlayOnlineMenu
{
    public static bool IsCalledFromApplyOptions;

    static void Postfix()
    {
        if (ApplyPlayOnlinePatch.IsOnlineOptionSelected)
        {
            if (!IsCalledFromApplyOptions)
                return;
            CustomOnlineMenuManager.Instance.ShowPlayOnlineMenu();
            IsCalledFromApplyOptions = false;
        }
    }
}

[HarmonyPatch(typeof(MMContainersController), "HideOptions")]
public static class HidePlayOnlineMenu
{
    static void Postfix()
    {
        if (ApplyPlayOnlinePatch.IsOnlineOptionSelected)
        {
            CustomOnlineMenuManager.Instance.StartCoroutine(CustomOnlineMenuManager.Instance.HidePlayOnlineMenu());
        }
    }
}