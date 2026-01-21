using System.IO;
using Fleeted.packets;
using HarmonyLib;
using Steamworks;
using UnityEngine;

namespace Fleeted.patches;

[HarmonyPatch(typeof(ShipColliderController), "OnCollisionEnter2D")]
public static class GetLastCollisionTypePatch
{
    public static bool IsLastCollisionABullet;
    public static Collision2D LastCollision;

    static void Prefix(ShipColliderController __instance, Collision2D collision)
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return;
        if (!InGameNetManager.Instance.GracePeriod.TrueDone()) return;

        Random.InitState(LobbyManager.Instance.seed);

        IsLastCollisionABullet = collision.transform.CompareTag("Bullet");
        LastCollision = collision;

        if (collision.transform.CompareTag("Player"))
        {
            var colliderSlot = __instance.GetComponent<ShipController>().playerN - 1;
            var collidedSlot = collision.transform.GetComponent<ShipController>().playerN - 1;
            if (InGameNetManager.IsSlotOwnedByThisClient(colliderSlot) &&
                !InGameNetManager.IsSlotOwnedByThisClient(collidedSlot))
            {
                Plugin.Logger.LogInfo($"Colliding with {collidedSlot}");
                InGameNetManager.Instance.controllersSlots[collidedSlot].CollisionDisable.Start();
            }
        }
    }
}

[HarmonyPatch(typeof(ShipColliderController), nameof(ShipColliderController.Explode))]
public static class SendExplode
{
    public static bool PermissionToDie;

    static bool Prefix(ShipColliderController __instance)
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return true;
        if (!InGameNetManager.Instance.GracePeriod.TrueDone()) return false;

        if (InGameNetManager.Instance.isHost || PermissionToDie)
        {
            PermissionToDie = false;
            return true;
        }

        return false;
    }

    static void Postfix(ShipColliderController __instance)
    {
        if (!InGameNetManager.Instance.isHost) return;

        var sourceShip = GetLastCollisionTypePatch.IsLastCollisionABullet
            ? GetLastCollisionTypePatch.LastCollision.gameObject.GetComponent<BulletController>().player - 1
            : __instance.shipController.playerN - 1;

        Plugin.Logger.LogInfo(
            $"Sending small explosion from {__instance.shipController.playerN - 1}, killed by {sourceShip}");

        var deathPacket = new DeathPacket
        {
            TargetShip = __instance.shipController.playerN - 1,
            SourceShip = sourceShip,
            IsExplosionBig = false,
        };

        using var memoryStream = new MemoryStream();
        using (var writer = new ProtocolWriter(memoryStream))
        {
            writer.Write(deathPacket);
        }

        var data = memoryStream.ToArray();

        InGameNetManager.Instance.SendPacket2All(SteamClient.SteamId, data, PacketType.Death, P2PSend.Reliable);
    }
}

[HarmonyPatch(typeof(ShipColliderController), "ExplodeBig")]
public static class SendExplodeBig
{
    static bool Prefix(ShipColliderController __instance)
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return true;
        if (!InGameNetManager.Instance.GracePeriod.TrueDone()) return false;

        if (InGameNetManager.Instance.isHost || SendExplode.PermissionToDie)
        {
            SendExplode.PermissionToDie = false;
            return true;
        }
        return false;
    }

    static void Postfix(ShipColliderController __instance)
    {
        if (!InGameNetManager.Instance.isHost) return;
        Plugin.Logger.LogInfo($"Sending big explosion from {__instance.shipController.playerN - 1}");

        var deathPacket = new DeathPacket
        {
            TargetShip = __instance.shipController.playerN - 1,
            SourceShip = __instance.shipController.playerN - 1,
            IsExplosionBig = true,
        };

        using var memoryStream = new MemoryStream();
        using (var writer = new ProtocolWriter(memoryStream))
        {
            writer.Write(deathPacket);
        }

        var data = memoryStream.ToArray();

        InGameNetManager.Instance.SendPacket2All(SteamClient.SteamId, data, PacketType.Death, P2PSend.Reliable);
    }
}
