using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Scrollable list of the levels of one difficulty.</summary>
public class LevelSelectScreen : ScreenBase
{
    private const int Columns = 5;
    private const float Tile = 168f;
    private const float Gap = 30f;
    private const float ViewportHeight = 1560f;

    private Difficulty _difficulty;

    public void Build(Difficulty difficulty)
    {
        _difficulty = difficulty;
        Palette.ForDifficulty(difficulty, out var top, out var bottom);

        // Header
        var back = UIKit.Button(Rt, "Back", new Vector2(130, 130), new Vector2(-420, 800), Palette.Blue, Palette.DeepBlue, 65f, 0.6f);
        UIKit.Icon(back.transform, UIKit.IconKind.Back, new Vector2(78, 78), new Vector2(-3, 6), Color.white);
        back.Clicked += () => App.ShowMenu();

        var title = UIKit.Label(Rt, LevelCatalog.Title(difficulty), 150, new Vector2(700, 190), new Vector2(40, 805),
            Color.Lerp(top, Color.white, 0.55f), bottom, TextAnchor.MiddleCenter, true, 1f);
        Pop(title.rectTransform, 0.05f);

        var sub = UIKit.Label(Rt, "Choose a level", 46, new Vector2(700, 70), new Vector2(40, 690),
            Color.white, new Color(0.75f, 0.92f, 1f), TextAnchor.MiddleCenter, true);
        Pop(sub.rectTransform, 0.15f);

        BuildList(top, bottom);
    }

    private static void Pop(RectTransform rt, float delay)
    {
        rt.localScale = Vector3.zero;
        rt.DOScale(1f, 0.5f).SetDelay(delay).SetEase(Ease.OutBack).SetLink(rt.gameObject);
    }

    private void BuildList(Color top, Color bottom)
    {
        int count = LevelCatalog.LevelsPerDifficulty;
        int rows = Mathf.CeilToInt(count / (float)Columns);
        float step = Tile + Gap;
        float contentHeight = 60f + rows * step + 60f;

        var viewport = UIKit.Rect("Viewport", Rt, new Vector2(1080, ViewportHeight), new Vector2(0, -150));
        viewport.gameObject.AddComponent<RectMask2D>();
        var catcher = viewport.gameObject.AddComponent<RawImage>();
        catcher.color = new Color(0, 0, 0, 0);

        var content = UIKit.Rect("Content", viewport, new Vector2(1080, contentHeight), Vector2.zero);
        content.anchorMin = content.anchorMax = new Vector2(0.5f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;

        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.scrollSensitivity = 60f;
        scroll.inertia = true;
        scroll.decelerationRate = 0.12f;

        int unlocked = App.Save.GetUnlockedLevel(_difficulty);
        int currentRow = 0;

        for (int level = 1; level <= count; level++)
        {
            int index = level - 1;
            int row = index / Columns;
            int col = index % Columns;

            float x = (col - (Columns - 1) * 0.5f) * step;
            float y = -(60f + Tile * 0.5f + row * step);
            bool isUnlocked = level <= unlocked;
            int stars = App.Save.GetStars(_difficulty, level);
            bool isCurrent = isUnlocked && stars == 0;
            if (isCurrent && currentRow == 0) currentRow = row;

            CreateTile(content, level, new Vector2(x, y), isUnlocked, stars, isCurrent, top, bottom, index);
        }

        float targetY = Mathf.Clamp(currentRow * step - 220f, 0f, Mathf.Max(0f, contentHeight - ViewportHeight));
        content.anchoredPosition = new Vector2(0, targetY);
    }

    private void CreateTile(RectTransform content, int level, Vector2 pos, bool unlocked, int stars, bool current,
        Color top, Color bottom, int order)
    {
        // Tile positions are relative to the content top, so convert to the content's centre anchored space.
        var tileTop = unlocked ? top : new Color(0.45f, 0.55f, 0.72f);
        var tileBottom = unlocked ? bottom : new Color(0.28f, 0.36f, 0.55f);

        var button = UIKit.Button(content, "Level " + level, new Vector2(Tile, Tile), Vector2.zero, tileTop, tileBottom, 40f, unlocked ? 1f : 0f);
        var rt = (RectTransform)button.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos;

        if (unlocked)
        {
            UIKit.Label(rt, level.ToString(), 78, new Vector2(Tile, 100), new Vector2(0, 22),
                Color.white, new Color(1f, 1f, 0.92f), TextAnchor.MiddleCenter, true);

            UIKit.Icon(rt, UIKit.IconKind.Stars, new Vector2(120, 40), new Vector2(0, -46), Color.white, false, stars);
        }
        else
        {
            UIKit.Icon(rt, UIKit.IconKind.Lock, new Vector2(78, 78), new Vector2(0, 2), new Color(0.82f, 0.88f, 0.98f));
        }

        button.SetInteractable(unlocked);
        button.Clicked += () => App.ShowGame(_difficulty, level);
        button.Rejected += () => rt.DOShakeAnchorPos(0.3f, new Vector2(14f, 0f), 20, 0f, false, true).SetLink(rt.gameObject);

        // Intro pop, staggered by row.
        button.Pop = 0f;
        float delay = 0.1f + (order / Columns) * 0.04f + (order % Columns) * 0.02f;
        DOTween.To(() => button.Pop, v => button.Pop = v, 1f, 0.4f).SetDelay(delay).SetEase(Ease.OutBack).SetLink(button.gameObject);

        if (current)
        {
            // Draw attention to the next level to play.
            DOTween.To(() => button.Pop, v => button.Pop = v, 1.07f, 0.7f)
                .SetDelay(1f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(button.gameObject);
        }
    }
}
