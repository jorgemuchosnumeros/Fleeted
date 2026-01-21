using UnityEngine;

namespace Fleeted.packets;

public class BulletStatePacket
{
    public int NetId;
    public int ServerTick;
    public Vector2 Position;
    public Vector2 Velocity;
    public bool Active;
}
