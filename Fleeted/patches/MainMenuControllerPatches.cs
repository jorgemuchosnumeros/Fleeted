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
        ApplyPlayOnlinePatch.IsOnlineOptionSelected = __instance.selection == 2;
    }
}

[HarmonyPatch(typeof(MainMenuController), "BackFromPlayMenu")]
public class PlayMenuControllerPatches
{
    static void Postfix(MainMenuController __instance)
    {
        if (ApplyPlayOnlinePatch.IsOnlineOptionSelected)
            CustomLobbyMenu.Instance.HideLobbyMenu();
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
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
    {
        List<CodeInstruction> newInstructions = new List<CodeInstruction>();
        int line = 0;

        Label label64 = il.DefineLabel();
        Label label72 = il.DefineLabel();

        foreach (CodeInstruction instruction in instructions)
        {
            CodeInstruction newInstruction;
            if (instruction.opcode == OpCodes.Ldc_I4_2)
            {
                newInstruction = new CodeInstruction(OpCodes.Ldc_I4_3);
                Plugin.Logger.LogWarning($"ManageApply: {line} - {instruction} -> {newInstruction}");
                newInstructions.Add(newInstruction);
            }
            else if (instruction.opcode == OpCodes.Ldc_I4_3)
            {
                newInstruction = new CodeInstruction(OpCodes.Ldc_I4_4);
                Plugin.Logger.LogWarning($"ManageApply: {line} - {instruction} -> {newInstruction}");
                newInstructions.Add(newInstruction);
            }
            else if (instruction.opcode == OpCodes.Ldc_I4_4)
            {
                newInstruction = new CodeInstruction(OpCodes.Ldc_I4_5);
                Plugin.Logger.LogWarning($"ManageApply: {line} - {instruction} -> {newInstruction}");
                newInstructions.Add(newInstruction);
            }
            else if (instruction.opcode == OpCodes.Ldc_I4_5)
            {
                newInstruction = new CodeInstruction(OpCodes.Ldc_I4_6);
                Plugin.Logger.LogWarning($"ManageApply: {line} - {instruction} -> {newInstruction}");
                newInstructions.Add(newInstruction);
            }
            else if (instruction.opcode == OpCodes.Ldc_I4_6)
            {
                newInstruction = new CodeInstruction(OpCodes.Ldc_I4_7);
                Plugin.Logger.LogWarning($"ManageApply: {line} - {instruction} -> {newInstruction}");
                newInstructions.Add(newInstruction);
            }
            else if (line == 59 && instruction.opcode == OpCodes.Bne_Un)
            {
                newInstruction = new CodeInstruction(OpCodes.Beq_S, label64); // Be Careful with this one
                newInstructions.Add(newInstruction);
                Plugin.Logger.LogWarning($"ManageApply: {line} - {instruction} -> {newInstruction}");
            }
            else if (line == 60 && instruction.opcode == OpCodes.Ldsfld)
            {
                var addingInstruction = new CodeInstruction(OpCodes.Ldarg_0);
                newInstructions.Add(addingInstruction);
                Plugin.Logger.LogMessage($"ManageApply: ADDING {line++} - {addingInstruction}");

                addingInstruction = new CodeInstruction(OpCodes.Ldfld,
                    AccessTools.Field(typeof(MainMenuController), nameof(MainMenuController.selection)));
                newInstructions.Add(addingInstruction);
                Plugin.Logger.LogMessage($"ManageApply: ADDING {line++} - {addingInstruction}");

                addingInstruction = new CodeInstruction(OpCodes.Ldc_I4_2);
                newInstructions.Add(addingInstruction);
                Plugin.Logger.LogMessage($"ManageApply: ADDING {line++} - {addingInstruction}");

                addingInstruction = new CodeInstruction(OpCodes.Bne_Un_S, label72); // Be Careful also with this one
                newInstructions.Add(addingInstruction);
                Plugin.Logger.LogMessage($"ManageApply: ADDING {line++} - {addingInstruction}");

                instruction.labels.Add(label64);
                newInstructions.Add(instruction); // Dont Forget the original instruction
                Plugin.Logger.LogInfo($"ManageApply: {line} - {instruction}");
            }
            else if (line == 72 && instruction.opcode == OpCodes.Ldarg_0)
            {
                instruction.labels.Add(label72);
                newInstructions.Add(instruction);
                Plugin.Logger.LogWarning($"ManageApply: {line} - {instruction}");
            }
            else
            {
                newInstructions.Add(instruction);
                //Plugin.Logger.LogInfo($"ManageApply: {line} - {instruction}");
            }

            line++;
        }

        return newInstructions;
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