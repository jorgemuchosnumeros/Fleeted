using HarmonyLib;
using UnityEngine.SceneManagement;

namespace Fleeted.patches;

[HarmonyPatch(typeof(SceneManager), "Internal_SceneLoaded")]
public static class MenuLoaded
{
    private static bool _initialized;

    public static void TrySetupMainMenu()
    {
        if (!SceneManager.GetSceneByName("Mainmenu").isLoaded)
            return;
        if (CustomMainMenu.Instance == null || CustomOnlineMenu.Instance == null ||
            CustomSettingsMenu.Instance == null || ArrowJoinInput.Instance == null ||
            CustomLobbyMenu.Instance == null)
            return;
        if (_initialized)
        {
            // Reinitialize if the menu scene was reloaded and UI objects were destroyed.
            if (CustomMainMenu.Instance.playOnlineOption != null &&
                CustomMainMenu.Instance.playLocalOption != null &&
                CustomMainMenu.Instance.mainMenu != null)
                return;
            _initialized = false;
        }

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

        _initialized = true;
    }

    static void Postfix(Scene scene)
    {
        if (scene.name == "Mainmenu")
            TrySetupMainMenu();
    }
}
