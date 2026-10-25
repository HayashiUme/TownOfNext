namespace TONX.Modules.Achievements.Core;

public static class AchievementTitleService
{
    public static int GetWearedTitleId(string friendCode)
        => CustomAchievementDataManager.Get(friendCode)?.WearedTitleId ?? 0;

    public static async Task<bool> SetAsync(string friendCode, int achievementId)
    {
        if (string.IsNullOrEmpty(friendCode)) return false;

        var id = Math.Max(0, achievementId);
        if (!await AchievementApiClient.SetWearedTitleAsync(friendCode, id)) return false;

        CustomAchievementDataManager.GetOrCreate(friendCode)?.SetWearedTitle(id);
        return true;
    }

    public static async Task RefreshFromCloudAsync(string friendCode)
    {
        if (string.IsNullOrEmpty(friendCode)) return;

        var id = await AchievementApiClient.GetWearedTitleAsync(friendCode);
        if (id is not int titleId) return;

        CustomAchievementDataManager.GetOrCreate(friendCode)?.SetWearedTitle(titleId);
        RefreshNameTags(friendCode);
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
