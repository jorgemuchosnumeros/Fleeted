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
        CustomOnlineMenuManager.Instance.StartCoroutine(CustomOnlineMenuManager.Instance.HidePlayOnlineMenu());
    }
}

[HarmonyPatch(typeof(MMContainersController), "ShowSettings")]
public static class ShowLobbySettingsMenu
{
    static void Postfix()
    {
        if (ApplyPlayOnlinePatch.IsOnlineOptionSelected)
        {
            CustomSettingsMenuManager.Instance.ShowLobbySettingsMenu();
        }
    }
}

[HarmonyPatch(typeof(MMContainersController), "HideSettings")]
public static class HideLobbySettingsMenu
{
    static void Postfix()
    {
        if (ApplyPlayOnlinePatch.IsOnlineOptionSelected)
        {
            CustomSettingsMenuManager.Instance.StartCoroutine(
                CustomSettingsMenuManager.Instance.HideLobbySettingsMenu());
        }
    }
}