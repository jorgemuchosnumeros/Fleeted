using UnityEngine;

namespace Fleeted.packets;

public class ShipStatePacket
{
    public int Slot;
    public int ServerTick;
    public Vector2 Position;
    public Vector2 Velocity;
    public float Rotation;
    public float StickRotation;
    public float AngularVelocity;
    public int LastProcessedInput;
    public int Ammo;
}
