using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Fleeted.patches;

[HarmonyPatch(typeof(PlayMenuController), "Update")]
public class LockMenuOnLoading
{
    static bool Prefix()
    {
        if (LobbyManager.Instance.isLoadingLock)
            return false;

        return true;
    }
}

[HarmonyPatch(typeof(PlayMenuController), "ManageCustomization")]
public class OnCustomizationSelectionPatch
{
    static void Postfix(PlayMenuController __instance, int playerN)
    {
        var playerPosesion =
            typeof(PlayMenuController).GetField("playerPosesion", BindingFlags.Instance | BindingFlags.NonPublic);

        if (((int[]) playerPosesion.GetValue(__instance))[playerN] != 0)
        {
            return;
        }

        if (InputController.inputController.inputs[playerN + "ADown"])
        {
            if (__instance.disabledGO[playerN - 1].activeSelf)
            {
                return;
            }

            CustomLobbyMenu.Instance.wasCharaSelected = true;
        }
    }
}

[HarmonyPatch(typeof(PlayMenuController), "ResetPlayer")]
public class OnResetPlayerPatch
{
    static void Postfix()
    {
        CustomLobbyMenu.Instance.wasCharaSelected = true;
    }
}

[HarmonyPatch(typeof(PlayMenuController), "ManageBotCustomization")]
public class OnBotCustomizationSelectionPatch
{
    static void Prefix(PlayMenuController __instance, int playerN)
    {
        var playerPosesion =
            typeof(PlayMenuController).GetField("playerPosesion", BindingFlags.Instance | BindingFlags.NonPublic);

        if (((int[]) playerPosesion.GetValue(__instance))[playerN] == 0)
        {
            return;
        }

        if (InputController.inputController.inputs[playerN + "ADown"])
        {
            var alreadySelectedCharas = (bool[]) typeof(PlayMenuController)
                .GetField("alreadySelectedCharas", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(__instance);

            var charaSelection = (int[]) typeof(PlayMenuController)
                .GetField("charaSelection", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(__instance);

            if (alreadySelectedCharas[charaSelection[((int[]) playerPosesion.GetValue(__instance))[playerN] - 1] - 1])
            {
                return;
            }

            CustomLobbyMenu.Instance.wasCharaSelected = true;
        }
    }
}

[HarmonyPatch(typeof(PlayMenuController), "RemoveBot")]
public class OnRemoveBotPatch
{
    static void Postfix()
    {
        CustomLobbyMenu.Instance.wasCharaSelected = true;
    }
}

[HarmonyPatch(typeof(PlayMenuController), "UpdateBotLabel")]
public class GreyOutAddBotButtonIfNotHostPatch
{
    static bool Prefix(PlayMenuController __instance)
    {
        if (!LobbyManager.Instance.hostOptions)
        {
            GreyOutLabel(__instance);
            return false;
        }

        return true;
    }

    static void GreyOutLabel(PlayMenuController __instance)
    {
        var addBot = typeof(PlayMenuController).GetField("addBot", BindingFlags.Instance | BindingFlags.NonPublic);
        var disabledColor =
            typeof(PlayMenuController).GetField("disabledColor", BindingFlags.Instance | BindingFlags.NonPublic);
        if ((bool) addBot.GetValue(__instance))
        {
            __instance.botToggleLabel.text = "< + >";
            __instance.botToggleLabel.color = (Color32) disabledColor.GetValue(__instance);
        }
        else
        {
            __instance.botToggleLabel.text = "< - >";
            __instance.botToggleLabel.color = (Color32) disabledColor.GetValue(__instance);
        }
    }
}

[HarmonyPatch(typeof(PlayMenuController), "AddBot")]
public class DisableAddBotButtonIfNotHostPatch
{
    static bool Prefix(PlayMenuController __instance)
    {
        if (!LobbyManager.Instance.hostOptions)
        {
            DisableButton(__instance);
            return false;
        }

        return true;
    }

    static void DisableButton(PlayMenuController __instance)
    {
        var disabledColor =
            typeof(PlayMenuController).GetField("disabledColor", BindingFlags.Instance | BindingFlags.NonPublic);
        __instance.botToggleLabel.color = (Color32) disabledColor.GetValue(__instance);

        GlobalAudio.globalAudio.PlayInvalid();
    }
}

[HarmonyPatch(typeof(PlayMenuController), "UpdateDisabledCharas")]
public class UpdateDisabledCharasPatch
{
    static bool Prefix(PlayMenuController __instance)
    {
        var charaSelection = (int[]) typeof(PlayMenuController).GetField("charaSelection",
                BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(__instance);
        var playerState = (int[]) typeof(PlayMenuController).GetField("playerState",
                BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(__instance);
        var playerPosesion = (int[]) typeof(PlayMenuController).GetField("playerPosesion",
                BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(__instance);
        var alreadySelectedCharas = (bool[]) typeof(PlayMenuController).GetField("alreadySelectedCharas",
                BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(__instance);

        for (int i = 0; i < charaSelection.Length; i++)
        {
            if (playerState[i] != 2 && playerPosesion[i + 1] == 0)
            {
                if (alreadySelectedCharas
                        [charaSelection[i] + 1]) // TODO: Still dont know how these private lists work (alreadySelectedCharas[charaSelection[i] - 1])
                {
                    __instance.disabledGO[i].SetActive(value: true);
                }
                else
                {
                    __instance.disabledGO[i].SetActive(value: false);
                }
            }
        }

        return false;
    }
}

[HarmonyPatch(typeof(PlayMenuController), "ManageMinimenuState")]
public static class ManageMinimenuStatePatch
{
    static void Postfix(PlayMenuController __instance)
    {
        int num = 0;
        int num2 = 0;

        var playerState = (int[]) typeof(PlayMenuController)
            .GetField("playerState", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(__instance);

        for (var i = 1; i < 9; i++)
        {
            if (playerState[i - 1] == 1)
            {
                num++;
            }
            else if (playerState[i - 1] == 2)
            {
                num2++;
            }
        }

        var disabledColor = (Color32) typeof(PlayMenuController)
            .GetField("disabledColor", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(__instance)!;
        var originalStartLabelColor = (Color32) typeof(PlayMenuController)
            .GetField("originalStartLabelColor", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(__instance)!;
        var readyToStart = typeof(PlayMenuController)
            .GetField("readyToStart", BindingFlags.Instance | BindingFlags.NonPublic);

        if (LobbyManager.Instance.isHost &&
            LobbyManager.OccupiedSlotsInPlayMenu(LobbyManager.Instance.CurrentLobby) >= 2)
        {
            __instance.startLabel.color = originalStartLabelColor;
            readyToStart.SetValue(__instance, true);
        }
        else
        {
            __instance.startLabel.color = disabledColor;
            readyToStart.SetValue(__instance, false);
        }
    }
}