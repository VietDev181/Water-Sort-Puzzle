using UnityEngine;
using UnityEngine.UI;

/// <summary>Small factory for building the shader driven UI from code.</summary>
public static class UIKit
{
    public const float ButtonPad = 18f;

    private static Font _font;
    private static readonly System.Collections.Generic.Dictionary<string, Material> SharedLabelMaterials =
        new System.Collections.Generic.Dictionary<string, Material>();
    private static readonly System.Collections.Generic.Dictionary<string, Material> SharedIconMaterials =
        new System.Collections.Generic.Dictionary<string, Material>();

    private static readonly int SizeId = Shader.PropertyToID("_Size");
    private static readonly int PadId = Shader.PropertyToID("_Pad");
    private static readonly int RadiusId = Shader.PropertyToID("_Radius");
    private static readonly int ColorTopId = Shader.PropertyToID("_ColorTop");
    private static readonly int ColorBottomId = Shader.PropertyToID("_ColorBottom");
    private static readonly int ShineId = Shader.PropertyToID("_Shine");
    private static readonly int PhaseId = Shader.PropertyToID("_Phase");
    private static readonly int FlatId = Shader.PropertyToID("_Flat");
    private static readonly int TopId = Shader.PropertyToID("_Top");
    private static readonly int BottomId = Shader.PropertyToID("_Bottom");
    private static readonly int RectId = Shader.PropertyToID("_Rect");
    private static readonly int KindId = Shader.PropertyToID("_Kind");
    private static readonly int IconColorId = Shader.PropertyToID("_IconColor");
    private static readonly int OutlineId = Shader.PropertyToID("_Outline");

    public static Font Font
    {
        get
        {
            if (_font == null)
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _font;
        }
    }

    public static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = position;
        return rt;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>Adds a RawImage using its own instance of the given shader (RawImage has proper 0..1 UVs).</summary>
    public static RawImage AddQuad(RectTransform rt, string shaderName, bool raycast = false)
    {
        var image = rt.gameObject.AddComponent<RawImage>();
        var material = ShaderLibrary.CreateMaterial(shaderName);
        image.material = material;
        image.color = Color.white;
        image.raycastTarget = raycast;
        rt.gameObject.AddComponent<OwnedMaterial>().Material = material;
        return image;
    }

    public static RawImage Quad(string name, Transform parent, string shaderName, Vector2 size, Vector2 position, bool raycast = false)
    {
        return AddQuad(Rect(name, parent, size, position), shaderName, raycast);
    }

    public static PressableButton Button(Transform parent, string name, Vector2 size, Vector2 position,
        Color top, Color bottom, float radius, float shine = 1f)
    {
        var rt = Rect(name, parent, size + Vector2.one * (ButtonPad * 2f), position);
        var image = AddQuad(rt, ShaderLibrary.Button, true);
        var mat = image.material;
        mat.SetVector(SizeId, new Vector4(rt.sizeDelta.x, rt.sizeDelta.y, 0, 0));
        mat.SetFloat(PadId, ButtonPad);
        mat.SetFloat(RadiusId, radius);
        mat.SetVector(ColorTopId, Palette.V(top));
        mat.SetVector(ColorBottomId, Palette.V(bottom));
        mat.SetFloat(ShineId, shine);
        mat.SetFloat(PhaseId, Random.value);

        var button = rt.gameObject.AddComponent<PressableButton>();
        button.Bind(image);
        return button;
    }

    /// <summary>Rounded panel without interaction (win dialog, dimmer ...).</summary>
    public static RawImage Panel(Transform parent, string name, Vector2 size, Vector2 position,
        Color top, Color bottom, float radius, bool flat = false, float pad = ButtonPad)
    {
        var rt = Rect(name, parent, size + Vector2.one * (pad * 2f), position);
        var image = AddQuad(rt, ShaderLibrary.Button, false);
        var mat = image.material;
        mat.SetVector(SizeId, new Vector4(rt.sizeDelta.x, rt.sizeDelta.y, 0, 0));
        mat.SetFloat(PadId, pad);
        mat.SetFloat(RadiusId, radius);
        mat.SetVector(ColorTopId, Palette.V(top));
        mat.SetVector(ColorBottomId, Palette.V(bottom));
        mat.SetFloat(ShineId, 0f);
        mat.SetFloat(FlatId, flat ? 1f : 0f);
        return image;
    }

    public static Text Label(Transform parent, string text, int fontSize, Vector2 size, Vector2 position,
        Color top, Color bottom, TextAnchor anchor = TextAnchor.MiddleCenter, bool outline = true, float shine = 0f)
    {
        var rt = Rect("Label", parent, size, position);
        var label = rt.gameObject.AddComponent<Text>();
        label.font = Font;
        label.fontStyle = FontStyle.Bold;
        label.fontSize = fontSize;
        label.alignment = anchor;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;
        label.color = Color.white;
        label.text = text;

        // Labels with identical styling share a material so the canvas can batch them.
        string key = $"{fontSize}|{size.x}|{top}|{bottom}|{shine}";
        if (!SharedLabelMaterials.TryGetValue(key, out var material) || material == null)
        {
            material = ShaderLibrary.CreateMaterial(ShaderLibrary.Text);
            float halfW = size.x * 0.5f;
            material.SetVector(TopId, Palette.V(top));
            material.SetVector(BottomId, Palette.V(bottom));
            material.SetVector(RectId, new Vector4(-halfW, halfW, -fontSize * 0.42f, fontSize * 0.46f));
            material.SetFloat(ShineId, shine);
            material.SetFloat(PhaseId, 0f);
            SharedLabelMaterials[key] = material;
        }
        label.material = material;

        if (outline)
        {
            var o = rt.gameObject.AddComponent<Outline>();
            o.effectColor = Palette.Ink;
            o.effectDistance = new Vector2(Mathf.Max(2f, fontSize * 0.045f), -Mathf.Max(2f, fontSize * 0.045f));
            o.useGraphicAlpha = true;
        }

        return label;
    }

    public enum IconKind
    {
        Stars = 0, Lock = 1, Back = 2, Undo = 3, Restart = 4, Plus = 5, SoundOn = 6, SoundOff = 7, Sparkle = 8
    }

    /// <summary>
    /// Icon quad. Static icons (stars < 0 keeps the default) share a material per look;
    /// pass unique = true when the caller animates shader properties afterwards.
    /// </summary>
    public static RawImage Icon(Transform parent, IconKind kind, Vector2 size, Vector2 position, Color color,
        bool unique = false, float stars = 3f)
    {
        var rt = Rect("Icon", parent, size, position);
        var image = rt.gameObject.AddComponent<RawImage>();
        image.color = Color.white;
        image.raycastTarget = false;

        string key = $"{(int)kind}|{color}|{stars}";
        Material material = null;
        if (!unique && SharedIconMaterials.TryGetValue(key, out material) && material != null)
        {
            image.material = material;
            return image;
        }

        material = ShaderLibrary.CreateMaterial(ShaderLibrary.Icon);
        material.SetFloat(KindId, (float)kind);
        material.SetVector(IconColorId, Palette.V(color));
        material.SetVector(OutlineId, Palette.V(new Color(0.05f, 0.12f, 0.34f, 1f)));
        material.SetFloat("_Stars", stars);
        material.SetFloat("_Anim", 3f);
        image.material = material;

        if (unique) rt.gameObject.AddComponent<OwnedMaterial>().Material = material;
        else SharedIconMaterials[key] = material;
        return image;
    }
}
