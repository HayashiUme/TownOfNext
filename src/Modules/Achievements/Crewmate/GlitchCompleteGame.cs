using TONX.Modules.Achievements.Core;
using TONX.Roles.Crewmate;
using UnityEngine;

namespace TONX.Modules.Achievements.Crewmate;

public sealed class GlitchCompleteGame : SingleUnlockAchievement<GlitchCompleteGame>
{
    public override int Id => 202501;
    public override Color TitleColor => new Color32(80, 190, 255, 255);
    public override AchievementCategory Category => AchievementCategory.Crewmate;
    public override bool CheckClear(PlayerControl player)
        => player?.Data != null && !player.Data.Disconnected
           && !player.Data.IsDead
           && PlayerState.GetByPlayerId(player.PlayerId)?.IsDead == false
           && player.GetRoleClass() is Glitch
           && CustomWinnerHolder.WinnerIds.Contains(player.PlayerId);
}
