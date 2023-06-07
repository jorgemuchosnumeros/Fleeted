using System;
using System.Collections.Generic;
using System.Linq;
using Fleeted.utils;
using UnityEngine;

namespace Fleeted;

public enum Arrows
{
    Down,
    Left,
    Right,
    Up,
}

public class ArrowJoinInput : MonoBehaviour
{
    public static ArrowJoinInput Instance;
    public bool isInputLocked;

    public Sprite downSprite;
    public Sprite leftSprite;
    public Sprite rightSprite;
    public Sprite upSprite;
    private readonly GameObject[] _arrowsGameObjects = new GameObject[6];

    private readonly Stack<Arrows> _arrowsInput = new();
    private GameState _gameState;

    private void Awake()
    {
        Instance = this;

        downSprite = SpritesExtra.SpriteFromName("Fleeted.assets.arrow_down_icon.png");
        leftSprite = SpritesExtra.SpriteFromName("Fleeted.assets.arrow_left_icon.png");
        rightSprite = SpritesExtra.SpriteFromName("Fleeted.assets.arrow_right_icon.png");
        upSprite = SpritesExtra.SpriteFromName("Fleeted.assets.arrow_up_icon.png");
    }

    private void LateUpdate()
    {
        if (!isInputLocked) return;

        if (GlobalController.globalController.screen != GlobalController.screens.optionsmenu)
        {
            ClearInput();
            return;
        }

        var isTypingAnArrow = InputController.inputController.inputs["1DDown"] ||
                              InputController.inputController.inputsRaw["0DDown"] ||
                              InputController.inputController.inputs["1LDown"] ||
                              InputController.inputController.inputsRaw["0LDown"] ||
                              InputController.inputController.inputs["1RDown"] ||
                              InputController.inputController.inputsRaw["0RDown"] ||
                              InputController.inputController.inputs["1UDown"] ||
                              InputController.inputController.inputsRaw["0UDown"];

        if (_arrowsInput.Count >= 6 && isTypingAnArrow)
        {
            GlobalAudio.globalAudio.PlayInvalid();
        }
        else if (InputController.inputController.inputs["1DDown"] ||
                 InputController.inputController.inputsRaw["0DDown"])
        {
            _arrowsInput.Push(Arrows.Down);
            PlayCharaFrameSound(_gameState);
        }
        else if (InputController.inputController.inputs["1LDown"] ||
                 InputController.inputController.inputsRaw["0LDown"])
        {
            _arrowsInput.Push(Arrows.Left);
            PlayCharaFrameSound(_gameState);
        }
        else if (InputController.inputController.inputs["1RDown"] ||
                 InputController.inputController.inputsRaw["0RDown"])
        {
            _arrowsInput.Push(Arrows.Right);
            PlayCharaFrameSound(_gameState);
        }
        else if (InputController.inputController.inputs["1UDown"] ||
                 InputController.inputController.inputsRaw["0UDown"])
        {
            _arrowsInput.Push(Arrows.Up);
            PlayCharaFrameSound(_gameState);
        }

        if (InputController.inputController.inputs["1BDown"] ||
            InputController.inputController.inputsRaw["0BDown"])
        {
            isInputLocked = false;
            ClearInput();
            GlobalAudio.globalAudio.PlayCancel();
        }

        if (InputController.inputController.inputsRaw["1ADown"] || InputController.inputController.inputsRaw["0ADown"])
        {
            LobbyManager.Instance.JoinByArrows(_arrowsInput.Reverse().ToArray());
            GlobalAudio.globalAudio.PlayAccept();
        }

        if (isTypingAnArrow)
        {
            CreateArrows(_arrowsInput.ToArray());
        }
    }

    private void ClearInput()
    {
        CustomOnlineMenu.Instance.GalleryTMP.color =
            CustomSettingsMenu.Instance.settingsControllerInstance.enabledColor;
        _arrowsInput.Clear();
        foreach (var arrowsGameObject in _arrowsGameObjects)
        {
            Destroy(arrowsGameObject);
        }
    }

    private void CreateArrows(Arrows[] input)
    {
        var inputR = input.Reverse().ToArray();

        for (int i = 0; i < inputR.Length; i++)
        {
            if (_arrowsGameObjects[i] == null)
            {
                var newArrow = new GameObject("Arrow Join");
                newArrow.transform.SetParent(CustomOnlineMenu.Instance.galleryOption.transform);
                newArrow.AddComponent<SpriteRenderer>();
                _arrowsGameObjects[i] = newArrow;
            }

            _arrowsGameObjects[i].GetComponent<SpriteRenderer>().sprite = inputR[i] switch
            {
                Arrows.Down => downSprite,
                Arrows.Left => leftSprite,
                Arrows.Right => rightSprite,
                Arrows.Up => upSprite,
                _ => throw new ArgumentOutOfRangeException() // How???
            };

            _arrowsGameObjects[i].transform.position = new Vector3(-4 + i * 4.5f, -5.3f, 0);
        }
    }

    private static void PlayCharaFrameSound(GameState instance)
    {
        GlobalAudio.globalAudio.Play(instance.charaFrameSound, 1f, 0.95f, 1);
    }

    public void JoinArrowField()
    {
        isInputLocked = true;
        CustomOnlineMenu.Instance.GalleryTMP.color =
            CustomSettingsMenu.Instance.settingsControllerInstance.disabledColor;
    }

    public void MapJoin()
    {
        _gameState = FindObjectOfType<GameState>();
    }
}