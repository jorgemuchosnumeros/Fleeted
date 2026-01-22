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
    private const float HardSnapDistanceThreshold = 5f;
    private const float CorrectionDurationSeconds = 0.12f;
    private const float OwnedReconcileDistanceThreshold = 0.35f;
    private const float OwnedDriftDistanceThreshold = 0.08f;
    private const float OwnedCorrectionDurationSeconds = 0.09f;
    private const float OwnedDriftCorrectionDurationSeconds = 0.4f;
    private const float OwnedMaxPositionCorrectionPerSecond = 5f;
    private const float OwnedMaxVelocityCorrectionPerSecond = 8f;
    private const float OwnedMaxRotationCorrectionPerSecond = 180f;
    private const float OwnedReconcileRotationThreshold = 8f;
    private const float OwnedDriftRotationThreshold = 2f;
    private const int MaxPredictionSteps = 32;
    private const float RemotePositionLerpSpeed = 14f;
    private const float RemoteRotationLerpSpeed = 16f;
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

        var posT = 1f - Mathf.Exp(-RemotePositionLerpSpeed * Time.deltaTime);
        var rotT = 1f - Mathf.Exp(-RemoteRotationLerpSpeed * Time.deltaTime);

        _rb.velocity = Vector2.Lerp(_rb.velocity, state.Velocity, posT);
        _rb.angularVelocity = Mathf.Lerp(_rb.angularVelocity, state.AngularVelocity, rotT);
        transform.position = Vector2.Lerp(transform.position, state.Position, posT);

        var stick = transform.GetChild(2).GetChild(2);
        var stickZ = stick.localRotation.eulerAngles.z;
        var stickLerp = Mathf.LerpAngle(stickZ, state.StickRotation, rotT);
        stick.localRotation = Quaternion.Euler(new Vector3(0, 0, stickLerp));

        var shipZ = transform.rotation.eulerAngles.z;
        var shipLerp = Mathf.LerpAngle(shipZ, state.Rotation, rotT);
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, shipLerp));
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
        var targetPosition = packet.Position;
        var targetVelocity = packet.Velocity;
        var targetRotation = packet.Rotation;
        var targetAngularVelocity = packet.AngularVelocity;
        if (InGameNetManager.Instance.ownedSlots.Contains(packet.Slot))
        {
            InGameNetManager.Instance.AcknowledgeInput(packet.Slot, packet.LastProcessedInput);
            if (TryPredictOwnedState(packet, out var predicted))
            {
                targetPosition = predicted.Position;
                targetVelocity = predicted.Velocity;
                targetRotation = predicted.Rotation;
                targetAngularVelocity = predicted.AngularVelocity;
            }
        }

        var position = transform.position;
        var discrepancy = Vector2.Distance(position, targetPosition);
        var rotationDiscrepancy = Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.z, targetRotation));
        if (discrepancy > HardSnapDistanceThreshold)
        {
            transform.position = targetPosition;
            _rb.velocity = targetVelocity;
            _rb.angularVelocity = targetAngularVelocity;
            transform.rotation = Quaternion.Euler(new Vector3(0, 0, targetRotation));
            _positionCorrection = Vector2.zero;
            _velocityCorrection = Vector2.zero;
            _rotationCorrection = 0f;
            _correctionTimeRemaining = 0f;
            _applyVelocityCorrection = false;
        }
        else if (discrepancy > OwnedReconcileDistanceThreshold)
        {
            _positionCorrection = targetPosition - (Vector2) transform.position;
            _velocityCorrection = targetVelocity - _rb.velocity;
            _rotationCorrection = Mathf.DeltaAngle(transform.eulerAngles.z, targetRotation);
            _correctionTimeRemaining = OwnedCorrectionDurationSeconds;
            _applyVelocityCorrection = true;
        }
        else if (discrepancy > OwnedDriftDistanceThreshold)
        {
            _positionCorrection = targetPosition - (Vector2) transform.position;
            _velocityCorrection = Vector2.zero;
            _rotationCorrection = Mathf.DeltaAngle(transform.eulerAngles.z, targetRotation);
            _correctionTimeRemaining = OwnedDriftCorrectionDurationSeconds;
            _applyVelocityCorrection = false;
        }
        else if (rotationDiscrepancy > OwnedReconcileRotationThreshold)
        {
            _positionCorrection = Vector2.zero;
            _velocityCorrection = Vector2.zero;
            _rotationCorrection = Mathf.DeltaAngle(transform.eulerAngles.z, targetRotation);
            _correctionTimeRemaining = OwnedCorrectionDurationSeconds;
            _applyVelocityCorrection = false;
        }
        else if (rotationDiscrepancy > OwnedDriftRotationThreshold)
        {
            _positionCorrection = Vector2.zero;
            _velocityCorrection = Vector2.zero;
            _rotationCorrection = Mathf.DeltaAngle(transform.eulerAngles.z, targetRotation);
            _correctionTimeRemaining = OwnedDriftCorrectionDurationSeconds;
            _applyVelocityCorrection = false;
        }

        var stick = transform.GetChild(2).GetChild(2);
        stick.localRotation = Quaternion.Euler(new Vector3(0, 0, packet.StickRotation));

        if (_controller.bullets != packet.Ammo)
        {
            _controller.bullets = packet.Ammo;
        }
    }

    private bool TryPredictOwnedState(ShipStatePacket baseState, out ShipStatePacket predicted)
    {
        predicted = baseState;
        if (!InGameNetManager.Instance.TryGetPendingInputsSnapshot(baseState.Slot, out var pending))
            return false;

        if (pending.Count == 0)
            return false;

        pending.Sort((a, b) => a.Sequence.CompareTo(b.Sequence));

        var startIndex = Mathf.Max(0, pending.Count - MaxPredictionSteps);
        var dt = InGameNetManager.NetTickDeltaSeconds;

        var position = baseState.Position;
        var velocity = baseState.Velocity;
        var rotation = baseState.Rotation;
        var angularVelocity = baseState.AngularVelocity;
        var stickRotation = baseState.StickRotation;
        var continuousRotationTimer = Mathf.Abs(NormalizeStickRotation(stickRotation)) >= 5f ? 0.8f : 0f;
        var thrustSpeed = GetThrustSpeed();
        var maxMagnitude = GetMaxMagnitude();
        const float turnSpeed = 60f;
        var mass = _rb != null ? _rb.mass : 1f;
        var fixedDt = Time.fixedDeltaTime > 0f ? Time.fixedDeltaTime : InGameNetManager.NetTickDeltaSeconds;

        for (var i = startIndex; i < pending.Count; i++)
        {
            ApplyPredictedInput(pending[i], fixedDt, thrustSpeed, maxMagnitude, turnSpeed, mass, ref position,
                ref velocity, ref rotation, ref angularVelocity, ref stickRotation, ref continuousRotationTimer);
        }

        predicted.Position = position;
        predicted.Velocity = velocity;
        predicted.Rotation = rotation;
        predicted.AngularVelocity = angularVelocity;
        return true;
    }

    private static void ApplyPredictedInput(InputPacket input, float dt, float thrustSpeed, float maxMagnitude,
        float turnSpeed, float mass, ref Vector2 position, ref Vector2 velocity, ref float rotation,
        ref float angularVelocity, ref float stickRotation, ref float continuousRotationTimer)
    {
        var rotationRad = rotation * Mathf.Deg2Rad;
        if (input.Thrust)
        {
            var forward = new Vector2(Mathf.Sin(rotationRad), Mathf.Cos(rotationRad));
            var forceToAdd = forward * (thrustSpeed * mass * dt);
            velocity += forceToAdd * (dt / mass);
        }

        UpdateStickRotation(input, dt, ref stickRotation, ref continuousRotationTimer, out var boostL, out var boostR);
        angularVelocity += ComputeAngularVelocityDelta(stickRotation, boostL, boostR, angularVelocity, turnSpeed);
        rotation = Mathf.Repeat(rotation + angularVelocity * dt, 360f);

        var speed = velocity.magnitude;
        if (speed > maxMagnitude)
            velocity = velocity * (maxMagnitude / speed);

        position += velocity * dt;
    }

    private static float NormalizeStickRotation(float stickRotation)
    {
        var rotation = stickRotation;
        if (rotation > 300f)
            rotation -= 360f;
        return rotation;
    }

    private static void UpdateStickRotation(InputPacket input, float dt, ref float stickRotation,
        ref float continuousRotationTimer, out bool boostL, out bool boostR)
    {
        boostL = false;
        boostR = false;

        var normalized = NormalizeStickRotation(stickRotation);
        var hasInput = input.TurnLeft || input.TurnRight;
        if (hasInput)
        {
            continuousRotationTimer = Mathf.Min(0.8f, continuousRotationTimer + dt);
            var speedFactor = Mathf.Clamp(0.6f + continuousRotationTimer, 0.7f, 1.4f);
            var stickSpeed = 60f * speedFactor;

            if (input.TurnLeft)
            {
                normalized += stickSpeed * dt;
                if (normalized > 20f && normalized < 300f)
                    normalized = 20f;
                boostL = true;
            }

            if (input.TurnRight)
            {
                normalized -= stickSpeed * dt;
                if (normalized < 340f && normalized > 300f)
                    normalized = 340f;
                boostR = true;
            }
        }
        else
        {
            continuousRotationTimer = Mathf.Max(0f, continuousRotationTimer - dt * 2f);
            if (normalized < 3f && normalized > -3f)
            {
                normalized = 0f;
            }
            else if (normalized > 0f && normalized < 5f)
            {
                normalized -= 10f * dt;
                if (normalized > 300f)
                    normalized = 0f;
            }
            else if (normalized < 0f && normalized > -5f)
            {
                normalized += 10f * dt;
                if (normalized < 10f)
                    normalized = 0f;
            }
        }

        stickRotation = Mathf.Repeat(normalized, 360f);
    }

    private static float ComputeAngularVelocityDelta(float stickRotation, bool boostL, bool boostR,
        float angularVelocity, float turnSpeed)
    {
        var turnQuantity = NormalizeStickRotation(stickRotation);
        if (Mathf.Abs(turnQuantity) < 0.035f)
            turnQuantity = 0f;

        turnQuantity *= 6f;
        if (turnQuantity >= 120f && boostL)
            turnQuantity += 50f;
        else if (turnQuantity <= -120f && boostR)
            turnQuantity -= 50f;

        if (angularVelocity >= 0f && turnQuantity > 0f)
        {
            if (turnQuantity > angularVelocity)
                return Mathf.Clamp(turnSpeed / 6f, 0f, 160f - angularVelocity);
            if (turnQuantity < angularVelocity)
                return -Mathf.Clamp(turnSpeed / 6f, 0f, angularVelocity - turnQuantity);
            return 0f;
        }

        if (angularVelocity <= 0f && turnQuantity < 0f)
        {
            if (turnQuantity < angularVelocity)
                return -Mathf.Clamp(turnSpeed / 6f, 0f, 160f + angularVelocity);
            if (turnQuantity > angularVelocity)
                return Mathf.Clamp(turnSpeed / 6f, 0f, turnQuantity - angularVelocity);
            return 0f;
        }

        if (angularVelocity > 0f && turnQuantity < 0f)
            return -Mathf.Clamp(turnSpeed / 6f, 0f, 160f + angularVelocity);

        if (angularVelocity < 0f && turnQuantity > 0f)
            return Mathf.Clamp(turnSpeed / 6f, 0f, 160f - angularVelocity);

        return 0f;
    }

    private float GetThrustSpeed()
    {
        if (GlobalController.globalController.mode == GlobalController.modes.leader
            && GameState.gameState != null
            && GameState.gameState.leaderPlayer == _controller.playerN)
            return 900f;

        return 1000f;
    }

    private float GetMaxMagnitude()
    {
        if (GlobalController.globalController.mode == GlobalController.modes.leader
            && GameState.gameState != null
            && GameState.gameState.leaderPlayer == _controller.playerN)
            return 9f;

        return 11f;
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

        if (Mathf.Abs(_rotationCorrection) > 0.001f)
        {
            var maxRotStep = OwnedMaxRotationCorrectionPerSecond * Time.deltaTime;
            var rotStep = Mathf.Clamp(_rotationCorrection, -maxRotStep, maxRotStep);
            transform.rotation = Quaternion.Euler(new Vector3(0, 0, transform.eulerAngles.z + rotStep));
            _rotationCorrection -= rotStep;
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

    public void ResetNetState()
    {
        _stateBuffer.Clear();
        _latestState = null;
        _positionCorrection = Vector2.zero;
        _velocityCorrection = Vector2.zero;
        _rotationCorrection = 0f;
        _correctionTimeRemaining = 0f;
        _applyVelocityCorrection = false;
    }

    public void Explode(DeathPacket packet)
    {
        Plugin.Logger.LogInfo(
            $"Received death from {packet.TargetShip} by {packet.SourceShip}");

        ExplodeNetShip(packet.IsExplosionBig, _controller, _ccollider);
    }
}
