﻿#define GOLDBERG

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
    public Friend CurrentLobbyOwner;
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
        SteamMatchmaking.OnLobbyMemberDataChanged += OnLobbyMemberDataChanged;
        SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeave;
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

            if (!playerBox.activeSelf) // if we have an empty slot
            {
                if (Players.ContainsKey(i)) // but, we still have a key referencing this slot
                {
                    Players.Remove(i);
                    if (isHost)
                        CurrentLobby.SetData($"Slot{i}", String.Empty);
                    else
                        CurrentLobby.SetMemberData($"Slot{i}", String.Empty);

                    Plugin.Logger.LogInfo($"Removing slot {i}");
                }

                continue;
            }

            var playerBoxDisable = playerBox.transform.GetChild(1).gameObject;

            if (playerBoxDisable.activeSelf &&
                Players[i].OwnerOfCharaId != SteamClient.SteamId) // Fix for weird disabled bug
            {
                playerBoxDisable.SetActive(false);
            }

            var playerBoxCanvas = playerBox.transform.GetChild(4);

            // If the Chara has already owner, do not change it, else we are making our own chara
            var steamId = Players.ContainsKey(i) ? Players[i].OwnerOfCharaId : SteamClient.SteamId.Value;

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

            var isBot = playerBoxCanvas.GetChild(1).GetComponent<TextMeshProUGUI>().text.Contains("*");

            var playerInfo = new PlayerInfo
            {
                OwnerOfCharaId = steamId,
                Chara = chara,
                IsBot = isBot,
            };

            Players[i] = playerInfo;

            charas +=
                $"slot {i}, owner: {Players[i].OwnerOfCharaId}, chara: {Players[i].Chara}, isBot: {Players[i].IsBot}\n";
            var playerInfoJson = JsonUtility.ToJson(Players[i]);

            if (isHost)
                CurrentLobby.SetData($"Slot{i}", playerInfoJson);
            else
            {
                CurrentLobby.SetMemberData($"Slot{i}", playerInfoJson);
            }
        }

        Plugin.Logger.LogInfo($"\n{charas}");
        CustomLobbyMenu.Instance.wasCharaSelected = false;
    }

    private void GetCharaSelection(Lobby lobby, ulong friend = 0)
    {
        var charas = String.Empty;

        for (var i = 0; i < 8; i++)
        {
            string playerInfoJson;
            if (isHost && friend != 0)
                playerInfoJson = lobby.GetMemberData(new Friend(friend), $"Slot{i}");
            else
                playerInfoJson = lobby.GetData($"Slot{i}");

            if (playerInfoJson == string.Empty) // if key is empty
            {
                if (!Players.ContainsKey(i)) continue;

                if (Players[i].OwnerOfCharaId != SteamClient.SteamId) // and it isn't our chara
                {
                    RemoveForeignChara(i);
                    if (isHost)
                        CurrentLobby.SetData($"Slot{i}", String.Empty);
                }

                continue;
            }

            var player = JsonUtility.FromJson<PlayerInfo>(playerInfoJson);

            charas += $"slot {i}, owner: {player.OwnerOfCharaId}, chara: {player.Chara}, isBot: {player.IsBot}\n";

            try
            {
                if (!Players.ContainsValue(player))
                {
                    if (player.OwnerOfCharaId == SteamClient.SteamId)
                        Players.Remove(i);
                    else
                        AddForeignChara(i, player);

                    if (isHost)
                        SendOwnCharaSelection();
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
        Players[slot] = player;

        var pmcInstance = CustomLobbyMenu.Instance.playMenuController;
        var icInstance = InputController.inputController;
        var playerBox = CustomLobbyMenu.Instance.playerBoxes[slot];

        playerBox.SetActive(true);
        playerBox.transform.GetChild(0).GetComponent<Animator>().SetInteger("state", 2);
        playerBox.transform.GetChild(4).GetChild(2).gameObject.SetActive(value: false);

        //bots[slot] = true;
        var bots = typeof(InputController).GetField("bots", BindingFlags.Instance | BindingFlags.NonPublic);
        var tmpBots = (bool[]) bots.GetValue(icInstance);
        tmpBots[slot == 0 ? 0 : slot + 1] = true; // Make the game think we added a bot to have the slot occupied
        bots.SetValue(icInstance, tmpBots);

        InputController.inputController.AssignParse(8); // Still dont know how parsing works here but
        // this number seems to do fine when representing
        // a net player

        //activePlayers[slot] = true;
        var activePlayers =
            typeof(PlayMenuController).GetField("activePlayers",
                BindingFlags.Instance | BindingFlags.NonPublic);
        var tmpActivePlayers = (bool[]) activePlayers.GetValue(pmcInstance);
        tmpActivePlayers[slot] = true;
        activePlayers.SetValue(pmcInstance, tmpActivePlayers);

        pmcInstance.PlayVoice((int) player.Chara);

        //charaSelection[slot] = player.Chara;
        var charaSelection = typeof(PlayMenuController).GetField("charaSelection",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var tmpCharaSelection = (int[]) charaSelection.GetValue(pmcInstance);
        tmpCharaSelection[slot] = (int) player.Chara;
        charaSelection.SetValue(pmcInstance, tmpCharaSelection);

        //alreadySelectedCharas[player.Chara] = true;
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
        var pmcInstance = CustomLobbyMenu.Instance.playMenuController;
        var icInstance = InputController.inputController;
        var playerBox = CustomLobbyMenu.Instance.playerBoxes[slot];
        playerBox.SetActive(false);

        GlobalAudio.globalAudio.PlayCancel();

        //bots[slot] = false;
        var bots = typeof(InputController).GetField("bots", BindingFlags.Instance | BindingFlags.NonPublic);
        var tmpBots = (bool[]) bots.GetValue(icInstance);
        tmpBots[slot == 0 ? 0 : slot + 1] = false; // Make the game think we removed a bot to have the slot unoccupied
        bots.SetValue(icInstance, tmpBots);

        foreach (var bot in tmpBots)
        {
            Plugin.Logger.LogWarning($"\n{bot}\n");
        }

        //charaSelection[slot] = -1;
        var charaSelection = typeof(PlayMenuController).GetField("charaSelection",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var tmpCharaSelection = (int[]) charaSelection.GetValue(pmcInstance);
        tmpCharaSelection[slot] = -1;
        charaSelection.SetValue(pmcInstance, tmpCharaSelection);

        //alreadySelectedCharas[charaSelection[playerPosesion[playerN] - 1] - 1] = false;
        var alreadySelectedCharas =
            typeof(PlayMenuController).GetField("alreadySelectedCharas",
                BindingFlags.Instance | BindingFlags.NonPublic);
        var tmpAlreadySelectedCharas = (bool[]) alreadySelectedCharas.GetValue(pmcInstance);
        tmpAlreadySelectedCharas[Players[slot].Chara] = false;
        alreadySelectedCharas.SetValue(pmcInstance, tmpAlreadySelectedCharas);

        Players.Remove(slot);

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
            hostOptions = false;
            if (lobby.MemberCount > lobby.MaxMembers)
            {
                CustomLobbyMenu.Instance.infoTMP.text = "Lobby is full, ask Host to raise member limit";
                CustomLobbyMenu.Instance.infoTMP.color = Color.red;
                lobby.Leave();
                return;
            }

            if (OccupiedSlotsInPlayMenu(lobby) >= 8)
            {
                CustomLobbyMenu.Instance.infoTMP.text = "Lobby is full, ask Host to remove bots";
                CustomLobbyMenu.Instance.infoTMP.color = Color.red;
                lobby.Leave();
                return;
            }

            CustomLobbyMenu.Instance.infoTMP.text = $"Joined {lobby.Owner.Name}'s lobby";
        }

        CurrentLobby = lobby;
        CurrentLobbyOwner = lobby.Owner;

        CustomLobbyMenu.Instance.ShowPlayMenuButtons();

        Plugin.Logger.LogInfo($"Joined Lobby: {lobby.Id}");
        Plugin.Logger.LogInfo($"Owner: {lobby.Owner.Name}");
    }

    private static int OccupiedSlotsInPlayMenu(Lobby lobby)
    {
        var j = 0;
        for (var i = 0; i < 8; i++)
        {
            if (lobby.GetData($"Slot{i}") == String.Empty) continue;
            j++;
        }

        return j;
    }

    private void OnLobbyDataChanged(Lobby lobby)
    {
        if (!isHost)
            GetCharaSelection(lobby);
    }

    private void OnLobbyMemberDataChanged(Lobby lobby, Friend friend)
    {
        if (isHost)
            GetCharaSelection(lobby, friend.Id.Value);
    }

    private void OnLobbyMemberLeave(Lobby lobby, Friend friend)
    {
        if (CurrentLobbyOwner.Equals(friend) && !isHost)
        {
            CurrentLobbyOwner = new Friend();

            //PlayMenuController.BackToMainmenu()
            typeof(PlayMenuController).GetMethod("BackToMainmenu", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(PlayMenuController.playMenuController, new object[] { });

            //PlayMenuController.PlayCancel();
            typeof(PlayMenuController).GetMethod("PlayCancel", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(PlayMenuController.playMenuController, new object[] { });
        }
        else
        {
            for (var i = 0; i < Players.Count + 1; i++)
            {
                if (!Players[i].OwnerOfCharaId.Equals(friend.Id)) continue;

                RemoveForeignChara(i);

                if (isHost)
                    CurrentLobby.SetData($"Slot{i}", String.Empty);
            }
        }
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
        ArrowJoinInput.Instance.ClearInput();
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

    public IEnumerator CreateMenu(int maxMembers)
    {
        CustomLobbyMenu.Instance.infoTMP.text = "Creating Lobby...";

        isLoadingLock = true;

        yield return new WaitForSeconds(1f);
        SteamMatchmaking.CreateLobbyAsync(maxMembers);
    }

    public IEnumerator JoinMenu(ulong id)
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