using HarmonyLib;

namespace Fleeted.patches;

[HarmonyPatch(typeof(StageMenuController), "HideStageMenu")]
public static class HideStageMenuPatch
{
    static void Postfix()
    {
        if (ApplyPlayOnlinePatch.IsOnlineOptionSelected)
            CustomLobbyMenu.Instance.StartCoroutine(CustomLobbyMenu.Instance.ShowPlayMenuButtons(0.25f));
    }
}

[HarmonyPatch(typeof(StageMenuController), "ChangeMode")]
public static class OnChangeModePatch
{
    static void Postfix()
    {
        CustomLobbyMenu.Instance.wasStageSettingsSelected = true;
    }
}

[HarmonyPatch(typeof(StageMenuController), "ChangeStage")]
public static class OnChangeStagePatch
{
    static void Postfix()
    {
        CustomLobbyMenu.Instance.wasStageSettingsSelected = true;
    }
}

[HarmonyPatch(typeof(StageMenuController), "ChangePoints")]
public static class OnChangePointsPatch
{
    static void Postfix()
    {
        CustomLobbyMenu.Instance.wasStageSettingsSelected = true;
    }
}

[HarmonyPatch(typeof(StageMenuController), "Initialize")]
public static class OnInitializePatch
{
    static void Postfix()
    {
        CustomLobbyMenu.Instance.wasStageSettingsSelected = true;
    }
}

[HarmonyPatch(typeof(StageMenuController), "StartGame")]
public static class OnStartGamePatch
{
    static void Postfix()
    {
        if (LobbyManager.Instance.isHost)
            LobbyManager.Instance.CurrentLobby.SetData("GameStarted", "yes");
    }
}