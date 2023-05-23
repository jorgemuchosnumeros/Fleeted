using System.IO;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace Fleeted;

[HarmonyPatch(typeof(MainMenuController), "Initialize")]
public static class SpawnMenuPatch
{
    static void Postfix()
    {
        CustomMenuManager.Instance.CreateSpace();
        CustomMenuManager.Instance.CreateOnlineOption();
        CustomMenuManager.Instance.menuSpawned = true;
    }
}

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

    private TextMeshProUGUI _playLocalTMP;
    private TextMeshProUGUI _playOnlineTMP;
    private Sprite connectSprite;

    private SpriteRenderer iconRenderer;

    public void Awake()
    {
        Instance = this;

        using var connectIconResource =
            Assembly.GetExecutingAssembly().GetManifestResourceStream("Fleeted.assets.connect_icon.png");
        using var resourceMemory = new MemoryStream();
        resourceMemory.SetLength(0);
        connectIconResource.CopyTo(resourceMemory);
        var imageBytes = resourceMemory.ToArray();
        Texture2D tex2D = new Texture2D(2, 2);
        tex2D.LoadImage(imageBytes);
        connectSprite = Sprite.Create(tex2D, new Rect(0, 0, tex2D.width, tex2D.height), Vector2.zero, 50f);
    }

    private void Update()
    {
        if (!menuSpawned)
            return;

        playLocalOption.transform.position = new Vector3(7.364f, 4.64f, 0f);
        _playLocalTMP.text = "Play (Local)";

        playOnlineOption.transform.position = new Vector3(7.364f, -0.36f, 0);
        _playOnlineTMP.text = "Play (Online)";
    }

    public void CreateSpace()
    {
        mainMenu = GameObject.Find("MainMenu");
        title = GameObject.Find("MainMenu/title");
        shipContainer = GameObject.Find("MainMenu/ShipContainer");
        canvas = GameObject.Find("MainMenu/Canvas");
        icons = GameObject.Find("MainMenu/Icons");
        shipsIcon = GameObject.Find("MainMenu/Icons/ships_icon");
        playLocalOption = GameObject.Find("MainMenu/Canvas/Options/Play");

        _playLocalTMP = playLocalOption.GetComponent<TextMeshProUGUI>();

        mainMenu.transform.localScale = Vector3.one * 0.8f;
        title.transform.localScale = Vector3.one * 1.25f;
        shipContainer.transform.localScale = Vector3.one * 1.20f;

        canvas.transform.position = Vector3.up * -5f;
        icons.transform.position = Vector3.up * -5f;
        shipsIcon.transform.position = new Vector3(-10.176f, 4.64f, 0f);
        shipsIcon.transform.localScale = Vector3.one * 1.25f;
    }

    public void CreateOnlineOption()
    {
        playOnlineOption = Instantiate(playLocalOption, playLocalOption.transform.parent);
        netIcon = Instantiate(shipsIcon, shipsIcon.transform.parent);

        _playOnlineTMP = playOnlineOption.GetComponent<TextMeshProUGUI>();
        iconRenderer = netIcon.GetComponent<SpriteRenderer>();

        netIcon.transform.position = new Vector3(-12.44f, -2.3f, 0);
        netIcon.transform.localScale = Vector3.one * 1.05f;
        iconRenderer.sprite = connectSprite;
    }
}