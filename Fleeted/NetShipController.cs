using System;
using System.Reflection;
using Fleeted.packets;
using UnityEngine;

namespace Fleeted;

public class NetShipController : MonoBehaviour
{
    private bool _alreadyShot;
    private ShipController _controller;
    private PacketType _latestID;
    private SpawnProjectilePacket _latestPSPacket;

    private ShipPacket _latestSPacket;
    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _controller = GetComponent<ShipController>();
    }

    private void Update()
    {
        if (_latestSPacket == null) return;

        switch (_latestID)
        {
            case PacketType.ShipUpdate:
            {
                _rb.velocity = _latestSPacket.Velocity;

                var posDiscrepancy = (new Vector2(transform.position.x, transform.position.y) - _latestSPacket.Position)
                    .sqrMagnitude;
                if (posDiscrepancy > 2f)
                {
                    transform.position = _latestSPacket.Position;
                }
                else if (posDiscrepancy > 0.2f)
                {
                    transform.position = Vector2.Lerp(transform.position, _latestSPacket.Position, 6f * Time.deltaTime);
                }

                var stick = transform.GetChild(2).GetChild(2);
                stick.localRotation = Quaternion.Slerp(stick.localRotation,
                    Quaternion.Euler(new Vector3(0, 0, _latestSPacket.StickRotation)), 3f * Time.deltaTime);

                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.Euler(new Vector3(0, 0, _latestSPacket.Rotation)), 10f * Time.deltaTime);
                break;
            }
            case PacketType.Death:
                break;
            case PacketType.SpawnProjectile:
                if (_alreadyShot) break;

                if (!_latestPSPacket.IsEmpty)
                {
                    //_controller.Shoot();
                    typeof(ShipController).GetMethod("Shoot", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(_controller, null);
                }
                else
                {
                    //_controller.ShootEmpty();
                    typeof(ShipController).GetMethod("ShootEmpty", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(_controller, null);
                }

                _alreadyShot = true;

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void ReceiveUpdates(ShipPacket packet, PacketType id)
    {
        _latestSPacket = packet;
        _latestID = id;
    }

    public void ReceiveUpdates(SpawnProjectilePacket packet, PacketType id)
    {
        _alreadyShot = false;
        _latestPSPacket = packet;
        _latestID = id;
    }
}