using System;
using System.Collections;
using System.Collections.Generic;
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

namespace Fleeted;

public class InGameNetManager : MonoBehaviour
{
    public static InGameNetManager Instance;

    public bool isHost;
    public bool isClient;

    public List<GameObject> shipsGO;
    public List<int> ownedSlots = new();

    private readonly Dictionary<int, NetBulletController> bulletControllers = new();
    private readonly Dictionary<int, Dictionary<int, BulletController>> bulletsSlots = new();
    private readonly Dictionary<int, NetShipController> controllersSlots = new();

    public readonly TimedAction MainSendTick = new(1.0f / 10);
    private readonly Dictionary<int, Rigidbody2D> rbBullets = new();
    private readonly Dictionary<int, Rigidbody2D> rbSlots = new();

    private int _bytesOut;

    private bool _isShipReferenceUpdatePending;
    private int _pps;
    private int _ppsOut;

    private Dictionary<PacketType, int> _specificBytesOut = new Dictionary<PacketType, int>();

    public HashSet<BulletController> ownedLiveBullets = new();

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

        if (MainSendTick.TrueDone() && GlobalController.globalController.screen == GlobalController.screens.game)
        {
            SendShipUpdates();

            SendProjectileUpdates();

            MainSendTick.Start();
        }
    }

    private void OnGUI()
    {
        if (!isClient) return;

        GUI.Label(new Rect(10, 30, 200, 40), $"Inbound: {_pps} PPS");
        GUI.Label(new Rect(10, 50, 200, 40), $"Outbound: {_ppsOut} PPS -- {_bytesOut} Bytes");
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

    public void ResetState()
    {
        _pps = 0;
        _ppsOut = 0;
        _bytesOut = 0;

        MainSendTick.Start();

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

    private void SendShipUpdates()
    {
        var bulkShipUpdate = new BulkShipUpdate
        {
            Updates = new List<ShipPacket>(),
        };

        foreach (var slot in ownedSlots)
        {
            var ship = LobbyManager.Instance.Players[slot].InGameShip;
            var position = ship.transform.position;
            var stickT = ship.transform.GetChild(2).GetChild(2);

            var shipPacket = new ShipPacket
            {
                Position = new Vector2(position.x, position.y),
                Rotation = ship.transform.rotation.eulerAngles.z,
                Slot = slot,
                StickRotation = stickT.localRotation.eulerAngles.z,
                Velocity = new Vector2(rbSlots[slot].velocity.x, rbSlots[slot].velocity.y),
            };

            bulkShipUpdate.Updates.Add(shipPacket);
        }

        using MemoryStream memoryStream = new MemoryStream();
        using (var writer = new ProtocolWriter(memoryStream))
        {
            writer.Write(bulkShipUpdate);
        }

        var data = memoryStream.ToArray();

        SendPacket(SteamClient.SteamId, data, PacketType.ShipUpdate, P2PSend.Unreliable);
    }

    private void SendProjectileUpdates()
    {
        if (!ownedLiveBullets.Any()) return;

        var bulkProjectileUpdate = new BulkProjectileUpdate
        {
            Updates = new List<UpdateProjectilePacket>(),
        };

        foreach (var bc in ownedLiveBullets)
        {
            var updateProjectilePacket = new UpdateProjectilePacket()
            {
                Id = bc.GetInstanceID(),
                Position = bc.transform.position,
                SourceShip = bc.player - 1,
                Velocity = rbBullets[bc.GetInstanceID()].velocity,
            };

            bulkProjectileUpdate.Updates.Add(updateProjectilePacket);
        }

        using MemoryStream memoryStream = new MemoryStream();
        using (var writer = new ProtocolWriter(memoryStream))
        {
            writer.Write(bulkProjectileUpdate);
        }

        var data = memoryStream.ToArray();

        SendPacket(SteamClient.SteamId, data, PacketType.ProjectileUpdate, P2PSend.Unreliable);
    }

    private void ReadPackets()
    {
        while (SteamNetworking.IsP2PPacketAvailable())
        {
            var rawPacket = SteamNetworking.ReadP2PPacket();

            if (rawPacket == null) return;

            var value = rawPacket.Value;
            var data = value.Data;

            using var memStream = new MemoryStream(data);
            using var packetReader = new ProtocolReader(memStream);

            var packet = packetReader.ReadPacket();

            if (packet.SteamId == SteamClient.SteamId) return;

            using var compressedStream = new MemoryStream(packet.Data);
            using var decompressStream = new DeflateStream(compressedStream, CompressionMode.Decompress);
            using var dataStream = new ProtocolReader(decompressStream);

            switch (packet.Id)
            {
                case PacketType.ShipUpdate:
                    var bulkShipPacket = dataStream.ReadBulkShipUpdate();

                    foreach (var shipPacket in bulkShipPacket.Updates)
                    {
                        if (ownedSlots.Contains(shipPacket.Slot)) break;

                        controllersSlots[shipPacket.Slot].ReceiveUpdates(shipPacket, PacketType.ShipUpdate);
                    }

                    break;
                case PacketType.Death:
                    break;
                case PacketType.SpawnProjectile:
                    var spawnProjectilePacket = dataStream.ReadSpawnProjectilePacket();
                    var slot = spawnProjectilePacket.SourceShip;

                    if (ownedSlots.Contains(slot)) break;

                    if (controllersSlots.TryGetValue(slot, out var c))
                    {
                        c.ReceiveUpdates(spawnProjectilePacket, PacketType.SpawnProjectile);
                    }

                    break;
                case PacketType.ProjectileUpdate:
                    var bulkProjectileUpdate = dataStream.ReadBulkProjectileUpdate();

                    foreach (var projectilePacket in bulkProjectileUpdate.Updates)
                    {
                        if (ownedSlots.Contains(projectilePacket.SourceShip)) continue;

                        Plugin.Logger.LogWarning(projectilePacket == null);

                        bulletControllers[projectilePacket.Id].ReceiveUpdates(projectilePacket);
                    }

                    break;
            }
        }
    }

    public void SendPacket(ulong from, byte[] data, PacketType type, P2PSend sendFlags)
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

        if (_specificBytesOut.ContainsKey(type))
            _specificBytesOut[type] += packetData.Length;
        else
            _specificBytesOut[type] = packetData.Length;

        var lobby = LobbyManager.Instance.CurrentLobby;

        foreach (var member in lobby.Members)
        {
            SteamNetworking.SendP2PPacket(member.Id, packetData, packetData.Length, 0, sendFlags);
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

        var ships = GameState.gameState.playerShips;
        for (var i = 0; i < 8; i++)
        {
            if (LobbyManager.Instance.Players.TryGetValue(i, out var value))
            {
                LobbyManager.Instance.Players[i] = value with {InGameShip = ships[i]};

                rbSlots.Add(i, ships[i].GetComponent<Rigidbody2D>());

                if (IsSlotOwnedByThisClient(i))
                    ownedSlots.Add(i);
                else
                {
                    controllersSlots.Add(i,
                        ships[i].TryGetComponent<NetShipController>(out var controller)
                            ? controller
                            : ships[i].AddComponent<NetShipController>());
                }

                var bullets =
                    (from object bullet in ships[i].transform.GetChild(8)
                        select ((Transform) bullet).GetComponent<BulletController>())
                    .ToDictionary(bulletController => bulletController.GetInstanceID());

                foreach (var b in bullets)
                {
                    rbBullets.Add(b.Key, b.Value.GetComponent<Rigidbody2D>());

                    bulletControllers.Add(b.Key,
                        b.Value.gameObject.TryGetComponent<NetBulletController>(out var controller)
                            ? controller
                            : b.Value.gameObject.AddComponent<NetBulletController>());
                }

                bulletsSlots.Add(i, bullets);
            }
        }
    }

    public static bool IsSlotOwnedByThisClient(int slot)
    {
        return LobbyManager.Instance.Players[slot].OwnerOfCharaId == SteamClient.SteamId;
    }

    public void StartGame(Lobby lobby)
    {
        if (lobby.GetData("GameStarted") != "yes") return;

        Plugin.Logger.LogInfo("Host let us Start");

        switch (GlobalController.globalController.screen)
        {
            case GlobalController.screens.playmenu:
            {
                var smcInstance = FindObjectOfType<StageMenuController>();

                StartClient();

                //smcInstance.StartGame();
                try
                {
                    typeof(StageMenuController).GetMethod("StartGame", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(smcInstance, null);
                }
                catch (Exception ex)
                {
                    Plugin.Logger.LogError(ex);
                    Plugin.Logger.LogError("PLEASE REPORT THIS ERROR!!!!!!");
                }

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

    public IEnumerator StartNewNetRound(GameState instance)
    {
        // Wait for everyone to be ready and host decides a date to start the Game,
        // ensuring everyone starts at the same time
        StartNewRoundWhenEveryoneReadyPatch.StartingInProgress = true;

        var lobby = LobbyManager.Instance.CurrentLobby;
        lobby.SetMemberData("Ready", "yes");
        while (LobbyManager.Instance.isHost)
        {
            Thread.Sleep(500);

            if (!IsEveryoneReady(lobby)) continue;

            lobby.SetData("StartTime",
                $"{DateTimeOffset.UtcNow.UtcTicks + 30000000L}"); // Give 3 seconds of headstart for the rest to be ready
            break;
        }

        yield return new WaitUntil(() => lobby.GetData("StartTime") != string.Empty);

        Plugin.Logger.LogInfo($"Current Time: {DateTimeOffset.UtcNow.UtcTicks}");
        var startTime = lobby.GetData("StartTime");
        Plugin.Logger.LogInfo($"Everyone is ready, starting at {startTime}");

        ShowConnectingMessage(true);

        yield return new WaitUntil(() => DateTimeOffset.UtcNow.UtcTicks >= long.Parse(startTime));

        ShowConnectingMessage(false);

        Plugin.Logger.LogInfo($"Current Time: {DateTimeOffset.UtcNow.UtcTicks}");
        Plugin.Logger.LogInfo("Starting Round...");

        lobby.SetMemberData("Ready", string.Empty);
        if (LobbyManager.Instance.isHost)
        {
            lobby.SetData("StartTime", string.Empty);
            lobby.SetData("GameStarted", string.Empty);
        }


        StartNewRoundWhenEveryoneReadyPatch.StartNewRound(instance);

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