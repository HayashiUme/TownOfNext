using System.Text;
using System.Text.Json;

namespace TONX.Modules.Achievements.Core;

public static class AchievementApiClient
{
    public static string ServerBaseUrl { get; set; } = "https://record.tonx.cc";

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(8) };
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static bool TitleApiUnavailable;

    public static async Task<(List<AchievementRecord> records, int status)> GetRecordsWithStatusAsync(string friendCode)
    {
        if (string.IsNullOrEmpty(friendCode)) return (null, 0);

        try
        {
            using var response = await Http.GetAsync($"{ServerBaseUrl}/api/achievements/{Uri.EscapeDataString(friendCode)}");
            if (!response.IsSuccessStatusCode)
            {
                Logger.Warn($"Fetch failed: HTTP {(int)response.StatusCode} ({friendCode})", "AchievementApi");
                return (null, (int)response.StatusCode);
            }

            var json = await response.Content.ReadAsStringAsync();
            var records = JsonSerializer.Deserialize<List<AchievementRecord>>(json, JsonOptions);
            if (records == null) return (null, (int)response.StatusCode);

            Logger.Info($"Fetched {records.Count} records for {friendCode}", "AchievementApi");
            return (records, (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            Logger.Warn($"Fetch Error: {ex.Message}", "AchievementApi");
            return (null, 0);
        }
    }

    public static async Task<bool> ReportProgressAsync(string friendCode, AchievementRecord record)
        => await PostAchievementAsync("progress", new
        {
            friend_code = friendCode,
            achievement_id = record.Id,
            achievement_name = record.Name,
            progress = record.Progress,
            required = record.Required,
        });

    public static async Task<bool> TriggerUnlockAsync(string friendCode, AchievementRecord record)
        => await PostAchievementAsync("unlock", new
        {
            friend_code = friendCode,
            achievement_id = record.Id,
            achievement_name = record.Name,
            description = record.Description,
            progress = record.Progress,
            required = record.Required,
        });

    public static async Task<bool> SetWearedTitleAsync(string friendCode, int achievementId)
    {
        if (string.IsNullOrEmpty(friendCode) || TitleApiUnavailable) return false;

        try
        {
            using var content = JsonContent(new
            {
                friend_code = friendCode,
                currentWearedAchievementId = achievementId,
            });
            using var response = await Http.PostAsync($"{ServerBaseUrl}/api/achievements/title", content);
            if ((int)response.StatusCode == 404)
            {
                TitleApiUnavailable = true;
                Logger.Warn("Title API unavailable on server (404), disabled", "AchievementApi");
                return false;
            }

            if (!response.IsSuccessStatusCode)
            {
                Logger.Warn($"Set title failed: HTTP {(int)response.StatusCode} ({friendCode})", "AchievementApi");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.Warn($"Set title Error: {ex.Message}", "AchievementApi");
            return false;
        }
    }

    public static async Task<int?> GetWearedTitleAsync(string friendCode)
    {
        if (string.IsNullOrEmpty(friendCode) || TitleApiUnavailable) return null;

        try
        {
            using var response = await Http.GetAsync($"{ServerBaseUrl}/api/achievements/title/{Uri.EscapeDataString(friendCode)}");
            if ((int)response.StatusCode == 404)
            {
                TitleApiUnavailable = true;
                Logger.Warn("Title API unavailable on server (404), disabled", "AchievementApi");
                return null;
            }

            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var dto = JsonSerializer.Deserialize<WearedTitleResponse>(json, JsonOptions);
            if (dto?.CurrentWearedAchievementId is not int id || id < 0) return null;
            return id;
        }
        catch (Exception ex)
        {
            Logger.Warn($"Get title Error: {ex.Message}", "AchievementApi");
            return null;
        }
    }

    private static async Task<bool> PostAchievementAsync(string route, object body)
    {
        try
        {
            using var content = JsonContent(body);
            using var response = await Http.PostAsync($"{ServerBaseUrl}/api/achievements/{route}", content);
            if (!response.IsSuccessStatusCode)
            {
                Logger.Warn($"Achievement upload failed: HTTP {(int)response.StatusCode}", "AchievementApi");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.Warn($"Achievement upload Error: {ex.Message}", "AchievementApi");
            return false;
        }
    }

    private static StringContent JsonContent(object body)
        => new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    private sealed class WearedTitleResponse
    {
        public int? CurrentWearedAchievementId { get; set; }
    }
}
