using System.Text;
using TONX.Modules.Achievements.Titles;

namespace TONX.Modules.Achievements.Core;

/// <summary>
/// 成就系统的事件分发与生命周期管理。
/// </summary>
public static class AchievementManager
{
    /// <summary>拉取某玩家的成就数据</summary>
    public static async Task FetchAsync(string friendCode)
    {
        var data = CustomAchievementDataManager.GetOrCreate(friendCode);
        if (data == null) return;

        await AchievementApiClient.EnsureFetchedAsync(data);
    }

    /// <summary>拉取在场的所有玩家</summary>
    public static async Task FetchAllAsync()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        foreach (var player in Main.AllPlayerControls)
        {
            if (player == null || string.IsNullOrEmpty(player.FriendCode)) continue;
            await FetchAsync(player.FriendCode);
        }
    }

    /// <summary>上传所有有增量的玩家。对局结束 / 房主断链时调用</summary>
    public static async Task<bool> FlushAllAsync()
    {
        if (!AmongUsClient.Instance.AmHost) return false;

        var all = CustomAchievementDataManager.GetAll();
        return await AchievementApiClient.SyncBatchAsync(all);
    }

    /// <summary>上传指定玩家的增量。玩家断链时调用</summary>
    public static async Task<bool> FlushAsync(string friendCode)
    {
        if (!AmongUsClient.Instance.AmHost) return false;

        var data = CustomAchievementDataManager.Get(friendCode);
        if (data == null || !data.HasPendingChanges) return true;

        return await AchievementApiClient.SyncBatchAsync(new[] { data });
    }
    
    public static void OnPlayerJoined(string friendCode)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (string.IsNullOrEmpty(friendCode)) return;

        _ = FetchAndAnnounceAsync(friendCode);
    }

    public static void OnLocalJoined()
    {
        var friendCode = CustomAchievementDataManager.LocalFriendCode;
        if (string.IsNullOrEmpty(friendCode))
        {
            Logger.Warn("LocalJoined: no local friend code, skip achievement fetch", "Achievement");
            return;
        }

        _ = FetchAndAnnounceAsync(friendCode);
    }

    private static async Task FetchAndAnnounceAsync(string friendCode)
    {
        var data = CustomAchievementDataManager.GetOrCreate(friendCode);
        if (data == null) return;

        await AchievementApiClient.EnsureFetchedAsync(data);

        
        _ = new LateTask(() =>
        {
            var player = Main.AllPlayerControls.FirstOrDefault(p => p.FriendCode == friendCode);
            if (player == null) return;

            AnnounceUnlocked(data, player);
        }, 0f, $"Achievement Announce {friendCode}");
    }
    
    private static void AnnounceUnlocked(CustomAchievementData data, PlayerControl player)
    {
        foreach (var record in data.TakeUnannounced())
        {
            var def = AchievementRegistry.GetById(record.Id);
            string colorHex = def?.TitleColorHex ?? "#FFD700";
            string name = string.IsNullOrEmpty(record.Name) ? def?.Name ?? record.Id.ToString() : record.Name;
            string msg = string.Format(GetString("Achievement.Unlocked"), colorHex, name);
            Utils.SendMessage(msg, player.PlayerId, $"<color=#FFD700>{GetString("Achievement.ChatTitle")}</color>");
        }
    }
    
    public static async Task FetchAndDisplayAchievementsAsync(PlayerControl player)
    {
        if (player == null) return;
        string friendCode = player.FriendCode;

        Utils.SendMessage(GetString("Achievement.LoadingFromServer"), player.PlayerId, $"<color=#FFD700>{GetString("Achievement.ChatTitle")}</color>");

        var (records, status) = await AchievementApiClient.GetRecordsWithStatusAsync(friendCode);
        if (records == null)
        {
            string failText = status > 0
                ? string.Format(GetString("Achievement.LoadingFromServer.Fail"), status)
                : GetString("Achievement.LoadingFromServer.ServerBomb");
            _ = new LateTask(() => Utils.SendMessage(failText,
                player.PlayerId, $"<color=#FF4444>{GetString("Achievement.ChatTitle")}</color>"), 0f, "Ach Display Fail");
            return;
        }

        var data = CustomAchievementDataManager.GetOrCreate(friendCode);
        data?.MergeServer(records);
        data?.MarkLoaded();

        _ = new LateTask(() =>
        {
            if (!records.Any())
            {
                Utils.SendMessage(GetString("Achievement.LoadingFromServer.Empty"), player.PlayerId, $"<color=#FFD700>{GetString("Achievement.ChatTitle")}</color>");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"{string.Format(GetString("Achievement.Info.AllCount"), records.Count)}\n");
            foreach (var r in records)
            {
                var def = AchievementRegistry.GetById(r.Id);
                string colorHex = def?.TitleColorHex ?? "#FFFFFF";
                string progressText = def != null && def.IsProgressAchievement ? $" <size=70%>({r.Progress}/{def.RequiredValue})</size>" : "";
                sb.AppendLine($"<color={colorHex}>【{r.Id}】{r.Name}</color>{progressText}");
                sb.AppendLine($"<size=70%>{string.Format(GetString("Achievement.Info.UnlockedAt"), r.UnlockedAt)}</size>\n");
            }
            sb.AppendLine($"\n{GetString("Achievement.Tip1")}");

            Utils.SendMessage(sb.ToString().TrimEnd(), player.PlayerId, $"<color=#FFD700>{GetString("Achievement.ChatTitle")}</color>");
        }, 0f, $"Ach Display {friendCode}");
    }

    
    public static async Task WearTitleAsync(PlayerControl player, int achievementId)
    {
        if (player == null) return;

        if (achievementId <= 0)
        {
            AchievementTitleHandler.SendTitleSyncRpc(player.PlayerId, 0);
            Utils.SendMessage(GetString("Achievement.Command.UnWear"), player.PlayerId, $"<color=#FFD700>{GetString("Achievement.ChatTitle")}</color>");
            return;
        }

        var def = AchievementRegistry.GetById(achievementId);
        if (def == null)
        {
            Utils.SendMessage(string.Format(GetString("Achievement.Command.Fail"), achievementId), player.PlayerId, $"<color=#FFD700>{GetString("Achievement.ChatTitle")}</color>");
            return;
        }

        var data = player.GetAchievementInfo(fetchIfMissing: true);
        if (data == null || !data.Has(achievementId))
        {
            var records = await AchievementApiClient.GetRecordsAsync(player.FriendCode);
            if (records != null)
            {
                var refreshed = CustomAchievementDataManager.GetOrCreate(player.FriendCode);
                refreshed?.MergeServer(records);
                data = refreshed;
            }
        }

        if (data == null || !data.Has(achievementId))
        {
            Utils.SendMessage(string.Format(GetString("Achievement.Command.NoBelong"), def.Id), player.PlayerId, $"<color=#FFD700>{GetString("Achievement.ChatTitle")}</color>");
            return;
        }

        AchievementTitleHandler.SendTitleSyncRpc(player.PlayerId, achievementId);
        Utils.SendMessage(string.Format(GetString("Achievement.Command.Wear"), def.TitleColorHex, def.TitleDisplay),
            player.PlayerId, $"<color=#FFD700>{GetString("Achievement.ChatTitle")}</color>");
    }
}
