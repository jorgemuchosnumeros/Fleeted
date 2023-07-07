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
        if (_latestSPacket == null) return;

        var posDiscrepancy = (new Vector2(transform.position.x, transform.position.y) - _latestSPacket.Position)
            .sqrMagnitude;


        _rb.velocity = _latestSPacket.Velocity;

        if (posDiscrepancy > 2f)
        {
            Plugin.Logger.LogInfo($"Correcting position discrepancy of bullet {GetInstanceID()} {posDiscrepancy}");
            transform.position = Vector2.Lerp(transform.position, _latestSPacket.Position, 6f * Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        if (_rb.velocity.sqrMagnitude <= 0.2f)
        {
            Plugin.Logger.LogInfo("Exploding Bullet that is not moving");
            Explode();
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