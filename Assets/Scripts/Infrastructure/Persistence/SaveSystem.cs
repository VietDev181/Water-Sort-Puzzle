using UnityEngine;

public class SaveSystem : ISaveSystem
{
    private static string UnlockKey(Difficulty d) => $"WS_Unlocked_{(int)d}";
    private static string StarKey(Difficulty d, int level) => $"WS_Stars_{(int)d}_{level}";

    public int GetUnlockedLevel(Difficulty difficulty)
    {
        return Mathf.Clamp(PlayerPrefs.GetInt(UnlockKey(difficulty), 1), 1, LevelCatalog.LevelsPerDifficulty);
    }

    public void UnlockLevel(Difficulty difficulty, int level)
    {
        level = Mathf.Clamp(level, 1, LevelCatalog.LevelsPerDifficulty);
        if (level <= GetUnlockedLevel(difficulty)) return;

        PlayerPrefs.SetInt(UnlockKey(difficulty), level);
        PlayerPrefs.Save();
    }

    public int GetStars(Difficulty difficulty, int level)
    {
        return PlayerPrefs.GetInt(StarKey(difficulty, level), 0);
    }

    public void SaveStars(Difficulty difficulty, int level, int stars)
    {
        if (stars <= GetStars(difficulty, level)) return;

        PlayerPrefs.SetInt(StarKey(difficulty, level), stars);
        PlayerPrefs.Save();
    }

    public int GetClearedCount(Difficulty difficulty)
    {
        int cleared = 0;
        for (int i = 1; i <= LevelCatalog.LevelsPerDifficulty; i++)
            if (GetStars(difficulty, i) > 0) cleared++;
        return cleared;
    }
}
