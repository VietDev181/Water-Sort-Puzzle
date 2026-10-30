using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Entry point of the game. Builds the canvas, the shader driven background and the screen flow:
/// Menu (difficulty) -> Level list -> Game.
/// </summary>
public class WaterSortApp : MonoBehaviour
{
    private static readonly int ProgressId = Shader.PropertyToID("_Progress");

    [Header("Services")]
    [SerializeField] private AudioService audioService;

    [Header("Shaders (referenced so they are included in builds)")]
    [SerializeField] private Shader[] shaders;

    [Header("Sound effects")]
    [SerializeField] private AudioClip popClip;
    [SerializeField] private AudioClip pourClip;
    [SerializeField] private AudioClip completeClip;
    [SerializeField] private AudioClip winClip;

    public ISaveSystem Save { get; private set; }
    public IAudioService Audio => audioService;
    public RectTransform OverlayRoot { get; private set; }
    public bool SoundOn => audioService.GetBGMVolume() > 0.01f;

    private RectTransform _canvasRect;
    private RectTransform _stage;
    private RawImage _fader;
    private ScreenBase _current;
    private bool _transitioning;
    private int _music = -1;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        ShaderLibrary.Register(shaders);
        Save = new SaveSystem();

        PressableButton.ClickSound = () => audioService.PlayClickSFX();

        BuildCanvas();
        ShowInstant(() => ShowMenuInternal());
    }

    // ---------------------------------------------------------------- scaffolding

    private void BuildCanvas()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        _canvasRect = (RectTransform)canvasGo.transform;

        // Full-canvas animated background.
        var bg = UIKit.Quad("Background", _canvasRect, ShaderLibrary.Background, Vector2.zero, Vector2.zero);
        UIKit.Stretch(bg.rectTransform);

        // Screens are designed for a 1080x1920 stage that is centred in the (possibly larger) canvas.
        _stage = UIKit.Rect("Stage", _canvasRect, new Vector2(1080, 1920), Vector2.zero);

        OverlayRoot = UIKit.Rect("Overlay", _canvasRect, Vector2.zero, Vector2.zero);
        UIKit.Stretch(OverlayRoot);

        _fader = UIKit.Quad("Transition", _canvasRect, ShaderLibrary.Transition, Vector2.zero, Vector2.zero);
        UIKit.Stretch(_fader.rectTransform);
        _fader.material.SetFloat(ProgressId, 1f);
        QualitySettings.vSyncCount = 0;
    }

    // ---------------------------------------------------------------- navigation

    public void ShowMenu() => GoTo(ShowMenuInternal);
    public void ShowLevels(Difficulty difficulty) => GoTo(() => ShowLevelsInternal(difficulty));
    public void ShowGame(Difficulty difficulty, int level) => GoTo(() => ShowGameInternal(difficulty, level));

    private void ShowMenuInternal()
    {
        SwitchMusic(false);
        var screen = ScreenBase.Create<MenuScreen>(this, _stage);
        screen.Build();
        SetCurrent(screen);
    }

    private void ShowLevelsInternal(Difficulty difficulty)
    {
        SwitchMusic(false);
        var screen = ScreenBase.Create<LevelSelectScreen>(this, _stage);
        screen.Build(difficulty);
        SetCurrent(screen);
    }

    private void ShowGameInternal(Difficulty difficulty, int level)
    {
        SwitchMusic(true);
        var screen = ScreenBase.Create<GameScreen>(this, _stage);
        screen.Build(difficulty, level);
        SetCurrent(screen);
    }

    private void SetCurrent(ScreenBase screen)
    {
        _current = screen;
    }

    private void ClearCurrent()
    {
        if (_current != null)
            Destroy(_current.gameObject);
        _current = null;
    }

    private void GoTo(Action build)
    {
        if (_transitioning) return;
        _transitioning = true;
        _fader.enabled = true;
        _fader.raycastTarget = true;

        DOVirtual.Float(0f, 1f, 0.38f, v => _fader.material.SetFloat(ProgressId, v))
            .SetEase(Ease.InQuad)
            .SetLink(gameObject)
            .OnComplete(() =>
            {
                ClearCurrent();
                build();
                Reveal();
            });
    }

    private void ShowInstant(Action build)
    {
        _transitioning = true;
        _fader.enabled = true;
        build();
        Reveal();
    }

    private void Reveal()
    {
        DOVirtual.Float(1f, 0f, 0.5f, v => _fader.material.SetFloat(ProgressId, v))
            .SetEase(Ease.OutQuad)
            .SetDelay(0.05f)
            .SetLink(gameObject)
            .OnComplete(() =>
            {
                _transitioning = false;
                _fader.raycastTarget = false;
                _fader.enabled = false;
            });
    }

    // ---------------------------------------------------------------- audio helpers

    private void SwitchMusic(bool game)
    {
        int wanted = game ? 1 : 0;
        if (_music == wanted) return;
        _music = wanted;
        if (game) audioService.PlayGameBGM();
        else audioService.PlayMainMenuBGM();
    }

    public void PlayPop() => audioService.PlaySFX(popClip, 0.8f);
    public void PlayPour() => audioService.PlaySFX(pourClip != null ? pourClip : popClip, 0.7f);
    public void PlayComplete() => audioService.PlaySFX(completeClip, 0.9f);
    public void PlayWin() => audioService.PlaySFX(winClip != null ? winClip : completeClip, 1f);

    public void ToggleSound()
    {
        float value = SoundOn ? 0.0001f : 1f;
        audioService.SetBGMVolume(value);
        audioService.SetSFXVolume(value);
    }
}
