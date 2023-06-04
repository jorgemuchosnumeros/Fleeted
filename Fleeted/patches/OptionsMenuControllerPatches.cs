using System.Reflection;
using HarmonyLib;

namespace Fleeted.patches;

[HarmonyPatch(typeof(OptionsMenuController), "ManageInput")]
public static class ManageInputOnlinePatch
{
    public static int OnlineSelection;

    static bool Prefix(OptionsMenuController __instance)
    {
        if (CustomOnlineMenu.Instance.moveOnlineOptions)
        {
            OnlineSelection = __instance.shipSelector.GetInteger("selection");
            OptionsMenuControllerPatches.ManageInputOnline(__instance);
            return false;
        }

        return true;
    }
}

public static class OptionsMenuControllerPatches
{
    public static void ManageInputOnline(OptionsMenuController __instance)
    {
        if (ArrowJoinInput.Instance.isInputLocked)
            return;

        if ((bool) typeof(OptionsMenuController).GetField("inCooldown", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(__instance)!)
        {
            return;
        }

        if (InputController.inputController.inputs["1U"] || InputController.inputController.inputsRaw["0U"])
        {
            if (InputController.inputController.inputs["1UDown"] || InputController.inputController.inputsRaw["0UDown"])
            {
                __instance.cooldownTimer = 0.1f;
                if (__instance.shipSelector.GetInteger("selection") == 1)
                {
                    __instance.shipSelector.SetInteger("selection", 3);
                    GlobalAudio.globalAudio.PlaySelection();
                }
                else if (__instance.shipSelector.GetInteger("selection") == 2)
                {
                    __instance.shipSelector.SetInteger("selection", 1);
                    GlobalAudio.globalAudio.PlaySelection();
                }
                else if (__instance.shipSelector.GetInteger("selection") == 3)
                {
                    __instance.shipSelector.SetInteger("selection", 2);
                    GlobalAudio.globalAudio.PlaySelection();
                }
            }
        }
        else if (InputController.inputController.inputs["1D"] || InputController.inputController.inputsRaw["0D"])
        {
            if (InputController.inputController.inputs["1DDown"] || InputController.inputController.inputsRaw["0DDown"])
            {
                __instance.cooldownTimer = 0.1f;
                if (__instance.shipSelector.GetInteger("selection") == 1)
                {
                    __instance.shipSelector.SetInteger("selection", 2);
                    GlobalAudio.globalAudio.PlaySelection();
                }
                else if (__instance.shipSelector.GetInteger("selection") == 2)
                {
                    __instance.shipSelector.SetInteger("selection", 3);
                    GlobalAudio.globalAudio.PlaySelection();
                }
                else if (__instance.shipSelector.GetInteger("selection") == 3)
                {
                    __instance.shipSelector.SetInteger("selection", 1);
                    GlobalAudio.globalAudio.PlaySelection();
                }
            }
        }
        else if (InputController.inputController.inputs["1ADown"] ||
                 InputController.inputController.inputsRaw["0ADown"])
        {
            __instance.cooldownTimer = 0.25f;
            if (__instance.shipSelector.GetInteger("selection") == 1)
            {
                GlobalController.globalController.screen = GlobalController.screens.settingsmenu;
                __instance.settings.SetActive(value: true);
                MMContainersController.mmContainersController.ShowSettings();
                GlobalAudio.globalAudio.PlayAccept();
            }
            else if (__instance.shipSelector.GetInteger("selection") == 2)
            {
                __instance.gallery.SetActive(value: true);
                ArrowJoinInput.Instance.JoinArrowField();
                GlobalAudio.globalAudio.PlayAccept();
            }
            else if (__instance.shipSelector.GetInteger("selection") == 3)
            {
                __instance.Exit();
            }
        }
        else if (InputController.inputController.inputs["1BDown"] ||
                 InputController.inputController.inputsRaw["0BDown"])
        {
            __instance.cooldownTimer = 0.25f;
            __instance.Exit();
        }
    }

    private static void Exit(this OptionsMenuController instance)
    {
        typeof(OptionsMenuController).GetMethod("Exit", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(instance, new object[] { });
    }
}