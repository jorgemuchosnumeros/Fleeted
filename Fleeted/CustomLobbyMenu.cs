using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Fleeted.patches;
using Fleeted.utils;
using Steamworks;
using TMPro;
using UnityEngine;

namespace Fleeted;

public class CustomLobbyMenu : MonoBehaviour
{
    public static CustomLobbyMenu Instance;

    public GameObject canvas;
    public GameObject info;
    public GameObject miniMenu;
    public GameObject playMenuButtons;
    public GameObject[] playerBoxes = new GameObject[8];

    public Canvas canvasCanvas;
    public TextMeshProUGUI infoTMP;

    public Sprite buttonBgSprite;
    public Sprite copySprite;
    public Sprite eyeSprite;
    public Sprite pointSprite;
    public PlayMenuController playMenuController;
    public bool wasCharaSelected;
    public bool wasStageSettingsSelected;
    private MMContainersController _mmContainersController;

    private Color _prevInfoColor;
    private string _prevInfoText;

    private TimedAction steamNamesChange = new(3f);

    private void Awake()
    {
        Instance = this;

        buttonBgSprite = SpritesExtra.SpriteFromName("Fleeted.assets.button_bg.png");
        copySprite = SpritesExtra.SpriteFromName("Fleeted.assets.copy_icon.png");
        eyeSprite = SpritesExtra.SpriteFromName("Fleeted.assets.eye_icon.png");
        pointSprite = SpritesExtra.SpriteFromName("Fleeted.assets.dot.png");
    }

    private void Start()
    {
        steamNamesChange.Start();
    }

    private void Update()
    {
        if (GlobalController.globalController.screen != GlobalController.screens.playmenu) return;

        Cursor.visible = true;
        if (steamNamesChange.TrueDone() && ApplyPlayOnlinePatch.IsOnlineOptionSelected)
        {
            ChangeToSteamNames();
            steamNamesChange.Start();
        }
    }

    public void MapLobby()
    {
        canvas = GameObject.Find("PlayMenu/Canvas");
        info = GameObject.Find("PlayMenu/Canvas/Info");
        miniMenu = GameObject.Find("PlayMenu/Canvas/MiniMenu");

        playerBoxes[0] = GameObject.Find("PlayMenu/PlayerBox");
        for (int i = 1; i < 8; i++)
            playerBoxes[i] = GameObject.Find($"PlayMenu/PlayerBox ({i})");

        canvasCanvas = canvas.GetComponent<Canvas>();
        infoTMP = info.GetComponent<TextMeshProUGUI>();
        playMenuController = FindObjectOfType<PlayMenuController>();
        _mmContainersController = FindObjectOfType<MMContainersController>();
    }

    public void SaveLobby()
    {
        _prevInfoText = infoTMP.text;
        _prevInfoColor = infoTMP.color;
    }

    public IEnumerator ShowPlayMenuButtons(float delay)
    {
        if (info == null)
            yield break;

        playMenuButtons = new GameObject("Custom Play Menu Buttons");
        playMenuButtons.transform.SetParent(info.transform, false);
        playMenuButtons.AddComponent<CustomPlayMenuButtons>();

        yield return new WaitForSeconds(delay);

        if (delay <= 0) yield break;

        // Reset to leave a chance to the CustomPlayMenuButtons script be ran again
        if (info != null)
        {
            info.SetActive(true);
            info.SetActive(false);
        }
    }

    public void HideLobbyMenu()
    {
        if (InGameNetManager.Instance != null)
            InGameNetManager.Instance.ResetState();

        if (LobbyManager.Instance != null)
        {
            if (!EqualityComparer<Steamworks.Data.Lobby>.Default.Equals(LobbyManager.Instance.CurrentLobby, default))
                LobbyManager.Instance.CurrentLobby.Leave();
            LobbyManager.Instance.isHost = false;
            LobbyManager.Instance.hostOptions = true;
        }

        if (infoTMP != null)
        {
            infoTMP.text = _prevInfoText;
            infoTMP.color = _prevInfoColor;
        }

        if (playMenuButtons != null)
            Destroy(playMenuButtons);
    }

