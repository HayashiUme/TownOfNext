namespace TONX.Modules.Achievements.Core;

public sealed class StaticAchievementToken
{
    public StaticAchievementToken(int achievementId, PlayerControl player = null)
    {
        if (player != null && (player.Data == null || player.Data.Disconnected)) return;

        // Only an omitted player selects the local identity. A missing actor code
        // must never redirect another player's achievement to the host.
        var friendCode = player == null ? CustomAchievementDataManager.LocalFriendCode : player.FriendCode;
        AchievementManager.TryUpdateUnit(friendCode, achievementId);
    }
}
