namespace TONX.Modules.Achievements.Core;

public sealed class StaticAchievementToken
{
    public StaticAchievementToken(int achievementId, PlayerControl player = null)
        => AchievementManager.TryUpdateUnit(player?.FriendCode ?? CustomAchievementDataManager.LocalFriendCode, achievementId);
}
