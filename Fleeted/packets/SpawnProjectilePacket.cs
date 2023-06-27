using UnityEngine;

namespace Fleeted.packets;

public class SpawnProjectilePacket
{
    public int Id;

    public Vector2 Origin;
    public int SourceShip;
}