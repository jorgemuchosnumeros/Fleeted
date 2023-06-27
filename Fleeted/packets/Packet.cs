using System;

namespace Fleeted.packets;

public class Packet
{
    public byte[] data;
    public PacketType Id;

    public Guid sender;
}