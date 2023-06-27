using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Fleeted.patches;

[HarmonyPatch(typeof(GameState), "StartNewRound")]
public static class StartNewRoundWhenEveryoneReadyPatch
{
    public static bool StartingInProgress;

    static bool Prefix(GameState __instance)
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return true;
        RemovePauseMenuDuringVictory1.RoundEndWhilePaused = false;

        if (!StartingInProgress)
            InGameNetManager.Instance.StartCoroutine(
                InGameNetManager.Instance
                    .StartNewNetRound(__instance));
        return false;
    }

    public static void StartNewRound(GameState instance)
    {
        GlobalAudio.globalAudio.exitingGame = false;

        //DeactivateNonPlayingShips();
        typeof(GameState).GetMethod("DeactivateNonPlayingShips", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(instance, new object[] { });

        instance.HideRaceLines();

        WorldSpawner.worldSpawner.HideZones();
        if (instance.cameraController == null)
        {
            instance.cameraController = GameObject.Find("Render Camera").GetComponent<CameraController>();
        }

        instance.cameraController.leaderMark.SetActive(value: false);
        instance.cameraController.ResetScreenshake();
        if (GlobalController.globalController.mix)
        {
            instance.SetRandomMode();
        }

        if (!GlobalController.globalController.lariUnlocked &&
            ResultsController.resultsController.lariTrigger.activeSelf)
        {
            LariTriggerController.lariTriggerController.DisableLariShip();
            ResultsController.resultsController.lariTrigger.SetActive(value: false);
        }

        ResultsController.resultsController.UpdateFadePos(instance.cameraController.camT.position);
        if (GlobalController.globalController.mode == GlobalController.modes.survival)
        {
            //StartNewRoundSurvival();
            typeof(GameState).GetMethod("StartNewRoundSurvival", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(instance, new object[] { });
        }
        else if (GlobalController.globalController.mode == GlobalController.modes.leader)
        {
            //StartNewRoundLeader();
            typeof(GameState).GetMethod("StartNewRoundLeader", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(instance, new object[] { });
        }
        else if (GlobalController.globalController.mode == GlobalController.modes.race)
        {
            //StartNewRoundRace();
            typeof(GameState).GetMethod("StartNewRoundRace", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(instance, new object[] { });
        }
        else if (GlobalController.globalController.mode == GlobalController.modes.yincana)
        {
            //StartNewRoundYincana();
            typeof(GameState).GetMethod("StartNewRoundYincana", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(instance, new object[] { });
        }
        else if (GlobalController.globalController.mode == GlobalController.modes.sumo)
        {
            //StartNewRoundSumo();
            typeof(GameState).GetMethod("StartNewRoundSumo", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(instance, new object[] { });
        }
        else if (GlobalController.globalController.mode == GlobalController.modes.story)
        {
            //StartNewRoundStory();
            typeof(GameState).GetMethod("StartNewRoundStory", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(instance, new object[] { });
        }

        instance.ResetShips();
        //ResetPlayersDistance();
        typeof(GameState).GetMethod("ResetPlayersDistance", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(instance, new object[] { });

        MusicController.musicController.PlayMusic();
        instance.onlyBotsLeft = false;
    }
}

[HarmonyPatch(typeof(GameState), "SetPlayers")]
public static class SetPlayersPatch
{
    static bool Prefix(GameState __instance, bool[] pl, int[] chara, bool[] bots1)
    {
        for (var i = 0; i < pl.Length; i++)
        {
            try
            {
                Plugin.Logger.LogInfo($"Occupied: {pl[i]}, Chara: {chara[i]}, isBot: {bots1[i]}");
            }
            catch (IndexOutOfRangeException)
            {
                Plugin.Logger.LogError($"Player slot {i} is unavailable!");
            }
        }

        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return true;
        SetPlayers(__instance, pl, chara, bots1);
        return false;
    }

    static void SetPlayers(GameState instance, bool[] pl, int[] chara, bool[] bots1)
    {
        var shipCustomizations =
            typeof(GameState).GetField("shipCustomizations", BindingFlags.Instance | BindingFlags.NonPublic);

        var shipControllers =
            typeof(GameState).GetField("shipControllers", BindingFlags.Instance | BindingFlags.NonPublic);

        var netPlayers = LobbyManager.Instance.Players;

        instance.activePlayers = new bool[8];
        instance.charas = new int[8];
        instance.bots = new bool[8];

        for (var i = 0; i < 8; i++)
        {
            try
            {
                instance.activePlayers[i] = true;
                instance.charas[i] = netPlayers[i].Chara + 1;
                instance.bots[i] = netPlayers[i].IsBot;
            }
            catch (KeyNotFoundException)
            {
                instance.activePlayers[i] = false;
                instance.charas[i] = 1;
                instance.bots[i] = false;
            }
        }


        shipCustomizations.SetValue(instance, new ShipCustomization[8]);
        instance.shipCharacter = new ShipCharacter[8];
        shipControllers.SetValue(instance, new ShipController[8]);

        for (int i = 0; i < 8; i++)
        {
            //shipCustomizations[i] = playerShips[i].GetComponent<ShipCustomization>();
            var tmpShipCustomizations = (ShipCustomization[]) shipCustomizations.GetValue(instance);
            tmpShipCustomizations[i] = instance.playerShips[i].GetComponent<ShipCustomization>();
            shipCustomizations.SetValue(instance, tmpShipCustomizations);

            instance.shipCharacter[i] = instance.playerShips[i].GetComponent<ShipCharacter>();

            //shipControllers = playerShips[i].GetComponent<ShipController>();
            var tmpShipControllers = (ShipController[]) shipControllers.GetValue(instance);
            tmpShipControllers[i] = instance.playerShips[i].GetComponent<ShipController>();
            shipControllers.SetValue(instance, tmpShipControllers);
        }

        for (int j = 0; j < instance.bots.Length; j++)
        {
            if (instance.bots[j])
            {
                //shipControllers[j].botController.activateWhenInGame = true;
                var tmpShipControllers = (ShipController[]) shipControllers.GetValue(instance);
                tmpShipControllers[j].botController.activateWhenInGame = true;
            }
            else
            {
                //shipControllers[j].botController.DeactivateAI();
                var tmpShipControllers = (ShipController[]) shipControllers.GetValue(instance);
                tmpShipControllers[j].botController.DeactivateAI();
            }
        }
    }
}

[HarmonyPatch(typeof(GameState), "Update")]
public static class KeepGameGoingDuringPause
{
    static bool Prefix(GameState __instance)
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return true;
        Update(__instance);
        return false;
    }

    static void Update(GameState instance)
    {
        if (instance.test)
        {
            return;
        }

        var checkIfSceneLoaded =
            typeof(GameState).GetField("checkIfSceneLoaded", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var asyncLoad =
            typeof(GameState).GetField("asyncLoad", BindingFlags.Instance | BindingFlags.NonPublic)!;

        if ((bool) checkIfSceneLoaded.GetValue(instance) && ((AsyncOperation) asyncLoad.GetValue(instance)).isDone)
        {
            //checkIfSceneLoaded = false;
            checkIfSceneLoaded.SetValue(instance, false);
            FadeController.fadeController.FadeOut();
            if (GlobalController.globalController.mode == GlobalController.modes.practice)
            {
                instance.SetPractice();
                return;
            }

            if (GlobalController.globalController.mode == GlobalController.modes.challenge)
            {
                //SetChallenge();
                typeof(GameState).GetMethod("SetChallenge", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(
                    instance, null);
                return;
            }

            instance.StartNewGame();
        }

        if ((GlobalController.globalController.screen == GlobalController.screens.game ||
             GlobalController.globalController.screen == GlobalController.screens.gamepause) &&
            !instance.avoidEndingDuringUnlock)
        {
            if (GlobalController.globalController.mode == GlobalController.modes.survival)
            {
                //ManageSurvivalMode();
                typeof(GameState).GetMethod("ManageSurvivalMode", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(instance, null);
            }
            else if (GlobalController.globalController.mode == GlobalController.modes.leader)
            {
                //ManageLeaderMode();
                typeof(GameState).GetMethod("ManageLeaderMode", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(
                    instance, null);
            }
            else if (GlobalController.globalController.mode == GlobalController.modes.race)
            {
                //ManageRaceMode();
                typeof(GameState).GetMethod("ManageRaceMode", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(
                    instance, null);
            }
            else if (GlobalController.globalController.mode == GlobalController.modes.yincana)
            {
                //ManageYincanaMode();
                typeof(GameState).GetMethod("ManageYincanaMode", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(instance, null);
            }
            else if (GlobalController.globalController.mode == GlobalController.modes.sumo)
            {
                // ManageSurvivalMode();
                typeof(GameState).GetMethod("ManageSurvivalMode", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(instance, null);
            }
        }
    }
}