using System;
using UnityEngine;
using UnityEngine.UI;

namespace Fleeted;

public class CustomPlayMenuButtons : MonoBehaviour
{
    public GameObject copyBg;
    public GameObject copyIcon;
    public GameObject eyeBg;
    public GameObject eyeIcon;
    public GameObject arrowsGroup;

    public float x = 0.027f;
    private readonly GameObject[] _arrowsRoomCode = new GameObject[6];

    private Camera _renderCamera;
    private bool _showArrows;

    private void Awake()
    {
        _renderCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
        CustomLobbyMenu.Instance.canvasCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        transform.SetParent(CustomLobbyMenu.Instance.canvas.transform, false);

        arrowsGroup = new GameObject("Arrows");
        arrowsGroup.transform.SetParent(transform, false);

        UpdateArrows();

        copyBg = new GameObject("Copy Button Background");
        copyBg.transform.SetParent(transform, false);

        copyIcon = new GameObject("Copy Icon");
        copyIcon.transform.SetParent(copyBg.transform, false);

        eyeBg = new GameObject("Eye Button Background");
        eyeBg.transform.SetParent(transform, false);

        eyeIcon = new GameObject("Eye Icon");
        eyeIcon.transform.SetParent(eyeBg.transform, false);


        var copyImageBg = copyBg.AddComponent<Image>();
        var copyButton = copyBg.AddComponent<Button>();
        var copyRectBg = copyBg.GetComponent<RectTransform>();

        var copyImageIcon = copyIcon.AddComponent<Image>();
        var copyRectIcon = copyIcon.GetComponent<RectTransform>();

        var eyeImageBg = eyeBg.AddComponent<Image>();
        var eyeButton = eyeBg.AddComponent<Button>();
        var eyeRectBg = eyeBg.GetComponent<RectTransform>();

        var eyeImageIcon = eyeIcon.AddComponent<Image>();
        var eyeRectIcon = eyeIcon.GetComponent<RectTransform>();

        copyBg.transform.localScale = Vector3.one;
        copyImageBg.sprite = CustomLobbyMenu.Instance.buttonBgSprite;
        copyButton.targetGraphic = copyImageBg;
        copyRectBg.sizeDelta = new Vector2(58f, 50f);
        copyButton.onClick.AddListener(OnCopyClick);

        copyImageIcon.sprite = CustomLobbyMenu.Instance.copySprite;
        copyRectIcon.sizeDelta = new Vector2(60f, 50f);

        eyeBg.transform.localScale = Vector3.one;
        eyeImageBg.sprite = CustomLobbyMenu.Instance.buttonBgSprite;
        eyeButton.targetGraphic = eyeImageBg;
        eyeRectBg.sizeDelta = new Vector2(58f, 50);
        eyeButton.onClick.AddListener(OnEyeClick);

        eyeImageIcon.sprite = CustomLobbyMenu.Instance.eyeSprite;
        eyeRectIcon.sizeDelta = new Vector2(60f, 50f);
    }

    private void Start()
    {
        Cursor.visible = true;
    }

    private void Update()
    {
        if (GlobalController.globalController.screen != GlobalController.screens.playmenu)
        {
            Destroy(gameObject);
        }

        copyBg.transform.position = _renderCamera.WorldToScreenPoint(new Vector3(999.885f, -0.115f, 0));
        eyeBg.transform.position = _renderCamera.WorldToScreenPoint(new Vector3(999.95f, -0.115f, 0));

        for (int i = 0; i < _arrowsRoomCode.Length; i++)
        {
            _arrowsRoomCode[i].transform.position =
                _renderCamera.WorldToScreenPoint(new Vector3(1000f + i * x, -0.115f, 0));
        }

        var smallerDimension = Screen.width > Screen.height ? Screen.height : Screen.width;
        CustomLobbyMenu.Instance.canvasCanvas.scaleFactor = smallerDimension / 610f;
    }

    private void OnDestroy()
    {
        CustomLobbyMenu.Instance.canvasCanvas.renderMode = RenderMode.WorldSpace;
        CustomLobbyMenu.Instance.canvas.transform.position = Vector3.zero;
        CustomLobbyMenu.Instance.canvas.transform.localScale = Vector3.one * 0.1f;
        CustomLobbyMenu.Instance.canvasCanvas.scaleFactor = 1f;
        Cursor.visible = false;
    }


    private void OnCopyClick()
    {
        var toArrowSymbols = LobbyManager.Instance.joinArrowCode;
        toArrowSymbols = toArrowSymbols.Replace("D", "↓");
        toArrowSymbols = toArrowSymbols.Replace("L", "←");
        toArrowSymbols = toArrowSymbols.Replace("R", "→");
        toArrowSymbols = toArrowSymbols.Replace("U", "↑");

        GUIUtility.systemCopyBuffer = toArrowSymbols;
        Plugin.Logger.LogInfo($"To clipboard: {toArrowSymbols}");
    }

    private void OnEyeClick()
    {
        _showArrows = !_showArrows;

        UpdateArrows();
    }

    private void UpdateArrows()
    {
        var arrows = LobbyManager.Instance.joinArrowCode;
        int line = 0;
        foreach (var arrow in arrows)
        {
            if (_arrowsRoomCode[line] == null)
            {
                var newArrow = new GameObject("Arrow Join");
                newArrow.transform.SetParent(arrowsGroup.transform);
                newArrow.AddComponent<Image>();
                newArrow.GetComponent<RectTransform>().sizeDelta = new Vector2(40.8f, 34f);
                _arrowsRoomCode[line] = newArrow;
            }

            if (_showArrows)
            {
                _arrowsRoomCode[line].GetComponent<Image>().sprite = arrow switch
                {
                    'D' => ArrowJoinInput.Instance.downSprite,
                    'L' => ArrowJoinInput.Instance.leftSprite,
                    'R' => ArrowJoinInput.Instance.rightSprite,
                    'U' => ArrowJoinInput.Instance.upSprite,
                    _ => throw new ArgumentOutOfRangeException(), // How???
                };
                _arrowsRoomCode[line].GetComponent<RectTransform>().sizeDelta = new Vector2(20f, 16.66666f);
            }
            else
            {
                _arrowsRoomCode[line].GetComponent<Image>().sprite = CustomLobbyMenu.Instance.pointSprite;
                _arrowsRoomCode[line].GetComponent<RectTransform>().sizeDelta = new Vector2(40.8f, 34f);
            }

            line++;
        }
    }
}