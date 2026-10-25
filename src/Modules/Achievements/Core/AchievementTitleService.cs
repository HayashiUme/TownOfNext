namespace TONX.Modules.Achievements.Core;

public static class AchievementTitleService
{
    private static readonly SemaphoreSlim SetGate = new(1, 1);
    private static readonly object VersionLock = new();
    private static readonly Dictionary<string, long> Versions = new(StringComparer.Ordinal);

    private static long NextVersion(string friendCode)
    {
        lock (VersionLock)
        {
            Versions.TryGetValue(friendCode, out var version);
            version++;
            Versions[friendCode] = version;
            return version;
        }
    }

    private static bool IsCurrent(string friendCode, long version)
    {
        lock (VersionLock)
            return Versions.TryGetValue(friendCode, out var current) && current == version;
    }

    public static int GetWearedTitleId(string friendCode)
        => CustomAchievementDataManager.Get(friendCode)?.WearedTitleId ?? 0;

    public static async Task<bool> SetAsync(string friendCode, int achievementId)
    {
        if (string.IsNullOrEmpty(friendCode)) return false;

        var id = Math.Max(0, achievementId);
        var version = NextVersion(friendCode);
        await SetGate.WaitAsync();
        try
        {
            if (!await AchievementApiClient.SetWearedTitleAsync(friendCode, id)) return false;

            if (IsCurrent(friendCode, version))
                CustomAchievementDataManager.GetOrCreate(friendCode)?.SetWearedTitle(id);
            return true;
        }
        finally
        {
            SetGate.Release();
        }
    }

    public static async Task RefreshFromCloudAsync(string friendCode)
    {
        if (string.IsNullOrEmpty(friendCode)) return;

        var version = NextVersion(friendCode);
        await SetGate.WaitAsync();
        try
        {
            var id = await AchievementApiClient.GetWearedTitleAsync(friendCode);
            if (id is not int titleId || !IsCurrent(friendCode, version)) return;

            CustomAchievementDataManager.GetOrCreate(friendCode)?.SetWearedTitle(titleId);
            RefreshNameTags(friendCode);
        }
        finally
        {
            SetGate.Release();
        }
    }

    public static void RefreshNameTags(string friendCode)
    {
        if (AmongUsClient.Instance?.AmHost != true || !GameStates.IsLobby) return;

        _ = new LateTask(() =>
        {
            var player = Main.AllPlayerControls.FirstOrDefault(x => x != null && x.FriendCode == friendCode);
            if (player != null) NameTagManager.ApplyFor(player);
        }, 0f, "RefreshAchievementNameTag");
    }
}
