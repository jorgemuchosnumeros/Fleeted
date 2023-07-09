using System;
using HarmonyLib;
using Random = UnityEngine.Random;

namespace Fleeted.patches;

[HarmonyPatch(typeof(WorldSpawner), "SetRandomMap")]
public static class WorldSpawnerSyncPatch0
{
    static void Prefix()
    {
        Random.InitState(LobbyManager.Instance.seed);
    }
}

[HarmonyPatch(typeof(WorldSpawner), "InitialInstantiateTile")]
public static class WorldSpawnerSyncPatch1
{
    static void Prefix()
    {
        Random.InitState(LobbyManager.Instance.seed);
    }
}

[HarmonyPatch(typeof(WorldSpawner), "InstantiateTileStatic")]
public static class WorldSpawnerSyncPatch2
{
    static void Prefix()
    {
        Random.InitState(LobbyManager.Instance.seed);
    }
}

[HarmonyPatch(typeof(WorldSpawner), "CreateMapNoramaLeader")]
public static class WorldSpawnerSyncPatch3
{
    static void Prefix()
    {
        Random.InitState(LobbyManager.Instance.seed);
    }
}

[HarmonyPatch(typeof(WorldSpawner), "CreateMapNoramaRace")]
public static class WorldSpawnerSyncPatch4
{
    static void Prefix()
    {
        Random.InitState(LobbyManager.Instance.seed);
    }
}

[HarmonyPatch(typeof(WorldSpawner), "EnsureRaceNoramaObstacles")]
public static class WorldSpawnerSyncPatch5
{
    static void Prefix()
    {
        Random.InitState(LobbyManager.Instance.seed);
    }
}

[HarmonyPatch(typeof(WorldSpawner), "CreateMapNoramaIni")]
public static class WorldSpawnerSyncPatch6
{
    static void Prefix()
    {
        Random.InitState(LobbyManager.Instance.seed);
    }
}

[HarmonyPatch(typeof(WorldSpawner), "CreateMapTirimoIni")]
public static class WorldSpawnerSyncPatch7
{
    static void Prefix()
    {
        Random.InitState(LobbyManager.Instance.seed);
    }
}

[HarmonyPatch(typeof(WorldSpawner), "CreateMapTirimo")]
public static class WorldSpawnerSyncPatch8
{
    static void Prefix()
    {
        Random.InitState(LobbyManager.Instance.seed);
    }
}

[HarmonyPatch(typeof(WorldSpawner), "CreateMapBaleraIni")]
public static class WorldSpawnerSyncPatch9
{
    static void Prefix()
    {
        Random.InitState(LobbyManager.Instance.seed);
    }
}

[HarmonyPatch(typeof(WorldSpawner), "CreateMapBalera")]
public static class WorldSpawnerSyncPatch10
{
    static void Prefix()
    {
        Random.InitState(LobbyManager.Instance.seed);
    }
}

[HarmonyPatch(typeof(WorldSpawner), "CreateMapNonopiIni")]
public static class WorldSpawnerSyncPatch11
{
    static void Prefix()
    {
        Random.InitState(LobbyManager.Instance.seed);
    }
}

[HarmonyPatch(typeof(WorldSpawner), "CreateMapNonopi")]
public static class WorldSpawnerSyncPatch12
{
    static void Prefix()
    {
        Random.InitState(LobbyManager.Instance.seed);
    }
}

[HarmonyPatch(typeof(WorldSpawner), "CreateMapLineraIni")]
public static class WorldSpawnerSyncPatch13
{
    static void Prefix()
    {
        Random.InitState(LobbyManager.Instance.seed);
    }
}

[HarmonyPatch(typeof(WorldSpawner), "CreateMapLinera")]
public static class WorldSpawnerSyncPatch14
{
    static void Prefix()
    {
        Random.InitState(LobbyManager.Instance.seed);
    }
}

[HarmonyPatch(typeof(WorldSpawner), "CreateMapBataliIni")]
public static class WorldSpawnerSyncPatch15
{
    static void Prefix()
    {
        Random.InitState(LobbyManager.Instance.seed);
    }
}

[HarmonyPatch(typeof(WorldSpawner), "CreateMapBatali")]
public static class WorldSpawnerSyncPatch16
{
    static void Prefix()
    {
        Random.InitState(LobbyManager.Instance.seed);
    }
}

[HarmonyPatch(typeof(WorldSpawner), "AddZones")]
public static class WorldSpawnerSyncPatch17
{
    static void Prefix()
    {
        Random.InitState(LobbyManager.Instance.seed);
    }
}

[HarmonyPatch(typeof(CameraController), "NewPos")]
public static class CameraControllerSyncPatch
{
    static void Prefix()
    {
        InGameNetManager.Instance.CameraMovementSeed++;
        Random.InitState(InGameNetManager.Instance.CameraMovementSeed);
    }

    static void Postfix()
    {
        if (LobbyManager.Instance.isHost)
            LobbyManager.Instance.UpdateSeed(LobbyManager.Instance.CurrentLobby,
                (int) DateTimeOffset.Now.Ticks);
    }
}