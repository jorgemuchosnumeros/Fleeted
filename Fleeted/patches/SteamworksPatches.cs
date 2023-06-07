#define GOLDBERG

using System;
using System.Reflection;
using HarmonyLib;
using Steamworks;

namespace Fleeted.patches;

// We get rid of the old Steamworks.NET and use Facepunch.Steamworks instead

[HarmonyPatch(typeof(SteamManager), "Awake")]
public static class DeactivateSteamManager
{
    static bool Prefix(SteamManager __instance)
    {
        SteamworksPatches.Awake(__instance);
        return false;
    }
}

public static class SteamworksPatches
{
    public static void Awake(SteamManager __instance)
    {
        var s_instance = typeof(SteamManager).GetField("s_instance", BindingFlags.Static | BindingFlags.NonPublic);
        var s_EverInialized =
            typeof(SteamManager).GetField("s_EverInialized", BindingFlags.Static | BindingFlags.NonPublic);

        if ((SteamManager) s_instance.GetValue(__instance) != null)
        {
            UnityEngine.Object.Destroy(__instance.gameObject);
            return;
        }

        s_instance.SetValue(__instance, __instance);

        if ((bool) s_EverInialized.GetValue(__instance))
        {
            throw new Exception("Tried to Initialize the SteamAPI twice in one session!");
        }

#if !GOLDBERG
        UnityEngine.Object.DontDestroyOnLoad(__instance.gameObject);
        try
        {
            if (SteamClient.RestartAppIfNecessary(1037190u))
            {
                Application.Quit();
                return;
            }
        }
        catch (DllNotFoundException ex)
        {
            Debug.LogError(
                "[Steamworks.NET] Could not load [lib]steam_api.dll/so/dylib. It's likely not in the correct location. Refer to the README for more details.\n" +
                ex, __instance);
            Application.Quit();
            return;
        }
#endif

        if (!SteamClient.IsValid)
            SteamClient.Init(1037190);
    }
}