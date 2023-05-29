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

            CustomMainMenuManager.Instance.MapMainMenu();
            CustomMainMenuManager.Instance.CreateMainMenuSpace();
            CustomMainMenuManager.Instance.CreateMainMenuOnlineOption();

            CustomOnlineMenuManager.Instance.MapOptionsMenu();
            CustomOnlineMenuManager.Instance.SaveOptionsMenuSpace();

            CustomMainMenuManager.Instance.menuSpawned = true;
        }
    }
}