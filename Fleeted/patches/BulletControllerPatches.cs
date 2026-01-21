using System.IO;
using Fleeted.packets;
using HarmonyLib;
using Steamworks;

namespace Fleeted.patches;

[HarmonyPatch(typeof(BulletController), "DeactivateBullet")]
public static class StopSendBulletInfo
{
    static void Postfix(BulletController __instance)
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return;
        if (!InGameNetManager.Instance.isHost) return;

        var netId = InGameNetManager.Instance.GetBulletNetId(__instance);
        if (!netId.HasValue) return;

        var destroyPacket = new BulletDestroyPacket
        {
            NetId = netId.Value,
        };

        using var memoryStream = new MemoryStream();
        using (var writer = new ProtocolWriter(memoryStream))
        {
            writer.Write(destroyPacket);
        }

        var data = memoryStream.ToArray();
        InGameNetManager.Instance.SendPacket2All(SteamClient.SteamId, data, PacketType.BulletDestroy,
            P2PSend.Reliable);
    }
}
