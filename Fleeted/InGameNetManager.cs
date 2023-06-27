using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Fleeted.patches;
using Steamworks;
using Steamworks.Data;
using TMPro;
using UnityEngine;

namespace Fleeted;

public class InGameNetManager : MonoBehaviour
{
    public static InGameNetManager Instance;

    /// Server owned
    public List<Connection> ServerConnections = new();

    /// Server owned
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SteamNetworkingUtils.InitRelayNetworkAccess();
        SteamNetworking.AllowP2PPacketRelay(true);

        SteamNetworkingSockets.OnConnectionStatusChanged += OnConnectionStatusChanged;
    }

    private void OnConnectionStatusChanged(Connection connection, ConnectionInfo connectionInfo)
    {
        var id = connectionInfo.Identity.SteamId;
        if (connectionInfo.EndReason != NetConnectionEnd.Invalid)
        {
            var members = LobbyManager.Instance.CurrentLobby.Members;
            switch (connectionInfo.State)
            {
                case ConnectionState.Connecting:

                    Plugin.Logger.LogInfo($"Connection Request from: {id}");

                    if (members.All(m => m.Id != id))
                    {
                        connection.Close();
                        Plugin.Logger.LogError("This user is not part of the lobby! Rejecting the connection");
                        break;
                    }

                    if (!SteamNetworking.AcceptP2PSessionWithUser(id))
                    {
                        connection.Close();
                        Plugin.Logger.LogError("Failed to accept connection");
                        break;
                    }

                    ServerConnections.Add(connection);

                    int _2mb = 2097152;
                    SteamNetworkingUtils.SendBufferSize = members.Count() * _2mb;

                    Plugin.Logger.LogInfo("Accepted the connection");
                    break;
                case ConnectionState.ClosedByPeer:
                case ConnectionState.ProblemDetectedLocally:
                    Plugin.Logger.LogInfo($"Killing connection from {id}");
                    connection.Close();
                    if (ServerConnections.Contains(connection))
                    {
                        ServerConnections.Remove(connection);

                        // TODO: Deal with Disconnected Ships
                    }

                    break;
            }
        }
        else
        {
            switch (connectionInfo.State)
            {
                case ConnectionState.Connected:
                    Plugin.Logger.LogInfo("Connected to server.");
                    break;

                case ConnectionState.ClosedByPeer:
                case ConnectionState.ProblemDetectedLocally:
                    Plugin.Logger.LogInfo($"Killing connection from {id}.");
                    connection.Close();

                    //TODO: Return to MainMenu
                    break;
            }
        }
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