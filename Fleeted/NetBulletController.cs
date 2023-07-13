using System.Reflection;
using Fleeted.packets;
using UnityEngine;

namespace Fleeted;

public class NetBulletController : MonoBehaviour
{
    private BulletController _bcontroller;
    private UpdateProjectilePacket _latestSPacket;
    private Rigidbody2D _rb;


    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _bcontroller = GetComponent<BulletController>();
    }

    private void Update()
    {
        if (GlobalController.globalController.screen != GlobalController.screens.game)
        {
            Plugin.Logger.LogInfo("Exploding Bullet that is not moving");
            Explode();
        }

        if (_latestSPacket == null) return;

        var posDiscrepancy = (new Vector2(transform.position.x, transform.position.y) - _latestSPacket.Position)
            .sqrMagnitude;

        var predictedPosition = _latestSPacket.Position;
        if (InGameNetManager.Instance.pingMap.TryGetValue(
                LobbyManager.Instance.Players[_bcontroller.player - 1].OwnerOfCharaId, out var ping))
        {
            predictedPosition =
                NetShipController.PredictObjectPosition(_latestSPacket.Position, _latestSPacket.Velocity, ping / 1000f);
        }

        _rb.velocity = _latestSPacket.Velocity;

        if (posDiscrepancy > 2f)
        {
            transform.position = Vector2.Lerp(transform.position, predictedPosition, 6f * Time.deltaTime);
        }
    }

    private void OnDisable()
    {
        _latestSPacket = null;
    }

    public void ReceiveUpdates(UpdateProjectilePacket packet)
    {
        _latestSPacket = packet;

        Plugin.Logger.LogInfo(
            $"Received a bullet from {packet.SourceShip}\nvelocity: {packet.Velocity}, position: {packet.Position}, id: {packet.Id}");
    }

    public void Explode()
    {
        _bcontroller.exploded = true;
        var obj = typeof(BulletController).GetField("obj", BindingFlags.Instance | BindingFlags.NonPublic);
        var tr = typeof(BulletController).GetField("tr", BindingFlags.Instance | BindingFlags.NonPublic);
        obj.SetValue(_bcontroller, ExplosionsController.explosionsController.GetExplosion());
        if (obj.GetValue(_bcontroller) != null)
        {
            ((GameObject) obj.GetValue(_bcontroller)).transform.localScale = new Vector2(0.5f, 0.5f);
            ((GameObject) obj.GetValue(_bcontroller)).transform.position =
                ((Transform) tr.GetValue(_bcontroller)).position;
        }

        ExplosionsController.explosionsController.PlayBulletExplosion();

        _bcontroller.DeactivateBullet();
    }
}