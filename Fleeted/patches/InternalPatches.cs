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

            CustomMenuManager.Instance.MapMenuGameObjects();
            CustomMenuManager.Instance.CreateMainMenuSpace();
            CustomMenuManager.Instance.CreateMainMenuOnlineOption();

            CustomMenuManager.Instance.MapOptionsMenu();
            CustomMenuManager.Instance.SaveOnlineMenuSpace();

            CustomMenuManager.Instance.menuSpawned = true;
        }
    }
}