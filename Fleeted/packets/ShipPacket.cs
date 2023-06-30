using System.Collections.Generic;
using UnityEngine;

namespace Fleeted.packets;

public class ShipPacket
{
    public Vector2 Position;

    public Vector3 Rotation;

    public int Slot;

    public Vector3 StickRotation;

    public Vector2 Velocity;
}

public class BulkShipUpdate
{
    public List<ShipPacket> Updates;
}