using TONX.Modules.Achievements.Core;
using TONX.Roles.Impostor;
using UnityEngine;

namespace TONX.Modules.Achievements.Impostor;

public sealed class HackerNoBodyReportWin : SingleUnlockAchievement<HackerNoBodyReportWin>
{
    public override int Id => 100702;
    public override Color TitleColor => new Color32(255, 25, 25, 255);
    public override AchievementCategory Category => AchievementCategory.Impostor;
    public override AchievementRarity Rarity => AchievementRarity.Rare;
    public override bool Hidden => true;
    public override bool CheckClear(PlayerControl player)
        => player?.Data != null && !player.Data.Disconnected
           && player.GetRoleClass() is Hacker { NoBodyReport: true }
           && CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor
           && (CustomWinnerHolder.WinnerIds.Contains(player.PlayerId)
               || CustomWinnerHolder.WinnerRoles.Contains(CustomRoles.Impostor));
}
