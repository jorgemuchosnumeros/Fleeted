using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace Fleeted;

//Im so sorry, Alva

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

[HarmonyPatch(typeof(MainMenuController), "ManageApply")]
public static class RaiseMenuLimitAtManageApply
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> newInstructions = new List<CodeInstruction>();
        int line = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            CodeInstruction newInstruction;
            if (instruction.opcode == OpCodes.Ldc_I4_2)
            {
                newInstruction = new CodeInstruction(OpCodes.Ldc_I4_3);
                Plugin.Logger.LogWarning($"ManageSelection: {line} - {instruction} -> {newInstruction}");
                newInstructions.Add(newInstruction);
            }
            else if (instruction.opcode == OpCodes.Ldc_I4_3)
            {
                newInstruction = new CodeInstruction(OpCodes.Ldc_I4_4);
                Plugin.Logger.LogWarning($"ManageSelection: {line} - {instruction} -> {newInstruction}");
                newInstructions.Add(newInstruction);
            }
            else if (instruction.opcode == OpCodes.Ldc_I4_4)
            {
                newInstruction = new CodeInstruction(OpCodes.Ldc_I4_5);
                Plugin.Logger.LogWarning($"ManageSelection: {line} - {instruction} -> {newInstruction}");
                newInstructions.Add(newInstruction);
            }
            else if (instruction.opcode == OpCodes.Ldc_I4_5)
            {
                newInstruction = new CodeInstruction(OpCodes.Ldc_I4_6);
                Plugin.Logger.LogWarning($"ManageSelection: {line} - {instruction} -> {newInstruction}");
                newInstructions.Add(newInstruction);
            }
            else if (instruction.opcode == OpCodes.Ldc_I4_6)
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