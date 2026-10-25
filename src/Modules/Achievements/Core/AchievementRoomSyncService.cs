namespace TONX.Modules.Achievements.Core;

public static class AchievementRoomSyncService
{
    private static readonly object SyncLock = new();
    private static Task RoomSyncTask;
    private static readonly Dictionary<string, Task> PlayerSyncTasks = new();
    private static readonly HashSet<string> SyncedPlayers = new(StringComparer.Ordinal);
    private static int SessionVersion;

    public static void ResetSession()
    {
        lock (SyncLock)
        {
            SessionVersion++;
            RoomSyncTask = Task.CompletedTask;
            PlayerSyncTasks.Clear();
            SyncedPlayers.Clear();
        }
    }

    public static void ForgetPlayer(string friendCode)
    {
        if (string.IsNullOrEmpty(friendCode)) return;
        lock (SyncLock)
        {
            SyncedPlayers.Remove(friendCode);
        }
    }

    public static Task SyncRoomAsync()
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return Task.CompletedTask;

        lock (SyncLock)
        {
            if (RoomSyncTask is { IsCompleted: false }) return RoomSyncTask;
            RoomSyncTask = SyncRoomCoreAsync(SessionVersion);
            return RoomSyncTask;
        }
    }

    public static Task SyncPlayerAsync(string friendCode)
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || string.IsNullOrEmpty(friendCode))
            return Task.CompletedTask;

        lock (SyncLock)
        {
            if (SyncedPlayers.Contains(friendCode)) return Task.CompletedTask;
            if (PlayerSyncTasks.TryGetValue(friendCode, out var existing) && !existing.IsCompleted)
                return existing;

            var task = SyncPlayerCoreAsync(friendCode, SessionVersion);
            PlayerSyncTasks[friendCode] = task;
            return task;
        }
    }

    private static async Task SyncRoomCoreAsync(int sessionVersion)
    {
        try
        {
            var players = Main.AllPlayerControls
                .Where(x => x != null && !string.IsNullOrEmpty(x.FriendCode))
                .Select(x => x.FriendCode)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            foreach (var friendCode in players)
            {
                lock (SyncLock)
                    if (sessionVersion != SessionVersion) return;

                await SyncPlayerAsync(friendCode, sessionVersion);
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Room achievement sync failed: {ex.Message}", "AchievementSync");
        }
        finally
        {
            lock (SyncLock)
                if (sessionVersion == SessionVersion) RoomSyncTask = null;
        }
    }

    private static Task SyncPlayerAsync(string friendCode, int sessionVersion)
    {
        if (string.IsNullOrEmpty(friendCode)) return Task.CompletedTask;

        lock (SyncLock)
        {
            if (sessionVersion != SessionVersion || SyncedPlayers.Contains(friendCode))
                return Task.CompletedTask;
            if (PlayerSyncTasks.TryGetValue(friendCode, out var existing) && !existing.IsCompleted)
                return existing;

            var task = SyncPlayerCoreAsync(friendCode, sessionVersion);
            PlayerSyncTasks[friendCode] = task;
            return task;
        }
    }

    private static async Task SyncPlayerCoreAsync(string friendCode, int sessionVersion)
    {
        var data = CustomAchievementDataManager.GetOrCreate(friendCode);
        if (data == null) return;

        var hasRoundSnapshot = data.HasRoundSnapshot;
        var (records, status) = await AchievementApiClient.GetRecordsWithStatusAsync(friendCode);
        if (records == null)
        {
            Logger.Warn($"Achievement sync failed for {friendCode}: HTTP/status {status}", "AchievementSync");
            return;
        }

        lock (SyncLock)
        {
            if (sessionVersion != SessionVersion) return;
        }

        data.MergeCloud(records);
        await AchievementTitleService.RefreshFromCloudAsync(friendCode);

        lock (SyncLock)
            if (sessionVersion != SessionVersion) return;

        var gained = hasRoundSnapshot
            ? data.GetNewlyUnlockedIds()
            .Select(AchievementRegistry.GetById)
            .Where(x => x != null)
            .OrderByDescending(x => (int)x.Rarity)
            .ThenBy(x => x.Id)
            .ToList()
            : new List<AchievementBase>();

        var player = Main.AllPlayerControls.FirstOrDefault(x => x != null && x.FriendCode == friendCode);
        var result = new GameAchievementResult
        {
            FriendCode = friendCode,
            PlayerId = player?.PlayerId ?? byte.MaxValue,
            Unlocked = gained,
        };

        if (gained.Count > 0)
        {
            if (Options.EnableAchievemntObtainer.GetBool())
                AchievementNotificationService.Send(new[] { result });
        }

        var uploaded = await AchievementProgressService.UploadPendingAsync(data);
        if (!uploaded) return;

        if (hasRoundSnapshot) data.CompleteRound();
        else data.StartRound();
        lock (SyncLock)
        {
            if (sessionVersion == SessionVersion) SyncedPlayers.Add(friendCode);
        }
    }
}
