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

    public void Write(PingPacket value)
    {
        Write(value.Response);
    }

    public void Write(InputPacket value)
    {
        Write(value.Slot);
        Write(value.Sequence);
        Write(value.Thrust);
        Write(value.TurnLeft);
        Write(value.TurnRight);
        Write(value.Shoot);
    }

    public void Write(DeathPacket value)
    {
        Write(value.SourceShip);
        Write(value.TargetShip);
        Write(value.IsExplosionBig);
    }

    public void Write(ShipStatePacket value)
    {
        Write(value.Slot);
        Write(value.ServerTick);
        Write(value.Position);
        Write(value.Velocity);
        Write(value.Rotation);
        Write(value.StickRotation);
        Write(value.AngularVelocity);
        Write(value.LastProcessedInput);
        Write(value.Ammo);
    }

    public void Write(BulletStatePacket value)
    {
        Write(value.NetId);
        Write(value.ServerTick);
        Write(value.Position);
        Write(value.Velocity);
        Write(value.Active);
    }

    public void Write(BulletSpawnPacket value)
    {
        Write(value.NetId);
        Write(value.Position);
        Write(value.Velocity);
        Write(value.Rotation);
        Write(value.Player);
        Write(value.ParryPlayer);
    }

    public void Write(WorldStatePacket value)
    {
        Write(value.RoundId);
        Write(value.ServerTick);
        Write(value.LeaderPlayer);
        Write(value.Ships.Count);
        foreach (var ship in value.Ships)
        {
            Write(ship);
        }

        Write(value.Bullets.Count);
        foreach (var bullet in value.Bullets)
        {
            Write(bullet);
        }
    }

    public void Write(LeaderChangePacket value)
    {
        Write(value.OldLeader);
        Write(value.NewLeader);
    }

    public void Write(BulletDestroyPacket value)
    {
        Write(value.NetId);
        Write(value.Position);
    }

    public void Write(TileDestroyPacket value)
    {
        Write(value.Position);
    }

    public void Write(CameraStatePacket value)
    {
        Write(value.ServerTick);
        Write(value.Position);
        Write(value.OrthographicSize);
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

    public PingPacket ReadPingPacket()
    {
        return new PingPacket()
        {
            Response = ReadBoolean(),
        };
    }

    public InputPacket ReadInputPacket()
    {
        return new InputPacket
        {
            Slot = ReadInt32(),
            Sequence = ReadInt32(),
            Thrust = ReadBoolean(),
            TurnLeft = ReadBoolean(),
            TurnRight = ReadBoolean(),
            Shoot = ReadBoolean(),
        };
    }

    public DeathPacket ReadDeathPacket()
    {
        return new DeathPacket
        {
            SourceShip = ReadInt32(),
            TargetShip = ReadInt32(),
            IsExplosionBig = ReadBoolean(),
        };
    }

    public ShipStatePacket ReadShipStatePacket()
    {
        return new ShipStatePacket
        {
            Slot = ReadInt32(),
            ServerTick = ReadInt32(),
            Position = ReadVector2(),
            Velocity = ReadVector2(),
            Rotation = ReadSingle(),
            StickRotation = ReadSingle(),
            AngularVelocity = ReadSingle(),
            LastProcessedInput = ReadInt32(),
            Ammo = ReadInt32(),
        };
    }

    public BulletStatePacket ReadBulletStatePacket()
    {
        return new BulletStatePacket
        {
            NetId = ReadInt32(),
            ServerTick = ReadInt32(),
            Position = ReadVector2(),
            Velocity = ReadVector2(),
            Active = ReadBoolean(),
        };
    }

    public BulletSpawnPacket ReadBulletSpawnPacket()
    {
        return new BulletSpawnPacket
        {
            NetId = ReadInt32(),
            Position = ReadVector2(),
            Velocity = ReadVector2(),
            Rotation = ReadSingle(),
            Player = ReadInt32(),
            ParryPlayer = ReadInt32(),
        };
    }

    public WorldStatePacket ReadWorldStatePacket()
    {
        var roundId = ReadInt32();
        var serverTick = ReadInt32();
        var leaderPlayer = ReadInt32();
        var shipCount = ReadInt32();
        var ships = new List<ShipStatePacket>(shipCount);
        for (var i = 0; i < shipCount; i++)
        {
            ships.Add(ReadShipStatePacket());
        }

        var bulletCount = ReadInt32();
        var bullets = new List<BulletStatePacket>(bulletCount);
        for (var i = 0; i < bulletCount; i++)
        {
            bullets.Add(ReadBulletStatePacket());
        }

        return new WorldStatePacket
        {
            RoundId = roundId,
            ServerTick = serverTick,
            LeaderPlayer = leaderPlayer,
            Ships = ships,
            Bullets = bullets,
        };
    }

    public LeaderChangePacket ReadLeaderChangePacket()
    {
        return new LeaderChangePacket
        {
            OldLeader = ReadInt32(),
            NewLeader = ReadInt32(),
        };
    }

    public BulletDestroyPacket ReadBulletDestroyPacket()
    {
        return new BulletDestroyPacket
        {
            NetId = ReadInt32(),
            Position = ReadVector2(),
        };
    }

    public TileDestroyPacket ReadTileDestroyPacket()
    {
        return new TileDestroyPacket
        {
            Position = ReadVector2(),
        };
    }

    public CameraStatePacket ReadCameraStatePacket()
    {
        return new CameraStatePacket
        {
            ServerTick = ReadInt32(),
            Position = ReadVector3(),
            OrthographicSize = ReadSingle(),
        };
    }
}
