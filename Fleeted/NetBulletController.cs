using System.Collections.Generic;
using System.Reflection;
using Fleeted.packets;
using Fleeted.utils;
using UnityEngine;

namespace Fleeted;

public class NetBulletController : MonoBehaviour
{
    private BulletController _bcontroller;
    private Rigidbody2D _rb;
    private readonly List<BulletStatePacket> _stateBuffer = new();
    private BulletStatePacket _latestState;
    private bool _isOwned;
    private int _netId;
    private const int InterpolationDelayTicks = 6;
    private const float ReconcileDistanceThreshold = 0.2f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _bcontroller = GetComponent<BulletController>();
    }

    private void Start()
    {
    }

    private void Update()
    {
        if (GlobalController.globalController.screen != GlobalController.screens.game)
            Explode();

        if (_latestState == null) return;

        if (_isOwned)
        {
            return;
        }

        var renderTick = InGameNetManager.Instance.GetEstimatedServerTick() - InterpolationDelayTicks;
        var state = GetInterpolatedState(renderTick);

        if (!gameObject.activeSelf && state.Active)
        {
            gameObject.SetActive(true);
        }

        _rb.velocity = state.Velocity;
        transform.position = state.Position;
    }

    private void OnDisable()
    {
        _latestState = null;
    }

    public void SetOwnership(bool isOwned, int netId)
    {
        _isOwned = isOwned;
        _netId = netId;
    }

    public void ReceiveState(BulletStatePacket packet)
    {
        if (_isOwned)
        {
            if (!packet.Active && gameObject.activeSelf)
            {
                ExplodeAt(packet.Position);
            }
            else if (!packet.Active && _bcontroller != null && !_bcontroller.exploded)
            {
                ExplodeAt(packet.Position);
            }
            return;
        }

        _latestState = packet;

        if (packet.Active && !gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        else if (!packet.Active && gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }

        _stateBuffer.Add(packet);
        TrimBuffer(packet.ServerTick);

        if (_isOwned)
        {
            Reconcile(packet);
        }
    }

    private void TrimBuffer(int latestTick)
    {
        var maxTicks = InGameNetManager.Instance.MaxBufferTicks;
        _stateBuffer.RemoveAll(state => latestTick - state.ServerTick > maxTicks);
    }

    private BulletStatePacket GetInterpolatedState(int renderTick)
    {
        if (_stateBuffer.Count == 0) return _latestState;
        if (_stateBuffer.Count == 1) return _stateBuffer[0];

        BulletStatePacket from = _stateBuffer[0];
        BulletStatePacket to = _stateBuffer[_stateBuffer.Count - 1];

        for (var i = 0; i < _stateBuffer.Count - 1; i++)
        {
            var a = _stateBuffer[i];
            var b = _stateBuffer[i + 1];
            if (a.ServerTick <= renderTick && renderTick <= b.ServerTick)
            {
                from = a;
                to = b;
                break;
            }
        }

        if (from.ServerTick == to.ServerTick) return to;

        var t = Mathf.Clamp01((renderTick - from.ServerTick) / (float) (to.ServerTick - from.ServerTick));

        return new BulletStatePacket
        {
            NetId = _netId,
            ServerTick = renderTick,
            Position = Vector2.Lerp(from.Position, to.Position, t),
            Velocity = Vector2.Lerp(from.Velocity, to.Velocity, t),
            Active = to.Active,
        };
    }

    private void Reconcile(BulletStatePacket packet)
    {
        if (Vector2.Distance(transform.position, packet.Position) > ReconcileDistanceThreshold)
        {
            transform.position = packet.Position;
            _rb.velocity = packet.Velocity;
        }
    }

    public void Explode()
    {
        var tr = typeof(BulletController).GetField("tr", BindingFlags.Instance | BindingFlags.NonPublic);
        var position = transform.position;
        if (tr != null && _bcontroller != null)
        {
            var trValue = tr.GetValue(_bcontroller) as Transform;
            if (trValue != null)
            {
                position = trValue.position;
            }
        }

        ExplodeAt(position);
    }

    private void ExplodeAt(Vector2 position)
    {
        if (_bcontroller == null) return;
        if (_bcontroller.bulletsContainer == null)
            return;
        if (_bcontroller.exploded)
            return;
        _bcontroller.exploded = true;

        var obj = typeof(BulletController).GetField("obj", BindingFlags.Instance | BindingFlags.NonPublic);
        if (obj == null) return;
        obj.SetValue(_bcontroller, ExplosionsController.explosionsController.GetExplosion());
        if (obj.GetValue(_bcontroller) != null)
        {
            ((GameObject) obj.GetValue(_bcontroller)).transform.localScale = new Vector2(0.5f, 0.5f);
            ((GameObject) obj.GetValue(_bcontroller)).transform.position = position;
        }

        ExplosionsController.explosionsController.PlayBulletExplosion();

        _bcontroller.DeactivateBullet();
    }
}
