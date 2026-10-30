using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central access to the custom UI shaders. The shaders are referenced from the scene
/// (so they are included in builds) and registered here at startup.
/// </summary>
public static class ShaderLibrary
{
    public const string Background = "WaterSort/UI/Background";
    public const string Button = "WaterSort/UI/Button";
    public const string Tube = "WaterSort/UI/Tube";
    public const string Stream = "WaterSort/UI/Stream";
    public const string Text = "WaterSort/UI/Text";
    public const string Icon = "WaterSort/UI/Icon";
    public const string Confetti = "WaterSort/UI/Confetti";
    public const string Rays = "WaterSort/UI/Rays";
    public const string Transition = "WaterSort/UI/Transition";

    private static readonly Dictionary<string, Shader> Shaders = new Dictionary<string, Shader>();

    public static void Register(IEnumerable<Shader> shaders)
    {
        if (shaders == null) return;
        foreach (var shader in shaders)
        {
            if (shader != null)
                Shaders[shader.name] = shader;
        }
    }

    public static Shader Get(string shaderName)
    {
        if (Shaders.TryGetValue(shaderName, out var shader) && shader != null)
            return shader;

        shader = Shader.Find(shaderName);
        if (shader != null)
            Shaders[shaderName] = shader;
        return shader;
    }

    public static Material CreateMaterial(string shaderName)
    {
        var shader = Get(shaderName);
        if (shader == null)
        {
            Debug.LogError($"Shader not found: {shaderName}");
            shader = Shader.Find("UI/Default");
        }

        return new Material(shader) { name = shaderName + " (instance)" };
    }
}
