using Fleeted.utils;
using TMPro;
using UnityEngine;

namespace Fleeted;

public class CustomMainMenu : MonoBehaviour
{
    public static CustomMainMenu Instance;
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

    private readonly TimedAction _delayCorrection = new(0.25f);
    private readonly TimedAction _delayDisableAnimator = new(0.4f);

    private SpriteRenderer _connectIconRenderer;
    private Sprite _connectSprite;

    private bool _delayCorrectionFlag;

    private TextMeshProUGUI _playLocalTMP;
    private TextMeshProUGUI _playOnlineTMP;

    private Animator _shipAnimator;

    public void Awake()
    {
        Instance = this;

        _delayCorrection.Start();

        _connectSprite = SpritesExtra.SpriteFromName("Fleeted.assets.connect_icon.png");
    }

    private void Update()
    {
        if (!menuSpawned)
            return;

        ApplyMainMenuTransforms();
        CustomOnlineMenu.Instance.ApplyTransforms();
        CustomSettingsMenu.Instance.ApplyTransforms();
    }

    private void ApplyMainMenuTransforms()
    {
        if (GlobalController.globalController.screen == GlobalController.screens.mainmenu)
        {
            if (_shipAnimator == null)
                return;

            if (mainMenuController.selection > 0)
            {
                if (_shipAnimator.isActiveAndEnabled)
                {
                    if (!_delayDisableAnimator.HasEverStated)
                        _delayDisableAnimator.Start();
                    _shipAnimator.enabled = !_delayDisableAnimator.TrueDone();
                }

                var menuBoatScalar = (mainMenuController.selection - 1) * 4.8f;

                shipCursorTarget = new Vector3(-14.6f, 4.5f - menuBoatScalar);

                if ((shipCursor.transform.position - shipCursorTarget).sqrMagnitude > 0.005f)
                {
                    shipCursor.transform.position = Vector3.MoveTowards(shipCursor.transform.position,
                        shipCursorTarget, 75f * Time.deltaTime);
                }
            }

            if (_delayCorrectionFlag)
            {
                _delayCorrection.Start();
                _delayCorrectionFlag = false;
            }

            if (_delayCorrection.TrueDone())
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
            _delayCorrectionFlag = true;
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

        mainMenuController = FindObjectOfType<MainMenuController>();

        _playLocalTMP = playLocalOption.GetComponent<TextMeshProUGUI>();
        _shipAnimator = shipAnimated.GetComponent<Animator>();
    }

    public void ForceHideMenu(bool hide)
    {
        title.SetActive(!hide);
        shipContainer.SetActive(!hide);
        icons.SetActive(!hide);
        canvas.GetComponent<Canvas>().enabled = !hide;
    }

    public void CreateMainMenuSpace()
    {
        _delayDisableAnimator.HasEverStated = false;

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