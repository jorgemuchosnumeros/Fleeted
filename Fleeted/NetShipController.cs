using System.Collections.Generic;
using System.Reflection;
using Fleeted.packets;
using Fleeted.patches;
using Fleeted.utils;
using UnityEngine;

namespace Fleeted;

public class NetShipController : MonoBehaviour
{
    const int DebugSteps = 20;
    public readonly TimedAction CollisionDisable = new(0.3f);

    private ShipColliderController _ccollider;
    private ShipController _controller;
    private GameObject[] _debugTrajectoryPoints = new GameObject[DebugSteps];

    private readonly List<ShipStatePacket> _stateBuffer = new();
    private ShipStatePacket _latestState;
    private Rigidbody2D _rb;
    private bool _isOwned;
    private const int InterpolationDelayTicks = 6;
    private const float HardSnapDistanceThreshold = 6f;
    private const float CorrectionDurationSeconds = 0.12f;
    private const float OwnedReconcileDistanceThreshold = 0.35f;
    private const float OwnedDriftDistanceThreshold = 0.08f;
    private const float OwnedCorrectionDurationSeconds = 0.18f;
    private const float OwnedDriftCorrectionDurationSeconds = 0.4f;
    private const float OwnedMaxPositionCorrectionPerSecond = 5f;
    private const float OwnedMaxVelocityCorrectionPerSecond = 8f;
    private Vector2 _positionCorrection;
    private Vector2 _velocityCorrection;
    private float _rotationCorrection;
    private float _correctionTimeRemaining;
    private bool _applyVelocityCorrection;


    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _controller = GetComponent<ShipController>();
        _ccollider = GetComponent<ShipColliderController>();
    }

    private void Update()
    {
        if (_latestState == null) return;

        var slot = _controller.playerN - 1;
        var isOwned = _isOwned || InGameNetManager.Instance.ownedSlots.Contains(slot);
        if (isOwned)
        {
            ApplyOwnedCorrections();
            return;
        }

        switch (GlobalController.globalController.screen)
        {
            case GlobalController.screens.gamecountdown:
            case GlobalController.screens.gameloading:
                return;
        }

        if (CollisionDisable.IsRunning()) return;

        var renderTick = InGameNetManager.Instance.GetEstimatedServerTick() - InterpolationDelayTicks;
        var state = GetInterpolatedState(renderTick);

        _rb.velocity = state.Velocity;
        _rb.angularVelocity = state.AngularVelocity;
        transform.position = state.Position;

        var stick = transform.GetChild(2).GetChild(2);
        stick.localRotation = Quaternion.Euler(new Vector3(0, 0, state.StickRotation));
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, state.Rotation));
    }

    private void DebugPredict(long ping)
    {
        for (var i = 0; i < DebugSteps; i++)
        {
            if (_debugTrajectoryPoints[i] == null)
            {
                _debugTrajectoryPoints[i] = new GameObject($"Debug Point{i}");
                var spriteRenderer = _debugTrajectoryPoints[i].AddComponent<SpriteRenderer>();

                spriteRenderer.sprite = CustomLobbyMenu.Instance.pointSprite;
                spriteRenderer.color = Color.green;
                spriteRenderer.sortingOrder = 9999;
            }

            if (_latestState == null)
                continue;

            _debugTrajectoryPoints[i].transform.localScale = Vector2.one * 0.5f;
            _debugTrajectoryPoints[i].transform.position = PredictObjectPosition(_latestState.Position,
                                                               _latestState.Velocity, _latestState.StickRotation,
                                                               i * (ping / 100f) / DebugSteps) +
                                                           new Vector2(-1.3f, -1.15f);
        }
    }

    public static Vector2 PredictObjectPosition(Vector2 receivedPos, Vector2 receivedVel, float rttSecs,
        float stickDeg = 0)
    {
        if (rttSecs > 0.35f) // Don't Bother on predicting if latency is this big, it's probably going to go way too wrong
            return receivedPos;

        var perceivedLatencySecs = rttSecs / 1.6f;

        return receivedPos + receivedVel * perceivedLatencySecs;

        // TODO: Predict Circular Movement (Pain)
        /*
        if (Mathf.Abs(stickDeg) < 5) // Assume its trajectory is straight
            return receivedPos + receivedVel * perceivedLatencySecs;
        
        var latencyFactor = perceivedLatencySecs * 4f;
        var rotationStickFactor = (stickDeg > 180 ? stickDeg - 360 : stickDeg) / 750f;
        
        var rotationReciprocal = 1 / rotationStickFactor;

        var rRev = rotationReciprocal < 0 ? -1 : 1;

        var invertedRotationReciprocalAbsSqrt = rRev * Mathf.Sqrt(Mathf.Abs(rotationReciprocal));
        var velPosAtan = (Mathf.Atan2(receivedVel.y - receivedPos.y, receivedVel.x - receivedPos.x) * Mathf.Rad2Deg + 90);

        var rotFx = receivedPos.x + invertedRotationReciprocalAbsSqrt * Mathf.Cos(velPosAtan);
        var rotFy = receivedPos.y + invertedRotationReciprocalAbsSqrt * Mathf.Sin(velPosAtan);

        var predX = rotFx + invertedRotationReciprocalAbsSqrt * Mathf.Cos(rRev * latencyFactor + velPosAtan + 180);
        var predY = rotFy + invertedRotationReciprocalAbsSqrt * Mathf.Sin(rRev * latencyFactor + velPosAtan + 180);

        return new Vector2(predX, predY);
        */
    }

    public static void ExplodeNetShip(bool big, ShipController controller, ShipColliderController colliderController)
    {
        if (big)
        {
            if (controller.alive)
            {
                SendExplode.PermissionToDie = true;
                typeof(ShipColliderController)
                    .GetMethod("ExplodeBig", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(colliderController, null);
            }
        }
        else
        {
            var exploded = (bool) typeof(ShipColliderController)
                .GetField("exploded", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(colliderController);

            if (!exploded)
            {
                SendExplode.PermissionToDie = true;
                colliderController.Explode();
            }
        }
    }

    public void SetOwnership(bool isOwned)
    {
        _isOwned = isOwned;
    }

    public void ReceiveState(ShipStatePacket packet)
    {
        _latestState = packet;
        _stateBuffer.Add(packet);
        TrimBuffer(packet.ServerTick);

        if (_controller.bullets != packet.Ammo)
        {
            _controller.bullets = packet.Ammo;
        }

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

    private ShipStatePacket GetInterpolatedState(int renderTick)
    {
        if (_stateBuffer.Count == 0) return _latestState;
        if (_stateBuffer.Count == 1) return _stateBuffer[0];

        ShipStatePacket from = _stateBuffer[0];
        ShipStatePacket to = _stateBuffer[_stateBuffer.Count - 1];

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

        return new ShipStatePacket
        {
            Slot = to.Slot,
            ServerTick = renderTick,
            Position = Vector2.Lerp(from.Position, to.Position, t),
            Velocity = Vector2.Lerp(from.Velocity, to.Velocity, t),
            Rotation = Mathf.LerpAngle(from.Rotation, to.Rotation, t),
            StickRotation = Mathf.LerpAngle(from.StickRotation, to.StickRotation, t),
            AngularVelocity = Mathf.Lerp(from.AngularVelocity, to.AngularVelocity, t),
            LastProcessedInput = to.LastProcessedInput,
        };
    }

    private void Reconcile(ShipStatePacket packet)
    {
        var position = transform.position;
        var discrepancy = Vector2.Distance(position, packet.Position);
        if (discrepancy > HardSnapDistanceThreshold)
        {
            transform.position = packet.Position;
            _rb.velocity = packet.Velocity;
            _rb.angularVelocity = packet.AngularVelocity;
            transform.rotation = Quaternion.Euler(new Vector3(0, 0, packet.Rotation));
            _positionCorrection = Vector2.zero;
            _velocityCorrection = Vector2.zero;
            _rotationCorrection = 0f;
            _correctionTimeRemaining = 0f;
            _applyVelocityCorrection = false;
        }
        else if (discrepancy > OwnedReconcileDistanceThreshold)
        {
            _positionCorrection = packet.Position - (Vector2) transform.position;
            _velocityCorrection = packet.Velocity - _rb.velocity;
            _rotationCorrection = 0f;
            _correctionTimeRemaining = OwnedCorrectionDurationSeconds;
            _applyVelocityCorrection = true;
        }
        else if (discrepancy > OwnedDriftDistanceThreshold)
        {
            _positionCorrection = packet.Position - (Vector2) transform.position;
            _velocityCorrection = Vector2.zero;
            _rotationCorrection = 0f;
            _correctionTimeRemaining = OwnedDriftCorrectionDurationSeconds;
            _applyVelocityCorrection = false;
        }

        var stick = transform.GetChild(2).GetChild(2);
        stick.localRotation = Quaternion.Euler(new Vector3(0, 0, packet.StickRotation));

        if (_controller.bullets != packet.Ammo)
        {
            _controller.bullets = packet.Ammo;
        }

        if (InGameNetManager.Instance.ownedSlots.Contains(packet.Slot))
        {
            InGameNetManager.Instance.AcknowledgeInput(packet.Slot, packet.LastProcessedInput);
        }
    }

    private void ApplyOwnedCorrections()
    {
        if (_correctionTimeRemaining <= 0f) return;

        var maxStep = OwnedMaxPositionCorrectionPerSecond * Time.deltaTime;
        var positionStep = Vector2.ClampMagnitude(_positionCorrection, maxStep);
        transform.position += (Vector3) positionStep;
        _positionCorrection -= positionStep;

        if (_applyVelocityCorrection)
        {
            var maxVelStep = OwnedMaxVelocityCorrectionPerSecond * Time.deltaTime;
            var velocityStep = Vector2.ClampMagnitude(_velocityCorrection, maxVelStep);
            _rb.velocity += velocityStep;
            _velocityCorrection -= velocityStep;
        }

        _correctionTimeRemaining -= Time.deltaTime;
        if (_correctionTimeRemaining <= 0f)
        {
            _positionCorrection = Vector2.zero;
            _velocityCorrection = Vector2.zero;
            _rotationCorrection = 0f;
            _applyVelocityCorrection = false;
        }
    }

    public void Explode(DeathPacket packet)
    {
        Plugin.Logger.LogInfo(
            $"Received death from {packet.TargetShip} by {packet.SourceShip}");

        ExplodeNetShip(packet.IsExplosionBig, _controller, _ccollider);
    }
}
