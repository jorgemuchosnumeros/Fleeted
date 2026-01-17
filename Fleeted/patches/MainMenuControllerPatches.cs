using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace Fleeted.patches;

[HarmonyPatch(typeof(MainMenuController), "ApplyOptions")]
public static class ApplyPlayOnlinePatch
{
    public static bool IsOnlineOptionSelected;

    static void Prefix(MainMenuController __instance)
    {
        if (IsOnlineOptionSelected)
        {
            ShowPlayOnlineMenu.IsCalledFromApplyOptions = true;
        }
    }
}

[HarmonyPatch(typeof(MainMenuController), "Update")]
public static class UpdateOnlineStatus
{
    static void Postfix(MainMenuController __instance)
    {
        ApplyPlayOnlinePatch.IsOnlineOptionSelected = __instance.selection switch
        {
            1 => false,
            2 => true,
            3 => false,
            4 => false,
            5 => false,
            6 => false,
            7 => false,
            _ => ApplyPlayOnlinePatch.IsOnlineOptionSelected
        };
    }
}

//Im so sorry, Alva
/*
 * This Changes MainMenuController's Update Method from this:
 
 ...
 if (Input.GetKeyDown("escape") && selectionChangeCooldown <= 0f)
		{
			if (selection == 6)
			{
				Quit();
				return;
			}
			selection = 6;
			ChangeSelection();
		}
	}
	
 * To this:
 
 ...
 if (Input.GetKeyDown("escape") && selectionChangeCooldown <= 0f)
		{
			if (selection == 7)
			{
				Quit();
				return;
			}
			selection = 7;
			ChangeSelection();
		}
	}
 */

[HarmonyPatch(typeof(MainMenuController), "Update")]
public static class RaiseMenuLimitAtUpdate
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> newInstructions = new List<CodeInstruction>();
        int line = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Ldc_I4_6)
            {
                var newInstruction = new CodeInstruction(OpCodes.Ldc_I4_7);
                Plugin.Logger.LogWarning($"Update: {line} - {instruction} -> {newInstruction}");
                newInstructions.Add(newInstruction);
            }
            else
                newInstructions.Add(instruction);

            line++;
        }

        return newInstructions;
    }
}

/*
 * This Changes MainMenuController's ManageApply Method from this:
 
 ...
 for (int i = 0; i < InputController.inputController.totalControllers; i++)
		{
			if (InputController.inputController.inputsRaw[i + "ADown"])
			{
				if (selection == 1)
				{
					ApplyPlay();
				}
				else if (selection == 2)
				{
					ApplyPractice();
				}
				else if (selection == 3)
				{
					ApplyStory();
				}
				else if (selection == 4)
				{
					InputController.inputController.ResetInput();
					InputController.inputController.SetStoryController(i);
					ApplyChallenge(i);
				}
				else if (selection == 5)
				{
					InputController.inputController.ResetInput();
					InputController.inputController.SetStoryController(i);
					ApplyOptions();
				}
				else if (selection == 6)
				{
					Exit();
				}
 ...
	
 * To this:
 
 ...
for (int i = 0; i < InputController.inputController.totalControllers; i++)
	{
		if (InputController.inputController.inputsRaw[i + "ADown"])
		{
			if (selection == 1)
			{
				ApplyPlay();
			}
			else if (selection == 3)
			{
				ApplyPractice();
			}
			else if (selection == 4)
			{
				ApplyStory();
			}
			else if (selection == 5)
			{
				InputController.inputController.ResetInput();
				InputController.inputController.SetStoryController(i);
				ApplyChallenge(i);
			}
			else if (selection == 6 || selection == 2)
			{
				InputController.inputController.ResetInput();
				InputController.inputController.SetStoryController(i);
				ApplyOptions();
			}
			else if (selection == 7)
			{
				Exit();
			}
 ...
 */

[HarmonyPatch(typeof(MainMenuController), "ManageApply")]
public static class RaiseMenuLimitAndAddApplyPlayFunctionAtManageApply
{
    static void Prefix(MainMenuController __instance, ref int __state)
    {
        __state = __instance.selection;
        __instance.selection = RemapSelection(__instance.selection);
    }

    static void Postfix(MainMenuController __instance, int __state)
    {
        __instance.selection = __state;
    }

    private static int RemapSelection(int selection)
    {
        return selection switch
        {
            2 => 5,
            3 => 2,
            4 => 3,
            5 => 4,
            6 => 5,
            7 => 6,
            _ => selection
        };
    }
}

/*
 * This Changes MainMenuController's ManageSelection Method from this:
 
 ...
 else if (InputController.inputController.inputsRaw[i + "U"])
			{
				if (InputController.inputController.inputsRaw[i + "UDown"] || !inCooldown)
				{
					selection--;
					if (selection < 1)
					{
						selection = 6;
					}
					ChangeSelection();
					break;
				}
			}
			else if (InputController.inputController.inputsRaw[i + "D"] && (InputController.inputController.inputsRaw[i + "DDown"] || !inCooldown))
			{
				selection++;
				if (selection > 6)
				{
					selection = 1;
				}
				ChangeSelection();
				break;
			}
		}
	}
	
 * To this:
 
 ...
 else if (InputController.inputController.inputsRaw[i + "U"])
			{
				if (InputController.inputController.inputsRaw[i + "UDown"] || !inCooldown)
				{
					selection--;
					if (selection < 1)
					{
						selection = 7;
					}
					ChangeSelection();
					break;
				}
			}
			else if (InputController.inputController.inputsRaw[i + "D"] && (InputController.inputController.inputsRaw[i + "DDown"] || !inCooldown))
			{
				selection++;
				if (selection > 7)
				{
					selection = 1;
				}
				ChangeSelection();
				break;
			}
		}
	}
 */

[HarmonyPatch(typeof(MainMenuController), "ManageSelection")]
public static class RaiseMenuLimitAtManageSelection
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> newInstructions = new List<CodeInstruction>();
        int line = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            CodeInstruction newInstruction;
            if (instruction.opcode == OpCodes.Ldc_I4_6)
            {
                newInstruction = new CodeInstruction(OpCodes.Ldc_I4_7);
                Plugin.Logger.LogWarning($"ManageSelection: {line} - {instruction} -> {newInstruction}");
                newInstructions.Add(newInstruction);
            }
            else
                newInstructions.Add(instruction);

            line++;
        }

        return newInstructions;
    }
}
