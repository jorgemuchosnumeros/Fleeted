using Fleeted.packets;
using UnityEngine;

namespace Fleeted;

public class NetBulletController : MonoBehaviour
{
    private UpdateProjectilePacket _latestSPacket;
    private Rigidbody2D _rb;


    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
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
}