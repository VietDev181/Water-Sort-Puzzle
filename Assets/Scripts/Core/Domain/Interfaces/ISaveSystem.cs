public interface ISaveSystem
{
    /// <summary>Highest level the player may play (1 based).</summary>
    int GetUnlockedLevel(Difficulty difficulty);
    void UnlockLevel(Difficulty difficulty, int level);

    int GetStars(Difficulty difficulty, int level);
    void SaveStars(Difficulty difficulty, int level, int stars);

    int GetClearedCount(Difficulty difficulty);
}
