using TONX.Modules.Achievements.Core;

namespace TONX.Modules.Achievements.Titles;

public static class AchievementTitleHandler
{
    public class AchievementTitleProvider : NameTagManager.INameTagProvider
    {
        public string GetPrefix(PlayerControl player)
        {
            int titleId = AchievementTitleService.GetWearedTitleId(player.FriendCode);
            if (titleId <= 0) return null;

            var achievement = AchievementRegistry.GetById(titleId);
            if (achievement == null) return null;

            return $"<size=75%><color={achievement.TitleColorHex}>[{GetString(achievement.Name)}]</color></size>";
        }
    }
}
