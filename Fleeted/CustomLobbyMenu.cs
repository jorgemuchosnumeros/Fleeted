#define GOLDBERG

using System;
using System.Collections;
using System.Reflection;
using Fleeted.utils;
using Steamworks;
using Steamworks.Data;
using TMPro;
using UnityEngine;
using Color = UnityEngine.Color;

namespace Fleeted;

public class CustomLobbyMenu : MonoBehaviour
{
    public static CustomLobbyMenu Instance;

    public static Lobby CurrentLobby;

    public GameObject info;
    private Result _createLobbyResult = Result.None;
    private TextMeshProUGUI _infoTMP;

    private bool _isFriendsOnly;

    private MMContainersController _mmContainersController;
    private Color _prevInfoColor;
    private string _prevInfoText;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
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
                _infoTMP.color = Color.red;
                _infoTMP.text = $"Failed to Create Lobby\n Result: {_createLobbyResult}";
                Plugin.Logger.LogInfo($"Failed to Create Lobby, Result: {_createLobbyResult}");
                return;
            }

#if GOLDBERG
            var body = await WebRequestExtra.GetBodyFromWebRequest($"http://127.0.0.1:3001/setLobby?id={lobby.Id}");
#else
            var body =
 await WebRequestExtra.GetBodyFromWebRequest($"https://u.antonioma.com/fleeted/setLobby.php?id={lobby.Id}");
#endif

            Plugin.Logger.LogInfo($"Received Arrow Code: {body}");

            Plugin.Logger.LogInfo($"Created Lobby: {lobby}");
        }

        CurrentLobby = lobby;
        CurrentLobby.SetJoinable(true);
        CurrentLobby.SetPrivate();

        Plugin.Logger.LogInfo($"Lobby Code: {CurrentLobby.Id}");

        _infoTMP.text = "Press a Button to Join or wait for other players";
        Plugin.Logger.LogInfo($"Joined Lobby: {lobby}");
        Plugin.Logger.LogInfo($"Owner: {lobby.Owner.Name}");
    }

    public async void JoinByArrows(Arrows[] input)
    {
        var joinArrowCode = String.Empty;
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

        Plugin.Logger.LogInfo(joinArrowCode);


#if GOLDBERG
        var body = await WebRequestExtra.GetBodyFromWebRequest($"http://127.0.0.1:3001/getLobby");
#else
        var body =
 await WebRequestExtra.GetBodyFromWebRequest($"https://u.antonioma.com/fleeted/getLobby.php?code={joinArrowCode}");
#endif

        if (ulong.TryParse(body, out ulong idLong))
        {
            Plugin.Logger.LogInfo(idLong);
            await SteamMatchmaking.JoinLobbyAsync(idLong);
        }
    }

    public IEnumerator CreateLobby(int memberLimitSelection, bool isFriendsOnly)
    {
        Plugin.Logger.LogInfo($"Create Lobby with limit of {memberLimitSelection} as {isFriendsOnly} FriendsOnly");

        _mmContainersController.HideSettings();
        _mmContainersController.HideOptions();
        CustomOnlineMenu.Instance.ForceHideMenu(true);
        CustomMainMenu.Instance.ForceHideMenu(true);

        typeof(MainMenuController).GetMethod("ApplyPlay", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(CustomMainMenu.Instance.mainMenuController, new object[] { });

        _isFriendsOnly = isFriendsOnly;
        StartCoroutine(ShowLobbyMenu(memberLimitSelection));

        yield return new WaitForSeconds(0.5f);

        CustomOnlineMenu.Instance.ForceHideMenu(false);
        CustomMainMenu.Instance.ForceHideMenu(false);
    }

    public void MapLobby()
    {
        info = GameObject.Find("PlayMenu/Canvas/Info");
        _infoTMP = info.GetComponent<TextMeshProUGUI>();
        _mmContainersController = FindObjectOfType<MMContainersController>();
    }

    public void SaveLobby()
    {
        _prevInfoText = _infoTMP.text;
        _prevInfoColor = _infoTMP.color;
    }

    public IEnumerator ShowLobbyMenu(int maxMembers)
    {
        _infoTMP.text = "Creating Lobby...";

        // TODO: Lock Input

        yield return new WaitForSeconds(1f); // Wait for one second, so we can show the cool connecting message :>
        SteamMatchmaking.CreateLobbyAsync(maxMembers);
    }

    public void HideLobbyMenu()
    {
        CurrentLobby.Leave();

        _infoTMP.text = _prevInfoText;
        _infoTMP.color = _prevInfoColor;
    }
}