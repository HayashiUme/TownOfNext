using TONX.Modules.Achievements.Core;
using TONX.Roles.Neutral;
using UnityEngine;

namespace TONX.Modules.Achievements.Neutral;

public sealed class JesterFirstMeetingWin : SingleUnlockAchievement<JesterFirstMeetingWin>
{
    public override int Id => 300202;
    public override Color TitleColor => new Color32(180, 130, 255, 255);
    public override AchievementCategory Category => AchievementCategory.Neutral;

    public override bool CheckClear(PlayerControl player)
        => player?.Data != null && !player.Data.Disconnected && player.Is(CustomRoles.Jester)
           && player.GetRoleClass() is Jester { ExileWinTriggered: true, WonInFirstMeeting: true }
           && (CustomWinnerHolder.WinnerIds.Contains(player.PlayerId)
               || CustomWinnerHolder.WinnerRoles.Contains(player.GetCustomRole()));
}
