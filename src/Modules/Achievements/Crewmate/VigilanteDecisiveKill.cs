using TONX.Modules.Achievements.Core;
using TONX.Roles.Crewmate;
using UnityEngine;

namespace TONX.Modules.Achievements.Crewmate;

public sealed class VigilanteDecisiveKill : SingleUnlockAchievement<VigilanteDecisiveKill>
{
    public override int Id => 201602;
    public override Color TitleColor => new Color32(80, 190, 255, 255);
    public override AchievementCategory Category => AchievementCategory.Crewmate;
    public override AchievementRarity Rarity => AchievementRarity.Epic;
    public override bool CheckClear(PlayerControl player)
        => player?.Data != null && !player.Data.Disconnected
           && player.GetRoleClass() is Vigilante { ImpostorKillWon: true }
           && CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate
           && (CustomWinnerHolder.WinnerIds.Contains(player.PlayerId)
               || CustomWinnerHolder.WinnerRoles.Contains(CustomRoles.Crewmate));
}
