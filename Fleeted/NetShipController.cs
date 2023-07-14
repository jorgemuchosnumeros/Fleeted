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

        if (CollisionDisable.IsRunning()) return;

        var predictedPosition = _latestSPacket.Position;

        if (InGameNetManager.Instance.pingMap.TryGetValue(
                LobbyManager.Instance.Players[_controller.playerN - 1].OwnerOfCharaId, out var ping))
        {
            //DebugPredict(ping);
            predictedPosition = PredictObjectPosition(_latestSPacket.Position, _latestSPacket.Velocity, ping / 1000f,
                _latestSPacket.StickRotation);
        }

        _rb.velocity = _latestSPacket.Velocity;
        var posDiscrepancy =
            (new Vector2(transform.position.x, transform.position.y) -
             predictedPosition) //_latestSPacket.Position change
            .sqrMagnitude;
        if (posDiscrepancy > 15f)
        {
            transform.position = predictedPosition;
        }
        else if (posDiscrepancy > 0.2f)
        {
            transform.position = Vector2.Lerp(transform.position, predictedPosition, 6f * Time.deltaTime);
        }

        var stick = transform.GetChild(2).GetChild(2);
        stick.localRotation = Quaternion.Slerp(stick.localRotation,
            Quaternion.Euler(new Vector3(0, 0, _latestSPacket.StickRotation)), 3f * Time.deltaTime);

        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.Euler(new Vector3(0, 0, _latestSPacket.Rotation)), 10f * Time.deltaTime);
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

            _debugTrajectoryPoints[i].transform.localScale = Vector2.one * 0.5f;
            _debugTrajectoryPoints[i].transform.position = PredictObjectPosition(_latestSPacket.Position,
                                                               _latestSPacket.Velocity, _latestSPacket.StickRotation,
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

    public void UpdatePosition(ShipPacket packet, PacketType id)
    {
        _latestSPacket = packet;
    }

    public void SpawnProjectile(SpawnProjectilePacket packet)
    {
        if (!packet.IsEmpty)
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
    }

    public void Explode(DeathPacket packet)
    {
        Plugin.Logger.LogInfo(
            $"Received death from {packet.TargetShip} by {packet.SourceShip}");

        ExplodeNetShip(packet.IsExplosionBig, _controller, _ccollider);
    }
}