using TONX.Modules.Achievements.Core;
using TONX.Roles.Neutral;
using UnityEngine;

namespace TONX.Modules.Achievements.Neutral;

public sealed class NeutralPosthumousFame : SingleUnlockAchievement<NeutralPosthumousFame>
{
    public override int Id => 500305;
    public override Color TitleColor => new Color32(180, 130, 255, 255);
    public override AchievementCategory Category => AchievementCategory.Neutral;
    public override AchievementRarity Rarity => AchievementRarity.Rare;
    public override bool CheckClear(PlayerControl player)
        => player?.Data != null && !player.Data.Disconnected && player.Data.IsDead
           && player.GetCustomRole().IsNeutral()
           && (CustomWinnerHolder.WinnerIds.Contains(player.PlayerId)
               || CustomWinnerHolder.WinnerRoles.Contains(player.GetCustomRole()))
           && (player.GetRoleClass() switch
           {
               Jester { ExileWinTriggered: true } => CustomWinnerHolder.WinnerTeam == CustomWinner.Jester
                   || CustomWinnerHolder.AdditionalWinnerRoles.Contains(CustomRoles.Jester),
               Terrorist { TaskDeathWinTriggered: true } => CustomWinnerHolder.WinnerTeam == CustomWinner.Terrorist,
               Innocent { FrameWinTriggered: true } => CustomWinnerHolder.WinnerTeam == CustomWinner.Innocent
                   || CustomWinnerHolder.AdditionalWinnerRoles.Contains(CustomRoles.Innocent),
               Provocateur { ExplosionWinGranted: true } => CustomWinnerHolder.AdditionalWinnerRoles.Contains(CustomRoles.Provocateur),
               Sunnyboy { DeathWinGranted: true } => CustomWinnerHolder.AdditionalWinnerRoles.Contains(CustomRoles.Sunnyboy),
               _ => false
           });
}
