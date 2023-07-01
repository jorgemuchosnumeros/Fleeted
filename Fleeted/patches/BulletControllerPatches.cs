using HarmonyLib;

namespace Fleeted.patches;

[HarmonyPatch(typeof(BulletController), "OnEnable")]
public static class StartSendBulletInfo
{
    static void Postfix(BulletController __instance)
    {
        var slot = __instance.player - 1;

        if (InGameNetManager.Instance.ownedSlots.Contains(slot))
        {
            InGameNetManager.Instance.ownedLiveBullets.Add(__instance);
            Plugin.Logger.LogInfo($"Adding Own Bullet {__instance.GetInstanceID()}");
        }
    }
}

[HarmonyPatch(typeof(BulletController), "DeactivateBullet")]
public static class StopSendBulletInfo
{
    static void Postfix(BulletController __instance)
    {
        if (InGameNetManager.Instance.ownedLiveBullets.Remove(__instance))
        {
            Plugin.Logger.LogInfo($"Removing Own Bullet {__instance.GetInstanceID()}");
        }
    }
}