using TONX.Modules.Achievements.Core;
using TONX.Roles.Crewmate;
using UnityEngine;

namespace TONX.Modules.Achievements.Crewmate;

public sealed class ObserverThreeShieldTruth : SingleUnlockAchievement<ObserverThreeShieldTruth>
{
    public override int Id => 202902;
    public override Color TitleColor => new Color32(80, 190, 255, 255);
    public override AchievementCategory Category => AchievementCategory.Crewmate;
    public override AchievementRarity Rarity => AchievementRarity.Epic;
    public override bool Hidden => true;
    public override bool CheckClear(PlayerControl player)
        => player?.Data != null && !player.Data.Disconnected
           && player.GetRoleClass() is Observer { ShieldNoticeCount: >= 3, ShieldNoticeFollowedByEvilExile: true }
           && CustomWinnerHolder.WinnerIds.Contains(player.PlayerId);
}
