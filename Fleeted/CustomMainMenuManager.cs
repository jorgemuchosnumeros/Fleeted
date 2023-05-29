using System.IO;
using System.Reflection;
using Fleeted.utils;
using TMPro;
using UnityEngine;

namespace Fleeted;

public class CustomMainMenuManager : MonoBehaviour
{
    public static CustomMainMenuManager Instance;
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

    public Vector3 shipCursorTarget;

    private readonly TimedAction _delayCorrectionMm = new(0.25f);
    private readonly TimedAction _delayDisableAnimatorMm = new(0.4f);

    private SpriteRenderer _connectIconRenderer;
    private Sprite _connectSprite;

    private bool _delayCorrectionMMFlag;

    private TextMeshProUGUI _playLocalTMP;
    private TextMeshProUGUI _playOnlineTMP;

    private Animator _shipAnimatorMm;

    public void Awake()
    {
        Instance = this;

        _delayCorrectionMm.Start();

        // Convert "assets/*_icon.png" to Sprites
        using var connectIconResource =
            Assembly.GetExecutingAssembly().GetManifestResourceStream("Fleeted.assets.connect_icon.png");

        using var connectIconResourceMemory = new MemoryStream();
        connectIconResourceMemory.SetLength(0);
        connectIconResource.CopyTo(connectIconResourceMemory);
        var imageBytes = connectIconResourceMemory.ToArray();
        Texture2D connectTex2D = new Texture2D(2, 2);
        connectTex2D.LoadImage(imageBytes);
        _connectSprite = Sprite.Create(connectTex2D, new Rect(0, 0, connectTex2D.width, connectTex2D.height),
            Vector2.zero, 50f);
    }

    private void Update()
    {
        if (!menuSpawned)
            return;

        ApplyMainMenuTransforms();
        CustomOnlineMenuManager.Instance.ApplyOnlineMenuTransforms();
    }

    private void ApplyMainMenuTransforms()
    {
        if (GlobalController.globalController.screen == GlobalController.screens.mainmenu)
        {
            if (_shipAnimatorMm == null)
                return;
            // Disabled the Animator of the Boat Cursor on the Main Menu and Remake the animations to support the 7th Option
            if (mainMenuController.selection > 0)
            {
                if (_shipAnimatorMm.isActiveAndEnabled)
                {
                    if (!_delayDisableAnimatorMm.HasEverStated)
                        _delayDisableAnimatorMm.Start();
                    _shipAnimatorMm.enabled = !_delayDisableAnimatorMm.TrueDone();
                }

                var menuBoatScalar = (mainMenuController.selection - 1) * 4.8f;

                shipCursorTarget = new Vector3(-14.6f, 4.5f - menuBoatScalar);

                if ((shipCursor.transform.position - shipCursorTarget).sqrMagnitude > 0.005f)
                {
                    shipCursor.transform.position = Vector3.MoveTowards(shipCursor.transform.position,
                        shipCursorTarget, 75f * Time.deltaTime);
                }
            }

            if (_delayCorrectionMMFlag)
            {
                _delayCorrectionMm.Start();
                _delayCorrectionMMFlag = false;
            }

            if (_delayCorrectionMm.TrueDone())
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
        _shipAnimatorMm = shipAnimated.GetComponent<Animator>();
    }

    public void CreateMainMenuSpace()
    {
        _delayDisableAnimatorMm.HasEverStated = false;

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
}