using System.Collections.Generic;
using UnityEngine;

namespace Fleeted.packets;

public class UpdateProjectilePacket
{
    public int Id;

    public Vector2 Position;

    public int SourceShip;

    public Vector2 Velocity;
}

public class BulkProjectileUpdate
{
    public List<UpdateProjectilePacket> Updates;
}