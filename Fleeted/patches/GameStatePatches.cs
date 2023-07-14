using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Fleeted.patches;

[HarmonyPatch(typeof(GameState), "StartNewRound")]
public static class StartNewRoundWhenEveryoneReadyPatch
{
    public static bool StartingInProgress;

    static bool Prefix(GameState __instance)
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return true;
        RemovePauseMenuDuringVictory1.RoundEndWhilePaused = false;

        Plugin.Logger.LogInfo("Starting new round!");
        if (!StartingInProgress)
        {
            InGameNetManager.Instance.StartCoroutine(
                InGameNetManager.Instance
                    .StartNewNetRound(__instance));
        }

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
            try
            {
                typeof(GameState).GetMethod("StartNewRoundSurvival", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(instance, new object[] { });
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError(ex);
            }
        }
        else if (GlobalController.globalController.mode == GlobalController.modes.leader)
        {
            //StartNewRoundLeader();
            typeof(GameState).GetMethod("StartNewRoundLeader", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(instance, new object[] { });
        }
        else if (GlobalController.globalController.mode == GlobalController.modes.race)
        {
            //StartNewRoundRace();
            typeof(GameState).GetMethod("StartNewRoundRace", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(instance, new object[] { });
        }
        else if (GlobalController.globalController.mode == GlobalController.modes.yincana)
        {
            //StartNewRoundYincana();
            typeof(GameState).GetMethod("StartNewRoundYincana", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(instance, new object[] { });
        }
        else if (GlobalController.globalController.mode == GlobalController.modes.sumo)
        {
            //StartNewRoundSumo();
            typeof(GameState).GetMethod("StartNewRoundSumo", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(instance, new object[] { });
        }
        else if (GlobalController.globalController.mode == GlobalController.modes.story)
        {
            //StartNewRoundStory();
            typeof(GameState).GetMethod("StartNewRoundStory", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(instance, new object[] { });
        }

        instance.ResetShips();
        //ResetPlayersDistance();
        typeof(GameState).GetMethod("ResetPlayersDistance", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(instance, new object[] { });

        MusicController.musicController.PlayMusic();
        instance.onlyBotsLeft = false;
    }
}

[HarmonyPatch(typeof(GameState), "SetPlayers")]
public static class SetPlayersPatch
{
    static bool Prefix(GameState __instance, bool[] pl, int[] chara, bool[] bots1)
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return true;
        SetPlayers(__instance);
        return false;
    }

    public static void SetPlayers(GameState instance)
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
            if (netPlayers.ContainsKey(i))
            {
                instance.activePlayers[i] = true;
                instance.charas[i] = netPlayers[i].Chara + 1;
                instance.bots[i] = netPlayers[i].IsBot;
                Plugin.Logger.LogInfo($"Net Chara: {netPlayers[i].Chara + 1}");
            }
            else
            {
                instance.activePlayers[i] = false;
                instance.charas[i] = 1;
                instance.bots[i] = false;
            }

            Plugin.Logger.LogInfo(
                $"Occupied: {instance.activePlayers[i]}, Chara: {instance.charas[i]}, isBot: {instance.bots[i]}");
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

[HarmonyPatch(typeof(GameState), "StartNewRoundRace")]
public static class DontDesyncRandom5
{
    static bool Prefix(GameState __instance)
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return true;
        StartNewRoundRace(__instance);
        return false;
    }

    static void StartNewRoundRace(GameState instance)
    {
        var roundEnded = typeof(GameState).GetField("roundEnded", BindingFlags.Instance | BindingFlags.NonPublic);

        var ResetPlayersDistance =
            typeof(GameState).GetMethod("ResetPlayersDistance", BindingFlags.Instance | BindingFlags.NonPublic);

        var deaths = typeof(GameState).GetField("deaths", BindingFlags.Instance | BindingFlags.NonPublic);

        var count = typeof(GameState).GetField("count", BindingFlags.Instance | BindingFlags.NonPublic);

        var shipControllers =
            typeof(GameState).GetField("shipControllers", BindingFlags.Instance | BindingFlags.NonPublic);

        var shipPositions = typeof(GameState).GetField("shipPositions", BindingFlags.Instance | BindingFlags.NonPublic);

        var r = typeof(GameState).GetField("r", BindingFlags.Instance | BindingFlags.NonPublic);

        var initialPositionsRace0 =
            typeof(GameState).GetField("initialPositionsRace0", BindingFlags.Instance | BindingFlags.NonPublic);

        var initialRotationsRace0 =
            typeof(GameState).GetField("initialRotationsRace0", BindingFlags.Instance | BindingFlags.NonPublic);

        var initialPositionsRace1 =
            typeof(GameState).GetField("initialPositionsRace1", BindingFlags.Instance | BindingFlags.NonPublic);

        var initialRotationsRace1 =
            typeof(GameState).GetField("initialRotationsRace1", BindingFlags.Instance | BindingFlags.NonPublic);

        var initialPositionsRace2 =
            typeof(GameState).GetField("initialPositionsRace2", BindingFlags.Instance | BindingFlags.NonPublic);

        var initialRotationsRace2 =
            typeof(GameState).GetField("initialRotationsRace2", BindingFlags.Instance | BindingFlags.NonPublic);

        var initialPositionsRace3 =
            typeof(GameState).GetField("initialPositionsRace3", BindingFlags.Instance | BindingFlags.NonPublic);

        var initialRotationsRace3 =
            typeof(GameState).GetField("initialRotationsRace3", BindingFlags.Instance | BindingFlags.NonPublic);

        var ShowCharaFrames =
            typeof(GameState).GetMethod("ShowCharaFrames", BindingFlags.Instance | BindingFlags.NonPublic);


        roundEnded.SetValue(instance, false);
        GlobalController.globalController.screen = GlobalController.screens.gamecountdown;
        instance.playersAlive = new bool[8];
        instance.totalPlayers = 0;
        Random.InitState(LobbyManager.Instance.seed);
        instance.raceOrientation = Random.Range(0, 4);
        if (instance.raceOrientation == 0 || instance.raceOrientation == 1)
        {
            instance.currentWinnerDistance = -40f;
        }
        else
        {
            instance.currentWinnerDistance = 40f;
        }

        ResetPlayersDistance.Invoke(instance, null);
        instance.HideRaceLines();
        for (int i = 0; i < 8; i++)
        {
            if (instance.activePlayers[i])
            {
                instance.shipCharacter[i].SetAsChara(instance.charas[i]);
                instance.playersAlive[i] = true;
                deaths.SetValue(instance, 0);
                instance.totalPlayers++;
            }
        }

        instance.actualPlayers = new int[instance.totalPlayers];
        count.SetValue(instance, 0);
        for (int j = 0; j < 8; j++)
        {
            if (instance.activePlayers[j])
            {
                instance.actualPlayers[(int) count.GetValue(instance)] = j + 1;
                count.SetValue(instance, (int) count.GetValue(instance) + 1);
            }
        }

        instance.cameraController = GameObject.Find("Render Camera").GetComponent<CameraController>();
        instance.cameraController.ResetCamera();
        WorldSpawner.worldSpawner.NewMap();
        for (int k = 0; k < instance.actualPlayers.Length; k++)
        {
            var tmpShipControllers = (ShipController[]) shipControllers.GetValue(instance);
            tmpShipControllers[instance.actualPlayers[k] - 1].playerN = instance.actualPlayers[k];
            shipControllers.SetValue(instance, tmpShipControllers);

            instance.playerShips[instance.actualPlayers[k] - 1].SetActive(value: true);
        }

        shipPositions.SetValue(instance, new int[8]);

        for (int l = 0; l < instance.actualPlayers.Length; l++)
        {
            Random.InitState(LobbyManager.Instance.seed + l);
            r.SetValue(instance, Random.Range(0, 8));
            while (((int[]) shipPositions.GetValue(instance))[(int) r.GetValue(instance)] != 0)
            {
                r.SetValue(instance, (int) r.GetValue(instance) + 1);
                if ((int) r.GetValue(instance) > 7)
                {
                    r.SetValue(instance, 0);
                }
            }

            var tmpShipPositions = (int[]) shipPositions.GetValue(instance);
            tmpShipPositions[(int) r.GetValue(instance)] = instance.actualPlayers[l];
            shipPositions.SetValue(instance, tmpShipPositions);
        }

        for (var m = 0; m < ((int[]) shipPositions.GetValue(instance)).Length; m++)
        {
            if (((int[]) shipPositions.GetValue(instance))[m] == 0) continue;

            switch (instance.raceOrientation)
            {
                case 0:
                    instance.playerShips[((int[]) shipPositions.GetValue(instance))[m] - 1].transform.position =
                        ((Vector2[]) initialPositionsRace0.GetValue(instance))[m];
                    instance.playerShips[((int[]) shipPositions.GetValue(instance))[m] - 1].transform.eulerAngles =
                        ((Vector3[]) initialRotationsRace0.GetValue(instance))[m];
                    break;
                case 1:
                    instance.playerShips[((int[]) shipPositions.GetValue(instance))[m] - 1].transform.position =
                        ((Vector2[]) initialPositionsRace1.GetValue(instance))[m];
                    instance.playerShips[((int[]) shipPositions.GetValue(instance))[m] - 1].transform.eulerAngles =
                        ((Vector3[]) initialRotationsRace1.GetValue(instance))[m];
                    break;
                case 2:
                    instance.playerShips[((int[]) shipPositions.GetValue(instance))[m] - 1].transform.position =
                        ((Vector2[]) initialPositionsRace2.GetValue(instance))[m];
                    instance.playerShips[((int[]) shipPositions.GetValue(instance))[m] - 1].transform.eulerAngles =
                        ((Vector3[]) initialRotationsRace2.GetValue(instance))[m];
                    break;
                default:
                    instance.playerShips[((int[]) shipPositions.GetValue(instance))[m] - 1].transform.position =
                        ((Vector2[]) initialPositionsRace3.GetValue(instance))[m];
                    instance.playerShips[((int[]) shipPositions.GetValue(instance))[m] - 1].transform.eulerAngles =
                        ((Vector3[]) initialRotationsRace3.GetValue(instance))[m];
                    break;
            }
        }

        instance.semaphore.ResetTrigger("cancel");
        instance.semaphore.SetTrigger("start");
        ShowCharaFrames.Invoke(instance, null);
    }
}

[HarmonyPatch(typeof(GameState), "SetRandomMode")]
public static class DontDesyncRandom6
{
    static void Prefix()
    {
        Random.InitState(LobbyManager.Instance.seed);
    }
}