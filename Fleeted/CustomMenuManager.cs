using System.IO;
using System.Reflection;
using Fleeted.patches;
using Fleeted.utils;
using TMPro;
using UnityEngine;

namespace Fleeted;

public class CustomMenuManager : MonoBehaviour
{
    public static CustomMenuManager Instance;
    public bool menuSpawned;

    // Main Menu
    public GameObject mainMenu;
    public GameObject title;
    public GameObject shipContainer;
    public GameObject canvas;
    public GameObject icons;
    public GameObject shipsIcon;
    public GameObject netIcon;
    public GameObject playLocalOption;
    public GameObject playOnlineOption;
    public GameObject shipCursor;
    public GameObject shipAnimated;

    public MainMenuController mainMenuController;

    // Online Menu
    public GameObject options;
    public GameObject optionsCanvas;
    public GameObject optionsIcons;
    public GameObject optionsMenuHeader;
    public GameObject settingsOption;
    public GameObject settingsIcon;
    public GameObject galleryOption;
    public GameObject galleryIcon;
    public GameObject controlsOption;
    public GameObject rulesOption;
    public GameObject creditsOption;
    public GameObject dataOption;
    public GameObject backOption;
    public GameObject controlsIcon;
    public GameObject rulesIcon;
    public GameObject creditsIcon;
    public GameObject dataIcon;
    public GameObject backIcon;
    public GameObject shipCursorOptions;

    public bool moveOnlineOptions;

    private readonly TimedAction _delayCorrectionMM = new(0.25f);

    private readonly TimedAction _delayCorrectionOM = new(0.25f);
    private readonly TimedAction _delayDisableAnimatorMM = new(0.4f);
    private SpriteRenderer _connectIconRenderer;
    private Sprite _connectSprite;
    private bool _delayCorrectionMMFlag;
    private bool _delayCorrectionOMFlag;
    private SpriteRenderer _galleryIconRenderer;
    private TextMeshProUGUI _galleryOptionTMP;

    private Sprite _hostSprite;
    private Sprite _joinSprite;
    private TextMeshProUGUI _menuHeaderOptionsTMP;

    private TextMeshProUGUI _playLocalTMP;
    private TextMeshProUGUI _playOnlineTMP;
    private Sprite _prevGalleryIconSprite;
    private string _prevGalleryOptionText;

    private string _prevOptionsMenuHeaderText;
    private Sprite _prevSettingsIconSprite;
    private string _prevSettingsOptionText;

    private SpriteRenderer _settingsIconRenderer;
    private TextMeshProUGUI _settingsOptionTMP;

    private Animator _shipAnimatorMM;

    private Animator _shipAnimatorOM;
    private Vector3 _shipCursorTarget;

    public void Awake()
    {
        Instance = this;

        _delayCorrectionMM.Start();

        // Convert "assets/*_icon.png" to Sprites
        using var connectIconResource =
            Assembly.GetExecutingAssembly().GetManifestResourceStream("Fleeted.assets.connect_icon.png");
        using var hostIconResource =
            Assembly.GetExecutingAssembly().GetManifestResourceStream("Fleeted.assets.host_icon.png");
        using var joinIconResource =
            Assembly.GetExecutingAssembly().GetManifestResourceStream("Fleeted.assets.join_icon.png");

        using var connectIconResourceMemory = new MemoryStream();
        connectIconResourceMemory.SetLength(0);
        connectIconResource.CopyTo(connectIconResourceMemory);
        var imageBytes = connectIconResourceMemory.ToArray();
        Texture2D connectTex2D = new Texture2D(2, 2);
        connectTex2D.LoadImage(imageBytes);
        _connectSprite = Sprite.Create(connectTex2D, new Rect(0, 0, connectTex2D.width, connectTex2D.height),
            Vector2.zero, 50f);

        using var hostIconResourceMemory = new MemoryStream();
        hostIconResourceMemory.SetLength(0);
        hostIconResource.CopyTo(hostIconResourceMemory);
        imageBytes = hostIconResourceMemory.ToArray();
        Texture2D hostTex2D = new Texture2D(2, 2);
        hostTex2D.LoadImage(imageBytes);
        _hostSprite = Sprite.Create(hostTex2D, new Rect(0, 0, hostTex2D.width, hostTex2D.height), Vector2.zero, 50f);

        using var joinIconResourceMemory = new MemoryStream();
        joinIconResourceMemory.SetLength(0);
        joinIconResource.CopyTo(joinIconResourceMemory);
        imageBytes = joinIconResourceMemory.ToArray();
        Texture2D joinTex2D = new Texture2D(2, 2);
        joinTex2D.LoadImage(imageBytes);
        _joinSprite = Sprite.Create(joinTex2D, new Rect(0, 0, joinTex2D.width, joinTex2D.height), Vector2.zero, 50f);
    }

