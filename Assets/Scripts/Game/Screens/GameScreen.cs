using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>The actual puzzle: tube board, pour animation, undo/restart/extra tube and the win dialog.</summary>
public class GameScreen : ScreenBase
{
    private const float BoardWidth = 1000f;
    private const float BoardHeight = 1240f;
    private static readonly Vector2 BoardCenter = new Vector2(0f, 20f);

    private static readonly int GrowId = Shader.PropertyToID("_Grow");
    private static readonly int TailId = Shader.PropertyToID("_Tail");
    private static readonly int LiquidId = Shader.PropertyToID("_Liquid");
    private static readonly int SizeId = Shader.PropertyToID("_Size");

    private Difficulty _difficulty;
    private int _level;
    private LevelInfo _info;

    private PuzzleState _puzzle;
    private PuzzleState _initial;
    private readonly Stack<PuzzleState> _history = new Stack<PuzzleState>();
    private readonly List<TubeView> _tubes = new List<TubeView>();

    private RectTransform _board;
    private Text _movesLabel;
    private PressableButton _undoButton;
    private PressableButton _addButton;

    private int _selected = -1;
    private int _moves;
    private bool _busy;
    private bool _finished;
    private bool _tubeAdded;

    private RawImage _confetti;
    private readonly List<GameObject> _overlayObjects = new List<GameObject>();

    public void Build(Difficulty difficulty, int level)
    {
        _difficulty = difficulty;
        _level = level;
        _info = LevelCatalog.GetInfo(difficulty, level);

        BuildHeader();
        _board = UIKit.Rect("Board", Rt, new Vector2(BoardWidth, BoardHeight), BoardCenter);
        BuildFooter();

        _initial = LevelCatalog.Generate(difficulty, level);
        ResetLevel();
    }

    private void OnDestroy()
    {
        foreach (var go in _overlayObjects)
            if (go != null) Destroy(go);
    }

    // ---------------------------------------------------------------- UI

    private void BuildHeader()
    {
        Palette.ForDifficulty(_difficulty, out var top, out var bottom);

        var back = UIKit.Button(Rt, "Back", new Vector2(130, 130), new Vector2(-420, 810), Palette.Blue, Palette.DeepBlue, 65f, 0.6f);
        UIKit.Icon(back.transform, UIKit.IconKind.Back, new Vector2(78, 78), new Vector2(-3, 6), Color.white);
        back.Clicked += () => App.ShowLevels(_difficulty);

        var restart = UIKit.Button(Rt, "Restart", new Vector2(130, 130), new Vector2(420, 810), Palette.Blue, Palette.DeepBlue, 65f, 0.6f);
        UIKit.Icon(restart.transform, UIKit.IconKind.Restart, new Vector2(80, 80), new Vector2(0, 6), Color.white);
        restart.Clicked += () => { if (!_busy) ResetLevel(); };

        UIKit.Label(Rt, "LEVEL " + _level, 104, new Vector2(700, 130), new Vector2(0, 835),
            Color.white, new Color(0.72f, 0.92f, 1f), TextAnchor.MiddleCenter, true, 0.7f);
        UIKit.Label(Rt, LevelCatalog.Title(_difficulty), 50, new Vector2(500, 70), new Vector2(0, 750),
            Color.Lerp(top, Color.white, 0.4f), bottom, TextAnchor.MiddleCenter, true);
    }