    public IEnumerator TransitionToLobby(int memberLimitSelection, bool isFriendsOnly)
    {
        _mmContainersController.HideSettings();
        _mmContainersController.HideOptions();
        CustomOnlineMenu.Instance.ForceHideMenu(true);
        CustomMainMenu.Instance.ForceHideMenu(true);

        typeof(MainMenuController).GetMethod("ApplyPlay", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(CustomMainMenu.Instance.mainMenuController, new object[] { });

        Plugin.Logger.LogInfo($"Create Lobby with limit of {memberLimitSelection} as {isFriendsOnly} FriendsOnly...");
        StartCoroutine(LobbyManager.Instance.CreateMenu(memberLimitSelection));

        yield return new WaitForSeconds(0.5f);

        CustomOnlineMenu.Instance.ForceHideMenu(false);
        CustomMainMenu.Instance.ForceHideMenu(false);
    }

    public IEnumerator TransitionToLobby(ulong id)
    {
        switch (GlobalController.globalController.screen)
        {
            case GlobalController.screens.mainmenu:
                var mmcInstance = FindObjectOfType<MainMenuController>();
                mmcInstance.selection = 2;
                ApplyPlayOnlinePatch.IsOnlineOptionSelected = true;
                typeof(MainMenuController).GetMethod("ApplyOptions", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(mmcInstance, null);
                yield return new WaitForSeconds(0.5f);
                break;
            case GlobalController.screens.optionsmenu:
                break;
            default:
                Plugin.Logger.LogInfo($"Cannot Join from screen: {GlobalController.globalController.screen}");
                yield break;
        }

        _mmContainersController.HideOptions();
        CustomOnlineMenu.Instance.ForceHideMenu(true);
        CustomMainMenu.Instance.ForceHideMenu(true);

        typeof(MainMenuController).GetMethod("ApplyPlay", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(CustomMainMenu.Instance.mainMenuController, new object[] { });

        Plugin.Logger.LogInfo("Joining Lobby...");
        StartCoroutine(LobbyManager.Instance.JoinMenu(id));

        yield return new WaitForSeconds(0.5f);

        CustomOnlineMenu.Instance.ForceHideMenu(false);
        CustomMainMenu.Instance.ForceHideMenu(false);
    }

    public void ChangeToSteamNames()
    {
        if (LobbyManager.Instance == null || LobbyManager.Instance.Players == null)
            return;
        if (playerBoxes == null || playerBoxes.Length == 0)
            return;

        var players = LobbyManager.Instance.Players;
        for (var i = 0; i < players.Count; i++)
        {
            if (i >= playerBoxes.Length) break;
            if (playerBoxes[i] == null || playerBoxes[i].activeSelf == false) continue;

            var playerBox = playerBoxes[i];

            if (!players.TryGetValue(i, out var playerInfo)) continue;

            var playerName = new Friend(playerInfo.OwnerOfCharaId).Name;
            if (playerBox.transform.childCount <= 4) continue;
            var charaNameParent = playerBox.transform.GetChild(4);
            if (charaNameParent.childCount <= 0) continue;
            var charaName = charaNameParent.GetChild(0);
            var tmpugui = charaName.GetComponent<TextMeshProUGUI>();
            var rectTransform = charaName.GetComponent<RectTransform>();
            if (tmpugui == null || rectTransform == null) continue;

            float adjustx;
            float adjusty;

            if (playerInfo.IsBot)
            {
                adjustx = 0f;
                adjusty = 0f;
            }
            else
            {
                adjustx = 90f;
                adjusty = 8f;
                tmpugui.text = playerName;
            }

            rectTransform.sizeDelta = new Vector2(150 + adjustx, 30);
            rectTransform.anchoredPosition = new Vector2(53.8f - adjustx / 2, 68.8f + adjusty);
        }
    }
}
