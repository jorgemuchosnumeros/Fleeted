using System.Collections.Generic;
using UnityEngine;

namespace Fleeted.packets;

public class ShipPacket
{
    public Vector2 Position;

    public float Rotation;

    public int Slot;

    public float StickRotation;

    public Vector2 Velocity;
}

public class BulkShipUpdate
{
    public List<ShipPacket> Updates;
}