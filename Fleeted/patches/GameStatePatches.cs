using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using Steamworks.Data;
using UnityEngine;

namespace Fleeted.patches;

[HarmonyPatch(typeof(GameState), "StartNewRound")]
public static class StartNewRoundWhenEveryoneReadyPatch
{
    static bool Prefix(GameState __instance)
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return true;
        GameState.gameState.StartCoroutine(StartNewNetRound(__instance));

        return false;
    }

    private static IEnumerator StartNewNetRound(GameState instance)
    {
        // Wait for everyone to be ready and host decides a date to start the Game,
        // ensuring everyone starts at the same time

        var lobby = LobbyManager.Instance.CurrentLobby;

        lobby.SetMemberData("Ready", "yes");

        while (LobbyManager.Instance.isHost)
        {
            Thread.Sleep(500);

            if (!IsEveryoneReady(lobby)) continue;

            lobby.SetData("StartTime",
                $"{DateTime.Now.Ticks + 30000000L}"); // Give 3 seconds of headstart for the rest to be ready
            break;
        }

        yield return new WaitUntil(() => lobby.GetData("StartTime") != string.Empty);

        Plugin.Logger.LogInfo($"Current Time: {DateTime.Now.Ticks}");
        Plugin.Logger.LogInfo($"Everyone is ready, starting at {lobby.GetData("StartTime")}");
        InGameNetManager.Instance.ConnectingMessage(true);

        yield return new WaitUntil(() => DateTime.Now.Ticks >= long.Parse(lobby.GetData("StartTime")));

        InGameNetManager.Instance.ConnectingMessage(false);

        Plugin.Logger.LogInfo($"Current Time: {DateTime.Now.Ticks}");
        Plugin.Logger.LogInfo("Starting Round...");


        LobbyManager.Instance.CurrentLobby.SetMemberData("Ready", String.Empty);

        if (LobbyManager.Instance.isHost)
            LobbyManager.Instance.CurrentLobby.SetData("StartTime", String.Empty);

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

    private static bool IsEveryoneReady(Lobby lobby)
    {
        return lobby.Members.All(member => lobby.GetMemberData(member, "Ready") == "yes");
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