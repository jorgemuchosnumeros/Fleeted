using HarmonyLib;

namespace Fleeted.patches;

[HarmonyPatch(typeof(ShipController), "Update")]
public static class ShipControllerNetInputPatch
{
    static void Prefix(ShipController __instance)
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return;
        if (InGameNetManager.Instance == null || !InGameNetManager.Instance.isHost) return;

        var slot = __instance.playerN - 1;
        if (slot < 0) return;
        if (InGameNetManager.IsSlotOwnedByThisClient(slot)) return;

        if (!InGameNetManager.Instance.TryGetNetInputState(slot, out var thrust, out var turnLeft,
                out var turnRight, out var shootDown))
            return;

        var inputIndex = slot + 1;
        var inputs = InputController.inputController.inputs;

        inputs[InputController.inputController.sA[inputIndex]] = thrust;
        inputs[InputController.inputController.sL[inputIndex]] = turnLeft;
        inputs[InputController.inputController.sR[inputIndex]] = turnRight;
        inputs[InputController.inputController.sBDown[inputIndex]] = shootDown;
        inputs[InputController.inputController.sB[inputIndex]] = shootDown;
    }

    static void Postfix(ShipController __instance)
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return;
        if (InGameNetManager.Instance == null || !InGameNetManager.Instance.isHost) return;

        var slot = __instance.playerN - 1;
        if (InGameNetManager.IsSlotOwnedByThisClient(slot)) return;

        var inputIndex = slot + 1;
        var inputs = InputController.inputController.inputs;

        inputs[InputController.inputController.sBDown[inputIndex]] = false;
        inputs[InputController.inputController.sB[inputIndex]] = false;

        InGameNetManager.Instance.ConsumeShootDown(slot);
    }
}