    private void BuildFooter()
    {
        _movesLabel = UIKit.Label(Rt, "MOVES 0", 54, new Vector2(600, 80), new Vector2(0, -690),
            Color.white, new Color(0.75f, 0.92f, 1f), TextAnchor.MiddleCenter, true);

        _undoButton = UIKit.Button(Rt, "Undo", new Vector2(380, 130), new Vector2(-230, -810),
            new Color(1f, 0.80f, 0.28f), new Color(0.96f, 0.52f, 0.08f), 65f);
        UIKit.Icon(_undoButton.transform, UIKit.IconKind.Undo, new Vector2(80, 80), new Vector2(-105, 6), Color.white);
        UIKit.Label(_undoButton.transform, "UNDO", 56, new Vector2(240, 80), new Vector2(50, 6),
            Color.white, new Color(1f, 0.97f, 0.8f), TextAnchor.MiddleCenter, true);
        _undoButton.Clicked += Undo;
        _undoButton.Rejected += () => ((RectTransform)_undoButton.transform).DOShakeAnchorPos(0.25f, new Vector2(10f, 0f), 18, 0f, false, true).SetLink(gameObject);

        _addButton = UIKit.Button(Rt, "Add tube", new Vector2(380, 130), new Vector2(230, -810),
            new Color(0.50f, 0.93f, 0.40f), new Color(0.12f, 0.64f, 0.28f), 65f);
        UIKit.Icon(_addButton.transform, UIKit.IconKind.Plus, new Vector2(70, 70), new Vector2(-105, 6), Color.white);
        UIKit.Label(_addButton.transform, "TUBE", 56, new Vector2(240, 80), new Vector2(50, 6),
            Color.white, new Color(0.95f, 1f, 0.9f), TextAnchor.MiddleCenter, true);
        _addButton.Clicked += AddTube;
    }

    private void UpdateButtons()
    {
        _undoButton.SetInteractable(_history.Count > 0 && !_busy && !_finished);
        _addButton.SetInteractable(!_tubeAdded && !_busy && !_finished);
        _movesLabel.text = "MOVES " + _moves;
    }

    // ---------------------------------------------------------------- level lifecycle

    private void ResetLevel()
    {
        _puzzle = _initial.Clone();
        _history.Clear();
        _moves = 0;
        _selected = -1;
        _busy = false;
        _finished = false;
        _tubeAdded = false;
        BuildBoard(true);
        UpdateButtons();
    }

    private void BuildBoard(bool animate)
    {
        foreach (var t in _tubes)
            if (t != null) Destroy(t.gameObject);
        _tubes.Clear();

        int n = _puzzle.TubeCount;
        int rows = Mathf.CeilToInt(n / 6f);
        int perRow = Mathf.CeilToInt(n / (float)rows);

        float cellW = BoardWidth / perRow;
        float cellH = BoardHeight / rows;
        float width = Mathf.Min(cellW, cellH / TubeView.Aspect, 240f);

        for (int i = 0; i < n; i++)
        {
            int row = i / perRow;
            int col = i % perRow;
            int inThisRow = Mathf.Min(perRow, n - row * perRow);

            float x = (col - (inThisRow - 1) * 0.5f) * cellW;
            float y = BoardHeight * 0.5f - cellH * (row + 0.5f);

            var tube = TubeView.Create(_board, width, new Vector2(x, y), i);
            tube.SetUnits(_puzzle.GetTube(i));
            tube.SetDone(_puzzle.IsTubeComplete(i), false);
            tube.Clicked += OnTubeClicked;
            _tubes.Add(tube);

            if (animate)
            {
                tube.Rect.localScale = Vector3.zero;
                tube.Rect.DOScale(1f, 0.5f).SetDelay(0.15f + i * 0.04f).SetEase(Ease.OutBack).SetLink(tube.gameObject);
            }
        }
    }

    private float LiftFor(TubeView tube) => tube.Height * 0.11f;

    // ---------------------------------------------------------------- input

    private void OnTubeClicked(TubeView tube)
    {
        if (_busy || _finished) return;
        int index = tube.Index;

        if (_selected < 0)
        {
            if (_puzzle.GetTube(index).Count == 0)
            {
                tube.Shake();
                return;
            }
            Select(index);
            return;
        }

        if (_selected == index)
        {
            Deselect();
            return;
        }

        if (_puzzle.CanPour(_selected, index))
        {
            Pour(_selected, index);
            return;
        }

        // Invalid target: switch selection to the tapped tube if it holds liquid.
        Deselect();
        if (_puzzle.GetTube(index).Count > 0)
            Select(index);
        else
            tube.Shake();
    }

    private void Select(int index)
    {
        _selected = index;
        _tubes[index].SetSelected(true, LiftFor(_tubes[index]));
        App.PlayPop();
    }

    private void Deselect()
    {
        if (_selected < 0) return;
        _tubes[_selected].SetSelected(false, 0f);
        _selected = -1;
    }

