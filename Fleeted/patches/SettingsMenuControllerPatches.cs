using System.Reflection;
using HarmonyLib;

namespace Fleeted.patches;

[HarmonyPatch(typeof(SettingsMenuController), "ManageInput")]
public static class ManageInputPatch
{
    static bool Prefix(SettingsMenuController __instance)
    {
        if (CustomSettingsMenuManager.Instance.moveOptions)
        {
            SettingsMenuControllerPatches.ManageInputLobbyCreate(__instance);
            return false;
        }

        return true;
    }
}

public static class SettingsMenuControllerPatches
{
    private static int memberLimitSelection = 8;
    private static bool isFriendsOnly;

    public static void ManageInputLobbyCreate(SettingsMenuController instance)
    {
        FieldInfo inCooldown =
            typeof(SettingsMenuController).GetField("inCooldown", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo cooldownTimer =
            typeof(SettingsMenuController).GetField("cooldownTimer", BindingFlags.Instance | BindingFlags.NonPublic);

        if (InputController.inputController.inputs["1U"] || InputController.inputController.inputsRaw["0U"])
        {
            if (!(bool) inCooldown?.GetValue(instance)! || InputController.inputController.inputs["1UDown"] ||
                InputController.inputController.inputsRaw["0UDown"])
            {
                cooldownTimer?.SetValue(instance, 0.25f);
                if (instance.shipSelector.GetInteger("selection") == 1)
                {
                    instance.shipSelector.SetInteger("selection", 4);
                }
                else
                {
                    instance.shipSelector.SetInteger("selection", instance.shipSelector.GetInteger("selection") - 1);
                }

                GlobalAudio.globalAudio.PlaySelection();
            }
        }
        else if (InputController.inputController.inputs["1D"] || InputController.inputController.inputsRaw["0D"])
        {
            if (!(bool) inCooldown?.GetValue(instance)! || InputController.inputController.inputs["1DDown"] ||
                InputController.inputController.inputsRaw["0DDown"])
            {
                cooldownTimer?.SetValue(instance, 0.25f);
                if (instance.shipSelector.GetInteger("selection") == 4)
                {
                    instance.shipSelector.SetInteger("selection", 1);
                }
                else
                {
                    instance.shipSelector.SetInteger("selection", instance.shipSelector.GetInteger("selection") + 1);
                }

                GlobalAudio.globalAudio.PlaySelection();
            }
        }
        else if (InputController.inputController.inputs["1ADown"] ||
                 InputController.inputController.inputsRaw["0ADown"])
        {
            if (instance.shipSelector.GetInteger("selection") < 3)
            {
                instance.shipSelector.SetInteger("selection", 3);
                GlobalAudio.globalAudio.PlaySelection();
            }
            else if (instance.shipSelector.GetInteger("selection") == 3)
            {
                instance.CreateLobby();
                GlobalAudio.globalAudio.PlayAccept();
                cooldownTimer?.SetValue(instance, 0.25f);
            }
            else if (instance.shipSelector.GetInteger("selection") < 4)
            {
                instance.shipSelector.SetInteger("selection", 4);
                GlobalAudio.globalAudio.PlaySelection();
            }
            else
            {
                instance.Exit();
            }
        }
        else if (InputController.inputController.inputs["1BDown"] ||
                 InputController.inputController.inputsRaw["0BDown"])
        {
            instance.Exit();
        }
        else if (InputController.inputController.inputs["1L"] || InputController.inputController.inputsRaw["0L"])
        {
            if ((bool) inCooldown?.GetValue(instance)! && !InputController.inputController.inputs["1LDown"] &&
                !InputController.inputController.inputsRaw["0LDown"])
            {
                return;
            }

            cooldownTimer?.SetValue(instance, 0.25f);
            if (instance.shipSelector.GetInteger("selection") != 3 &&
                instance.shipSelector.GetInteger("selection") != 4)
            {
                if (instance.shipSelector.GetInteger("selection") == 1)
                {
                    instance.SetMemberLimit(left: true);
                }
                else if (instance.shipSelector.GetInteger("selection") == 2)
                {
                    instance.ChangeFriendsOnly();
                }
            }
        }
        else
        {
            if ((!InputController.inputController.inputs["1R"] && !InputController.inputController.inputsRaw["0R"]) ||
                ((bool) inCooldown?.GetValue(instance)! && !InputController.inputController.inputs["1RDown"] &&
                 !InputController.inputController.inputsRaw["0RDown"]))
            {
                return;
            }

            cooldownTimer?.SetValue(instance, 0.25f);
            if (instance.shipSelector.GetInteger("selection") != 3 &&
                instance.shipSelector.GetInteger("selection") != 4)
            {
                if (instance.shipSelector.GetInteger("selection") == 1)
                {
                    instance.SetMemberLimit(left: false);
                }
                else if (instance.shipSelector.GetInteger("selection") == 2)
                {
                    instance.ChangeFriendsOnly();
                }
            }
        }
    }

    private static void Exit(this SettingsMenuController instance)
    {
        typeof(SettingsMenuController).GetMethod("Exit", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(instance, new object[] { });
    }

    private static void CreateLobby(this SettingsMenuController instance)
    {
        CustomSettingsMenuManager.Instance.CreateLobby(memberLimitSelection, isFriendsOnly);
    }

    private static void SetMemberLimit(this SettingsMenuController instance, bool left)
    {
        if (left)
        {
            memberLimitSelection--;
            if (memberLimitSelection < 2)
            {
                memberLimitSelection = 2;
                GlobalAudio.globalAudio.PlayInvalid();
            }
            else
            {
                GlobalAudio.globalAudio.PlayAccept();
            }
        }
        else
        {
            memberLimitSelection++;
            if (memberLimitSelection > 8)
            {
                memberLimitSelection = 8;
                GlobalAudio.globalAudio.PlayInvalid();
            }
            else
            {
                GlobalAudio.globalAudio.PlayAccept();
            }
        }

        instance.ManageMemberLimitArrows();
        instance.values[0].text = memberLimitSelection.ToString();
    }

    private static void ManageMemberLimitArrows(this SettingsMenuController instance)
    {
        instance.resolutionArrows[0].color = instance.enabledColor;
        instance.resolutionArrows[1].color = instance.enabledColor;
        if (memberLimitSelection == 2)
        {
            instance.resolutionArrows[0].color = instance.disabledColor;
        }

        if (memberLimitSelection == 8)
        {
            instance.resolutionArrows[1].color = instance.disabledColor;
        }
    }

    private static void ChangeFriendsOnly(this SettingsMenuController instance)
    {
        isFriendsOnly = !isFriendsOnly;
        instance.UpdateFriendsOnly();
        GlobalAudio.globalAudio.PlayAccept();
        instance.shipSelector2.SetTrigger("apply");
    }

    private static void UpdateFriendsOnly(this SettingsMenuController instance)
    {
        if (isFriendsOnly)
        {
            instance.values[1].text = "Yes";
        }
        else
        {
            instance.values[1].text = "No";
        }
    }
}