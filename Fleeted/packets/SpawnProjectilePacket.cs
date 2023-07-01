using UnityEngine;

namespace Fleeted.packets;

public class SpawnProjectilePacket
{
    public int Id;

    public bool IsEmpty;

    public Vector2 Origin;

    public int SourceShip;
}