    private void Undo()
    {
        if (_busy || _finished || _history.Count == 0) return;
        Deselect();
        _puzzle = _history.Pop();
        _moves = Mathf.Max(0, _moves - 1);
        Refresh();
        UpdateButtons();
    }

    private void AddTube()
    {
        if (_busy || _finished || _tubeAdded) return;
        _tubeAdded = true;
        _selected = -1;
        _history.Clear();
        _puzzle.AddEmptyTube();
        BuildBoard(false);
        UpdateButtons();
    }

    private void Refresh()
    {
        for (int i = 0; i < _tubes.Count; i++)
        {
            _tubes[i].SetUnits(_puzzle.GetTube(i));
            _tubes[i].SetDone(_puzzle.IsTubeComplete(i), false);
            _tubes[i].Wobble = 0.6f;
        }
    }

    // ---------------------------------------------------------------- pouring

    private static Vector2 Rotate(Vector2 v, float radians)
    {
        float c = Mathf.Cos(radians), s = Mathf.Sin(radians);
        return new Vector2(c * v.x - s * v.y, s * v.x + c * v.y);
    }

    private void Pour(int from, int to)
    {
        _busy = true;
        _selected = -1;
        UpdateButtons();

        var src = _tubes[from];
        var dst = _tubes[to];

        _history.Push(_puzzle.Clone());
        int color = _puzzle.TopColor(from);
        int count = _puzzle.PourCount(from, to);
        var srcRuns = src.CopyRuns();
        var dstRuns = dst.CopyRuns();
        float dstBefore = dst.TotalAmount;
        _puzzle.Pour(from, to);
        _moves++;

        int dir = dst.Home.x >= src.Home.x ? 1 : -1;
        Vector2 lipLocal = src.LipLocal(dir);
        Vector2 startPos = src.Rect.anchoredPosition;
        Vector2 home = src.Home;
        Vector2 lipWorld = new Vector2(dst.Home.x, dst.Home.y + dst.MouthLocalY + dst.Width * 0.38f);

        const float angleStart = 58f;
        const float angleEnd = 84f;

        void Place(float blend, float theta, Vector2 fromPos)
        {
            float z = -dir * theta * Mathf.Deg2Rad;
            Vector2 target = lipWorld - Rotate(lipLocal, z);
            src.Rect.anchoredPosition = Vector2.Lerp(fromPos, target, blend);
            src.Rect.localEulerAngles = new Vector3(0f, 0f, -dir * theta * blend);
        }

        RawImage stream = null;
        Material streamMat = null;
        float streamWidth = dst.Width * 0.17f;
        Color liquidColor = Palette.Liquid(color);

        void UpdateStream(float dstUnits)
        {
            if (stream == null) return;
            float surfaceY = dst.Home.y + dst.SurfaceLocalY(dstUnits) + 4f;
            float length = Mathf.Max(lipWorld.y - surfaceY, 1f);
            var rt = stream.rectTransform;
            rt.anchoredPosition = new Vector2(lipWorld.x, lipWorld.y);
            rt.sizeDelta = new Vector2(streamWidth, length);
            streamMat.SetVector(SizeId, new Vector4(streamWidth, length, 0, 0));
        }

        void StartStream()
        {
            stream = UIKit.Quad("Stream", _board, ShaderLibrary.Stream, new Vector2(streamWidth, 10f), lipWorld);
            stream.rectTransform.pivot = new Vector2(0.5f, 1f);
            streamMat = stream.material;
            streamMat.SetVector(LiquidId, Palette.V(liquidColor));
            streamMat.SetFloat(GrowId, 0f);
            streamMat.SetFloat(TailId, 0f);
            UpdateStream(dstBefore);
            stream.transform.SetSiblingIndex(_board.childCount - 1);
            src.Rect.SetAsLastSibling();
            DOVirtual.Float(0f, 1f, 0.14f, v => { if (streamMat != null) streamMat.SetFloat(GrowId, v); })
                .SetEase(Ease.OutQuad).SetLink(gameObject);
            App.PlayPour();
        }

        void PourStep(float p)
        {
            Place(1f, Mathf.Lerp(angleStart, angleEnd, p), startPos);

            var s = new List<TubeView.Run>(srcRuns);
            var last = s[s.Count - 1];
            last.Amount = srcRuns[srcRuns.Count - 1].Amount - count * p;
            if (last.Amount <= 0.001f) s.RemoveAt(s.Count - 1);
            else s[s.Count - 1] = last;
            src.SetRuns(s);

            var d = new List<TubeView.Run>(dstRuns);
            if (d.Count > 0)
            {
                var top = d[d.Count - 1];
                top.Amount += count * p;
                d[d.Count - 1] = top;
            }
            else if (p > 0.001f)
            {
                d.Add(new TubeView.Run(color, count * p));
            }
            dst.SetRuns(d);
            dst.Wobble = 0.75f;

            UpdateStream(dstBefore + count * p);
        }

        void EndStream()
        {
            var mat = streamMat;
            var obj = stream != null ? stream.gameObject : null;
            DOVirtual.Float(0f, 1f, 0.2f, v => { if (mat != null) mat.SetFloat(TailId, v); })
                .SetEase(Ease.InQuad)
                .SetLink(gameObject)
                .OnComplete(() => { if (obj != null) Destroy(obj); });
        }

        float pourDuration = 0.3f + 0.22f * count;

        var seq = DOTween.Sequence().SetLink(gameObject);
        seq.Append(DOVirtual.Float(0f, 1f, 0.36f, b => Place(b, angleStart, startPos)).SetEase(Ease.InOutSine));
        seq.AppendCallback(StartStream);
        seq.Append(DOVirtual.Float(0f, 1f, pourDuration, PourStep).SetEase(Ease.Linear));
        seq.AppendCallback(EndStream);
        seq.Append(DOVirtual.Float(1f, 0f, 0.34f, b => Place(b, angleEnd, home)).SetEase(Ease.InOutSine));
        seq.OnComplete(() => FinishPour(from, to));
    }

