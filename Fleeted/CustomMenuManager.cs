using System.IO;
using System.Reflection;
using Fleeted.utils;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Fleeted;

public class CustomMenuManager : MonoBehaviour
{
    public static CustomMenuManager Instance;
    public bool menuSpawned;

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
    public GameObject options;

    [FormerlySerializedAs("optionsMenuText")]
    public GameObject optionsMenuHeader;

    [FormerlySerializedAs("onlineMenu")] public GameObject online;

    public MainMenuController mainMenuController;

    private readonly TimedAction _delayCorrection = new(0.25f);
    private readonly TimedAction _delayDisableAnimator = new(0.4f);
    private Sprite _connectSprite;
    private SpriteRenderer _iconRenderer;
    private TextMeshProUGUI _optionsMenuHeaderTMP;

    private TextMeshProUGUI _playLocalTMP;
    private TextMeshProUGUI _playOnlineTMP;

    private string _prevOptionsMenuHeaderTMP;

    private Animator _shipAnimator;

    private Vector3 _shipCursorTarget;

    public void Awake()
    {
        Instance = this;

        _delayCorrection.Start();

        // Convert "assets/connect_icon.png" to a Sprite
        using var connectIconResource =
            Assembly.GetExecutingAssembly().GetManifestResourceStream("Fleeted.assets.connect_icon.png");
        using var resourceMemory = new MemoryStream();
        resourceMemory.SetLength(0);
        connectIconResource.CopyTo(resourceMemory);
        var imageBytes = resourceMemory.ToArray();
        Texture2D tex2D = new Texture2D(2, 2);
        tex2D.LoadImage(imageBytes);
        _connectSprite = Sprite.Create(tex2D, new Rect(0, 0, tex2D.width, tex2D.height), Vector2.zero, 50f);
    }

    private void Update()
    {
        if (!menuSpawned)
            return;

        if (GlobalController.globalController.screen == GlobalController.screens.mainmenu)
        {
            if (_shipAnimator == null || shipCursor == null || playLocalOption == null || playOnlineOption == null)
                return;

            // Disabled the Animator of the Boat Cursor on the Main Menu and Remake the animations to support the 7th Option
            if (mainMenuController.selection > 0)
            {
                if (_shipAnimator.isActiveAndEnabled)
                {
                    if (!_delayDisableAnimator.HasEverStated)
                        _delayDisableAnimator.Start();
                    _shipAnimator.enabled = !_delayDisableAnimator.TrueDone();
                }

                var menuBoatScalar = (mainMenuController.selection - 1) * 4.8f;

                _shipCursorTarget = new Vector3(-14.6f, 4.5f - menuBoatScalar);

                if ((shipCursor.transform.position - _shipCursorTarget).sqrMagnitude > 0.005f)
                {
                    shipCursor.transform.position = Vector3.MoveTowards(shipCursor.transform.position,
                        _shipCursorTarget, 75f * Time.deltaTime);
                }
            }

            // Wait To Fully enter to the Main Menu Before Pinning the Custom Options
            if (!_delayCorrection.TrueDone())
                _delayCorrection.Start();

            if (_delayCorrection.TrueDone())
                _delayCorrection.Start();
            else
                return;
        }
        else
        {
            _delayCorrection.TurnOff();
            return;
        }

        // Pinning the Custom Options
        playLocalOption.transform.position = new Vector3(7.364f, 4.64f, 0f);
        _playLocalTMP.text = "Play (Local)";

        playOnlineOption.transform.position = new Vector3(7.364f, -0.36f, 0);
        _playOnlineTMP.text = "Play (Online)";
    }

    public void MapMenuGameObjects()
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
        _shipAnimator = shipAnimated.GetComponent<Animator>();
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
        _iconRenderer = netIcon.GetComponent<SpriteRenderer>();

        netIcon.transform.position = new Vector3(-12.44f, -2.3f, 0);
        netIcon.transform.localScale = Vector3.one * 1.05f;
        _iconRenderer.sprite = _connectSprite;
    }

    public void MapOptionsMenu()
    {
        options = GameObject.Find("/Options");
        optionsMenuHeader = GameObject.Find("/Options/OptionsMenu/Canvas/Options");
        _optionsMenuHeaderTMP = optionsMenuHeader.GetComponent<TextMeshProUGUI>();
    }

    public void SaveOnlineMenuSpace()
    {
        _prevOptionsMenuHeaderTMP = _optionsMenuHeaderTMP.text;
    }

    public void ApplyPlayOnline()
    {
        Plugin.Logger.LogInfo("Play Online!");
    }

    public void ShowPlayOnlineMenu(MMContainersController instance) // Modify Options Menu
    {
        Plugin.Logger.LogInfo("Show Play Online!");

        _optionsMenuHeaderTMP.text = "Online";
    }

    public void HidePlayOnlineMenu(MMContainersController instance) // Restore Options Menu
    {
        Plugin.Logger.LogInfo("Hide Play Online!");

        _optionsMenuHeaderTMP.text = _prevOptionsMenuHeaderTMP;
    }
}