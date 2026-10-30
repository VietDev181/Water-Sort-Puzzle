using System;
using System.Collections.Generic;

/// <summary>
/// Depth-first solver with a node budget. Used to make sure generated levels are solvable.
/// </summary>
public static class PuzzleSolver
{
    private const int MaxDepth = 220;

    public static bool IsSolvable(PuzzleState start, int nodeBudget = 40000)
    {
        int n = start.TubeCount;
        var tubes = new int[n][];
        var counts = new int[n];
        for (int i = 0; i < n; i++)
        {
            tubes[i] = new int[PuzzleState.Capacity];
            var src = start.GetTube(i);
            counts[i] = src.Count;
            for (int j = 0; j < src.Count; j++)
                tubes[i][j] = src[j];
        }

        var visited = new HashSet<ulong>();
        int budget = nodeBudget;
        return Search(tubes, counts, visited, ref budget, 0);
    }

    private static bool Search(int[][] tubes, int[] counts, HashSet<ulong> visited, ref int budget, int depth)
    {
        if (IsSolved(tubes, counts)) return true;
        if (depth >= MaxDepth || budget <= 0) return false;
        if (!visited.Add(Hash(tubes, counts))) return false;
        budget--;

        int n = tubes.Length;
        int cap = PuzzleState.Capacity;

        // Moves onto non-empty tubes first, then onto the first empty tube.
        for (int pass = 0; pass < 2; pass++)
        {
            bool emptyUsed = false;
            for (int to = 0; to < n; to++)
            {
                bool toEmpty = counts[to] == 0;
                if ((pass == 0) == toEmpty) continue;
                if (counts[to] >= cap) continue;
                if (toEmpty)
                {
                    if (emptyUsed) continue;
                    emptyUsed = true;
                }

                for (int from = 0; from < n; from++)
                {
                    if (from == to || counts[from] == 0) continue;
                    if (IsUniform(tubes[from], counts[from]) && (toEmpty || counts[from] == cap)) continue;

                    int color = tubes[from][counts[from] - 1];
                    if (!toEmpty && tubes[to][counts[to] - 1] != color) continue;

                    int run = 0;
                    for (int k = counts[from] - 1; k >= 0 && tubes[from][k] == color; k--) run++;
                    int move = Math.Min(run, cap - counts[to]);

                    for (int k = 0; k < move; k++)
                    {
                        counts[from]--;
                        tubes[to][counts[to]++] = color;
                    }

                    bool ok = Search(tubes, counts, visited, ref budget, depth + 1);

                    for (int k = 0; k < move; k++)
                    {
                        counts[to]--;
                        tubes[from][counts[from]++] = color;
                    }

                    if (ok) return true;
                    if (budget <= 0) return false;
                }
            }
        }

        return false;
    }

    private static bool IsUniform(int[] tube, int count)
    {
        for (int i = 1; i < count; i++)
            if (tube[i] != tube[0]) return false;
        return true;
    }

    private static bool IsSolved(int[][] tubes, int[] counts)
    {
        for (int i = 0; i < tubes.Length; i++)
        {
            if (counts[i] == 0) continue;
            if (counts[i] != PuzzleState.Capacity || !IsUniform(tubes[i], counts[i])) return false;
        }
        return true;
    }

    // Order independent hash: tubes are encoded then combined with a commutative mix.
    private static ulong Hash(int[][] tubes, int[] counts)
    {
        ulong sum = 0, xor = 0;
        for (int i = 0; i < tubes.Length; i++)
        {
            ulong code = (ulong)counts[i] + 1;
            for (int j = 0; j < counts[i]; j++)
                code = code * 31UL + (ulong)(tubes[i][j] + 1);

            ulong m = code * 0x9E3779B97F4A7C15UL;
            m ^= m >> 29;
            m *= 0xBF58476D1CE4E5B9UL;
            m ^= m >> 32;
            sum += m;
            xor ^= m * 0x94D049BB133111EBUL;
        }
        return sum * 31UL + xor;
    }
}
