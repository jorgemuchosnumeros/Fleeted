using System.Collections.Generic;

namespace Fleeted.packets;

public class WorldStatePacket
{
    public int RoundId;
    public int ServerTick;
    public int LeaderPlayer;
    public List<ShipStatePacket> Ships;
    public List<BulletStatePacket> Bullets;
}
