using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// One glass tube. All of the visuals (glass, liquid layers, glow, tilt aware surface) come from the Tube shader;
/// this class only feeds it layer colours/amounts and exposes the geometry needed for pour animations.
/// </summary>
public class TubeView : MonoBehaviour, IPointerClickHandler
{
    public struct Run
    {
        public int Color;
        public float Amount;

        public Run(int color, float amount)
        {
            Color = color;
            Amount = amount;
        }
    }

    public const float Aspect = 2.7f;

    private static readonly int SizeId = Shader.PropertyToID("_Size");
    private static readonly int PivotId = Shader.PropertyToID("_Pivot");
    private static readonly int AmountsId = Shader.PropertyToID("_Amounts");
    private static readonly int TiltId = Shader.PropertyToID("_Tilt");
    private static readonly int WobbleId = Shader.PropertyToID("_Wobble");
    private static readonly int ShadowId = Shader.PropertyToID("_Shadow");
    private static readonly int GlowId = Shader.PropertyToID("_Glow");
    private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
    private static readonly int DoneId = Shader.PropertyToID("_Done");
    private static readonly int[] LayerIds =
    {
        Shader.PropertyToID("_C0"), Shader.PropertyToID("_C1"), Shader.PropertyToID("_C2"), Shader.PropertyToID("_C3")
    };

    public event Action<TubeView> Clicked;

    public int Index;
    public Vector2 Home;
    public float Wobble;
    public bool IsDone { get; private set; }

    public RectTransform Rect { get; private set; }
    public float Width { get; private set; }
    public float Height { get; private set; }

    private Material _material;
    private readonly List<Run> _runs = new List<Run>(4);
    private float _glow;
    private float _done;
    private Tween _moveTween;
    private Tween _glowTween;
    private Tween _doneTween;

    public static TubeView Create(Transform parent, float width, Vector2 home, int index)
    {
        float height = width * Aspect;
        var rt = UIKit.Rect("Tube " + index, parent, new Vector2(width, height), home);
        var image = UIKit.AddQuad(rt, ShaderLibrary.Tube, true);

        var view = rt.gameObject.AddComponent<TubeView>();
        view.Rect = rt;
        view.Width = width;
        view.Height = height;
        view.Home = home;
        view.Index = index;
        view._material = image.material;

        view._material.SetVector(SizeId, new Vector4(width, height, 0, 0));
        view._material.SetVector(PivotId, new Vector4(0.5f, 0.5f, 0, 0));
        view._material.SetVector(GlowColorId, Palette.V(new Color(1f, 0.95f, 0.5f)));
        view.Push();
        return view;
    }

    // ---------------------------------------------------------------- geometry (mirrors WSTube.shader)

    private float BodyBottom => 0.30f * Width;
    private float BodyTop => Height - 0.10f * Width;
    private float Wall => 0.055f * Width;
    private float LipMargin => 0.11f * Width;

    public float SlotHeight => (BodyTop - BodyBottom - Wall - LipMargin) / 4f;

    /// <summary>Local y (relative to the rect centre) of the liquid surface for a given amount of units.</summary>
    public float SurfaceLocalY(float units) => BodyBottom + Wall + SlotHeight * units - Height * 0.5f;

    /// <summary>Local y (relative to the rect centre) of the opening.</summary>
    public float MouthLocalY => BodyTop - Height * 0.5f;

    /// <summary>Point of the rim that the liquid leaves from when the tube tips towards <paramref name="dir"/>.</summary>
    public Vector2 LipLocal(int dir)
    {
        return new Vector2(dir * 0.27f * Width, BodyTop - 0.06f * Width - Height * 0.5f);
    }

    // ---------------------------------------------------------------- content

    public IReadOnlyList<Run> Runs => _runs;
    public List<Run> CopyRuns() => new List<Run>(_runs);

    public float TotalAmount
    {
        get
        {
            float sum = 0f;
            foreach (var r in _runs) sum += r.Amount;
            return sum;
        }
    }

    public void SetUnits(IReadOnlyList<int> units)
    {
        _runs.Clear();
        for (int i = 0; i < units.Count; i++)
        {
            if (_runs.Count > 0 && _runs[_runs.Count - 1].Color == units[i])
            {
                var last = _runs[_runs.Count - 1];
                last.Amount += 1f;
                _runs[_runs.Count - 1] = last;
            }
            else
            {
                _runs.Add(new Run(units[i], 1f));
            }
        }
        Push();
    }

    public void SetRuns(List<Run> runs)
    {
        _runs.Clear();
        _runs.AddRange(runs);
        Push();
    }

    public void SetDemo(params int[] units)
    {
        SetUnits(units);
    }

