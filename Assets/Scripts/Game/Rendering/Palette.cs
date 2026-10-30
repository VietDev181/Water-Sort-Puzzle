using UnityEngine;

public static class Palette
{
    // Ordered so that the first few colours are the most distinct from each other.
    public static readonly Color[] Liquids =
    {
        new Color(0.92f, 0.22f, 0.25f), // red
        new Color(0.22f, 0.45f, 0.95f), // blue
        new Color(1.00f, 0.84f, 0.18f), // yellow
        new Color(0.20f, 0.72f, 0.30f), // green
        new Color(0.62f, 0.32f, 0.88f), // purple
        new Color(1.00f, 0.55f, 0.12f), // orange
        new Color(0.18f, 0.82f, 0.88f), // cyan
        new Color(1.00f, 0.50f, 0.75f), // pink
        new Color(0.68f, 0.88f, 0.20f), // lime
        new Color(0.56f, 0.34f, 0.20f), // brown
        new Color(0.78f, 0.12f, 0.50f), // magenta
        new Color(0.58f, 0.64f, 0.72f), // slate
    };

    public static Color Liquid(int index) => Liquids[Mathf.Abs(index) % Liquids.Length];

    public static void ForDifficulty(Difficulty d, out Color top, out Color bottom)
    {
        switch (d)
        {
            case Difficulty.Easy:
                top = new Color(0.50f, 0.93f, 0.40f);
                bottom = new Color(0.12f, 0.64f, 0.28f);
                break;
            case Difficulty.Medium:
                top = new Color(1.00f, 0.80f, 0.28f);
                bottom = new Color(0.96f, 0.52f, 0.08f);
                break;
            default:
                top = new Color(1.00f, 0.48f, 0.55f);
                bottom = new Color(0.78f, 0.14f, 0.32f);
                break;
        }
    }

    public static readonly Color Blue = new Color(0.30f, 0.62f, 1.00f);
    public static readonly Color DeepBlue = new Color(0.12f, 0.32f, 0.78f);
    public static readonly Color White = Color.white;
    public static readonly Color Ice = new Color(0.72f, 0.92f, 1.00f);
    public static readonly Color Gold = new Color(1.00f, 0.80f, 0.25f);
    public static readonly Color Orange = new Color(1.00f, 0.55f, 0.15f);
    public static readonly Color Ink = new Color(0.04f, 0.10f, 0.30f, 0.95f);

    public static Vector4 V(Color c) => new Vector4(c.r, c.g, c.b, c.a);
}
