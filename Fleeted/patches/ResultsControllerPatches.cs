using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Fleeted.patches;

[HarmonyPatch(typeof(ResultsController), "ShowPause")]
public static class DontChangeTimeScaleOnPausePatch
{
    static bool Prefix(ResultsController __instance)
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return true;
        ShowPauseOnline(__instance);
        Random.InitState(LobbyManager.Instance.seed);
        return false;
    }

    private static void ShowPauseOnline(ResultsController instance)
    {
        InputController.inputController.StopAllRumble();
        MusicController.musicController.PauseMusic();
        //PlayWhistle();
        typeof(ResultsController).GetMethod("PlayWhistle", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(instance, null);

        StoryResultsController.storyResultsController.RecoverCameraColor();
        instance.continueLabel.color = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);

        if (GlobalController.globalController.screen == GlobalController.screens.gamecountdown) return;

        GlobalController.globalController.screen = GlobalController.screens.gamepause;
        instance.paused = true;

        instance.storyResults.SetActive(value: false);
        instance.results.SetActive(value: true);
        instance.challengeResults.SetActive(value: false);

        var selectionShip =
            typeof(ResultsController).GetField("selectionShip", BindingFlags.Instance | BindingFlags.NonPublic);
        //selectionShip = instance.selectionShipPlay;
        selectionShip?.SetValue(instance, instance.selectionShipPlay);
        //selectionShip.SetInteger("selection", 1);
        ((Animator) selectionShip?.GetValue(instance))?.SetInteger("selection", 1);

        instance.counter.SetBool("out", false);
        instance.quadAnimator.SetBool("in", true);

        //pauseSelectionCooldown = 0.25f;
        var pauseSelectionCooldown =
            typeof(ResultsController).GetField("pauseSelectionCooldown",
                BindingFlags.Instance | BindingFlags.NonPublic);
        pauseSelectionCooldown.SetValue(instance, 0.25f);
    }
}

[HarmonyPatch(typeof(ResultsController), "HidePause")]
public static class RemovePauseMenuDuringVictory0
{
    static void Postfix(ResultsController __instance)
    {
        if (RemovePauseMenuDuringVictory1.RoundEndWhilePaused && LobbyManager.Instance.isHost)
        {
            Plugin.Logger.LogWarning("Removing Pause After Awarding Points!");
            //EndResults();
            typeof(ResultsController).GetMethod("EndResults", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(__instance, null);
        }
    }
}

[HarmonyPatch(typeof(ResultsController), nameof(ResultsController.AwardPoints))]
public static class RemovePauseMenuDuringVictory1
{
    public static bool RoundEndWhilePaused;

    static void Prefix(ResultsController __instance)
    {
        var pauseSelection =
            typeof(ResultsController).GetField("pauseSelection", BindingFlags.Instance | BindingFlags.NonPublic);

        if (GlobalController.globalController.screen == GlobalController.screens.gamepause)
        {
            RoundEndWhilePaused = true;
            Plugin.Logger.LogWarning("Round Ended While Paused!");
        }

        if (!LobbyManager.Instance.isHost)
        {
            __instance.continueLabel.color = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, 125);
        }
    }
}

[HarmonyPatch(typeof(ResultsController), "EndResults")]
public static class RemovePauseMenuDuringVictory2
{
    public static bool HostContinued;

    static bool Prefix(ResultsController __instance)
    {
        if (LobbyManager.Instance.isHost)
        {
            LobbyManager.Instance.CurrentLobby.SetData("GameStarted", "yes");
            LobbyManager.Instance.UpdateSeed(LobbyManager.Instance.CurrentLobby,
                (int) DateTimeOffset.Now.Ticks);
        }


        if (LobbyManager.Instance.isHost || HostContinued)
        {
            GlobalController.globalController.screen = GlobalController.screens.game;
            HostContinued = false;
            return true;
        }

        __instance.continueLabel.color = GlobalController.globalController.screen switch
        {
            GlobalController.screens.gameresults => new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, 125),
            GlobalController.screens.gamepause => new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue,
                byte.MaxValue),
            _ => __instance.continueLabel.color
        };

        return false;
    }
}

[HarmonyPatch(typeof(ResultsController), "Exit")]
public static class ExitEveryoneIfHostExitsGame
{
    private static void Prefix(ResultsController __instance)
    {
        var exitConfirmation = (bool) typeof(ResultsController)
            .GetField("exitConfirmation", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
        var reached = (bool) typeof(ResultsController)
            .GetField("reached", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);

        if (!exitConfirmation && !reached) return;

        //__instance.pauseSelectionCooldown = 0.25f;
        var pauseSelectionCooldown = typeof(ResultsController)
            .GetField("pauseSelectionCooldown", BindingFlags.Instance | BindingFlags.NonPublic);
        pauseSelectionCooldown.SetValue(__instance, 0.25f);

        if (LobbyManager.Instance.isHost)
        {
            LobbyManager.Instance.CurrentLobby.SetData("GameStopped", "yes");
            LobbyManager.Instance.CurrentLobby.SetData("GameStarted", string.Empty);
        }

        InGameNetManager.Instance.AbandonConnection();
        InGameNetManager.Instance.StopClient();
    }
}