    private void Push()
    {
        var amounts = Vector4.zero;
        for (int i = 0; i < 4; i++)
        {
            if (i < _runs.Count)
            {
                _material.SetVector(LayerIds[i], Palette.V(Palette.Liquid(_runs[i].Color)));
                amounts[i] = _runs[i].Amount;
            }
        }
        _material.SetVector(AmountsId, amounts);
    }

    // ---------------------------------------------------------------- state

    public void SetSelected(bool selected, float lift)
    {
        _moveTween?.Kill();
        _glowTween?.Kill();

        var target = Home + (selected ? new Vector2(0f, lift) : Vector2.zero);
        _moveTween = Rect.DOAnchorPos(target, selected ? 0.22f : 0.18f)
            .SetEase(selected ? Ease.OutBack : Ease.OutQuad)
            .SetLink(gameObject);
        _glowTween = DOTween.To(() => _glow, v => _glow = v, selected ? 1f : 0f, 0.2f).SetLink(gameObject);

        if (selected) Wobble = Mathf.Max(Wobble, 0.5f);
    }

    public void SetDone(bool done, bool animate = true)
    {
        IsDone = done;
        _doneTween?.Kill();
        if (!animate)
        {
            _done = done ? 1f : 0f;
            return;
        }
        _doneTween = DOTween.To(() => _done, v => _done = v, done ? 1f : 0f, done ? 0.6f : 0.2f)
            .SetEase(done ? Ease.OutBounce : Ease.OutQuad)
            .SetLink(gameObject);
    }

    /// <summary>Celebration for a finished tube: a pop plus sparkles flying out of the opening.</summary>
    public void Celebrate(Color liquid)
    {
        Rect.DOPunchScale(Vector3.one * 0.1f, 0.45f, 6, 0.6f).SetLink(gameObject);

        var parent = Rect.parent;
        var origin = Rect.anchoredPosition + new Vector2(0f, MouthLocalY * 0.55f);
        Color[] tints = { Color.white, new Color(1f, 0.92f, 0.45f), Color.Lerp(liquid, Color.white, 0.5f) };

        for (int i = 0; i < 9; i++)
        {
            float angle = (i / 9f) * Mathf.PI * 2f + UnityEngine.Random.Range(-0.3f, 0.3f);
            float dist = Width * UnityEngine.Random.Range(0.55f, 1.05f);
            float size = Width * UnityEngine.Random.Range(0.16f, 0.3f);

            var spark = UIKit.Icon(parent, UIKit.IconKind.Sparkle, new Vector2(size, size), origin,
                tints[i % tints.Length]);
            var rt = spark.rectTransform;
            rt.localScale = Vector3.zero;
            var target = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist + new Vector2(0f, Width * 0.15f);

            var seq = DOTween.Sequence().SetLink(spark.gameObject);
            seq.Append(rt.DOAnchorPos(target, 0.65f).SetEase(Ease.OutCubic));
            seq.Join(rt.DOScale(1f, 0.2f).SetEase(Ease.OutBack));
            seq.Join(rt.DORotate(new Vector3(0, 0, UnityEngine.Random.Range(-180f, 180f)), 0.65f, RotateMode.FastBeyond360));
            seq.Insert(0.35f, rt.DOScale(0f, 0.3f).SetEase(Ease.InQuad));
            seq.OnComplete(() => Destroy(spark.gameObject));
        }
    }

    public void Shake()
    {
        _moveTween?.Kill();
        Rect.anchoredPosition = Home;
        _moveTween = Rect.DOShakeAnchorPos(0.3f, new Vector2(Width * 0.12f, 0f), 18, 0f, false, true, ShakeRandomnessMode.Harmonic)
            .SetLink(gameObject)
            .OnComplete(() => Rect.anchoredPosition = Home);
    }

    /// <summary>Gentle idle sway used by the decorative tubes on the menu.</summary>
    public void StartSway(float degrees, float duration, float delay)
    {
        Rect.localEulerAngles = new Vector3(0, 0, -degrees);
        Rect.DORotate(new Vector3(0, 0, degrees), duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetDelay(delay)
            .SetLink(gameObject);
        Wobble = 0.55f;
    }

    private void LateUpdate()
    {
        float z = Rect.localEulerAngles.z;
        if (z > 180f) z -= 360f;
        float tilt = z * Mathf.Deg2Rad;

        Wobble = Mathf.MoveTowards(Wobble, 0f, Time.deltaTime * 0.8f);

        _material.SetFloat(TiltId, tilt);
        _material.SetFloat(WobbleId, Wobble);
        _material.SetFloat(ShadowId, 1f - Mathf.Clamp01(Mathf.Abs(tilt) / 0.3f));
        _material.SetFloat(GlowId, _glow);
        _material.SetFloat(DoneId, _done);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Clicked?.Invoke(this);
    }
}
