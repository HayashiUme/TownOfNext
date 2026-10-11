namespace TONX.Modules.Achievements.Titles;

public static class PlayerAchievementData
{
    // PlayerId → 已佩戴成就 ID（0 = 未佩戴）
    private static readonly Dictionary<byte, int> EquippedTitles = new();

    public static void SetEquippedTitle(byte playerId, int achievementId)
    {
        if (achievementId <= 0)
            EquippedTitles.Remove(playerId);
        else
            EquippedTitles[playerId] = achievementId;
    }

    public static int GetEquippedTitle(byte playerId)
        => EquippedTitles.TryGetValue(playerId, out var id) ? id : 0;
    
    public static void RemovePlayer(byte playerId)
        => EquippedTitles.Remove(playerId);

    public static void ResetTitles()
    {
        EquippedTitles.Clear();
    }
}
