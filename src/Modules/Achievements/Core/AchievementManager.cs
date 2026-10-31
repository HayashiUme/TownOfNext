using System.Text;

namespace TONX.Modules.Achievements.Core;

public enum TitleChangeResult
{
    Ok,
    Unknown,
    NotOwned,
    UploadFailed,
    NotHost,
}

public static class AchievementManager
{
    public static void OnPlayerJoined(string friendCode)
    {
        if (AmongUsClient.Instance?.AmHost != true) return;
        _ = AchievementRoomSyncService.SyncPlayerAsync(friendCode);
    }

    public static void OnLocalJoined()
    {
        var friendCode = CustomAchievementDataManager.LocalFriendCode;
        if (string.IsNullOrEmpty(friendCode))
        {
            Logger.Warn("Local achievement sync skipped: friend code unavailable", "Achievement");
            return;
        }

        if (AmongUsClient.Instance?.AmHost != true)
            _ = SyncLocalAsync(friendCode);
    }

    public static void TryUpdateUnit(string friendCode, int achievementId)
        => AchievementProgressService.RecordUnit(friendCode, achievementId);

    public static int GetWearedTitleId(string friendCode)
        => AchievementTitleService.GetWearedTitleId(friendCode);

    public static Task<bool> WearPendingAchievementTitle(string friendCode, int achievementId)
        => AchievementTitleService.SetAsync(friendCode, achievementId);

    public static async Task<TitleChangeResult> WearAchievementTitle(string friendCode, int achievementId)
    {
        if (AmongUsClient.Instance?.AmHost != true) return TitleChangeResult.NotHost;
        if (string.IsNullOrEmpty(friendCode)) return TitleChangeResult.NotOwned;
        if (achievementId > 0 && AchievementRegistry.GetById(achievementId) == null)
            return TitleChangeResult.Unknown;

        if (!await AchievementTitleService.SetAsync(friendCode, achievementId))
            return TitleChangeResult.UploadFailed;

        AchievementTitleService.RefreshNameTags(friendCode);
        return TitleChangeResult.Ok;
    }

    public static async Task<bool> SettleGameAsync()
    {
        if (!await AchievementSettlement.SettleAsync()) return false;
        return true;
    }

    public static Task DisplayAchievements(PlayerControl player)
    {
        if (player == null) return Task.CompletedTask;

        var data = CustomAchievementDataManager.Get(player.FriendCode);
        if (data == null || !data.HasData)
        {
            Utils.SendMessage(GetString("Achievement.LoadingFromServer.ServerBomb"), player.PlayerId,
                $"<color=#FF4444>{GetString("Achievement.ChatTitle")}</color>");
            return Task.CompletedTask;
        }

        var records = data.Records.Where(CustomAchievementData.IsUnlocked).ToList();
        if (records.Count == 0)
        {
            Utils.SendMessage(GetString("Achievement.LoadingFromServer.Empty"), player.PlayerId,
                $"<color=#FFD700>{GetString("Achievement.ChatTitle")}</color>");
            return Task.CompletedTask;
        }

        var message = new StringBuilder()
            .AppendLine(string.Format(GetString("Achievement.Info.AllCount"), records.Count))
            .AppendLine();

        foreach (var record in records)
        {
            var definition = AchievementRegistry.GetById(record.Id);
            var color = definition?.TitleColorHex ?? "#FFFFFF";
            var progress = definition?.IsProgressAchievement == true
                ? $" <size=70%>({record.Progress}/{definition.RequiredValue})</size>"
                : "";
            var name = definition != null ? GetString(definition.Name) : record.Name;
            message.AppendLine($"<color={color}>【{record.Id}】{name}</color>{progress}");
            message.AppendLine($"<size=70%>{string.Format(GetString("Achievement.Info.UnlockedAt"), record.UnlockedAt)}</size>");
        }

        message.AppendLine().Append(GetString("Achievement.Tip1"));
        Utils.SendMessage(message.ToString().TrimEnd(), player.PlayerId,
            $"<color=#FFD700>{GetString("Achievement.ChatTitle")}</color>");
        return Task.CompletedTask;
    }

    private static async Task SyncLocalAsync(string friendCode)
    {
        var data = CustomAchievementDataManager.GetOrCreate(friendCode, true);
        var (records, _) = await AchievementApiClient.GetRecordsWithStatusAsync(friendCode);
        if (records == null) return;

        data.MergeCloud(records);
        var title = await AchievementApiClient.GetWearedTitleAsync(friendCode);
        if (title is int titleId) data.SetWearedTitle(titleId);
        data.StartRound();
    }
    public static async Task WearAchievementTitleAndReplyAsync(PlayerControl player, int titleId)
    {
        if (player == null) return;

        var result = await TONX.Modules.Achievements.Core.AchievementManager.WearAchievementTitle(player.FriendCode, titleId);
        var def = TONX.Modules.Achievements.Core.AchievementRegistry.GetById(titleId);

        string reply = result switch
        {
            TONX.Modules.Achievements.Core.TitleChangeResult.Unknown
                => string.Format(GetString("Achievement.Command.Fail"), titleId),
            TONX.Modules.Achievements.Core.TitleChangeResult.NotOwned
                => string.Format(GetString("Achievement.Command.NoBelong"), titleId),
            TONX.Modules.Achievements.Core.TitleChangeResult.Ok => def == null
                ? GetString("Achievement.Command.UnWear")
                : string.Format(GetString("Achievement.Command.Wear"), def.TitleColorHex, GetString(def.Name)),
            _ => string.Format(GetString("Achievement.Command.Fail"), titleId),
        };

        _ = new LateTask(() =>
        {
            Utils.SendMessage(reply, player.PlayerId, $"<color=#FFD700>{GetString("Achievement.ChatTitle")}</color>");
        },0f, "WearAchievementTitleAndReplyAsync");
    }
}
