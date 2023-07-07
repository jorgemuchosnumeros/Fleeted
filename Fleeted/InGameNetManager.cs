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

namespace Fleeted;

public class InGameNetManager : MonoBehaviour
{
    public static InGameNetManager Instance;

    public bool isHost;
    public bool isClient;

    public List<GameObject> shipsGO;
    public List<int> ownedSlots = new();

    public int CameraMovementSeed;
    private readonly Stopwatch _pingStopwatch = new();

    private readonly Dictionary<int, NetBulletController> bulletControllers = new();
    private readonly Dictionary<int, Dictionary<int, BulletController>> bulletsSlots = new();
    public readonly Dictionary<int, NetShipController> controllersSlots = new();
    public readonly TimedAction GracePeriod = new(3.5f);
    private readonly Dictionary<ulong, HashSet<int>> killVoters = new();
    private readonly Dictionary<int, int> killVotes = new();

    public readonly TimedAction MainSendTick = new(1.0f / 10);

    private readonly Dictionary<SteamId, long> pingMap = new();
    public readonly TimedAction PingSendTick = new(1.0f);

    private readonly Dictionary<int, Rigidbody2D> rbBullets = new();
    public readonly Dictionary<int, Rigidbody2D> rbSlots = new();

    private bool _isShipReferenceUpdatePending;

    private Dictionary<PacketType, int> _specificBytesOut = new();

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

        if (GlobalController.globalController.screen != GlobalController.screens.game) return;

        if (MainSendTick.TrueDone())
        {
            SendShipUpdates();

            SendProjectileUpdates();

            MainSendTick.Start();
        }

        if (PingSendTick.TrueDone())
        {
            SendPingPacket(false);

            PingSendTick.Start();
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
        MainSendTick.Start();
        PingSendTick.Start();
        _pingStopwatch.Start();

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

        SendPacket2All(SteamClient.SteamId, data, PacketType.ShipUpdate, P2PSend.Unreliable);
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
            if (!rbBullets.ContainsKey(bc.GetInstanceID())) continue;

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

        SendPacket2All(SteamClient.SteamId, data, PacketType.ProjectileUpdate, P2PSend.Unreliable);
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
                    var deathPacket = dataStream.ReadDeathPacket();
                    var target = deathPacket.TargetShip;

                    if (ownedSlots.Contains(target)) break;

                    controllersSlots[target].ReceiveUpdates(deathPacket, PacketType.Death);
                    break;
                case PacketType.Kill:
                    var killPacket = dataStream.ReadKillPacket();
                    var defendant = killPacket.TargetShip;
                    var prosecutor = packet.SteamId;

                    if (!ownedSlots.Contains(defendant)) break;

                    var memberCount = LobbyManager.Instance.CurrentLobby.Members.Count();
                    var voteLimit = memberCount / 2 + 1;

                    var repeatedVote = false;
                    if (killVoters.TryGetValue(prosecutor, out _))
                    {
                        if (!killVoters[prosecutor].Add(defendant))
                        {
                            repeatedVote = true;
                        }
                    }
                    else
                    {
                        killVoters.Add(prosecutor, new HashSet<int> {defendant});
                    }

                    if (!repeatedVote)
                    {
                        if (killVotes.TryGetValue(defendant, out _))
                            killVotes[defendant]++;
                        else
                            killVotes.Add(defendant, 1);
                    }


                    Plugin.Logger.LogInfo(
                        $"{defendant} is accused of unreliably dying, votes: {killVotes[defendant]}/{memberCount / 2 + 1}");

                    if (killVotes[defendant] >= voteLimit)
                    {
                        var player = LobbyManager.Instance.Players[defendant].InGameShip;
                        var controller = player.GetComponent<ShipController>();
                        var ccontroller = player.GetComponent<ShipColliderController>();

                        Plugin.Logger.LogInfo($"Killed {killPacket.TargetShip} by vote");

                        NetShipController.ExplodeNetShip(killPacket.IsExplosionBig, controller, ccontroller);
                    }

                    break;
                case PacketType.SpawnProjectile:
                    var spawnProjectilePacket = dataStream.ReadSpawnProjectilePacket();
                    var slot = spawnProjectilePacket.SourceShip;

                    if (ownedSlots.Contains(slot)) break;

                    if (controllersSlots.TryGetValue(slot, out var sppc))
                    {
                        sppc.ReceiveUpdates(spawnProjectilePacket, PacketType.SpawnProjectile);
                    }

                    break;
                case PacketType.ProjectileUpdate:
                    var bulkProjectileUpdate = dataStream.ReadBulkProjectileUpdate();

                    foreach (var projectilePacket in bulkProjectileUpdate.Updates)
                    {
                        if (ownedSlots.Contains(projectilePacket.SourceShip)) continue;

                        Plugin.Logger.LogWarning(projectilePacket == null);

                        if (bulletControllers.TryGetValue(projectilePacket.Id, out var bcontroller))
                        {
                            bcontroller.ReceiveUpdates(projectilePacket);
                        }
                    }

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

        SteamNetworking.SendP2PPacket(to, packetData, packetData.Length, 0, sendFlags);
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
        killVotes.Clear();
        killVoters.Clear();

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

                var bullets = new Dictionary<int, BulletController>();
                foreach (var bullet in ships[i].transform.GetChild(8))
                {
                    if (((Transform) bullet).TryGetComponent<BulletController>(out var bulletController))
                    {
                        bullets.Add(bulletController.GetInstanceID(), bulletController);
                    }
                }

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

        foreach (var id in LobbyManager.Instance.CurrentLobby.Members.Select(member => member.Id)
                     .Where(id => id != SteamClient.SteamId))
        {
            pingMap.Add(id, 0);
        }
    }

    public static bool IsSlotOwnedByThisClient(int slot)
    {
        return LobbyManager.Instance.Players[slot].OwnerOfCharaId == SteamClient.SteamId;
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

        StartClient();

        for (var i = 0; i < 10; i++) // Warm up the connection ???
            SendPingPacket(false, flag: P2PSend.Unreliable);

        Plugin.Logger.LogInfo($"Current Time: {DateTime.UtcNow.Ticks + serverClientTimeDiff}");
        var startTime = lobby.GetData("StartTime");
        Plugin.Logger.LogInfo($"Everyone is ready, starting at {startTime}");

        ShowConnectingMessage(true);

        yield return new WaitUntil(() => DateTime.UtcNow.Ticks + serverClientTimeDiff >= long.Parse(startTime));

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