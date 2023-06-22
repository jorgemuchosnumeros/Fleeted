using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Steamworks;
using UnityEngine;

namespace Fleeted
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public new static ManualLogSource Logger;

        public bool firstSteamworksInit;
        public static string BuildGUID => Assembly.GetExecutingAssembly().ManifestModule.ModuleVersionId.ToString();

        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            Application.runInBackground = true;

            new Harmony("patch.fleeted").PatchAll();
        }

        void Update()
        {
            if (!SteamClient.IsValid)
                return;


            SteamClient.RunCallbacks();
            if (!firstSteamworksInit)
            {
                firstSteamworksInit = true;

                var lobbyManager = new GameObject("LobbyManager");
                lobbyManager.AddComponent<LobbyManager>();
                DontDestroyOnLoad(lobbyManager);

                var customMainMenu = new GameObject("CustomMainMenu");
                customMainMenu.AddComponent<CustomMainMenu>();
                DontDestroyOnLoad(customMainMenu);

                var customOnlineMenu = new GameObject("CustomOnlineMenu");
                customOnlineMenu.AddComponent<CustomOnlineMenu>();
                DontDestroyOnLoad(customOnlineMenu);

                var customSettingsMenu = new GameObject("CustomSettingsMenu");
                customSettingsMenu.AddComponent<CustomSettingsMenu>();
                DontDestroyOnLoad(customSettingsMenu);

                var arrowJoinInput = new GameObject("ArrowJoinInput");
                arrowJoinInput.AddComponent<ArrowJoinInput>();
                DontDestroyOnLoad(arrowJoinInput);

                var customLobbyMenu = new GameObject("CustomLobbyMenu");
                customLobbyMenu.AddComponent<CustomLobbyMenu>();
                DontDestroyOnLoad(customLobbyMenu);

                var inGameNetManager = new GameObject("InGameNetManager");
                inGameNetManager.AddComponent<InGameNetManager>();
                DontDestroyOnLoad(inGameNetManager);
            }
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(10, Screen.height - 20, 400, 40), $"Fleeted ID: {BuildGUID}");
        }

        private void OnApplicationQuit()
        {
            SteamClient.Shutdown();
        }
    }
}