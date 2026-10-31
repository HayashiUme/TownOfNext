using TONX.Modules.Achievements.Core;
using TONX.Roles.Neutral;
using UnityEngine;

namespace TONX.Modules.Achievements.Neutral;

public sealed class ProsecutorInterceptionWin : SingleUnlockAchievement<ProsecutorInterceptionWin>
{
    public override int Id => 302902;
    public override Color TitleColor => new Color32(180, 130, 255, 255);
    public override AchievementCategory Category => AchievementCategory.Neutral;

    public override bool CheckClear(PlayerControl player)
        => player?.Data != null && player.IsAlive() && player.Is(CustomRoles.Prosecutors)
           && player.GetRoleClass() is Prosecutors { SurvivalWinGranted: true, InterceptedKillers: >= 2 }
           && (CustomWinnerHolder.WinnerIds.Contains(player.PlayerId)
               || CustomWinnerHolder.WinnerRoles.Contains(player.GetCustomRole()));
}
