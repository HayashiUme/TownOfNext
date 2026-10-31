using TONX.Modules.Achievements.Core;
using TONX.Roles.Crewmate;
using UnityEngine;

namespace TONX.Modules.Achievements.Crewmate;

public sealed class DeceiverMisfireWin : SingleUnlockAchievement<DeceiverMisfireWin>
{
    public override int Id => 202102;
    public override Color TitleColor => new Color32(80, 190, 255, 255);
    public override AchievementCategory Category => AchievementCategory.Crewmate;
    public override AchievementRarity Rarity => AchievementRarity.Rare;
    public override bool Hidden => true;
    public override bool CheckClear(PlayerControl player)
        => player?.Data != null && !player.Data.Disconnected
           && player.GetRoleClass() is Deceiver { CounterfeitMisfireWin: true }
           && CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate
           && (CustomWinnerHolder.WinnerIds.Contains(player.PlayerId)
               || CustomWinnerHolder.WinnerRoles.Contains(CustomRoles.Crewmate));
}
