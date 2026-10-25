using System.Collections.Concurrent;

namespace TONX.Modules.Achievements.Core;

public static class AchievementProgressService
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> UploadGates = new();

    public static void RecordUnit(string friendCode, int achievementId)
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || !GameStates.IsInGame) return;

        var definition = AchievementRegistry.GetById(achievementId);
        var data = CustomAchievementDataManager.GetOrCreate(friendCode);
        if (definition == null || data == null || !data.RecordUnit(definition)) return;

        Logger.Info($"Recorded achievement unit locally: {friendCode} [{achievementId}]", "Achievement");
    }

    public static async Task<bool> UploadPendingAsync(CustomAchievementData data)
    {
        if (data == null) return false;

        var gate = UploadGates.GetOrAdd(data.FriendCode, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            var success = true;
            foreach (var record in data.GetPendingRecords())
            {
                var uploaded = CustomAchievementData.IsUnlocked(record)
                    ? await AchievementApiClient.TriggerUnlockAsync(data.FriendCode, record)
                    : await AchievementApiClient.ReportProgressAsync(data.FriendCode, record);

                if (uploaded) data.MarkUploaded(record);
                else success = false;
            }

            return success;
        }
        finally
        {
            gate.Release();
        }
    }
}