    private void FinishPour(int from, int to)
    {
        var src = _tubes[from];
        var dst = _tubes[to];

        src.Rect.localEulerAngles = Vector3.zero;
        src.Rect.anchoredPosition = src.Home;
        src.SetUnits(_puzzle.GetTube(from));
        dst.SetUnits(_puzzle.GetTube(to));
        dst.Wobble = 1f;
        _busy = false;

        if (_puzzle.IsTubeComplete(to))
        {
            dst.SetDone(true);
            dst.Celebrate(Palette.Liquid(_puzzle.TopColor(to)));
            App.PlayComplete();
        }

        UpdateButtons();
        if (_puzzle.IsSolved)
            Win();
    }

    // ---------------------------------------------------------------- win

    private void Win()
    {
        _finished = true;
        int stars = LevelCatalog.StarsFor(_moves, _info);
        App.Save.SaveStars(_difficulty, _level, stars);
        App.Save.UnlockLevel(_difficulty, _level + 1);
        UpdateButtons();
        App.PlayWin();

        DOVirtual.DelayedCall(0.8f, () => ShowWinDialog(stars)).SetLink(gameObject);
    }

    private void ShowWinDialog(int stars)
    {
        // Overlay layers live on the full-canvas root so they also cover the area outside the stage.
        var overlay = App.OverlayRoot;

        var dim = UIKit.Panel(overlay, "Dim", overlay.rect.size + new Vector2(80f, 80f), Vector2.zero,
            new Color(0.02f, 0.05f, 0.18f), new Color(0.02f, 0.05f, 0.18f), 0f, true, 0f);
        dim.raycastTarget = true;
        dim.color = new Color(1, 1, 1, 0f);
        dim.DOFade(0.62f, 0.35f).SetLink(dim.gameObject);
        _overlayObjects.Add(dim.gameObject);

        _confetti = UIKit.Quad("Confetti", overlay, ShaderLibrary.Confetti, overlay.rect.size, Vector2.zero);
        _confetti.material.SetFloat("_Start", Time.time);
        _overlayObjects.Add(_confetti.gameObject);

        var root = UIKit.Rect("Win dialog", overlay, Vector2.zero, new Vector2(0, 0));
        _overlayObjects.Add(root.gameObject);
        root.localScale = Vector3.one * 0.5f;
        root.DOScale(1f, 0.55f).SetEase(Ease.OutBack).SetLink(root.gameObject);

        // Card
        UIKit.Panel(root, "Card", new Vector2(820, 900), Vector2.zero,
            new Color(0.27f, 0.50f, 0.95f), new Color(0.10f, 0.20f, 0.58f), 80f);
        // Lighter inner field
        UIKit.Panel(root, "Inner", new Vector2(740, 820), new Vector2(0, -6),
            new Color(0.20f, 0.38f, 0.82f), new Color(0.12f, 0.26f, 0.66f), 64f, true, 0f);

        // Banner
        UIKit.Panel(root, "Banner", new Vector2(700, 150), new Vector2(0, 450),
            new Color(1f, 0.82f, 0.30f), new Color(0.96f, 0.50f, 0.08f), 75f);
        UIKit.Label(root, "COMPLETE!", 100, new Vector2(680, 140), new Vector2(0, 458),
            Color.white, new Color(1f, 0.97f, 0.82f), TextAnchor.MiddleCenter, true, 1f);

        UIKit.Label(root, $"LEVEL {_level}  -  {LevelCatalog.Title(_difficulty)}", 46, new Vector2(700, 70), new Vector2(0, 330),
            new Color(0.80f, 0.94f, 1f), new Color(0.62f, 0.84f, 1f), TextAnchor.MiddleCenter, true);

        // Rotating sunburst behind the stars
        var rays = UIKit.Quad("Rays", root, ShaderLibrary.Rays, new Vector2(620, 620), new Vector2(0, 150));
        rays.material.SetVector("_RayColor", Palette.V(new Color(1f, 0.9f, 0.45f)));

        var starIcon = UIKit.Icon(root, UIKit.IconKind.Stars, new Vector2(600, 200), new Vector2(0, 150), Color.white, true);
        starIcon.material.SetFloat("_Stars", stars);
        starIcon.material.SetFloat("_Anim", 0f);
        DOVirtual.Float(0f, 3f, 1.3f, v => { if (starIcon != null) starIcon.material.SetFloat("_Anim", v); })
            .SetDelay(0.5f).SetEase(Ease.Linear).SetLink(root.gameObject);

        // Moves pill
        UIKit.Panel(root, "Moves pill", new Vector2(420, 90), new Vector2(0, -55),
            new Color(0.07f, 0.14f, 0.42f), new Color(0.07f, 0.14f, 0.42f), 45f, true, 0f);
        UIKit.Label(root, $"MOVES  {_moves}", 54, new Vector2(400, 80), new Vector2(0, -52),
            Color.white, new Color(0.78f, 0.93f, 1f), TextAnchor.MiddleCenter, true);

        bool hasNext = _level < LevelCatalog.LevelsPerDifficulty;
        float y = -190f;
        if (hasNext)
        {
            var next = UIKit.Button(root, "Next", new Vector2(540, 150), new Vector2(0, y),
                new Color(0.50f, 0.93f, 0.40f), new Color(0.12f, 0.64f, 0.28f), 75f);
            UIKit.Label(next.transform, "NEXT", 80, new Vector2(420, 110), new Vector2(0, 6),
                Color.white, new Color(0.95f, 1f, 0.9f), TextAnchor.MiddleCenter, true);
            next.Clicked += () => App.ShowGame(_difficulty, _level + 1);
            y -= 160f;
        }

        var levels = UIKit.Button(root, "Levels", new Vector2(540, 110), new Vector2(0, hasNext ? y : -230f),
            new Color(0.45f, 0.72f, 1f), new Color(0.18f, 0.40f, 0.88f), 55f, 0.5f);
        UIKit.Label(levels.transform, "LEVELS", 58, new Vector2(420, 90), new Vector2(0, 6),
            Color.white, new Color(0.85f, 0.95f, 1f), TextAnchor.MiddleCenter, true);
        levels.Clicked += () => App.ShowLevels(_difficulty);
    }
}
