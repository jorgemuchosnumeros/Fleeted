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
            SteamAPI.Init();

            new Harmony("patch.fleeted").PatchAll();
        }

        void Update()
        {
            if (!SteamManager.Initialized)
                return;

            SteamAPI.RunCallbacks();
            if (!firstSteamworksInit)
            {
                firstSteamworksInit = true;

                var customMenuManager = new GameObject("CustomMenuManager");
                customMenuManager.AddComponent<CustomMenuManager>();
                DontDestroyOnLoad(customMenuManager);
            }
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(10, Screen.height - 20, 400, 40), $"Fleeted ID: {BuildGUID}");
        }
    }
}