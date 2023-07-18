using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Fleeted.patches;

[HarmonyPatch(typeof(CameraController), "Update")]
public static class ManageCameraEvenInPause0
{
    static bool Prefix(CameraController __instance)
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return true;
        Update(__instance);
        return false;
    }

    static void Update(CameraController instance)
    {
        var updateNavmeshNextFrame =
            typeof(CameraController).GetField("updateNavmeshNextFrame", BindingFlags.Instance | BindingFlags.NonPublic);

        if ((bool) updateNavmeshNextFrame.GetValue(instance))
        {
            updateNavmeshNextFrame.SetValue(instance, false);
            WorldSpawner.worldSpawner.UpdateNavMesh();
        }

        if (GlobalController.globalController.screen == GlobalController.screens.game ||
            GlobalController.globalController.screen == GlobalController.screens.gamepause)
        {
            if (GlobalController.globalController.mode == GlobalController.modes.survival)
            {
                typeof(CameraController).GetMethod("ManageSurvivalMode", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(instance, new object[] { });
            }
            else if (GlobalController.globalController.mode == GlobalController.modes.leader)
            {
                typeof(CameraController).GetMethod("ManageLeaderMode", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(instance, new object[] { });
            }
            else if (GlobalController.globalController.mode == GlobalController.modes.race)
            {
                typeof(CameraController).GetMethod("ManageRaceMode", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(instance, new object[] { });
            }
            else if (GlobalController.globalController.mode == GlobalController.modes.yincana)
            {
                typeof(CameraController).GetMethod("ManageYincanaMode", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(instance, new object[] { });
            }
            else if (GlobalController.globalController.mode == GlobalController.modes.sumo)
            {
                typeof(CameraController).GetMethod("ManageSumoMode", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(instance, new object[] { });
            }
            else if (GlobalController.globalController.mode == GlobalController.modes.story)
            {
                typeof(CameraController).GetMethod("ManageStoryMode", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(instance, new object[] { });
            }

            var screenshakeTimer =
                typeof(CameraController).GetField("screenshakeTimer", BindingFlags.Instance | BindingFlags.NonPublic);
            if ((float) screenshakeTimer.GetValue(instance) > 0f)
            {
                typeof(CameraController).GetMethod("Screenshake", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(instance, new object[] { });
            }
        }
    }
}

[HarmonyPatch(typeof(CameraController), nameof(CameraController.GetLeader))]
public static class ManageCameraEvenInPause1
{
    static bool Prefix(CameraController __instance)
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return true;
        GetLeader(__instance);
        return false;
    }

    static void GetLeader(CameraController instance)
    {
        var leaderPlayer =
            typeof(CameraController).GetField("leaderPlayer", BindingFlags.Instance | BindingFlags.NonPublic);
        var leaderShip =
            typeof(CameraController).GetField("leaderShip", BindingFlags.Instance | BindingFlags.NonPublic);
        var leaderShipT =
            typeof(CameraController).GetField("leaderShipT", BindingFlags.Instance | BindingFlags.NonPublic);
        var movingLeaderStarColor =
            typeof(CameraController).GetField("movingLeaderStarColor", BindingFlags.Instance | BindingFlags.NonPublic);
        var centeringOnLeaderTimer =
            typeof(CameraController).GetField("centeringOnLeaderTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        var notFirstLeader =
            typeof(CameraController).GetField("notFirstLeader", BindingFlags.Instance | BindingFlags.NonPublic);
        var previousCameraPosition =
            typeof(CameraController).GetField("previousCameraPosition", BindingFlags.Instance | BindingFlags.NonPublic);

        instance.leaderMark.SetActive(value: true);
        GameObject[] array = GameObject.FindGameObjectsWithTag("Player");

        //leaderPlayer = GameState.gameState.leaderPlayer;
        leaderPlayer.SetValue(instance, GameState.gameState.leaderPlayer);

        for (int i = 0; i < array.Length; i++)
        {
            if (!(array[i].GetComponent<ShipController>() == null) && array[i].GetComponent<ShipController>().playerN ==
                (int) leaderPlayer.GetValue(instance))
            {
                //leaderShip = array[i];
                leaderShip.SetValue(instance, array[i]);

                //leaderShipT = leaderShip.transform;
                leaderShipT.SetValue(instance, ((GameObject) leaderShip.GetValue(instance)).transform);

                break;
            }
        }

        if (GlobalController.globalController.screen == GlobalController.screens.game ||
            GlobalController.globalController.screen == GlobalController.screens.gamepause)
        {
            instance.leaderMarkSR.color = (Color32) movingLeaderStarColor.GetValue(instance);
        }

        //centeringOnLeaderTimer = 0f;
        centeringOnLeaderTimer.SetValue(instance, 0f);

        if (!(bool) notFirstLeader.GetValue(instance))
        {
            //notFirstLeader = true;
            notFirstLeader.SetValue(instance, false);
            instance.centeringOnLeader = false;
        }
        else
        {
            instance.centeringOnLeader = true;
        }

        //previousCameraPosition = camT.position;
        previousCameraPosition.SetValue(instance, (Vector2) instance.camT.position);
    }
}

[HarmonyPatch(typeof(CameraController), "OnTriggerExit2D")]
public static class ManageCameraEvenInPause2
{
    static bool Prefix(Collider2D collision)
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return true;
        OnTriggerExit2D(collision);
        return false;
    }

    static void OnTriggerExit2D(Collider2D collision)
    {
        if (GlobalController.globalController.screen == GlobalController.screens.game ||
            GlobalController.globalController.screen == GlobalController.screens.gamepause)
        {
            collision.GetComponent<VisualCollider>().Offscreen();
        }
    }
}

[HarmonyPatch(typeof(CameraController), "OnTriggerEnter2D")]
public static class ManageCameraEvenInPause3
{
    static bool Prefix(Collider2D collision)
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return true;
        OnTriggerEnter2D(collision);
        return false;
    }

    static void OnTriggerEnter2D(Collider2D collision)
    {
        if (GlobalController.globalController.screen == GlobalController.screens.game ||
            GlobalController.globalController.screen == GlobalController.screens.gamepause)
        {
            collision.GetComponent<VisualCollider>().Onscreen();
        }
    }
}

[HarmonyPatch(typeof(CameraController), "Screenshake")]
public static class ManageCameraEvenInPause4
{
    private static bool PauseBypass;

    static void Prefix()
    {
        if (PauseBypass) return;

        if (GlobalController.globalController.screen == GlobalController.screens.gamepause)
        {
            ResultsController.resultsController.paused = false;
            PauseBypass = true;
        }
    }

    static void Postfix()
    {
        if (PauseBypass)
        {
            ResultsController.resultsController.paused = true;
            PauseBypass = false;
        }
    }
}

[HarmonyPatch(typeof(CameraController), "MoveRace")]
public static class MoveRaceShakeSync
{
    static void Postfix(CameraController __instance)
    {
        var screenshakeTimer = (float)
            typeof(CameraController).GetField("screenshakeTimer", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(__instance);

        if (screenshakeTimer > 0) return;

        var raceOrientation = (int)
            typeof(CameraController).GetField("raceOrientation", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(__instance);

        switch (raceOrientation)
        {
            case 0: // Up
            case 2: // Down
                __instance.transform.position = Vector3.Lerp(__instance.transform.position,
                    new Vector3(0f, __instance.transform.position.y, __instance.transform.position.z),
                    Time.deltaTime * 6f);
                break;
            case 1: // Right
            case 3: // Left
                __instance.transform.position = Vector3.Lerp(__instance.transform.position,
                    new Vector3(__instance.transform.position.x, 0f, __instance.transform.position.z),
                    Time.deltaTime * 6f);
                break;
        }
    }
}

// Testing
/*
[HarmonyPatch(typeof(CameraController), "Move")]
public static class NoCameraMovementTest0
{
    static bool Prefix()
    {
        return false;
    }
}

[HarmonyPatch(typeof(CameraController), "Resize")]
public static class NoCameraMovementTest1
{
    static bool Prefix()
    {
        return false;
    }
}
*/