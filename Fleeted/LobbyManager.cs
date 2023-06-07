#define GOLDBERG

using System;
using System.Collections;
using Fleeted.utils;
using Steamworks;
using Steamworks.Data;
using UnityEngine;
using Color = UnityEngine.Color;

namespace Fleeted;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;

    public bool isHost;

    public string joinArrowCode;
    private Result _createLobbyResult = Result.None;
    public Lobby CurrentLobby;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
    }

    private void Update()
    {
    }

    private void OnLobbyCreated(Result result, Lobby lobby)
    {
        _createLobbyResult = result;
    }

    private async void OnLobbyEntered(Lobby lobby)
    {
        if (_createLobbyResult != Result.None)
        {
            if (_createLobbyResult != Result.OK)
            {
                CustomLobbyMenu.Instance.infoTMP.color = Color.red;
                CustomLobbyMenu.Instance.infoTMP.text = $"Failed to Create Lobby\n Reason: {_createLobbyResult}";
                Plugin.Logger.LogInfo($"Failed to Create Lobby, Reason: {_createLobbyResult}");
                return;
            }

            try
            {
#if GOLDBERG
                var body = await WebRequestExtra.GetBodyFromWebRequest($"http://127.0.0.1:3001/setLobby?id={lobby.Id}");
#else
                var body =
 await WebRequestExtra.GetBodyFromWebRequest($"https://u.antonioma.com/fleeted/setLobby.php?id={lobby.Id}");
#endif
                CustomLobbyMenu.Instance.infoTMP.text = "Press a Button to Join and wait for other players";
                joinArrowCode = body;
                Plugin.Logger.LogInfo($"Received Arrow Code: {body}");
                Plugin.Logger.LogInfo($"Created Lobby: {lobby}");
                isHost = true;
            }
            catch (Exception ex)
            {
                CustomLobbyMenu.Instance.infoTMP.text = "Failed to Create Lobby\n Reason: Probably Antonio";
                CustomLobbyMenu.Instance.infoTMP.color = Color.red;
                Plugin.Logger.LogError($"Failed to Create Lobby, Reason: {ex}");
                return;
            }
        }
        else
        {
            isHost = false;
        }

        CurrentLobby = lobby;

        if (isHost)
        {
            _createLobbyResult = Result.None;
            CurrentLobby.SetJoinable(true);
            CurrentLobby.SetPrivate();
        }
        else
        {
            CustomLobbyMenu.Instance.infoTMP.text = $"Joined {lobby.Owner.Name}'s lobby";
        }

        Plugin.Logger.LogInfo($"Lobby Code: {CurrentLobby.Id}");
        Plugin.Logger.LogInfo($"Joined Lobby: {lobby}");
        Plugin.Logger.LogInfo($"Owner: {lobby.Owner.Name}");

        CustomLobbyMenu.Instance.ShowPlayMenuButtons();
    }

    public async void JoinByArrows(Arrows[] input)
    {
        joinArrowCode = String.Empty;
        foreach (var arrow in input)
        {
            switch (arrow)
            {
                case Arrows.Down:
                    joinArrowCode += "D";
                    break;
                case Arrows.Left:
                    joinArrowCode += "L";
                    break;
                case Arrows.Right:
                    joinArrowCode += "R";
                    break;
                case Arrows.Up:
                    joinArrowCode += "U";
                    break;
            }
        }

        if (joinArrowCode == String.Empty)
        {
            return;
        }

        Plugin.Logger.LogInfo($"Joining with code: {joinArrowCode}");


#if GOLDBERG
        var body = await WebRequestExtra.GetBodyFromWebRequest($"http://127.0.0.1:3001/getLobby?id=UUDDLR");
#else
        var body =
 await WebRequestExtra.GetBodyFromWebRequest($"https://u.antonioma.com/fleeted/getLobby.php?code={joinArrowCode}");
#endif
        if (ulong.TryParse(body, out ulong id))
        {
            Plugin.Logger.LogInfo($"Received Lobby ID: {id}");
            StartCoroutine(CustomLobbyMenu.Instance.TransitionToLobby(id));
        }
    }

    public void CreateLobby(int memberLimitSelection, bool isFriendsOnly)
    {
        Plugin.Logger.LogInfo($"Create Lobby with limit of {memberLimitSelection} as {isFriendsOnly} FriendsOnly...");
        StartCoroutine(CreateMenu(memberLimitSelection));
    }

    public void JoinLobby(ulong id)
    {
        Plugin.Logger.LogInfo($"Joining Lobby...");
        StartCoroutine(JoinMenu(id));
    }

    private IEnumerator CreateMenu(int maxMembers)
    {
        CustomLobbyMenu.Instance.infoTMP.text = "Creating Lobby...";

        // TODO: Lock Input

        yield return new WaitForSeconds(1f);
        SteamMatchmaking.CreateLobbyAsync(maxMembers);
    }

    private IEnumerator JoinMenu(ulong id)
    {
        CustomLobbyMenu.Instance.infoTMP.text = "Joining Lobby...";

        // TODO: Lock Input

        yield return new WaitForSeconds(1f);
        SteamMatchmaking.JoinLobbyAsync(id);
    }
}