using System.Text;
using System.Text.Json;

namespace TONX.Modules.Achievements.Core;

public static class AchievementApiClient
{
    public static string ServerBaseUrl { get; set; } = "https://record.tonx.cc";

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(8) };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const int FetchThrottleSeconds = 30;

    private static readonly Dictionary<string, DateTime> LastFetchAt = new();
    private static readonly object ThrottleLock = new();

    private static bool IsThrottled(string friendCode)
    {
        var key = CustomAchievementData.NormalizeFriendCode(friendCode);
        if (string.IsNullOrEmpty(key)) return true;

        lock (ThrottleLock)
        {
            if (!LastFetchAt.TryGetValue(key, out var last)) return false;
            return (DateTime.UtcNow - last).TotalSeconds < FetchThrottleSeconds;
        }
    }

    private static void MarkFetched(string friendCode)
    {
        var key = CustomAchievementData.NormalizeFriendCode(friendCode);
        if (string.IsNullOrEmpty(key)) return;

        lock (ThrottleLock) LastFetchAt[key] = DateTime.UtcNow;
    }

    public static async Task<(List<AchievementRecord> records, int status)> GetRecordsWithStatusAsync(string friendCode)
    {
        var key = CustomAchievementData.NormalizeFriendCode(friendCode);
        if (string.IsNullOrEmpty(key)) return (null, 0);

        try
        {
            var response = await Http.GetAsync($"{ServerBaseUrl}/api/achievements/{Uri.EscapeDataString(key)}");
            if (!response.IsSuccessStatusCode)
            {
                Logger.Warn($"Fetch failed: HTTP {(int)response.StatusCode} ({key})", "AchievementApi");
                return (null, (int)response.StatusCode);
            }

            var json = await response.Content.ReadAsStringAsync();
            var records = JsonSerializer.Deserialize<List<AchievementRecord>>(json, JsonOptions) ?? new();
            MarkFetched(key);
            Logger.Info($"Fetched {records.Count} records for {key}", "AchievementApi");
            return (records, (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            Logger.Warn($"Fetch Error: {ex.Message}", "AchievementApi");
            return (null, 0);
        }
    }

    public static async Task<List<AchievementRecord>> GetRecordsAsync(string friendCode)
        => (await GetRecordsWithStatusAsync(friendCode)).records;

    public static async Task<bool> EnsureFetchedAsync(CustomAchievementData data)
    {
        if (data == null || string.IsNullOrEmpty(data.FriendCode)) return false;
        if (data.HasData && IsThrottled(data.FriendCode)) return true;

        var records = await GetRecordsAsync(data.FriendCode);
        if (records == null) return false;

        data.MergeServer(records);
        data.MarkLoaded();
        return true;
    }

    public static async Task<bool> SyncBatchAsync(IEnumerable<CustomAchievementData> players)
    {
        if (players == null) return false;

        var targets = players
            .Where(p => p != null && p.HasPendingChanges)
            .Select(p => (Data: p, Items: p.PeekDirty()))
            .Where(x => x.Items.Count > 0)
            .ToList();

        if (targets.Count == 0) return true;

        try
        {
            var body = JsonSerializer.Serialize(new
            {
                players = targets.Select(x => new
                {
                    friend_code = x.Data.FriendCode,
                    items = x.Items.Select(r => new
                    {
                        achievement_id = r.Id,
                        achievement_name = r.Name,
                        description = r.Description,
                        progress = r.Progress,
                        required = r.Required,
                        unlocked = r.Unlocked,
                        unlocked_at = r.UnlockedAt,
                    }).ToList(),
                }).ToList(),
            });

            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await Http.PostAsync($"{ServerBaseUrl}/api/achievements/sync", content);

            if (!response.IsSuccessStatusCode)
            {
                Logger.Warn($"Sync failed: HTTP {(int)response.StatusCode}", "AchievementApi");
                return false;
            }

            foreach (var target in targets) target.Data.CommitDirty();
            Logger.Info($"Synced {targets.Count} players", "AchievementApi");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warn($"Sync Error: {ex.Message}", "AchievementApi");
            return false;
        }
    }

    public static async Task<bool> ReportProgressAsync(string friendCode, int achievementId, string achievementName, int progress, int required)
    {
        try
        {
            var body = JsonSerializer.Serialize(new
            {
                friend_code = friendCode,
                achievement_id = achievementId,
                achievement_name = achievementName,
                progress,
                required,
            });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await Http.PostAsync($"{ServerBaseUrl}/api/achievements/progress", content);

            if (!response.IsSuccessStatusCode)
            {
                Logger.Warn($"Progress report failed: HTTP {(int)response.StatusCode}", "AchievementApi");
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warn($"Progress report Error: {ex.Message}", "AchievementApi");
            return false;
        }
    }

    public static async Task<bool> TriggerUnlockAsync(string friendCode, int achievementId, string achievementName, string description, int progress, int required)
    {
        try
        {
            var body = JsonSerializer.Serialize(new
            {
                friend_code = friendCode,
                achievement_id = achievementId,
                achievement_name = achievementName,
                description,
                progress,
                required,
            });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await Http.PostAsync($"{ServerBaseUrl}/api/achievements/unlock", content);

            if (!response.IsSuccessStatusCode)
            {
                Logger.Warn($"Unlock failed: HTTP {(int)response.StatusCode}", "AchievementApi");
                return false;
            }
            Logger.Info($"Unlock uploaded: {friendCode} id={achievementId}", "AchievementApi");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warn($"Unlock Error: {ex.Message}", "AchievementApi");
            return false;
        }
    }
}
