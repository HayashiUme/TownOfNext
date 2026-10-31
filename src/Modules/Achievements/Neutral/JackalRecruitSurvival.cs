using TONX.Modules.Achievements.Core;
using TONX.Roles.Neutral;
using UnityEngine;

namespace TONX.Modules.Achievements.Neutral;

public sealed class JackalRecruitSurvival : SingleUnlockAchievement<JackalRecruitSurvival>
{
    public override int Id => 300602;
    public override Color TitleColor => new Color32(180, 130, 255, 255);
    public override AchievementCategory Category => AchievementCategory.Neutral;
    public override AchievementRarity Rarity => AchievementRarity.Rare;
    public override bool CheckClear(PlayerControl player)
        => player?.Data != null && !player.Data.Disconnected && player.IsAlive()
           && player.GetCountTypes() == CountTypes.Jackal
           && player.GetRoleClass() is Jackal { HasSurvivingRecruit: true }
           && CustomWinnerHolder.WinnerTeam == CustomWinner.Jackal
           && (CustomWinnerHolder.WinnerIds.Contains(player.PlayerId)
               || CustomWinnerHolder.WinnerRoles.Contains(CustomRoles.Jackal));
}
