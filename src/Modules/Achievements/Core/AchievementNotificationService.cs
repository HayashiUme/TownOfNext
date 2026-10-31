using System.Text;

namespace TONX.Modules.Achievements.Core;

public static class AchievementNotificationService
{
    public static void Send(IReadOnlyList<GameAchievementResult> results)
    {
        if (results == null) return;

        foreach (var result in results.Where(x => x != null && x.PlayerId != byte.MaxValue && x.Unlocked.Count > 0))
            _ = new LateTask(() => Send(result.PlayerId, result.Unlocked), 0f, "AchievementNotification");
    }

    private static void Send(byte playerId, IReadOnlyList<AchievementBase> achievements)
    {
        var message = new StringBuilder()
            .Append("<align=\"center\">")
            .Append("<size=150%>").Append(GetString("Achievement.GameEnd.Title")).Append("</size>\n")
            .Append(string.Format(GetString("Achievement.GameEnd.Count"), achievements.Count))
            .Append("</align><size=75%>\n");

        foreach (var achievement in achievements)
        {
            message.Append("\n<color=").Append(achievement.Rarity.GetColorCode()).Append(">★</color> ")
                .Append("<color=").Append(achievement.TitleColorHex).Append(">【")
                .Append(GetString(achievement.Name)).Append("】</color> <size=70%>")
                .Append(achievement.Rarity.GetDisplayName()).Append("</size>");
        }

        message.Append("</size>");
        Utils.SendMessage(message.ToString(), playerId, $"<color=#FFD700>{GetString("Achievement.ChatTitle")}</color>");
    }
}
