using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Fleeted.patches;

[HarmonyPatch(typeof(BotController), "Update")]
public class RemoveAIIfNotHostPatch0
{
    static bool Prefix()
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return true;

        return LobbyManager.Instance.isHost;
    }
}

[HarmonyPatch(typeof(BotController), "GetDestinationLeader")]
public class DontDesyncRandom0
{
    static void Prefix()
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return;
        Random.InitState(LobbyManager.Instance.seed);
    }
}

[HarmonyPatch(typeof(BotController), nameof(BotController.InitialResetAgent))]
public class DontDesyncRandom1
{
    static bool Prefix(BotController __instance)
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return true;
        InitialResetAgent(__instance);
        return false;
    }

    static void InitialResetAgent(BotController instance)
    {
        var timeSinceStarted =
            typeof(BotController).GetField("timeSinceStarted", BindingFlags.Instance | BindingFlags.NonPublic);

        var reduceRaceWarpDistance =
            typeof(BotController).GetField("reduceRaceWarpDistance", BindingFlags.Instance | BindingFlags.NonPublic);

        var isLeader =
            typeof(BotController).GetField("isLeader", BindingFlags.Instance | BindingFlags.NonPublic);

        var EnsureCameraReference =
            typeof(BotController).GetMethod("EnsureCameraReference", BindingFlags.Instance | BindingFlags.NonPublic);

        var RemoveNavmeshHoles =
            typeof(BotController).GetMethod("RemoveNavmeshHoles", BindingFlags.Instance | BindingFlags.NonPublic);

        var activated =
            typeof(BotController).GetField("activated", BindingFlags.Instance | BindingFlags.NonPublic);

        var agentResetCooldown =
            typeof(BotController).GetField("agentResetCooldown", BindingFlags.Instance | BindingFlags.NonPublic);

        var ResetAgent =
            typeof(BotController).GetMethod("ResetAgent", BindingFlags.Instance | BindingFlags.NonPublic);

        var initialRaceTimer =
            typeof(BotController).GetField("initialRaceTimer", BindingFlags.Instance | BindingFlags.NonPublic);

        var initialLeaderTimer =
            typeof(BotController).GetField("initialLeaderTimer", BindingFlags.Instance | BindingFlags.NonPublic);

        var initialLeaderTrick =
            typeof(BotController).GetField("initialLeaderTrick", BindingFlags.Instance | BindingFlags.NonPublic);

        var ChangeCurrentDestination =
            typeof(BotController).GetMethod("ChangeCurrentDestination", BindingFlags.Instance | BindingFlags.NonPublic);

        var openRaceLeft =
            typeof(BotController).GetField("openRaceLeft", BindingFlags.Instance | BindingFlags.NonPublic);

        var ManageInitialRacePosition =
            typeof(BotController).GetMethod("ManageInitialRacePosition",
                BindingFlags.Instance | BindingFlags.NonPublic);

        var currentDestination =
            typeof(BotController).GetField("currentDestination", BindingFlags.Instance | BindingFlags.NonPublic);

        var SetDestination =
            typeof(BotController).GetMethod("SetDestination", BindingFlags.Instance | BindingFlags.NonPublic);

        var getDestinationTimer =
            typeof(BotController).GetField("getDestinationTimer", BindingFlags.Instance | BindingFlags.NonPublic);

        var timeWithoutAccelerating =
            typeof(BotController).GetField("timeWithoutAccelerating", BindingFlags.Instance | BindingFlags.NonPublic);

        var agentStoppedTimer =
            typeof(BotController).GetField("agentStoppedTimer", BindingFlags.Instance | BindingFlags.NonPublic);

        var inproperAgentTimer =
            typeof(BotController).GetField("inproperAgentTimer", BindingFlags.Instance | BindingFlags.NonPublic);

        var setDestinationNextFrame =
            typeof(BotController).GetField("setDestinationNextFrame", BindingFlags.Instance | BindingFlags.NonPublic);

        var currentFreeWay =
            typeof(BotController).GetField("currentFreeWay", BindingFlags.Instance | BindingFlags.NonPublic);

        var freeWayTimer =
            typeof(BotController).GetField("freeWayTimer", BindingFlags.Instance | BindingFlags.NonPublic);

        var initialLeaderDestination =
            typeof(BotController).GetField("initialLeaderDestination", BindingFlags.Instance | BindingFlags.NonPublic);

        timeSinceStarted.SetValue(instance, 0f);
        reduceRaceWarpDistance.SetValue(instance, false);
        isLeader.SetValue(instance, false);
        EnsureCameraReference.Invoke(instance, null);
        RemoveNavmeshHoles.Invoke(instance, null);

        if (!(bool) activated.GetValue(instance))
        {
            return;
        }

        agentResetCooldown.SetValue(instance, 0f);
        ResetAgent.Invoke(instance, null);
        timeSinceStarted.SetValue(instance, 0f);
        initialRaceTimer.SetValue(instance, 100f);
        initialLeaderTimer.SetValue(instance, 100f);
        initialLeaderTrick.SetValue(instance, false);
        if (GlobalController.globalController.mode == GlobalController.modes.yincana)
        {
            ChangeCurrentDestination.Invoke(instance, new object[] {Vector2.zero});
        }
        else if (GlobalController.globalController.mode == GlobalController.modes.race)
        {
            Random.InitState(LobbyManager.Instance.seed);
            if (Random.Range(0f, 1f) < 0.5f)
            {
                openRaceLeft.SetValue(instance, true);
            }
            else
            {
                openRaceLeft.SetValue(instance, false);
            }

            ManageInitialRacePosition.Invoke(instance, null);
        }
        else if (GlobalController.globalController.mode == GlobalController.modes.leader)
        {
            initialLeaderTimer.SetValue(instance, 0f);
            Random.InitState(LobbyManager.Instance.seed);
            if (Random.Range(0f, 1f) < 0.2f)
            {
                initialLeaderTrick.SetValue(instance, true);
            }
        }
        else
        {
            ChangeCurrentDestination.Invoke(instance,
                new object[] {(Vector2) (instance.TR.position + instance.TR.up * 3f)});
        }

        if (instance.agent.isOnNavMesh)
        {
            SetDestination.Invoke(instance, new object[] {(Vector2) currentDestination.GetValue(instance)});
        }

        getDestinationTimer.SetValue(instance, 0.1f);
        timeWithoutAccelerating.SetValue(instance, 0f);
        agentStoppedTimer.SetValue(instance, 0f);
        inproperAgentTimer.SetValue(instance, 0f);
        instance.agent.isStopped = false;
        setDestinationNextFrame.SetValue(instance, false);
        currentFreeWay.SetValue(instance, false);
        freeWayTimer.SetValue(instance, 0f);
        if (GameState.gameState.leaderPlayer == instance.playerN)
        {
            initialLeaderDestination.SetValue(instance, true);
        }
    }
}

[HarmonyPatch(typeof(BotController), nameof(BotController.InitialResetAgent))]
public class DontDesyncRandom2
{
    static void Prefix()
    {
        Random.InitState(LobbyManager.Instance.seed);
    }
}

[HarmonyPatch(typeof(BotController1), "Update")]
public class RemoveAIIfNotHostPatch1
{
    static bool Prefix()
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return true;
        return LobbyManager.Instance.isHost;
    }
}