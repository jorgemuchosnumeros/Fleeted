using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Fleeted.packets;

internal struct ProtocolConst
{
    public const int SegmentBits = 0x7F;
    public const int ContinueBit = 0x80;
}

public class ProtocolWriter : BinaryWriter
{
    public ProtocolWriter(Stream output) : base(output)
    {
    }

    public override void Write(int value)
    {
        while (true)
        {
            if ((value & ~ProtocolConst.SegmentBits) == 0)
            {
                Write((byte) value);
                return;
            }

            Write((byte) ((value & ProtocolConst.SegmentBits) | ProtocolConst.ContinueBit));

            value >>>= 7;
        }
    }

    public override void Write(ulong value)
    {
        while (true)
        {
            if ((value & ~(ulong) ProtocolConst.SegmentBits) == 0)
            {
                Write((byte) value);
                return;
            }

            Write((byte) ((value & ProtocolConst.SegmentBits) | ProtocolConst.ContinueBit));

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

    public void Write(Packet value)
    {
        Write((int) value.Id);
        Write(value.SteamId);
        Write(value.Data.Length);
        Write(value.Data);
    }

    public void Write(ShipPacket value)
    {
        Write(value.Slot);
        Write(value.Position);
        Write(value.Velocity);
        Write(value.Rotation);
        Write(value.StickRotation);
    }

    public void Write(BulkShipUpdate value)
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
            value |= (currentByte & ProtocolConst.SegmentBits) << position;

            if ((currentByte & ProtocolConst.ContinueBit) == 0) break;

            position += 7;

            if (position >= 32) throw new ArithmeticException("VarInt is too big");
        }

        return value;
    }

    public override ulong ReadUInt64()
    {
        var value = 0ul;
        var position = 0;

        while (true)
        {
            var currentByte = ReadByte();
            value |= (ulong) (currentByte & ProtocolConst.SegmentBits) << position;

            if ((currentByte & ProtocolConst.ContinueBit) == 0) break;

            position += 7;

            if (position >= 64) throw new ArithmeticException("VarULong is too big");
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

    public Packet ReadPacket()
    {
        return new Packet
        {
            Id = (PacketType) ReadInt32(),
            SteamId = ReadUInt64(),
            Data = ReadBytes(ReadInt32()),
        };
    }

    public ShipPacket ReadShipPacket()
    {
        return new ShipPacket
        {
            Slot = ReadInt32(),
            Position = ReadVector2(),
            Velocity = ReadVector2(),
            Rotation = ReadVector3(),
            StickRotation = ReadVector3(),
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