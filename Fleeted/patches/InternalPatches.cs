using HarmonyLib;
using UnityEngine.SceneManagement;

namespace Fleeted.patches;

[HarmonyPatch(typeof(SceneManager), "Internal_SceneLoaded")]
public static class MenuLoaded
{
    static void Postfix(Scene scene)
    {
        if (scene == SceneManager.GetSceneByName("Mainmenu"))
        {
            Plugin.Logger.LogInfo("Menu Loaded");

            CustomMainMenu.Instance.MapMainMenu();
            CustomMainMenu.Instance.CreateMainMenuSpace();
            CustomMainMenu.Instance.CreateMainMenuOnlineOption();
            CustomMainMenu.Instance.menuSpawned = true;

            CustomOnlineMenu.Instance.MapMenu();
            CustomOnlineMenu.Instance.SaveMenuSpace();

            CustomSettingsMenu.Instance.MapMenu();
            CustomSettingsMenu.Instance.SaveMenuSpace();

            ArrowJoinInput.Instance.MapJoin();

            CustomLobbyMenu.Instance.MapLobby();
            CustomLobbyMenu.Instance.SaveLobby();
        }
    }
}