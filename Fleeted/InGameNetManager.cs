using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading;
using Fleeted.packets;
using Fleeted.patches;
using Fleeted.utils;
using Steamworks;
using Steamworks.Data;
using TMPro;
using UnityEngine;

using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace Fleeted;

public class InGameNetManager : MonoBehaviour
{
    public static float PacketDelaySeconds = 0.0725f;
    public static float PacketDelayJitterSeconds = 0.015f;
    private const float NetTickRate = 50f;
    private const float MaxStateBufferSeconds = 0.5f;
    private const float CameraSyncRate = 5f;
    public static InGameNetManager Instance;

    public bool isHost;
    public bool isClient;

    public List<int> ownedSlots = new();

    public int CameraMovementSeed;
    private readonly Stopwatch _pingStopwatch = new();

    private readonly Dictionary<int, NetBulletController> bulletControllers = new();
    private readonly Dictionary<int, Dictionary<int, BulletController>> bulletsSlots = new();
    public readonly Dictionary<int, NetShipController> controllersSlots = new();
    public readonly TimedAction GracePeriod = new(3.5f);

    public readonly TimedAction NetSendTick = new(1.0f / NetTickRate);
    private readonly TimedAction _cameraSyncTick = new(1.0f / CameraSyncRate);
    private readonly TimedAction _netDebugLogTick = new(1.0f);

    public readonly Dictionary<SteamId, long> pingMap = new();
    public readonly TimedAction PingSendTick = new(1.0f);

    private readonly Dictionary<int, Rigidbody2D> rbBullets = new();
    public readonly Dictionary<int, Rigidbody2D> rbSlots = new();

    private bool _isShipReferenceUpdatePending;

    // Debug
    private bool _logClientDiff;
    private long _serverClientTimeDiff;
    private long _startTime;

    private int _serverTick;
    private int _latestServerTick;
    private float _latestServerTickTime;

    private readonly Dictionary<int, int> _localInputSequences = new();
    private readonly Dictionary<int, List<InputPacket>> _pendingLocalInputs = new();
    private readonly Dictionary<int, NetInputState> _netInputsBySlot = new();
    private readonly Dictionary<int, int> _lastProcessedInput = new();
    private readonly Dictionary<int, int> _bulletNetIds = new();
    private readonly Dictionary<int, BulletController> _bulletByNetId = new();
    private readonly Dictionary<Vector2, TileLandController> _tileByPosition = new();
    private int _inputsSent;
    private int _inputsReceived;
    private int _worldStatesSent;
    private int _worldStatesReceived;
    private int _packetsReceived;
    private int _packetErrors;
    private int _lastWorldStateBytes;
    private int _lastWorldStateShips;
    private int _lastWorldStateBullets;
    private readonly Dictionary<int, bool> _lastShootHeld = new();
    private const float CameraCorrectionDurationSeconds = 0.2f;
    private const float CameraSnapDistanceThreshold = 6f;
    private Vector3 _cameraPositionCorrection;
    private float _cameraSizeCorrection;
    private float _cameraCorrectionTimeRemaining;

    private sealed class NetInputState
    {
        public bool Thrust;
        public bool TurnLeft;
        public bool TurnRight;
        public bool ShootDown;
    }

    public bool inGame => GlobalController.globalController.screen switch
    {
        GlobalController.screens.gameresults => true,
        GlobalController.screens.gameloading => true,
        GlobalController.screens.gamecountdown => true,
        GlobalController.screens.gamepause => true,
        GlobalController.screens.game => true,
        _ => false,
    };

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SteamNetworkingUtils.InitRelayNetworkAccess();
        SteamNetworking.AllowP2PPacketRelay(true);

        SteamNetworking.OnP2PSessionRequest += OnP2PSessionRequest;

