using System.Collections.Generic;

namespace Fleeted.packets;

public class WorldStatePacket
{
    public int ServerTick;
    public List<ShipStatePacket> Ships;
    public List<BulletStatePacket> Bullets;
}
