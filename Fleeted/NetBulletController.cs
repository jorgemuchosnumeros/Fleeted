using System.Reflection;
using Fleeted.packets;
using Fleeted.utils;
using UnityEngine;

namespace Fleeted;

public class NetBulletController : MonoBehaviour
{
    private readonly TimedAction _stillBulletLifeTime = new(0.5f);
    private BulletController _bcontroller;
    private UpdateProjectilePacket _latestSPacket;
    private bool _letBulletLoose;
    private Rigidbody2D _rb;
    private Vector2 _savedBulletLifeTimePos = Vector2.positiveInfinity;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _bcontroller = GetComponent<BulletController>();
    }

    private void Start()
    {
        _stillBulletLifeTime.Start();
    }

    private void Update()
    {
        if (GlobalController.globalController.screen != GlobalController.screens.game)
            Explode();

        if (_latestSPacket == null) return;

        if (_stillBulletLifeTime.TrueDone())
        {
            var position = (Vector2) transform.position;
            var diff = (position - _savedBulletLifeTimePos).magnitude;

            if (diff > 0.2f)
                _letBulletLoose = true;

            _savedBulletLifeTimePos = position;
            _stillBulletLifeTime.Start();
        }

        var posDiscrepancy = (new Vector2(transform.position.x, transform.position.y) - _latestSPacket.Position)
            .sqrMagnitude;

        var predictedPosition = _latestSPacket.Position;

        if (LobbyManager.Instance.Players.TryGetValue(_bcontroller.player - 1, out var player))
        {
            if (InGameNetManager.Instance.pingMap.TryGetValue(player.OwnerOfCharaId, out var ping))
            {
                predictedPosition =
                    NetShipController.PredictObjectPosition(_latestSPacket.Position, _latestSPacket.Velocity,
                        ping / 1000f);
            }
        }


        _rb.velocity = _latestSPacket.Velocity;

        if (posDiscrepancy > 2f && !_letBulletLoose)
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