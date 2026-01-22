using System.IO;
using Fleeted.packets;
using HarmonyLib;
using Steamworks;
using UnityEngine;

namespace Fleeted.patches;

[HarmonyPatch(typeof(LeaderChangeController), nameof(LeaderChangeController.ManageChange))]
public static class SyncLeaderChangePatch
{
    static bool Prefix(LeaderChangeController __instance, int player1, int player2)
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return true;
        if (!InGameNetManager.Instance.isHost)
            return false;

        return true;
    }

    static void Postfix(LeaderChangeController __instance, int player1, int player2)
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return;
        if (!InGameNetManager.Instance.isHost) return;
        if (GameState.gameState == null) return;
        if (GameState.gameState.leaderPlayer != player2) return;

        var packet = new LeaderChangePacket
        {
            OldLeader = player1,
            NewLeader = player2,
        };

        Plugin.Logger.LogInfo(
            $"LeaderChange send (collision): old={packet.OldLeader}, new={packet.NewLeader}, localLeader={GameState.gameState.leaderPlayer}");

        using var memoryStream = new MemoryStream();
        using (var writer = new ProtocolWriter(memoryStream))
        {
            writer.Write(packet);
        }

        var data = memoryStream.ToArray();
        InGameNetManager.Instance.SendPacket2All(SteamClient.SteamId, data, PacketType.LeaderChange, P2PSend.Reliable);
    }
}

[HarmonyPatch(typeof(GameState), nameof(GameState.RelayLeader))]
public static class SyncRelayLeaderPatch
{
    private static int _oldLeader;

    static bool Prefix(GameState __instance, int newLeader, int shipEmissor)
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return true;
        if (!InGameNetManager.Instance.isHost)
            return false;

        _oldLeader = __instance.leaderPlayer;
        return true;
    }

    static void Postfix(GameState __instance, int newLeader, int shipEmissor)
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return;
        if (!InGameNetManager.Instance.isHost) return;
        if (__instance.leaderPlayer == _oldLeader) return;

        var packet = new LeaderChangePacket
        {
            OldLeader = _oldLeader,
            NewLeader = __instance.leaderPlayer,
        };

        Plugin.Logger.LogInfo(
            $"LeaderChange send (bullet): old={packet.OldLeader}, new={packet.NewLeader}, localLeader={__instance.leaderPlayer}");

        using var memoryStream = new MemoryStream();
        using (var writer = new ProtocolWriter(memoryStream))
        {
            writer.Write(packet);
        }

        var data = memoryStream.ToArray();
        InGameNetManager.Instance.SendPacket2All(SteamClient.SteamId, data, PacketType.LeaderChange, P2PSend.Reliable);
    }
}
