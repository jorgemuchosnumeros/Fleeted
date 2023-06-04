using System.Collections;
using Fleeted.patches;
using Fleeted.utils;
using TMPro;
using UnityEngine;

namespace Fleeted;

public class CustomSettingsMenu : MonoBehaviour
{
    public static CustomSettingsMenu Instance;

    public bool moveOptions;

    public SettingsMenuController settingsControllerInstance;

    public GameObject menuHeader;
    public GameObject resolution;
    public GameObject fullscreen;
    public GameObject apply;
    public GameObject language;
    public GameObject sideArt;
    public GameObject masterVolume;
    public GameObject musicVolume;
    public GameObject soundsVolume;
    public GameObject voicesVolume;
    public GameObject controllerRumble;
    public GameObject resolutionValue;
    public GameObject resolutionRightArrow;
    public GameObject fullscreenValue;
    public GameObject back;
    private TextMeshProUGUI _applyTMP;

    private TimedAction _delayCorrection = new(0.25f);

    private bool _delayCorrectionFlag;

    private TextMeshProUGUI _fullscreenTMP;
    private TextMeshProUGUI _fullscreenValueTMP;
    private TextMeshProUGUI _menuHeaderTMP;
    private string _prevApplyText;
    private string _prevFullscreenText;

    private string _prevMenuHeaderText;
    private string _prevResolutionText;
    private TextMeshProUGUI _resolutionRightArrowTMP;
    private TextMeshProUGUI _resolutionTMP;
    private TextMeshProUGUI _resolutionValueTMP;


    private void Awake()
    {
        Instance = this;
    }

    public void MapMenu()
    {
        settingsControllerInstance = FindObjectOfType<SettingsMenuController>();
        menuHeader = GameObject.Find("Options/Settings/Canvas/Settings");
        resolution = GameObject.Find("Options/Settings/Canvas/Resolution/Label");
        fullscreen = GameObject.Find("Options/Settings/Canvas/Full screen/Label");
        apply = GameObject.Find("Options/Settings/Canvas/Apply");
        language = GameObject.Find("Options/Settings/Canvas/Language");
        sideArt = GameObject.Find("Options/Settings/Canvas/Side art");
        masterVolume = GameObject.Find("Options/Settings/Canvas/Master volume");
        musicVolume = GameObject.Find("Options/Settings/Canvas/Music volume");
        soundsVolume = GameObject.Find("Options/Settings/Canvas/Sounds volume");
        voicesVolume = GameObject.Find("Options/Settings/Canvas/Voices volume");
        controllerRumble = GameObject.Find("Options/Settings/Canvas/Controller rumble");
        resolutionValue = GameObject.Find("Options/Settings/Canvas/Resolution/Value");
        resolutionRightArrow = GameObject.Find("Options/Settings/Canvas/Resolution/>");
        fullscreenValue = GameObject.Find("Options/Settings/Canvas/Full screen/Value");

        back = GameObject.Find("Options/Settings/Canvas/Back");

        _menuHeaderTMP = menuHeader.GetComponent<TextMeshProUGUI>();
        _resolutionTMP = resolution.GetComponent<TextMeshProUGUI>();
        _fullscreenTMP = fullscreen.GetComponent<TextMeshProUGUI>();
        _applyTMP = apply.GetComponent<TextMeshProUGUI>();
        _resolutionValueTMP = resolutionValue.GetComponent<TextMeshProUGUI>();
        _resolutionRightArrowTMP = resolutionRightArrow.GetComponent<TextMeshProUGUI>();
        _fullscreenValueTMP = fullscreenValue.GetComponent<TextMeshProUGUI>();
    }

    public void ApplyTransforms()
    {
        if (GlobalController.globalController.screen == GlobalController.screens.settingsmenu)
        {
            if (_delayCorrectionFlag)
            {
                _delayCorrection.Start();
                _delayCorrectionFlag = false;
            }

            if (!_delayCorrection.TrueDone())
                return;

            back.transform.position = moveOptions ? new Vector3(-10, 4.5f, 0) : new Vector3(-10, -24.6f, 0);
        }
        else
        {
            _delayCorrectionFlag = true;
        }
    }

    public void SaveMenuSpace()
    {
        _prevMenuHeaderText = _menuHeaderTMP.text;
        _prevResolutionText = _resolutionTMP.text;
        _prevFullscreenText = _fullscreenTMP.text;
        _prevApplyText = _applyTMP.text;
    }

    public void ShowLobbySettingsMenu() // Modify Settings Menu
    {
        moveOptions = true;

        CustomOnlineMenu.Instance.ShowPlayOnlineMenu();

        _menuHeaderTMP.text = "Host";
        _resolutionTMP.text = "Max Players";
        _fullscreenTMP.text = "Friends Only";
        _applyTMP.text = "Create Lobby";

        language.SetActive(false);
        sideArt.SetActive(false);
        masterVolume.SetActive(false);
        musicVolume.SetActive(false);
        soundsVolume.SetActive(false);
        voicesVolume.SetActive(false);

        _resolutionValueTMP.text = SettingsMenuControllerPatches.memberLimitSelection.ToString();
        if (_resolutionValueTMP.text == "8")
            _resolutionRightArrowTMP.color = settingsControllerInstance.disabledColor;

        _fullscreenValueTMP.text = SettingsMenuControllerPatches.isFriendsOnly ? "Yes" : "No";

        // FIXME: Probably the controller option will freak out
        // when plugged in, but I dont have the means to test this
    }

    public IEnumerator HideLobbySettingsMenu() // Restore Settings Menu
    {
        yield return new WaitForSeconds(0.2f);

        moveOptions = false;

        CustomOnlineMenu.Instance.ShowPlayOnlineMenu();

        language.SetActive(true);
        sideArt.SetActive(true);
        masterVolume.SetActive(true);
        musicVolume.SetActive(true);
        soundsVolume.SetActive(true);
        voicesVolume.SetActive(true);
    }
}