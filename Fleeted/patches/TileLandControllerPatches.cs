using System.IO;
using Fleeted.packets;
using HarmonyLib;
using Steamworks;
using UnityEngine;

namespace Fleeted.patches;

[HarmonyPatch(typeof(TileLandController), nameof(TileLandController.DisableTile))]
public static class SyncTileDestroyPatch
{
    public static bool SuppressNetSend;

    static bool Prefix(TileLandController __instance)
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return true;
        if (SuppressNetSend) return true;
        if (!InGameNetManager.Instance.isHost) return false;

        var destroyPacket = new TileDestroyPacket
        {
            Position = __instance.transform.position,
        };

        using var memoryStream = new MemoryStream();
        using (var writer = new ProtocolWriter(memoryStream))
        {
            writer.Write(destroyPacket);
        }

        var data = memoryStream.ToArray();
        InGameNetManager.Instance.SendPacket2All(SteamClient.SteamId, data, PacketType.TileDestroy, P2PSend.Reliable);
        return true;
    }
}
