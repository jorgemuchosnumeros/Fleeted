using System.IO;
using System.Linq;
using HarmonyLib;

namespace Fleeted.patches;

[HarmonyPatch(typeof(SaveLoadManager), nameof(SaveLoadManager.Save))]
public class SaveErroringRandomlyPatch
{
    static bool Prefix(SaveLoadManager __instance, int saveFile)
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return true;
        Save(__instance, saveFile);
        return false;
    }

    static void Save(SaveLoadManager instance, int saveFile)
    {
        SaveData saveData = new SaveData();
        GlobalController globalController = GlobalController.globalController;
        saveData.lariUnlocked = globalController.lariUnlocked;
        saveData.unlock0 = globalController.unlocks[0];
        saveData.unlock1 = globalController.unlocks[1];
        saveData.unlock2 = globalController.unlocks[2];
        saveData.unlock3 = globalController.unlocks[3];
        saveData.unlock4 = globalController.unlocks[4];
        saveData.unlock5 = globalController.unlocks[5];
        saveData.unlock6 = globalController.unlocks[6];
        saveData.unlock7 = globalController.unlocks[7];
        saveData.unlock8 = globalController.unlocks[8];
        saveData.challenges = IntArrayToString(globalController.challenges);
        saveData.stageSettings0 = globalController.stageSettings[0];
        saveData.stageSettings1 = globalController.stageSettings[1];
        saveData.stageSettings2 = globalController.stageSettings[2];
        saveData.freedom = globalController.freedom;
        saveData.firstGame = globalController.firstGame;
        try
        {
            SaveLoad.saveLoad.WriteData(saveData, saveFile);
        }
        catch (IOException)
        {
            Plugin.Logger.LogError("Save Failed for some reason :(");
        }
    }

    private static string IntArrayToString(int[] array)
    {
        return array.Aggregate("", (current, t) => current != "" ? current + "," + t : current + t);
    }
}