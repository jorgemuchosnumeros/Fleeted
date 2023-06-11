#define GOLDBERG

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Fleeted.utils;
using Steamworks;
using Steamworks.Data;
using TMPro;
using UnityEngine;
using Color = UnityEngine.Color;

namespace Fleeted;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;

    public bool isHost;
    public bool isLoadingLock;
    public bool hostOptions = true;

    public string joinArrowCode;
    private Result _createLobbyResult = Result.None;
    public Lobby CurrentLobby;

    public Dictionary<int, PlayerInfo> Players = new();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
        SteamMatchmaking.OnLobbyDataChanged += OnLobbyDataChanged;
    }

    private void Update()
    {
        if (!CustomLobbyMenu.Instance.wasCharaSelected) return;

        SendOwnCharaSelection();
        CustomLobbyMenu.Instance.ChangeToSteamNames();
    }

    private void SendOwnCharaSelection()
    {
        var charas = String.Empty;

        for (var i = 0; i < 8; i++)
        {
            var playerBox = CustomLobbyMenu.Instance.playerBoxes[i];

            if (!playerBox.activeSelf)
            {
                if (Players.ContainsKey(i))
                {
                    Players.Remove(i);
                    CurrentLobby.SetData($"Slot{i}", String.Empty);
                    Plugin.Logger.LogInfo($"Removing slot {i}");
                }

                continue;
            }

            var playerBoxCanvas = playerBox.transform.GetChild(4); // 4 stands for canvas
            var isBot = playerBoxCanvas.GetChild(1).GetComponent<TextMeshProUGUI>().text.Contains("*");

            // weird hack, we dont want to use the names of the TMPUGUI.text cuz those are going to be tampered by other
            // users, we use the SpriteRenderer.sprite.name instead
            var chara = playerBox.transform.GetChild(2).GetChild(0).GetComponent<SpriteRenderer>().sprite.name switch
            {
                "tofe" => 0u,
                "nobu" => 1u,
                "taka" => 2u,
                "waba" => 3u,
                "miki" => 4u,
                "lico" => 5u,
                "naru" => 6u,
                "pita" => 7u,
                "lari" => 8u,
                _ => throw new ArgumentOutOfRangeException()
            };

            var playerInfo = new PlayerInfo
            {
                OwnerOfCharaId = SteamClient.SteamId.Value,
                Chara = chara,
                IsBot = isBot,
            };

            Players[i] = playerInfo;

            charas +=
                $"slot {i}, owner: {Players[i].OwnerOfCharaId}, chara: {Players[i].Chara}, isBot: {Players[i].IsBot}\n";
            var playerInfoJson = JsonUtility.ToJson(Players[i]);

            CurrentLobby.SetData($"Slot{i}", playerInfoJson);
        }

        Plugin.Logger.LogInfo($"\n{charas}");
        CustomLobbyMenu.Instance.wasCharaSelected = false;
    }

    private void GetForeignCharaSelection(Lobby lobby)
    {
        var charas = String.Empty;

        for (int i = 0; i < 8; i++)
        {
            var playerInfoJson = lobby.GetData($"Slot{i}");

            if (playerInfoJson == string.Empty)
            {
                if (Players.ContainsKey(i) && !isHost)
                {
                    RemoveForeignChara(i);
                }

                continue;
            }

            var player = JsonUtility.FromJson<PlayerInfo>(playerInfoJson);

            charas += $"slot {i}, owner: {player.OwnerOfCharaId}, chara: {player.Chara}, isBot: {player.IsBot}\n";

            try
            {
                if (!Players.ContainsValue(player) && !isHost)
                {
                    AddForeignChara(i, player);
                }
            }
            catch (Exception ex) // For some reason unity does not catch exceptions from this function
            {
                Plugin.Logger.LogError(ex);
                return;
            }
        }

        if (charas != String.Empty)
        {
            Plugin.Logger.LogInfo($"Received Chara Update:\n{charas}");
        }
    }

    private void AddForeignChara(int slot, PlayerInfo player)
    {
        var pmcInstance = CustomLobbyMenu.Instance.playMenuController;
        var playerPosesion = player.IsBot ? 0 : slot;
        var playerBox = CustomLobbyMenu.Instance.playerBoxes[slot];

        Players[slot] = player;

        playerBox.SetActive(true);
        pmcInstance.PlayVoice((int) player.Chara);
        playerBox.transform.GetChild(0).GetComponent<Animator>().SetInteger("state", 2);
        playerBox.transform.GetChild(4).GetChild(2).gameObject.SetActive(value: false);

        //alreadySelectedCharas[charaSelection[playerPosesion[playerN] - 1] - 1] = true;
        var alreadySelectedCharas =
            typeof(PlayMenuController).GetField("alreadySelectedCharas",
                BindingFlags.Instance | BindingFlags.NonPublic);
        var tmpAlreadySelectedCharas = (bool[]) alreadySelectedCharas.GetValue(pmcInstance);
        tmpAlreadySelectedCharas[player.Chara] = true;
        alreadySelectedCharas.SetValue(pmcInstance, tmpAlreadySelectedCharas);

        pmcInstance.ships[slot].SetAsChara((int) player.Chara + 1);
        pmcInstance.charasSR[slot].sprite = pmcInstance.charas[(int) player.Chara];
        pmcInstance.charas_sSR[slot].sprite = pmcInstance.charas_s[(int) player.Chara];
        pmcInstance.disabledCharasSR[slot].sprite = pmcInstance.charas_s[(int) player.Chara];
        pmcInstance.charaNames[slot].text = player.CharaName();

        var asterisk = player.IsBot ? "*" : string.Empty;
        playerBox.transform.GetChild(4).GetChild(1).GetComponent<TextMeshProUGUI>().text = $"{slot + 1}{asterisk}";
        playerBox.transform.GetChild(1).gameObject.SetActive(false);

        CustomLobbyMenu.Instance.ChangeToSteamNames();

        Plugin.Logger.LogWarning($"Adding Chara at {slot} as {player.CharaName()} from {player.OwnerOfCharaId}");
    }

    private void RemoveForeignChara(int slot)
    {
        Players.Remove(slot);

        var playerBox = CustomLobbyMenu.Instance.playerBoxes[slot];
        playerBox.SetActive(false);

        GlobalAudio.globalAudio.PlayCancel();

        Plugin.Logger.LogError($"Removing Chara at {slot}");
    }

    private void OnLobbyCreated(Result result, Lobby lobby)
    {
        _createLobbyResult = result;
    }

    private async void OnLobbyEntered(Lobby lobby)
    {
        isLoadingLock = false;
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
                Plugin.Logger.LogInfo($"Created Lobby: {lobby.Id}");
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

        if (isHost)
        {
            _createLobbyResult = Result.None;
            lobby.SetJoinable(true);
            lobby.SetPrivate();
        }
        else
        {
            CustomLobbyMenu.Instance.infoTMP.text = $"Joined {lobby.Owner.Name}'s lobby";
            hostOptions = false;
        }

        CurrentLobby = lobby;
        CustomLobbyMenu.Instance.ShowPlayMenuButtons();

        Plugin.Logger.LogInfo($"Joined Lobby: {lobby.Id}");
        Plugin.Logger.LogInfo($"Owner: {lobby.Owner.Name}");
    }

    private void OnLobbyDataChanged(Lobby lobby)
    {
        GetForeignCharaSelection(lobby);
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
        ArrowJoinInput.Instance.isInputLocked = false;


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

        isLoadingLock = true;

        yield return new WaitForSeconds(1f);
        SteamMatchmaking.CreateLobbyAsync(maxMembers);
    }

    private IEnumerator JoinMenu(ulong id)
    {
        CustomLobbyMenu.Instance.infoTMP.text = "Joining Lobby...";

        isLoadingLock = true;

        yield return new WaitForSeconds(1f);
        SteamMatchmaking.JoinLobbyAsync(id);
    }

    public struct PlayerInfo
    {
        public ulong OwnerOfCharaId;
        public uint Chara;
        public bool IsBot;
    }
}