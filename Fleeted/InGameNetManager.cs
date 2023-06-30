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
    public enum CTarget
    {
        Server,
        Clients,
    }

    public static InGameNetManager Instance;

    public List<GameObject> shipsGO;

    public bool isHost;
    public bool isClient;

    public List<int> OwnedSlots = new();
    private int _bytesOut = 0;

    private bool _isShipReferenceUpdatePending;
    private int _pps = 0;
    private int _ppsOut = 0;

    private Dictionary<PacketType, int> _specificBytesOut = new Dictionary<PacketType, int>();

    private float _ticker2 = 0f;
    private int _total = 0;
    private int _totalBytesOut = 0;
    private int _totalOut = 0;

    public TimedAction MainSendTick = new(1.0f / 10);
    private Dictionary<int, Rigidbody2D> RbSlots = new();

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

            MainSendTick.Start();
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

    public void ResetState()
    {
        _ticker2 = 0f;
        _pps = 0;
        _total = 0;
        _ppsOut = 0;
        _totalOut = 0;
        _bytesOut = 0;
        _totalBytesOut = 0;

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
        var bulkShipUpdate = new BulkShipUpdate()
        {
            Updates = new List<ShipPacket>(),
        };

        foreach (var slot in OwnedSlots)
        {
            var ship = LobbyManager.Instance.Players[slot].InGameShip;
            var position = ship.transform.position;
            var stickT = ship.transform.GetChild(2).GetChild(2);

            var shipPacket = new ShipPacket
            {
                Position = new Vector2(position.x, position.y),
                Rotation = ship.transform.rotation.eulerAngles,
                StickRotation = stickT.localRotation.eulerAngles,
                Slot = slot,
                Velocity = RbSlots[slot].velocity,
            };

            bulkShipUpdate.Updates.Add(shipPacket);
        }

        using MemoryStream memoryStream = new MemoryStream();

        using (var writer = new ProtocolWriter(memoryStream))
        {
            writer.Write(bulkShipUpdate);
        }

        byte[] data = memoryStream.ToArray();

        SendPacket(SteamClient.SteamId, data, PacketType.ShipUpdate, P2PSend.Unreliable);
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

            _total++;

            using var compressedStream = new MemoryStream(packet.Data);
            using var decompressStream = new DeflateStream(compressedStream, CompressionMode.Decompress);
            using var dataStream = new ProtocolReader(decompressStream);

            switch (packet.Id)
            {
                case PacketType.ShipUpdate:
                    BulkShipUpdate bulkShipPacket;
                    try
                    {
                        bulkShipPacket = dataStream.ReadBulkShipUpdate();
                    }
                    catch (EndOfStreamException)
                    {
                        // FIXME: Im not sure what exactly causes this error but it appears to leave the packet unaffected
                        break;
                    }

                    foreach (var shipPacket in bulkShipPacket.Updates)
                    {
                        if (OwnedSlots.Contains(shipPacket.Slot)) continue;
                        var ship = LobbyManager.Instance.Players[shipPacket.Slot].InGameShip;
                        var stick = ship.transform.GetChild(2).GetChild(2);


                        RbSlots[shipPacket.Slot].velocity = shipPacket.Velocity;
                        stick.localRotation = Quaternion.Euler(shipPacket.StickRotation);
                    }

                    break;
                case PacketType.Death:
                    break;
                case PacketType.SpawnProjectile:
                    break;
                case PacketType.ProjectileUpdate:
                    break;
            }

            if (!isHost) return;

            switch (packet.Id) // Echo the packets
            {
                case PacketType.ShipUpdate:
                case PacketType.ProjectileUpdate:
                    SendPacket(packet.SteamId, packet.Data, packet.Id, P2PSend.Unreliable);
                    break;
                case PacketType.Death:
                case PacketType.SpawnProjectile:
                    SendPacket(packet.SteamId, packet.Data, packet.Id, P2PSend.Reliable);
                    break;
            }
        }
    }

    public void SendPacket(ulong from, byte[] data, PacketType type, P2PSend sendFlags)
    {
        _totalOut++;

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

        _totalBytesOut += packetData.Length;

        if (_specificBytesOut.ContainsKey(type))
            _specificBytesOut[type] += packetData.Length;
        else
            _specificBytesOut[type] = packetData.Length;

        var lobby = LobbyManager.Instance.CurrentLobby;
        switch (isHost)
        {
            case false:
                SteamNetworking.SendP2PPacket(lobby.Owner.Id, packetData, packetData.Length, 0, sendFlags);
                break;
            case true:
                foreach (var member in lobby.Members)
                {
                    if (member.Id == lobby.Owner.Id) continue;
                    SteamNetworking.SendP2PPacket(member.Id, packetData, packetData.Length, 0, sendFlags);
                }

                break;
        }
    }

    public void UpdateShipReferences()
    {
        OwnedSlots.Clear();
        RbSlots.Clear();

        for (var i = 0; i < 8; i++)
        {
            if (LobbyManager.Instance.Players.TryGetValue(i, out var value))
            {
                LobbyManager.Instance.Players[i] = value with {InGameShip = GameState.gameState.playerShips[i]};

                RbSlots.Add(i, GameState.gameState.playerShips[i].GetComponent<Rigidbody2D>());

                if (IsSlotOwnedByThisClient(i))
                    OwnedSlots.Add(i);
            }
        }
    }

    private static bool IsSlotOwnedByThisClient(int slot)
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
                $"{DateTime.Now.Ticks + 30000000L}"); // Give 3 seconds of headstart for the rest to be ready
            break;
        }

        yield return new WaitUntil(() => lobby.GetData("StartTime") != string.Empty);

        Plugin.Logger.LogInfo($"Current Time: {DateTime.Now.Ticks}");
        var startTime = lobby.GetData("StartTime");
        Plugin.Logger.LogInfo($"Everyone is ready, starting at {startTime}");

        ShowConnectingMessage(true);

        yield return new WaitUntil(() => DateTime.Now.Ticks >= long.Parse(startTime));

        ShowConnectingMessage(false);

        Plugin.Logger.LogInfo($"Current Time: {DateTime.Now.Ticks}");
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