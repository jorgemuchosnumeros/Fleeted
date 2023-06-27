using System.Collections.Generic;
using UnityEngine;

namespace Fleeted.packets;

public class ShipPacket
{
    public int Flags;

    public ulong OwnerID;

    public Vector2 Position;

    public Vector3 Rotation;
    public int Slot;

    public Vector2 Velocity;
}

public enum ShipStateFlags
{
    IsBot = 1 << 0,
    Fire = 2 << 0,
    ExpansiveWave = 3 << 0,
    Dead = 4 << 0,
}

public class ShipFlagsPacket
{
    public int Slot;

    public int StateVector;
}

public class BulkShipUpdate
{
    public List<ShipPacket> Updates;
}

public class BulkShipFlagsUpdate
{
    public List<ShipFlagsPacket> Updates;
}