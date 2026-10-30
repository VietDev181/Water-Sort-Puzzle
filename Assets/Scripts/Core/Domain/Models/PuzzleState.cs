using System.Collections.Generic;

/// <summary>
/// Pure puzzle model. A tube is a list of colour ids ordered bottom -> top.
/// </summary>
public class PuzzleState
{
    public const int Capacity = 4;

    private readonly List<List<int>> _tubes = new List<List<int>>();

    public int TubeCount => _tubes.Count;
    public IReadOnlyList<int> GetTube(int index) => _tubes[index];

    public PuzzleState() { }

    public PuzzleState(IEnumerable<IEnumerable<int>> tubes)
    {
        foreach (var t in tubes)
            _tubes.Add(new List<int>(t));
    }

    public PuzzleState Clone()
    {
        var copy = new PuzzleState();
        foreach (var t in _tubes)
            copy._tubes.Add(new List<int>(t));
        return copy;
    }

    public void AddEmptyTube() => _tubes.Add(new List<int>(Capacity));

    public int TopColor(int tube)
    {
        var t = _tubes[tube];
        return t.Count == 0 ? -1 : t[t.Count - 1];
    }

    /// <summary>Number of identical units stacked on top of the tube.</summary>
    public int TopRun(int tube)
    {
        var t = _tubes[tube];
        if (t.Count == 0) return 0;

        int color = t[t.Count - 1];
        int run = 0;
        for (int i = t.Count - 1; i >= 0 && t[i] == color; i--)
            run++;
        return run;
    }

    public int PourCount(int from, int to)
    {
        if (from == to) return 0;

        var src = _tubes[from];
        var dst = _tubes[to];
        if (src.Count == 0 || dst.Count >= Capacity) return 0;
        if (dst.Count > 0 && dst[dst.Count - 1] != src[src.Count - 1]) return 0;

        return System.Math.Min(TopRun(from), Capacity - dst.Count);
    }

    public bool CanPour(int from, int to) => PourCount(from, to) > 0;

    /// <summary>Moves as many matching units as fit. Returns the amount moved.</summary>
    public int Pour(int from, int to)
    {
        int count = PourCount(from, to);
        if (count == 0) return 0;

        var src = _tubes[from];
        var dst = _tubes[to];
        int color = src[src.Count - 1];

        src.RemoveRange(src.Count - count, count);
        for (int i = 0; i < count; i++)
            dst.Add(color);

        return count;
    }

    public bool IsTubeComplete(int tube)
    {
        var t = _tubes[tube];
        if (t.Count != Capacity) return false;
        for (int i = 1; i < t.Count; i++)
            if (t[i] != t[0]) return false;
        return true;
    }

    public bool IsSolved
    {
        get
        {
            for (int i = 0; i < _tubes.Count; i++)
            {
                if (_tubes[i].Count == 0) continue;
                if (!IsTubeComplete(i)) return false;
            }
            return true;
        }
    }
}