        ResetState();
    }

    private void Update()
    {
        if (_logClientDiff)
        {
            Thread.Sleep(20);
            Plugin.Logger.LogInfo($"{DateTime.UtcNow.Ticks + _serverClientTimeDiff} < {_startTime}");
        }

        if (!isClient) return;

        var inCountdown = GlobalController.globalController.screen == GlobalController.screens.gamecountdown;

        if (inCountdown && _isShipReferenceUpdatePending)
        {
            UpdateShipReferences();
            _isShipReferenceUpdatePending = false;
        }
        else if (!inCountdown)
        {
            _isShipReferenceUpdatePending = true;
        }

        ReadPackets();
        if (GlobalController.globalController.screen != GlobalController.screens.game) return;

        if (NetSendTick.TrueDone())
        {
            SendInputUpdates();
            if (isHost)
            {
                SendWorldState();
            }

            NetSendTick.Start();
        }

        if (PingSendTick.TrueDone())
        {
            SendPingPacket(false);

            PingSendTick.Start();
        }

        if (!isHost)
        {
            ApplyCameraCorrections();
        }

        if (isHost && _cameraSyncTick.TrueDone())
        {
            SendCameraState();
            _cameraSyncTick.Start();
        }

        if (_netDebugLogTick.TrueDone())
        {
            Plugin.Logger.LogInfo(
                $"NetDebug: inputs sent={_inputsSent}, recv={_inputsReceived}, world sent={_worldStatesSent}, recv={_worldStatesReceived}, packets={_packetsReceived}, errors={_packetErrors}, world bytes={_lastWorldStateBytes}, ships={_lastWorldStateShips}, bullets={_lastWorldStateBullets}");
            _inputsSent = 0;
            _inputsReceived = 0;
            _worldStatesSent = 0;
            _worldStatesReceived = 0;
            _packetsReceived = 0;
            _packetErrors = 0;
            _lastWorldStateBytes = 0;
            _lastWorldStateShips = 0;
            _lastWorldStateBullets = 0;
            _netDebugLogTick.Start();
        }
    }

    private void OnGUI()
    {
        if (!isClient) return;

        var members = LobbyManager.Instance.CurrentLobby.Members.Where(member => member.Id != SteamClient.SteamId)
            .ToList();

        for (var i = 0; i < members.Count; i++)
        {
            var ping = 0L;
            if (pingMap.TryGetValue(members[i].Id, out var gotPing))
            {
                ping = gotPing;
            }

            GUI.Label(new Rect(10, 30 + i * 20, 200, 40), $"{members[i].Name} Ping: {ping} ms");
        }
    }

    private void OnP2PSessionRequest(SteamId steamId)
    {
        if (LobbyManager.Instance.CurrentLobby.Members.Select(member => member.Id).ToList()
            .Contains(steamId)) // Is player in the lobby
        {
            Plugin.Logger.LogInfo($"Accepting Connection Request from: {steamId}");
            SteamNetworking.AcceptP2PSessionWithUser(steamId);
        }
    }

    public void AbandonConnection()
    {
        foreach (var id in LobbyManager.Instance.CurrentLobby.Members.Select(member => member.Id).ToList())
        {
            Plugin.Logger.LogInfo($"Disconnection from: {id}");
            SteamNetworking.CloseP2PSessionWithUser(id);
        }
    }

    public void ResetState()
    {
        NetSendTick.Start();
        _cameraSyncTick.Start();
        _netDebugLogTick.Start();
        PingSendTick.Start();
        _pingStopwatch.Start();
        _serverTick = 0;
        _latestServerTick = 0;
        _latestServerTickTime = 0f;
        _inputsSent = 0;
        _inputsReceived = 0;
        _worldStatesSent = 0;
        _worldStatesReceived = 0;
        _packetsReceived = 0;
        _packetErrors = 0;
        _lastWorldStateBytes = 0;
        _lastWorldStateShips = 0;
        _lastWorldStateBullets = 0;
        _lastShootHeld.Clear();

        LobbyManager.Instance.Players.Clear();

        isHost = false;
        isClient = false;
    }

    public void StartClient(bool asHost = false)
    {
        Plugin.Logger.LogInfo($"Starting client {(asHost ? "and server" : string.Empty)}");

        isHost = asHost;
        isClient = true;
    }

    public void StopClient(bool asHost = false)
    {
        Plugin.Logger.LogInfo($"Stopping client {(asHost ? "and server" : string.Empty)}");

        isHost = asHost;
        isClient = true;
    }

    private void SendInputUpdates()
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return;
        if (InputController.inputController == null) return;

        foreach (var slot in ownedSlots)
        {
            var inputIndex = slot + 1;
            var thrust = InputController.inputController.inputs[InputController.inputController.sA[inputIndex]];
            var turnLeft = InputController.inputController.inputs[InputController.inputController.sL[inputIndex]];
            var turnRight = InputController.inputController.inputs[InputController.inputController.sR[inputIndex]];
            var shootHeld = InputController.inputController.inputs[InputController.inputController.sB[inputIndex]] ||
                            InputController.inputController.inputs[InputController.inputController.sX[inputIndex]];
            var shootDown = InputController.inputController.inputs[InputController.inputController.sBDown[inputIndex]] ||
                            InputController.inputController.inputs[InputController.inputController.sXDown[inputIndex]];
            if (!_lastShootHeld.TryGetValue(slot, out var wasHeld))
                wasHeld = false;

            var inputPacket = new InputPacket
            {
                Slot = slot,
                Sequence = ++_localInputSequences[slot],
                Thrust = thrust,
                TurnLeft = turnLeft,
                TurnRight = turnRight,
                Shoot = shootDown || (shootHeld && !wasHeld),
            };

            _lastShootHeld[slot] = shootHeld;

            _pendingLocalInputs[slot].Add(inputPacket);
            _inputsSent++;

            if (isHost)
            {
                _lastProcessedInput[slot] = inputPacket.Sequence;
                continue;
            }

            using MemoryStream memoryStream = new MemoryStream();
            using (var writer = new ProtocolWriter(memoryStream))
            {
                writer.Write(inputPacket);
            }

            var data = memoryStream.ToArray();
            var hostId = LobbyManager.Instance.CurrentLobby.Owner.Id;

            SendPacketToSomeone(SteamClient.SteamId, hostId, data, PacketType.InputUpdate, P2PSend.Unreliable);
        }
    }

    private void SendWorldState()
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return;

        var worldState = new WorldStatePacket
        {
            ServerTick = _serverTick,
            Ships = new List<ShipStatePacket>(),
            Bullets = new List<BulletStatePacket>(),
        };

        foreach (var slot in LobbyManager.Instance.Players.Keys)
        {
            var ship = LobbyManager.Instance.Players[slot].InGameShip;
            if (ship == null || !rbSlots.ContainsKey(slot)) continue;

            var stickT = ship.transform.GetChild(2).GetChild(2);
            var shipController = ship.GetComponent<ShipController>();
            worldState.Ships.Add(new ShipStatePacket
            {
                Slot = slot,
                ServerTick = _serverTick,
                Position = ship.transform.position,
                Velocity = rbSlots[slot].velocity,
                Rotation = ship.transform.rotation.eulerAngles.z,
                StickRotation = stickT.localRotation.eulerAngles.z,
                AngularVelocity = rbSlots[slot].angularVelocity,
                LastProcessedInput = _lastProcessedInput.TryGetValue(slot, out var lastInput) ? lastInput : 0,
                Ammo = shipController != null ? shipController.bullets : 0,
            });
        }

        foreach (var slotBullets in bulletsSlots.Values)
        {
            foreach (var bulletEntry in slotBullets)
            {
                var netId = bulletEntry.Key;
                var bullet = bulletEntry.Value;
                if (bullet == null) continue;

                if (!rbBullets.TryGetValue(netId, out var rb)) continue;

                if (!bullet.gameObject.activeSelf) continue;

                worldState.Bullets.Add(new BulletStatePacket
                {
                    NetId = netId,
                    ServerTick = _serverTick,
                    Position = bullet.transform.position,
                    Velocity = rb.velocity,
                    Active = true,
                });
            }
        }

        using MemoryStream memoryStream = new MemoryStream();
        using (var writer = new ProtocolWriter(memoryStream))
        {
            writer.Write(worldState);
        }

        var data = memoryStream.ToArray();
        SendPacket2All(SteamClient.SteamId, data, PacketType.WorldState, P2PSend.Unreliable);
        _worldStatesSent++;
        _lastWorldStateBytes = data.Length;
        _lastWorldStateShips = worldState.Ships.Count;
        _lastWorldStateBullets = worldState.Bullets.Count;

        _serverTick++;
    }

    private void SendCameraState()
    {
        if (!ApplyPlayOnlinePatch.IsOnlineOptionSelected) return;

        var cameraController = FindObjectOfType<CameraController>();
        if (cameraController == null || cameraController.cam == null) return;

        var cameraState = new CameraStatePacket
        {
            ServerTick = _serverTick,
            Position = cameraController.camT.position,
            OrthographicSize = cameraController.cam.orthographicSize,
        };

        using MemoryStream memoryStream = new MemoryStream();
        using (var writer = new ProtocolWriter(memoryStream))
        {
            writer.Write(cameraState);
        }

        var data = memoryStream.ToArray();
        SendPacket2All(SteamClient.SteamId, data, PacketType.CameraState, P2PSend.Unreliable);
    }

    private void SendPingPacket(bool response, ulong sender = 0, P2PSend flag = P2PSend.Reliable)
    {
        var pingPacket = new PingPacket()
        {
            Response = response,
        };

        using MemoryStream memoryStream = new MemoryStream();
        using (var writer = new ProtocolWriter(memoryStream))
        {
            writer.Write(pingPacket);
        }

        var data = memoryStream.ToArray();

        if (response)
        {
            SendPacketToSomeone(SteamClient.SteamId, sender, data, PacketType.Ping, flag);
        }
        else
        {
            SendPacket2All(SteamClient.SteamId, data, PacketType.Ping, flag);
            _pingStopwatch.Restart();
        }
    }

    private void ReadPackets()
    {
        while (SteamNetworking.IsP2PPacketAvailable())
        {
            try
            {
                var rawPacket = SteamNetworking.ReadP2PPacket();

                if (rawPacket == null) return;

                var value = rawPacket.Value;
                var data = value.Data;

                using var memStream = new MemoryStream(data);
                using var packetReader = new ProtocolReader(memStream);

                var packet = packetReader.ReadPacket();

                if (packet.SteamId == SteamClient.SteamId) continue;

                _packetsReceived++;

                using var compressedStream = new MemoryStream(packet.Data);
                using var decompressStream = new DeflateStream(compressedStream, CompressionMode.Decompress);
                using var dataStream = new ProtocolReader(decompressStream);

                if (packet.Id == PacketType.WorldState)
                {
                    _lastWorldStateBytes = packet.Data.Length;
                }

                switch (packet.Id)
                {
                case PacketType.Death:
                    var deathPacket = dataStream.ReadDeathPacket();
                    var target = deathPacket.TargetShip;

                    controllersSlots[target].Explode(deathPacket);
                    break;
                case PacketType.Ping:
                    var pingPacket = dataStream.ReadPingPacket();

                    if (!pingPacket.Response)
                    {
                        SendPingPacket(true, packet.SteamId);
                    }
                    else
                    {
                        if (pingMap.TryGetValue(packet.SteamId, out _))
                        {
                            pingMap[packet.SteamId] = _pingStopwatch.ElapsedMilliseconds;
                        }
                        else
                        {
                            pingMap.Add(packet.SteamId, _pingStopwatch.ElapsedMilliseconds);
                        }
                    }

                    break;
                case PacketType.InputUpdate:
                    if (!isHost) break;
                    var inputPacket = dataStream.ReadInputPacket();
                    _inputsReceived++;
                    if (!_netInputsBySlot.TryGetValue(inputPacket.Slot, out var state))
                    {
                        state = new NetInputState();
                        _netInputsBySlot[inputPacket.Slot] = state;
                    }

                    state.Thrust = inputPacket.Thrust;
                    state.TurnLeft = inputPacket.TurnLeft;
                    state.TurnRight = inputPacket.TurnRight;
                    if (inputPacket.Shoot)
                        state.ShootDown = true;

                    _lastProcessedInput[inputPacket.Slot] = inputPacket.Sequence;
                    break;
                case PacketType.WorldState:
                    var worldState = dataStream.ReadWorldStatePacket();
                    _worldStatesReceived++;
                    RecordServerTick(worldState.ServerTick);
                    _lastWorldStateShips = worldState.Ships.Count;
                    _lastWorldStateBullets = worldState.Bullets.Count;

                    foreach (var shipState in worldState.Ships)
                    {
                        if (controllersSlots.TryGetValue(shipState.Slot, out var netController))
                        {
                            netController.ReceiveState(shipState);
                        }
                    }

                    foreach (var bulletState in worldState.Bullets)
                    {
                        if (bulletControllers.TryGetValue(bulletState.NetId, out var bulletController))
                        {
                            bulletController.ReceiveState(bulletState);
                        }
                    }

                    break;
                case PacketType.BulletDestroy:
                    var bulletDestroyPacket = dataStream.ReadBulletDestroyPacket();
                    if (bulletControllers.TryGetValue(bulletDestroyPacket.NetId, out var netBulletController))
                    {
                        netBulletController.Explode();
                    }
                    break;
                case PacketType.TileDestroy:
                    var tileDestroyPacket = dataStream.ReadTileDestroyPacket();
                    DisableTileAt(tileDestroyPacket.Position);
                    break;
                case PacketType.CameraState:
                    var cameraStatePacket = dataStream.ReadCameraStatePacket();
                    RecordServerTick(cameraStatePacket.ServerTick);
                    ApplyCameraState(cameraStatePacket);
                    break;
                }
            }
            catch (Exception ex)
            {
                _packetErrors++;
                Plugin.Logger.LogWarning($"Packet read error: {ex}");
            }
        }
    }

    public void SendPacket2All(ulong from, byte[] data, PacketType type, P2PSend sendFlags)
    {
        using var compressOut = new MemoryStream();
        using (var deflateStream = new DeflateStream(compressOut, CompressionLevel.Optimal))
        {
            deflateStream.Write(data, 0, data.Length);
        }

        var compressed = compressOut.ToArray();
        using MemoryStream packetStream = new MemoryStream();
        Packet packet = new Packet
        {
            Id = type,
            SteamId = from,
            Data = compressed
        };

        using (var writer = new ProtocolWriter(packetStream))
        {
            writer.Write(packet);
        }

        var packetData = packetStream.ToArray();

        foreach (var member in LobbyManager.Instance.CurrentLobby.Members)
        {
            StartCoroutine(SendP2PPacket(member.Id, packetData, sendFlags));
        }
    }

    public void SendPacketToSomeone(ulong from, ulong to, byte[] data, PacketType type, P2PSend sendFlags)
    {
        using var compressOut = new MemoryStream();
        using (var deflateStream = new DeflateStream(compressOut, CompressionLevel.Optimal))
        {
            deflateStream.Write(data, 0, data.Length);
        }

        var compressed = compressOut.ToArray();

        using MemoryStream packetStream = new MemoryStream();
        Packet packet = new Packet
        {
            Id = type,
            SteamId = from,
            Data = compressed
        };

        using (var writer = new ProtocolWriter(packetStream))
        {
            writer.Write(packet);
        }

        var packetData = packetStream.ToArray();

        StartCoroutine(SendP2PPacket(to, packetData, sendFlags));
    }

    private static IEnumerator SendP2PPacket(SteamId id, byte[] packetData, P2PSend sendFlags)
    {
        var delay = PacketDelaySeconds;
        if (PacketDelayJitterSeconds > 0f)
        {
            delay += UnityEngine.Random.Range(0f, PacketDelayJitterSeconds);
        }

        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        SteamNetworking.SendP2PPacket(id, packetData, packetData.Length, 0, sendFlags);
    }

    public static void ConfigurePacketDelay(float baseSeconds, float jitterSeconds = 0f)
    {
        PacketDelaySeconds = Mathf.Max(0f, baseSeconds);
        PacketDelayJitterSeconds = Mathf.Max(0f, jitterSeconds);
        Plugin.Logger.LogInfo(
            $"Packet delay configured: base={PacketDelaySeconds:0.000}s jitter={PacketDelayJitterSeconds:0.000}s");
    }

    public int MaxBufferTicks => Mathf.CeilToInt(MaxStateBufferSeconds * NetTickRate);

    public int GetEstimatedServerTick()
    {
        if (_latestServerTickTime <= 0f) return _latestServerTick;
        var delta = Time.time - _latestServerTickTime;
        return _latestServerTick + Mathf.FloorToInt(delta * NetTickRate);
    }

    public void RecordServerTick(int serverTick)
    {
        _latestServerTick = serverTick;
        _latestServerTickTime = Time.time;
    }

    public void ApplyNetInputs(InputController controller)
    {
        if (!isHost || !isClient) return;
        if (GlobalController.globalController.screen != GlobalController.screens.game) return;

        foreach (var kvp in _netInputsBySlot)
        {
            var slot = kvp.Key;
            var state = kvp.Value;
            var inputIndex = slot + 1;

            controller.inputs[controller.sA[inputIndex]] = state.Thrust;
            controller.inputs[controller.sL[inputIndex]] = state.TurnLeft;
            controller.inputs[controller.sR[inputIndex]] = state.TurnRight;
            controller.inputs[controller.sBDown[inputIndex]] = state.ShootDown;
            controller.inputs[controller.sB[inputIndex]] = state.ShootDown;

            state.ShootDown = false;
        }
    }

    public void AcknowledgeInput(int slot, int lastProcessedInput)
    {
        if (!_pendingLocalInputs.TryGetValue(slot, out var pending)) return;
        pending.RemoveAll(input => input.Sequence <= lastProcessedInput);
    }

    public bool TryGetNetInputState(int slot, out bool thrust, out bool turnLeft, out bool turnRight, out bool shootDown)
    {
        if (_netInputsBySlot.TryGetValue(slot, out var state))
        {
            thrust = state.Thrust;
            turnLeft = state.TurnLeft;
            turnRight = state.TurnRight;
            shootDown = state.ShootDown;
            return true;
        }

        thrust = false;
        turnLeft = false;
        turnRight = false;
        shootDown = false;
        return false;
    }

    public void ConsumeShootDown(int slot)
    {
        if (_netInputsBySlot.TryGetValue(slot, out var state))
        {
            state.ShootDown = false;
        }
    }

    public int? GetBulletNetId(BulletController controller)
    {
        var instanceId = controller.GetInstanceID();
        return _bulletNetIds.TryGetValue(instanceId, out var netId) ? netId : null;
    }

    private void DisableTileAt(Vector2 position)
    {
        var key = QuantizeTilePosition(position);

        if (_tileByPosition.Count == 0)
        {
            BuildTileMap();
        }

        if (_tileByPosition.TryGetValue(key, out var tile) && tile != null)
        {
            Fleeted.patches.SyncTileDestroyPatch.SuppressNetSend = true;
            tile.DisableTile();
            Fleeted.patches.SyncTileDestroyPatch.SuppressNetSend = false;
            return;
        }

        var tiles = FindObjectsOfType<TileLandController>();
        foreach (var tileController in tiles)
        {
            if (QuantizeTilePosition(tileController.transform.position) == key)
            {
                _tileByPosition[key] = tileController;
                Fleeted.patches.SyncTileDestroyPatch.SuppressNetSend = true;
                tileController.DisableTile();
                Fleeted.patches.SyncTileDestroyPatch.SuppressNetSend = false;
                break;
            }
        }
    }

    private void BuildTileMap()
    {
        _tileByPosition.Clear();
        var tiles = FindObjectsOfType<TileLandController>();
        foreach (var tile in tiles)
        {
            _tileByPosition[QuantizeTilePosition(tile.transform.position)] = tile;
        }
    }

    private static Vector2 QuantizeTilePosition(Vector2 position)
    {
        var x = Mathf.Round(position.x * 100f) / 100f;
        var y = Mathf.Round(position.y * 100f) / 100f;
        return new Vector2(x, y);
    }

    private void ApplyCameraState(CameraStatePacket cameraState)
    {
        if (isHost) return;

        var cameraController = FindObjectOfType<CameraController>();
        if (cameraController == null || cameraController.cam == null) return;

        var currentPos = cameraController.camT.position;
        var positionDiff = Vector3.Distance(currentPos, cameraState.Position);
        var sizeDiff = Mathf.Abs(cameraController.cam.orthographicSize - cameraState.OrthographicSize);

        if (positionDiff > CameraSnapDistanceThreshold)
        {
            cameraController.camT.position = cameraState.Position;
            cameraController.cam.orthographicSize = cameraState.OrthographicSize;
            _cameraPositionCorrection = Vector3.zero;
            _cameraSizeCorrection = 0f;
            _cameraCorrectionTimeRemaining = 0f;
            return;
        }

        if (positionDiff > 0.25f || sizeDiff > 0.05f)
        {
            _cameraPositionCorrection = cameraState.Position - currentPos;
            _cameraSizeCorrection = cameraState.OrthographicSize - cameraController.cam.orthographicSize;
            _cameraCorrectionTimeRemaining = CameraCorrectionDurationSeconds;
        }
    }

    private void ApplyCameraCorrections()
    {
        if (_cameraCorrectionTimeRemaining <= 0f) return;

        var cameraController = FindObjectOfType<CameraController>();
        if (cameraController == null || cameraController.cam == null) return;

        var t = Mathf.Clamp01(Time.deltaTime / _cameraCorrectionTimeRemaining);

        cameraController.camT.position += _cameraPositionCorrection * t;
        _cameraPositionCorrection -= _cameraPositionCorrection * t;

        cameraController.cam.orthographicSize += _cameraSizeCorrection * t;
        _cameraSizeCorrection -= _cameraSizeCorrection * t;

        _cameraCorrectionTimeRemaining -= Time.deltaTime;
        if (_cameraCorrectionTimeRemaining <= 0f)
        {
            _cameraPositionCorrection = Vector3.zero;
            _cameraSizeCorrection = 0f;
        }
    }


    public void UpdateShipReferences()
    {
        ownedSlots.Clear();
        rbSlots.Clear();
        controllersSlots.Clear();
        bulletsSlots.Clear();
        rbBullets.Clear();
        bulletControllers.Clear();
        pingMap.Clear();
        _localInputSequences.Clear();
        _pendingLocalInputs.Clear();
        _netInputsBySlot.Clear();
        _lastProcessedInput.Clear();
        _bulletNetIds.Clear();
        _bulletByNetId.Clear();
        var ships = GameState.gameState.playerShips;
        for (var i = 0; i < 8; i++)
        {
            if (LobbyManager.Instance.Players.TryGetValue(i, out var value))
            {
                LobbyManager.Instance.Players[i] = value with {InGameShip = ships[i]};

                rbSlots.Add(i, ships[i].GetComponent<Rigidbody2D>());

                var isOwned = IsSlotOwnedByThisClient(i);
                if (isOwned)
                {
                    ownedSlots.Add(i);
                    _localInputSequences[i] = 0;
                    _pendingLocalInputs[i] = new List<InputPacket>(64);
                    _lastShootHeld[i] = false;
                }
                else
                {
                    _netInputsBySlot[i] = new NetInputState();
                }

                if (ships[i].TryGetComponent<NetShipController>(out var netController))
                {
                    netController.SetOwnership(isOwned);
                }
                else
                {
                    netController = ships[i].AddComponent<NetShipController>();
                    netController.SetOwnership(isOwned);
                }

                controllersSlots.Add(i, netController);

                if (!isHost && !isOwned)
                {
                    rbSlots[i].isKinematic = true;
                }

                var bullets = new Dictionary<int, BulletController>();
                var shipController = ships[i].GetComponent<ShipController>();
                if (shipController != null && shipController.bulletsGO != null)
                {
                    for (var b = 0; b < shipController.bulletsGO.Length; b++)
                    {
                        var bulletObj = shipController.bulletsGO[b];
                        if (bulletObj != null && bulletObj.TryGetComponent<BulletController>(out var bulletController))
                        {
                            bulletController.bulletsContainer = shipController.bulletsContainer;
                            var netId = (i << 8) | b;
                            bullets.Add(netId, bulletController);
                            _bulletNetIds[bulletController.GetInstanceID()] = netId;
                            _bulletByNetId[netId] = bulletController;

                            var rb = bulletController.GetComponent<Rigidbody2D>();
                            rbBullets[netId] = rb;

                            if (!isHost && !isOwned)
                            {
                                rb.isKinematic = true;
                            }

                            if (bulletController.gameObject.TryGetComponent<NetBulletController>(
                                    out var netBulletController))
                            {
                                netBulletController.SetOwnership(isOwned, netId);
                            }
                            else
                            {
                                netBulletController = bulletController.gameObject.AddComponent<NetBulletController>();
                                netBulletController.SetOwnership(isOwned, netId);
                            }

                            bulletControllers[netId] = netBulletController;
                        }
                    }
                }

                bulletsSlots.Add(i, bullets);
            }
        }

        foreach (var id in LobbyManager.Instance.CurrentLobby.Members.Select(member => member.Id)
                     .Where(id => id != SteamClient.SteamId))
        {
            pingMap.Add(id, 0);
        }
    }

    public static bool IsSlotOwnedByThisClient(int slot)
    {
        if (slot < 0) return false;
        return LobbyManager.Instance.Players.TryGetValue(slot, out var player) &&
               player.OwnerOfCharaId == SteamClient.SteamId;
    }


    public void OnStartGame(Lobby lobby)
    {
        Plugin.Logger.LogInfo("Host let us Start");

        SetPlayersPatch.SetPlayers(GameState.gameState);

        switch (GlobalController.globalController.screen)
        {
            case GlobalController.screens.playmenu:
            {
                var smcInstance = FindObjectOfType<StageMenuController>();

                //smcInstance.StartGame();
                typeof(StageMenuController).GetMethod("StartGame", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(smcInstance, null);
                break;
            }
            case GlobalController.screens.gameresults:
            {
                var rcInstance = ResultsController.resultsController;

                RemovePauseMenuDuringVictory2.HostContinued = true;
                //rcInstance.EndResults();
                typeof(ResultsController).GetMethod("EndResults", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(rcInstance, null);
                break;
            }
        }
    }

    public void OnStopGame(Lobby lobby)
    {
        Plugin.Logger.LogInfo("Host Stopping Game");

        switch (GlobalController.globalController.screen)
        {
            case GlobalController.screens.gameresults:
            case GlobalController.screens.gameloading:
            case GlobalController.screens.gamecountdown:
            case GlobalController.screens.gamepause:
            case GlobalController.screens.game:
                var rcInstance = ResultsController.resultsController;

                //rcInstance.exitConfirmations = true
                typeof(ResultsController)
                    .GetField("exitConfirmation", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(rcInstance, true);

                //rcInstance.Exit();
                typeof(ResultsController).GetMethod("Exit", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(rcInstance, null);

                break;
        }
    }

    public IEnumerator StartNewNetRound(GameState instance)
    {
        // Wait for everyone to be ready and host decides a date to start the Game,
        // ensuring everyone starts at the same time
        StartNewRoundWhenEveryoneReadyPatch.StartingInProgress = true;

        CameraMovementSeed = LobbyManager.Instance.seed;

        var lobby = LobbyManager.Instance.CurrentLobby;
        lobby.SetMemberData("Ready", "yes");
        lobby.SetData("StartTime", string.Empty);

        var serverClientTimeDiff = NTP.GetNetworkTime().Ticks - DateTime.UtcNow.Ticks;
        _serverClientTimeDiff = serverClientTimeDiff;

        while (LobbyManager.Instance.isHost)
        {
            Thread.Sleep(500);

            if (IsEveryoneReady(lobby))
            {
                lobby.SetData("StartTime",
                    $"{DateTime.UtcNow.Ticks + serverClientTimeDiff + 50000000L}"); // Give 5 seconds of headstart for the rest to be ready
                break;
            }
        }

        yield return new WaitUntil(() => lobby.GetData("StartTime") != string.Empty);

        StartClient(LobbyManager.Instance.isHost);

        for (var i = 0; i < 10; i++) // Warm up the connection ???
            SendPingPacket(false, flag: P2PSend.Unreliable);

        Plugin.Logger.LogInfo($"Current Time: {DateTime.UtcNow.Ticks + serverClientTimeDiff}");
        var startTime = long.Parse(lobby.GetData("StartTime"));
        _startTime = startTime;

        Plugin.Logger.LogInfo($"Everyone is ready, starting at {startTime}");

        ShowConnectingMessage(true);

        _logClientDiff = true;
        yield return new WaitUntil(() => DateTime.UtcNow.Ticks + serverClientTimeDiff >= startTime); // FIXME: Buggy AF
        _logClientDiff = false;

        ShowConnectingMessage(false);

        Plugin.Logger.LogInfo($"Current Time: {DateTime.UtcNow.Ticks + serverClientTimeDiff}");
        Plugin.Logger.LogInfo("Starting Round...");

        lobby.SetMemberData("Ready", string.Empty);
        if (LobbyManager.Instance.isHost)
        {
            lobby.SetData("StartTime", string.Empty);
            lobby.SetData("GameStarted", string.Empty);
        }

        StartNewRoundWhenEveryoneReadyPatch.StartNewRound(instance);

        GracePeriod.Start();

        StartNewRoundWhenEveryoneReadyPatch.StartingInProgress = false;
    }

    private bool IsEveryoneReady(Lobby lobby)
    {
        return lobby.Members.All(member => lobby.GetMemberData(member, "Ready") == "yes");
    }

    private void ShowConnectingMessage(bool show)
    {
        if (show)
        {
            var connecting = new GameObject("Connecting Info");
            connecting.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var message = new GameObject("Message");
            message.transform.SetParent(connecting.transform);
            message.AddComponent<TextMeshProUGUI>().text = "Waiting for other Players...";

            message.transform.position = new Vector3(Screen.width / 2, Screen.height / 2);
            message.transform.localScale = new Vector3(0.8f, 0.8f);
        }
        else
        {
            var connecting = GameObject.Find("Connecting Info");
            if (connecting != null)
            {
                Destroy(connecting);
            }
        }
    }
}
