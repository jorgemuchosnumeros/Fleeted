using System.Collections;
using System.IO;
using System.Reflection;
using Fleeted.packets;
using HarmonyLib;
using Steamworks;
using UnityEngine;

namespace Fleeted.patches;

[HarmonyPatch(typeof(BulletController), "DeactivateBullet")]
public static class StopSendBulletInfo
{
    private static readonly FieldInfo InvisibleTimerField =
        typeof(BulletController).GetField("invisibleTimer", BindingFlags.Instance | BindingFlags.NonPublic);

    static void Prefix(BulletController __instance)
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return;
        if (!InGameNetManager.Instance.isHost) return;
        if (InGameNetManager.UseBulletSpawnSync)
        {
            var invisibleTimer = InvisibleTimerField != null
                ? (float) InvisibleTimerField.GetValue(__instance)
                : 0f;

            if (!__instance.exploded && invisibleTimer >= 1f)
                return;
        }

        var netId = InGameNetManager.Instance.GetBulletNetId(__instance);
        if (!netId.HasValue) return;

        var destroyPacket = new BulletDestroyPacket
        {
            NetId = netId.Value,
            Position = __instance.transform.position,
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

[HarmonyPatch(typeof(BulletController), "OnEnable")]
public static class SendBulletSpawnInfo
{
    static void Postfix(BulletController __instance)
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return;
        if (!InGameNetManager.UseBulletSpawnSync) return;
        if (!InGameNetManager.Instance.isHost)
        {
            var netId = InGameNetManager.Instance.GetBulletNetId(__instance);
            if (netId.HasValue && !InGameNetManager.Instance.ConsumeAuthorizedBulletSpawn(netId.Value))
            {
                __instance.gameObject.SetActive(false);
            }
            return;
        }
        if (GlobalController.globalController.screen != GlobalController.screens.game) return;

        InGameNetManager.Instance.StartCoroutine(SendSpawnAfterSetup(__instance));
    }

    private static IEnumerator SendSpawnAfterSetup(BulletController controller)
    {
        yield return new WaitForFixedUpdate();

        if (controller == null) yield break;
        if (!controller.gameObject.activeSelf) yield break;
        if (!InGameNetManager.UseBulletSpawnSync) yield break;
        if (!InGameNetManager.Instance.isHost) yield break;

        var netId = InGameNetManager.Instance.GetBulletNetId(controller);
        if (!netId.HasValue) yield break;

        var rb = controller.GetComponent<Rigidbody2D>();
        if (rb == null) yield break;

        var spawnPacket = new BulletSpawnPacket
        {
            NetId = netId.Value,
            Position = controller.transform.position,
            Velocity = rb.velocity,
            Rotation = controller.transform.rotation.eulerAngles.z,
            Player = controller.player,
            ParryPlayer = controller.parryPlayer,
        };

        using var memoryStream = new MemoryStream();
        using (var writer = new ProtocolWriter(memoryStream))
        {
            writer.Write(spawnPacket);
        }

        var data = memoryStream.ToArray();
        InGameNetManager.Instance.SendPacket2All(SteamClient.SteamId, data, PacketType.BulletSpawn,
            P2PSend.Reliable);
    }
}
