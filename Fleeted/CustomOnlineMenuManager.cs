using System.Collections;
using System.IO;
using System.Reflection;
using Fleeted.patches;
using Fleeted.utils;
using TMPro;
using UnityEngine;

namespace Fleeted;

public class CustomOnlineMenuManager : MonoBehaviour
{
    public static CustomOnlineMenuManager Instance;

    public GameObject canvas;
    public GameObject icons;
    public GameObject menuHeader;
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
    public GameObject shipCursor;

    public bool moveOnlineOptions;

    public Sprite hostSprite;
    public Sprite joinSprite;

    private readonly TimedAction _delayCorrection = new(0.25f);
    private bool _delayCorrectionFlag;
    private SpriteRenderer _galleryIconRenderer;
    private TextMeshProUGUI _galleryTMP;

    private TextMeshProUGUI _menuHeaderTMP;

    private Sprite _prevGalleryIconSprite;
    private Sprite _prevSettingsIconSprite;

    private SpriteRenderer _settingsIconRenderer;
    private TextMeshProUGUI _settingsTMP;

    private Animator _shipAnimator;

    private void Awake()
    {
        Instance = this;

        using var hostIconResource =
            Assembly.GetExecutingAssembly().GetManifestResourceStream("Fleeted.assets.host_icon.png");
        using var joinIconResource =
            Assembly.GetExecutingAssembly().GetManifestResourceStream("Fleeted.assets.join_icon.png");

        using var hostIconResourceMemory = new MemoryStream();
        hostIconResourceMemory.SetLength(0);
        hostIconResource.CopyTo(hostIconResourceMemory);
        var imageBytes = hostIconResourceMemory.ToArray();
        Texture2D hostTex2D = new Texture2D(2, 2);
        hostTex2D.LoadImage(imageBytes);
        hostSprite = Sprite.Create(hostTex2D, new Rect(0, 0, hostTex2D.width, hostTex2D.height), Vector2.zero, 50f);

        using var joinIconResourceMemory = new MemoryStream();
        joinIconResourceMemory.SetLength(0);
        joinIconResource.CopyTo(joinIconResourceMemory);
        imageBytes = joinIconResourceMemory.ToArray();
        Texture2D joinTex2D = new Texture2D(2, 2);
        joinTex2D.LoadImage(imageBytes);
        joinSprite = Sprite.Create(joinTex2D, new Rect(0, 0, joinTex2D.width, joinTex2D.height), Vector2.zero, 50f);
    }

    public void ApplyTransforms()
    {
        if (GlobalController.globalController.screen == GlobalController.screens.optionsmenu)
        {
            if (_delayCorrectionFlag)
            {
                _delayCorrection.Start();
                _delayCorrectionFlag = false;
            }

            if (!_delayCorrection.TrueDone())
                return;

            if (moveOnlineOptions)
            {
                var menuBoatScalar = (ManageInputOnlinePatch.OnlineSelection - 1) * 6;

                CustomMainMenuManager.Instance.shipCursorTarget = new Vector3(-20.5f, 3.35f - menuBoatScalar);

                if ((shipCursor.transform.position - CustomMainMenuManager.Instance.shipCursorTarget)
                    .sqrMagnitude > 0.005f)
                {
                    shipCursor.transform.position = Vector3.MoveTowards(shipCursor.transform.position,
                        CustomMainMenuManager.Instance.shipCursorTarget, 75f * Time.deltaTime);
                }

                backOption.transform.position = new Vector3(7.8f, -8.8f, 0);
                canvas.transform.position = new Vector3(0, -10, 0);
                backIcon.transform.position = new Vector3(-16, -8.8f, 0);
                icons.transform.position = new Vector3(0, -10, 0);
                settingsIcon.transform.position = new Vector3(-18.3f, 1.1f, 0);
                settingsIcon.transform.localScale = Vector3.one;
                galleryIcon.transform.position = new Vector3(-18.5f, -5, 0);
                galleryIcon.transform.localScale = Vector3.one;
            }
            else
            {
                galleryIcon.transform.localScale = Vector3.one * 0.8f;
                galleryIcon.transform.position = new Vector3(-16, 7.09f, 0);
                settingsIcon.transform.localScale = Vector3.one * 0.8f;
                settingsIcon.transform.position = new Vector3(-16, 13.11f, 0);
                icons.transform.position = new Vector3(0, 0, 0);
                backIcon.transform.position = new Vector3(-16, -23, 0);
                canvas.transform.position = new Vector3(0, 0, 0);
                backOption.transform.position = new Vector3(7.8f, -22.8f, 0);
            }
        }
        else
        {
            _delayCorrectionFlag = true;
        }
    }

    public void MapMenu()
    {
        canvas = GameObject.Find("Options/OptionsMenu/Canvas");
        icons = GameObject.Find("Options/OptionsMenu/Icons");
        menuHeader = GameObject.Find("/Options/OptionsMenu/Canvas/Options");
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
        shipCursor = GameObject.Find("Options/OptionsMenu/ShipContainer");

        _menuHeaderTMP = menuHeader.GetComponent<TextMeshProUGUI>();
        _shipAnimator = shipCursor.GetComponent<Animator>();
        _settingsTMP = settingsOption.GetComponent<TextMeshProUGUI>();
        _galleryTMP = galleryOption.GetComponent<TextMeshProUGUI>();
        _settingsIconRenderer = settingsIcon.GetComponent<SpriteRenderer>();
        _galleryIconRenderer = galleryIcon.GetComponent<SpriteRenderer>();
    }

    public void SaveMenuSpace()
    {
        _prevSettingsIconSprite = _settingsIconRenderer.sprite;
        _prevGalleryIconSprite = _galleryIconRenderer.sprite;
    }

    public void ShowPlayOnlineMenu() // Modify Options Menu
    {
        _menuHeaderTMP.text = "Online";
        _settingsTMP.text = "Host";
        _galleryTMP.text = "Join";

        _settingsIconRenderer.sprite = hostSprite;
        _galleryIconRenderer.sprite = joinSprite;

        controlsOption.SetActive(false);
        controlsIcon.SetActive(false);
        rulesOption.SetActive(false);
        rulesIcon.SetActive(false);
        creditsOption.SetActive(false);
        creditsIcon.SetActive(false);
        dataOption.SetActive(false);
        dataIcon.SetActive(false);

        moveOnlineOptions = true;

        _shipAnimator.enabled = false;
    }

    public IEnumerator HidePlayOnlineMenu() // Restore Options Menu
    {
        yield return new WaitForSeconds(0.2f);

        _shipAnimator.enabled = true;

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
    }
}