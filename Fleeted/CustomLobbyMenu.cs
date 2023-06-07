using System.Collections;
using System.Reflection;
using Fleeted.utils;
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

    public Canvas canvasCanvas;
    public TextMeshProUGUI infoTMP;

    public Sprite buttonBgSprite;
    public Sprite copySprite;
    public Sprite eyeSprite;
    public Sprite pointSprite;
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

        canvasCanvas = canvas.GetComponent<Canvas>();
        infoTMP = info.GetComponent<TextMeshProUGUI>();
        _mmContainersController = FindObjectOfType<MMContainersController>();
    }

    public void SaveLobby()
    {
        _prevInfoText = infoTMP.text;
        _prevInfoColor = infoTMP.color;
    }

    public void ShowPlayMenuButtons()
    {
        playMenuButtons = new GameObject("Custom Play Menu Buttons");
        playMenuButtons.transform.SetParent(miniMenu.transform, false);
        playMenuButtons.AddComponent<CustomPlayMenuButtons>();
    }

    public void HideLobbyMenu()
    {
        LobbyManager.Instance.CurrentLobby.Leave();
        LobbyManager.Instance.isHost = false;

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

        LobbyManager.Instance.CreateLobby(memberLimitSelection, isFriendsOnly);

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

        LobbyManager.Instance.JoinLobby(id);

        yield return new WaitForSeconds(0.5f);

        CustomOnlineMenu.Instance.ForceHideMenu(false);
        CustomMainMenu.Instance.ForceHideMenu(false);
    }
}