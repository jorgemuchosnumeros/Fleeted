using System.Reflection;
using Steamworks.Data;
using TMPro;
using UnityEngine;

namespace Fleeted;

public class InGameNetManager : MonoBehaviour
{
    public static InGameNetManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void StartGame(Lobby lobby)
    {
        if (lobby.GetData("GameStarted") != "yes") return;

        var smcInstance = FindObjectOfType<StageMenuController>();

        //StartGame();
        typeof(StageMenuController).GetMethod("StartGame", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(smcInstance, new object[] { });
    }

    public void ConnectingMessage(bool show)
    {
        if (show)
        {
            var connecting = new GameObject("Connecting Info");
            connecting.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var message = new GameObject("Message");
            message.transform.SetParent(connecting.transform);
            message.AddComponent<TextMeshProUGUI>().text = "Waiting for other Players...";

            message.transform.position = new Vector3(Screen.width / 2, Screen.height / 2);
            message.transform.localScale = new Vector3(0.8f, 0.8f);
        }
        else
        {
            var connecting = GameObject.Find("Connecting Info");
            if (connecting != null)
            {
                Destroy(connecting);
            }
        }
    }
}