namespace TONX.Modules.Achievements.Player;

public sealed class AchievementRecord
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string UnlockedAt { get; set; } = "";
    public bool Unlocked { get; set; }
    public int Progress { get; set; }
    public int Required { get; set; }
}
/// <summary>
/// 成就数据
/// </summary>
public static class PlayerAchievementData
{
    // FriendCode → 已解锁成就列表
    private static readonly Dictionary<string, List<AchievementRecord>> AchievementCache = new();

    // PlayerId → 已佩戴成就 ID（0 = 未佩戴）
    private static readonly Dictionary<byte, int> EquippedTitles = new();

    public static void UpdateCache(string friendCode, List<AchievementRecord> achievements)
    {
        if (string.IsNullOrEmpty(friendCode)) return;
        AchievementCache[friendCode] = achievements ?? new();
        Logger.Info($"缓存更新：{friendCode} 拥有 {AchievementCache[friendCode].Count} 个成就", "AchievementData");
    }

    public static void AddToCache(string friendCode, AchievementRecord record)
    {
        if (string.IsNullOrEmpty(friendCode) || record == null) return;
        if (!AchievementCache.ContainsKey(friendCode))
            AchievementCache[friendCode] = new();

        var existed = AchievementCache[friendCode].FirstOrDefault(r => r.Id == record.Id);
        if (existed == null) AchievementCache[friendCode].Add(record);
        else if (record.Progress > existed.Progress) existed.Progress = record.Progress;
    }

    public static List<AchievementRecord> GetCached(string friendCode)
    {
        if (string.IsNullOrEmpty(friendCode)) return new();
        return AchievementCache.TryGetValue(friendCode, out var list) ? list : new();
    }

    public static AchievementRecord GetRecord(string friendCode, int achievementId)
        => GetCached(friendCode).FirstOrDefault(r => r.Id == achievementId);

    public static bool HasAchievement(string friendCode, int achievementId)
        => GetCached(friendCode).Any(r => r.Id == achievementId);

    public static void SetEquippedTitle(byte playerId, int achievementId)
    {
        if (achievementId <= 0)
            EquippedTitles.Remove(playerId);
        else
            EquippedTitles[playerId] = achievementId;
    }

    public static int GetEquippedTitle(byte playerId)
        => EquippedTitles.TryGetValue(playerId, out var id) ? id : 0;

    public static void ResetTitles()
    {
        EquippedTitles.Clear();
    }
}
