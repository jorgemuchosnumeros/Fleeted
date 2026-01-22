using UnityEngine;

namespace Fleeted.packets;

public struct BulletSpawnPacket
{
    public int NetId;
    public Vector2 Position;
    public Vector2 Velocity;
    public float Rotation;
    public int Player;
    public int ParryPlayer;
}
