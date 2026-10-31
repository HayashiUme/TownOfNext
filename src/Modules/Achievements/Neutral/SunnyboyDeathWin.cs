using TONX.Modules.Achievements.Core;
using TONX.Roles.Neutral;
using UnityEngine;

namespace TONX.Modules.Achievements.Neutral;

public sealed class SunnyboyDeathWin : SingleUnlockAchievement<SunnyboyDeathWin>
{
    public override int Id => 302701;
    public override Color TitleColor => new Color32(180, 130, 255, 255);
    public override AchievementCategory Category => AchievementCategory.Neutral;
    public override bool CheckClear(PlayerControl player)
        => player?.Data != null && !player.Data.Disconnected
           && player.GetRoleClass() is Sunnyboy { DeathWinGranted: true }
           && CustomWinnerHolder.WinnerIds.Contains(player.PlayerId);
}
