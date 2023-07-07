using HarmonyLib;

namespace Fleeted.patches;

[HarmonyPatch(typeof(AchievementsController), "UnlockAchievements")]
public static class UnlockAchievementsStud
{
    static bool Prefix(AchievementsController __instance)
    {
        return false;
    }
}