    private void Update()
    {
        if (!menuSpawned)
            return;

        ApplyMainMenuTransforms();


        if (GlobalController.globalController.screen == GlobalController.screens.optionsmenu)
        {
            if (_delayCorrectionOMFlag)
            {
                _delayCorrectionOM.Start();
                _delayCorrectionOMFlag = false;
            }

            if (_delayCorrectionOM.TrueDone())
            {
                if (moveOnlineOptions)
                {
                    var menuBoatScalar = (ManageInputOnlinePatch.OnlineSelection - 1) * 6;

                    _shipCursorTarget = new Vector3(-20.5f, 3.35f - menuBoatScalar);

                    if ((shipCursorOptions.transform.position - _shipCursorTarget).sqrMagnitude > 0.005f)
                    {
                        shipCursorOptions.transform.position = Vector3.MoveTowards(shipCursorOptions.transform.position,
                            _shipCursorTarget, 75f * Time.deltaTime);
                    }

                    backOption.transform.position = new Vector3(7.8f, -8.8f, 0);
                    optionsCanvas.transform.position = new Vector3(0, -10, 0);
                    backIcon.transform.position = new Vector3(-16, -8.8f, 0);
                    optionsIcons.transform.position = new Vector3(0, -10, 0);
                    settingsIcon.transform.position = new Vector3(-18.3f, 1.1f, 0);
                    settingsIcon.transform.localScale = Vector3.one;
                    galleryIcon.transform.position = new Vector3(-18.5f, -4.8f, 0);
                    galleryIcon.transform.localScale = Vector3.one;
                }
                else
                {
                    galleryIcon.transform.localScale = Vector3.one * 0.8f;
                    galleryIcon.transform.position = new Vector3(-16, 7.09f, 0);
                    settingsIcon.transform.localScale = Vector3.one * 0.8f;
                    settingsIcon.transform.position = new Vector3(-16, 13.11f, 0);
                    optionsIcons.transform.position = new Vector3(0, 0, 0);
                    backIcon.transform.position = new Vector3(-16, -23, 0);
                    optionsCanvas.transform.position = new Vector3(0, 0, 0);
                    backOption.transform.position = new Vector3(7.8f, -22.8f, 0);
                }
            }
        }
        else
        {
            _delayCorrectionOMFlag = true;
        }
    }

