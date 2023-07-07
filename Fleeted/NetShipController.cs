using System;
using System.Reflection;
using Fleeted.packets;
using Fleeted.patches;
using Fleeted.utils;
using UnityEngine;

namespace Fleeted;

public class NetShipController : MonoBehaviour
{
    public readonly TimedAction CollisionDisable = new(1f / 2);

    private bool _alreadyDead;
    private bool _alreadyShot;
    private ShipColliderController _ccollider;
    private ShipController _controller;
    private DeathPacket _latestDPacket;
    private PacketType _latestID;
    private SpawnProjectilePacket _latestPSPacket;

    private ShipPacket _latestSPacket;

    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _controller = GetComponent<ShipController>();
        _ccollider = GetComponent<ShipColliderController>();
    }

    private void Update()
    {
        if (_latestSPacket == null) return;

        switch (GlobalController.globalController.screen)
        {
            case GlobalController.screens.gamecountdown:
            case GlobalController.screens.gameloading:
                return;
        }

        if (!InGameNetManager.Instance.GracePeriod.TrueDone()) return;

        switch (_latestID)
        {
            case PacketType.ShipUpdate:
            {
                if (CollisionDisable.IsRunning()) break;

                _rb.velocity = _latestSPacket.Velocity;

                var posDiscrepancy = (new Vector2(transform.position.x, transform.position.y) - _latestSPacket.Position)
                    .sqrMagnitude;
                if (posDiscrepancy > 4f)
                {
                    transform.position = _latestSPacket.Position;
                }
                else if (posDiscrepancy > 0.4f)
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
                if (_alreadyDead) break;

                _alreadyDead = true;

                Plugin.Logger.LogInfo(
                    $"Received death from {_latestDPacket.TargetShip} by {_latestDPacket.SourceShip}");

                ExplodeNetShip(_latestDPacket.IsExplosionBig, _controller, _ccollider);

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

    public static void ExplodeNetShip(bool big, ShipController controller, ShipColliderController ccontroller)
    {
        if (big)
        {
            if (controller.alive)
            {
                SendExplode.PermissionToDie = true;
                typeof(ShipColliderController)
                    .GetMethod("ExplodeBig", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(ccontroller, null);
            }
        }
        else
        {
            var exploded = (bool) typeof(ShipColliderController)
                .GetField("exploded", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ccontroller);

            if (!exploded)
            {
                SendExplode.PermissionToDie = true;
                ccontroller.Explode();
            }
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

    public void ReceiveUpdates(DeathPacket packet, PacketType id)
    {
        _alreadyDead = false;
        _latestDPacket = packet;
        _latestID = id;
    }
}