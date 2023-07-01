using Fleeted.packets;
using UnityEngine;

namespace Fleeted;

public class NetBulletController : MonoBehaviour
{
    private UpdateProjectilePacket _latestSPacket;

    public void ReceiveUpdates(UpdateProjectilePacket packet)
    {
        _latestSPacket = packet;

        Plugin.Logger.LogInfo(
            $"Received a bullet from {packet.SourceShip}\nvelocity: {packet.Velocity}, position: {packet.Position}, id: {packet.Id}");
    }
}