    private void ApplyMainMenuTransforms()
    {
        if (GlobalController.globalController.screen == GlobalController.screens.mainmenu)
        {
            // Disabled the Animator of the Boat Cursor on the Main Menu and Remake the animations to support the 7th Option
            if (mainMenuController.selection > 0)
            {
                if (_shipAnimatorMM.isActiveAndEnabled)
                {
                    if (!_delayDisableAnimatorMM.HasEverStated)
                        _delayDisableAnimatorMM.Start();
                    _shipAnimatorMM.enabled = !_delayDisableAnimatorMM.TrueDone();
                }

                var menuBoatScalar = (mainMenuController.selection - 1) * 4.8f;

                _shipCursorTarget = new Vector3(-14.6f, 4.5f - menuBoatScalar);

                if ((shipCursor.transform.position - _shipCursorTarget).sqrMagnitude > 0.005f)
                {
                    shipCursor.transform.position = Vector3.MoveTowards(shipCursor.transform.position,
                        _shipCursorTarget, 75f * Time.deltaTime);
                }
            }

            if (_delayCorrectionMMFlag)
            {
                _delayCorrectionMM.Start();
                _delayCorrectionMMFlag = false;
            }

            if (_delayCorrectionMM.TrueDone())
            {
                // Pinning the Custom Options
                playLocalOption.transform.position = new Vector3(7.364f, 4.64f, 0f);
                _playLocalTMP.text = "Play (Local)";

                playOnlineOption.transform.position = new Vector3(7.364f, -0.36f, 0);
                _playOnlineTMP.text = "Play (Online)";
            }
        }
        else
        {
            _delayCorrectionMMFlag = true;
        }
    }

    public void MapMainMenu()
    {
        mainMenu = GameObject.Find("MainMenu");
        title = GameObject.Find("MainMenu/title");
        shipContainer = GameObject.Find("MainMenu/ShipContainer");
        canvas = GameObject.Find("MainMenu/Canvas");
        icons = GameObject.Find("MainMenu/Icons");
        shipsIcon = GameObject.Find("MainMenu/Icons/ships_icon");
        playLocalOption = GameObject.Find("MainMenu/Canvas/Options/Play");
        shipCursor = GameObject.Find("MainMenu/ShipContainer/ShipContainer2/Ship");
        shipAnimated = GameObject.Find("MainMenu/ShipContainer");

        mainMenuController = FindObjectOfType<MainMenuController>().GetComponent<MainMenuController>();

        _playLocalTMP = playLocalOption.GetComponent<TextMeshProUGUI>();
        _shipAnimatorMM = shipAnimated.GetComponent<Animator>();
    }

    public void CreateMainMenuSpace()
    {
        _delayDisableAnimatorMM.HasEverStated = false;

        mainMenu.transform.localScale = Vector3.one * 0.8f;
        title.transform.localScale = Vector3.one * 1.25f;
        shipContainer.transform.localScale = Vector3.one * 1.20f;

        canvas.transform.position = Vector3.up * -5f;
        icons.transform.position = Vector3.up * -5f;
        shipsIcon.transform.position = new Vector3(-10.176f, 4.64f, 0f);
        shipsIcon.transform.localScale = Vector3.one * 1.25f;
    }

    public void CreateMainMenuOnlineOption()
    {
        playOnlineOption = Instantiate(playLocalOption, playLocalOption.transform.parent);
        netIcon = Instantiate(shipsIcon, shipsIcon.transform.parent);

        _playOnlineTMP = playOnlineOption.GetComponent<TextMeshProUGUI>();
        _connectIconRenderer = netIcon.GetComponent<SpriteRenderer>();

        netIcon.transform.position = new Vector3(-12.44f, -2.3f, 0);
        netIcon.transform.localScale = Vector3.one * 1.05f;
        _connectIconRenderer.sprite = _connectSprite;
    }

