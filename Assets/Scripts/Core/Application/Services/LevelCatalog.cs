using System;

public struct LevelInfo
{
    public Difficulty Difficulty;
    public int Level;
    public int ColorCount;
    public int EmptyTubes;
    public int ParMoves;
}

/// <summary>Describes the level ladder and generates solvable puzzles for it.</summary>
public static class LevelCatalog
{
    public const int LevelsPerDifficulty = 50;

    public static string Title(Difficulty d)
    {
        switch (d)
        {
            case Difficulty.Easy: return "EASY";
            case Difficulty.Medium: return "MEDIUM";
            default: return "HARD";
        }
    }

    public static void ColorRange(Difficulty d, out int min, out int max)
    {
        switch (d)
        {
            case Difficulty.Easy: min = 3; max = 6; break;
            case Difficulty.Medium: min = 6; max = 9; break;
            default: min = 9; max = 12; break;
        }
    }

    public static LevelInfo GetInfo(Difficulty d, int level)
    {
        ColorRange(d, out int min, out int max);
        float t = (level - 1) / (float)(LevelsPerDifficulty - 1);
        int colors = min + (int)Math.Round(t * (max - min));
        return new LevelInfo
        {
            Difficulty = d,
            Level = level,
            ColorCount = colors,
            EmptyTubes = 2,
            ParMoves = colors * 2 + 2
        };
    }

    public static PuzzleState Generate(Difficulty d, int level)
    {
        var info = GetInfo(d, level);
        PuzzleState last = null;

        for (int attempt = 0; attempt < 40; attempt++)
        {
            var rng = new Random((int)d * 100003 + level * 7919 + attempt * 104729);
            last = Shuffled(info, rng);
            if (PuzzleSolver.IsSolvable(last))
                return last;
        }

        // Extremely unlikely: fall back to a puzzle with one extra empty tube, which is always easier.
        last.AddEmptyTube();
        return last;
    }

    private static PuzzleState Shuffled(LevelInfo info, Random rng)
    {
        int cap = PuzzleState.Capacity;
        var units = new int[info.ColorCount * cap];
        for (int c = 0; c < info.ColorCount; c++)
            for (int k = 0; k < cap; k++)
                units[c * cap + k] = c;

        PuzzleState state;
        do
        {
            for (int i = units.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (units[i], units[j]) = (units[j], units[i]);
            }

            state = new PuzzleState();
            for (int t = 0; t < info.ColorCount; t++)
            {
                var tube = new int[cap];
                Array.Copy(units, t * cap, tube, 0, cap);
                state = Append(state, tube);
            }
        }
        while (AnyTubeComplete(state));

        for (int e = 0; e < info.EmptyTubes; e++)
            state.AddEmptyTube();

        return state;
    }

    private static PuzzleState Append(PuzzleState state, int[] tube)
    {
        var tubes = new System.Collections.Generic.List<int[]>();
        for (int i = 0; i < state.TubeCount; i++)
        {
            var src = state.GetTube(i);
            var arr = new int[src.Count];
            for (int j = 0; j < src.Count; j++) arr[j] = src[j];
            tubes.Add(arr);
        }
        tubes.Add(tube);
        return new PuzzleState(tubes);
    }

    private static bool AnyTubeComplete(PuzzleState state)
    {
        for (int i = 0; i < state.TubeCount; i++)
            if (state.IsTubeComplete(i)) return true;
        return false;
    }

    public static int StarsFor(int moves, LevelInfo info)
    {
        if (moves <= info.ParMoves + info.ColorCount) return 3;
        if (moves <= info.ParMoves * 2 + info.ColorCount) return 2;
        return 1;
    }
}
