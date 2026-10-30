using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Title screen: game name plus the three difficulty cards.</summary>
public class MenuScreen : ScreenBase
{
    private PressableButton _soundButton;
    private RawImage _soundIcon;

    public void Build()
    {
        BuildTitle();
        BuildDecorTubes();
        BuildCards();
        BuildSoundButton();
    }

    private void BuildTitle()
    {
        var top = UIKit.Label(Rt, "WATER SORT", 128, new Vector2(1060, 160), new Vector2(0, 690),
            Color.white, new Color(0.70f, 0.90f, 1f), TextAnchor.MiddleCenter, true, 1f);
        var bottom = UIKit.Label(Rt, "PUZZLE", 210, new Vector2(1060, 240), new Vector2(0, 530),
            new Color(1f, 0.97f, 0.55f), new Color(1f, 0.55f, 0.12f), TextAnchor.MiddleCenter, true, 1f);

        Intro((RectTransform)top.transform, 0.05f, 120f);
        Intro((RectTransform)bottom.transform, 0.2f, 120f);

        // Gentle breathing on the main title.
        bottom.rectTransform.DOScale(1.05f, 1.6f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetDelay(1f).SetLink(bottom.gameObject);
    }

    private void Intro(RectTransform rt, float delay, float offset)
    {
        var start = rt.anchoredPosition;
        rt.anchoredPosition = start + new Vector2(0, offset);
        rt.localScale = Vector3.one * 0.7f;
        rt.DOAnchorPos(start, 0.7f).SetDelay(delay).SetEase(Ease.OutBack).SetLink(rt.gameObject);
        rt.DOScale(1f, 0.7f).SetDelay(delay).SetEase(Ease.OutBack).SetLink(rt.gameObject);
    }

    private void BuildDecorTubes()
    {
        var holder = UIKit.Rect("Decor", Rt, Vector2.zero, Vector2.zero);
        int[][] fills =
        {
            new[] { 1, 1, 6, 6 },
            new[] { 0, 0, 2 },
            new[] { 3, 3, 3, 4 },
            new[] { 5, 7, 7 },
            new[] { 4, 4, 0, 0 },
        };

        float w = 170f;
        for (int i = 0; i < fills.Length; i++)
        {
            float x = (i - 2) * 195f;
            float y = 195f + (i % 2 == 0 ? 0f : -12f);
            var tube = TubeView.Create(holder, w, new Vector2(x, y), i);
            tube.GetComponent<RawImage>().raycastTarget = false;
            tube.SetUnits(fills[i]);
            tube.StartSway(3.5f, 2.2f + i * 0.25f, i * 0.2f);

            tube.Rect.localScale = Vector3.zero;
            tube.Rect.DOScale(1f, 0.6f).SetDelay(0.3f + i * 0.08f).SetEase(Ease.OutBack).SetLink(tube.gameObject);
        }
    }

    private void BuildCards()
    {
        var label = UIKit.Label(Rt, "SELECT DIFFICULTY", 60, new Vector2(1000, 90), new Vector2(0, -105),
            Color.white, new Color(0.75f, 0.92f, 1f), TextAnchor.MiddleCenter, true, 0.6f);
        label.rectTransform.localScale = Vector3.zero;
        label.rectTransform.DOScale(1f, 0.5f).SetDelay(0.6f).SetEase(Ease.OutBack).SetLink(label.gameObject);

        var values = new[] { Difficulty.Easy, Difficulty.Medium, Difficulty.Hard };
        for (int i = 0; i < values.Length; i++)
        {
            var difficulty = values[i];
            Palette.ForDifficulty(difficulty, out var top, out var bottom);

            var button = UIKit.Button(Rt, difficulty + " card", new Vector2(880, 214), new Vector2(0, -290 - i * 262), top, bottom, 70f);
            var rt = (RectTransform)button.transform;

            LevelCatalog.ColorRange(difficulty, out int min, out int max);
            int cleared = App.Save.GetClearedCount(difficulty);

            UIKit.Label(rt, LevelCatalog.Title(difficulty), 92, new Vector2(520, 120), new Vector2(-150, 28),
                Color.white, new Color(1f, 1f, 1f), TextAnchor.MiddleCenter, true, 0.8f);
            UIKit.Label(rt, $"{min}-{max} colors", 40, new Vector2(520, 60), new Vector2(-150, -52),
                new Color(1f, 1f, 0.9f), new Color(1f, 0.95f, 0.8f), TextAnchor.MiddleCenter, true);

            UIKit.Label(rt, $"{cleared}/{LevelCatalog.LevelsPerDifficulty}", 76, new Vector2(300, 100), new Vector2(285, 26),
                Color.white, new Color(1f, 0.97f, 0.7f), TextAnchor.MiddleCenter, true);
            UIKit.Label(rt, "CLEARED", 32, new Vector2(300, 50), new Vector2(285, -48),
                new Color(1f, 1f, 0.9f), new Color(1f, 0.95f, 0.8f), TextAnchor.MiddleCenter, true);

            button.Clicked += () => App.ShowLevels(difficulty);

            button.Pop = 0f;
            DOTween.To(() => button.Pop, v => button.Pop = v, 1f, 0.55f)
                .SetDelay(0.7f + i * 0.12f).SetEase(Ease.OutBack).SetLink(button.gameObject);
        }
    }

    private void BuildSoundButton()
    {
        _soundButton = UIKit.Button(Rt, "Sound", new Vector2(120, 120), new Vector2(440, 850),
            Palette.Blue, Palette.DeepBlue, 60f, 0.6f);
        _soundIcon = UIKit.Icon(_soundButton.transform, App.SoundOn ? UIKit.IconKind.SoundOn : UIKit.IconKind.SoundOff,
            new Vector2(76, 76), new Vector2(0, 6), Color.white, true);
        _soundButton.Clicked += () =>
        {
            App.ToggleSound();
            _soundIcon.material.SetFloat("_Kind", App.SoundOn ? (float)UIKit.IconKind.SoundOn : (float)UIKit.IconKind.SoundOff);
        };
    }
}