    public void MapOptionsMenu()
    {
        options = GameObject.Find("/Options");
        optionsCanvas = GameObject.Find("Options/OptionsMenu/Canvas");
        optionsIcons = GameObject.Find("Options/OptionsMenu/Icons");
        optionsMenuHeader = GameObject.Find("/Options/OptionsMenu/Canvas/Options");
        settingsOption = GameObject.Find("Options/OptionsMenu/Canvas/Settings");
        settingsIcon = GameObject.Find("Options/OptionsMenu/Icons/gear_icon");
        galleryOption = GameObject.Find("Options/OptionsMenu/Canvas/Gallery");
        galleryIcon = GameObject.Find("Options/OptionsMenu/Icons/gallery_icon");
        controlsOption = GameObject.Find("Options/OptionsMenu/Canvas/Controls");
        rulesOption = GameObject.Find("Options/OptionsMenu/Canvas/Rules");
        creditsOption = GameObject.Find("Options/OptionsMenu/Canvas/Credits");
        dataOption = GameObject.Find("Options/OptionsMenu/Canvas/Data");
        backOption = GameObject.Find("Options/OptionsMenu/Canvas/Back");
        controlsIcon = GameObject.Find("Options/OptionsMenu/Icons/controller_icon");
        rulesIcon = GameObject.Find("Options/OptionsMenu/Icons/rules_icon");
        creditsIcon = GameObject.Find("Options/OptionsMenu/Icons/credits_icon");
        dataIcon = GameObject.Find("Options/OptionsMenu/Icons/icon_data");
        backIcon = GameObject.Find("Options/OptionsMenu/Icons/arrow_icon");
        shipCursorOptions = GameObject.Find("Options/OptionsMenu/ShipContainer");

        _menuHeaderOptionsTMP = optionsMenuHeader.GetComponent<TextMeshProUGUI>();
        _shipAnimatorOM = shipCursorOptions.GetComponent<Animator>();
        _settingsOptionTMP = settingsOption.GetComponent<TextMeshProUGUI>();
        _galleryOptionTMP = galleryOption.GetComponent<TextMeshProUGUI>();
        _settingsIconRenderer = settingsIcon.GetComponent<SpriteRenderer>();
        _galleryIconRenderer = galleryIcon.GetComponent<SpriteRenderer>();
    }

    public void SaveOptionsMenuSpace()
    {
        _prevOptionsMenuHeaderText = _menuHeaderOptionsTMP.text;
        _prevSettingsOptionText = _settingsOptionTMP.text;
        _prevGalleryOptionText = _galleryOptionTMP.text;
        _prevSettingsIconSprite = _settingsIconRenderer.sprite;
        _prevGalleryIconSprite = _galleryIconRenderer.sprite;
    }

    public void ApplyPlayOnline()
    {
        Plugin.Logger.LogInfo("Play Online!");
    }

    public void ShowPlayOnlineMenu(MMContainersController instance) // Modify Options Menu
    {
        _menuHeaderOptionsTMP.text = "Online";
        _settingsOptionTMP.text = "Host";
        _galleryOptionTMP.text = "Join";

        _settingsIconRenderer.sprite = _hostSprite;
        _galleryIconRenderer.sprite = _joinSprite;

        controlsOption.SetActive(false);
        controlsIcon.SetActive(false);
        rulesOption.SetActive(false);
        rulesIcon.SetActive(false);
        creditsOption.SetActive(false);
        creditsIcon.SetActive(false);
        dataOption.SetActive(false);
        dataIcon.SetActive(false);

        moveOnlineOptions = true;

        _shipAnimatorOM.enabled = false;
    }

    public void HidePlayOnlineMenu(MMContainersController instance) // Restore Options Menu
    {
        _shipAnimatorOM.enabled = true;

        moveOnlineOptions = false;

        dataIcon.SetActive(true);
        dataOption.SetActive(true);
        creditsIcon.SetActive(true);
        creditsOption.SetActive(true);
        rulesIcon.SetActive(true);
        rulesOption.SetActive(true);
        controlsIcon.SetActive(true);
        controlsOption.SetActive(true);

        _settingsIconRenderer.sprite = _prevSettingsIconSprite;
        _galleryIconRenderer.sprite = _prevGalleryIconSprite;

        _galleryOptionTMP.text = _prevGalleryOptionText;
        _settingsOptionTMP.text = _prevSettingsOptionText;
        _menuHeaderOptionsTMP.text = _prevOptionsMenuHeaderText;
    }
}