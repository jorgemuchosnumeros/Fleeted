using UnityEngine;

namespace Fleeted.packets;

public class CameraStatePacket
{
    public int ServerTick;
    public Vector3 Position;
    public float OrthographicSize;
}
