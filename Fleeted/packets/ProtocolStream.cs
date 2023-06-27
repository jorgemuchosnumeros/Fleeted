using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Fleeted.packets;

public class ProtocolWriter : BinaryWriter
{
    public override void Write(int value)
    {
        while (true)
        {
            if ((value & ~0x7F) == 0)
            {
                Write((byte) value);
                return;
            }

            Write((byte) ((value & 0x7F) | 0x80));

            value >>>= 7;
        }
    }

    public void Write(Vector2 value)
    {
        Write(value.x);
        Write(value.y);
    }

    public void Write(Vector3 value)
    {
        Write(value.x);
        Write(value.y);
        Write(value.z);
    }

    public void Write(ShipPacket value)
    {
        Write(value.Slot);
        Write(value.OwnerID);
        Write(value.Position);
        Write(value.Velocity);
        Write(value.Rotation);
        Write(value.Flags);
    }

    public void Write(ShipFlagsPacket value)
    {
        Write(value.Slot);
        Write(value.StateVector);
    }

    public void Write(BulkShipUpdate value)
    {
        Write(value.Updates.Count);
        foreach (var update in value.Updates)
        {
            Write(update);
        }
    }

    public void Write(BulkShipFlagsUpdate value)
    {
        Write(value.Updates.Count);
        foreach (var update in value.Updates)
        {
            Write(update);
        }
    }

    public void Write(DeathPacket value)
    {
        Write(value.SourceShip);
        Write(value.TargetShip);
    }

    public void Write(SpawnProjectilePacket value)
    {
        Write(value.SourceShip);
        Write(value.Id);
        Write(value.Origin);
    }

    public void Write(UpdateProjectilePacket value)
    {
        Write(value.SourceShip);
        Write(value.Id);
        Write(value.Position);
        Write(value.Velocity);
        Write(value.Enabled);
    }
}

public class ProtocolReader : BinaryReader
{
    public ProtocolReader(Stream input) : base(input)
    {
    }

    public override int ReadInt32()
    {
        var value = 0;
        var position = 0;

        while (true)
        {
            var currentByte = ReadByte();
            value |= (currentByte & 0x7F) << position;

            if ((currentByte & 0x80) == 0) break;

            position += 7;

            if (position >= 32) throw new ArithmeticException("VarInt is too big");
        }

        return value;
    }

    public Vector2 ReadVector2()
    {
        return new Vector2
        {
            x = ReadSingle(),
            y = ReadSingle(),
        };
    }

    public Vector3 ReadVector3()
    {
        return new Vector3
        {
            x = ReadSingle(),
            y = ReadSingle(),
            z = ReadSingle(),
        };
    }

    public ShipPacket ReadShipPacket()
    {
        return new ShipPacket
        {
            Slot = ReadInt32(),
            OwnerID = ReadUInt64(),
            Position = ReadVector2(),
            Velocity = ReadVector2(),
            Rotation = ReadVector3(),
            Flags = ReadInt32(),
        };
    }

    public ShipFlagsPacket ReadShipFlagsPacket()
    {
        return new ShipFlagsPacket
        {
            Slot = ReadInt32(),
            StateVector = ReadInt32(),
        };
    }

    public BulkShipUpdate ReadBulkShipUpdate()
    {
        var count = ReadInt32();
        var updates = new List<ShipPacket>(count);
        for (int i = 0; i < count; i++)
        {
            updates.Add(ReadShipPacket());
        }

        return new BulkShipUpdate
        {
            Updates = updates,
        };
    }

    public BulkShipFlagsUpdate ReadBulkShipFlagsUpdate()
    {
        var count = ReadInt32();
        var updates = new List<ShipFlagsPacket>(count);
        for (int i = 0; i < count; i++)
        {
            updates.Add(ReadShipFlagsPacket());
        }

        return new BulkShipFlagsUpdate
        {
            Updates = updates,
        };
    }

    public DeathPacket ReadDeathPacket()
    {
        return new DeathPacket
        {
            SourceShip = ReadInt32(),
            TargetShip = ReadInt32(),
        };
    }

    public SpawnProjectilePacket ReadSpawnProjectilePacket()
    {
        return new SpawnProjectilePacket
        {
            SourceShip = ReadInt32(),
            Id = ReadInt32(),
            Origin = ReadVector2(),
        };
    }

    public UpdateProjectilePacket ReadUpdateProjectilePacket()
    {
        return new UpdateProjectilePacket
        {
            SourceShip = ReadInt32(),
            Id = ReadInt32(),
            Position = ReadVector2(),
            Velocity = ReadVector2(),
            Enabled = ReadBoolean()
        };
    }

    public BulkProjectileUpdate ReadBulkProjectileUpdate()
    {
        var count = ReadInt32();
        var updates = new List<UpdateProjectilePacket>(count);
        for (int i = 0; i < count; i++)
        {
            updates.Add(ReadUpdateProjectilePacket());
        }

        return new BulkProjectileUpdate
        {
            Updates = updates,
        };
    }
}