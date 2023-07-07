using System.IO;
using System.Reflection;
using Fleeted.packets;
using HarmonyLib;
using Steamworks;
using UnityEngine;

namespace Fleeted.patches;

[HarmonyPatch(typeof(ShipController), "Shoot")]
public static class SendShooting
{
    static void Postfix(ShipController __instance)
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return;

        Random.InitState(LobbyManager.Instance.seed);

        if (!InGameNetManager.IsSlotOwnedByThisClient(__instance.playerN - 1)) return;

        var bu = (GameObject) typeof(ShipController).GetField("bu", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(__instance);
        if (bu != null)
        {
            var spawnProjectilePacket = new SpawnProjectilePacket
            {
                Origin = bu.transform.position,
                SourceShip = __instance.playerN - 1,
                IsEmpty = false,
            };

            using MemoryStream memoryStream = new MemoryStream();
            using (var writer = new ProtocolWriter(memoryStream))
            {
                writer.Write(spawnProjectilePacket);
            }

            var data = memoryStream.ToArray();

            InGameNetManager.Instance.SendPacket2All(SteamClient.SteamId, data, PacketType.SpawnProjectile,
                P2PSend.Reliable);
        }
    }
}

[HarmonyPatch(typeof(ShipController), "ShootEmpty")]
public static class SendEmptyShooting
{
    static void Postfix(ShipController __instance)
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return;

        if (!InGameNetManager.IsSlotOwnedByThisClient(__instance.playerN - 1)) return;

        var spawnProjectilePacket = new SpawnProjectilePacket
        {
            Origin = __instance.transform.position,
            SourceShip = __instance.playerN - 1,
            IsEmpty = true,
        };

        using MemoryStream memoryStream = new MemoryStream();
        using (var writer = new ProtocolWriter(memoryStream))
        {
            writer.Write(spawnProjectilePacket);
        }

        var data = memoryStream.ToArray();

        InGameNetManager.Instance.SendPacket2All(SteamClient.SteamId, data, PacketType.SpawnProjectile,
            P2PSend.Reliable);
    }
}