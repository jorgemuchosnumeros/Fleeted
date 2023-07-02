using System.Collections;
using System.Reflection;
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

    private void Awake()
    {
        Instance = this;

        buttonBgSprite = SpritesExtra.SpriteFromName("Fleeted.assets.button_bg.png");
        copySprite = SpritesExtra.SpriteFromName("Fleeted.assets.copy_icon.png");
        eyeSprite = SpritesExtra.SpriteFromName("Fleeted.assets.eye_icon.png");
        pointSprite = SpritesExtra.SpriteFromName("Fleeted.assets.dot.png");
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
        playMenuButtons = new GameObject("Custom Play Menu Buttons");
        playMenuButtons.transform.SetParent(info.transform, false);
        playMenuButtons.AddComponent<CustomPlayMenuButtons>();

        yield return new WaitForSeconds(delay);

        if (delay <= 0) yield break;

        // Reset to leave a chance to the CustomPlayMenuButtons script be ran again
        info.SetActive(true);
        info.SetActive(false);
    }

    public void HideLobbyMenu()
    {
        // TODO: Abandon Connection
        InGameNetManager.Instance.ResetState();

        LobbyManager.Instance.CurrentLobby.Leave();
        LobbyManager.Instance.isHost = false;
        LobbyManager.Instance.hostOptions = true;

        infoTMP.text = _prevInfoText;
        infoTMP.color = _prevInfoColor;

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
        var players = LobbyManager.Instance.Players;
        for (var i = 0; i < players.Count; i++)
        {
            if (playerBoxes[i].activeSelf == false) continue;

            var playerBox = playerBoxes[i];

            var playerName = new Friend(players[i].OwnerOfCharaId).Name;
            var charaName = playerBox.transform.GetChild(4).GetChild(0);
            var tmpugui = charaName.GetComponent<TextMeshProUGUI>();
            var rectTransform = charaName.GetComponent<RectTransform>();

            float adjustx;
            float adjusty;

            if (players[i].IsBot)
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