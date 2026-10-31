using TONX.Modules.Achievements.Core;
using TONX.Roles.Neutral;
using UnityEngine;

namespace TONX.Modules.Achievements.Neutral;

public sealed class NeutralLinkedAfterDeathWin : SingleUnlockAchievement<NeutralLinkedAfterDeathWin>
{
    public override int Id => 500304;
    public override Color TitleColor => new Color32(180, 130, 255, 255);
    public override AchievementCategory Category => AchievementCategory.Neutral;
    public override AchievementRarity Rarity => AchievementRarity.Rare;
    public override bool CheckClear(PlayerControl player)
        => player?.Data != null && !player.Data.Disconnected && player.Data.IsDead
           && (player.GetRoleClass() is Follower { BetWinGranted: true }
               or Lawyer { ClientWinGranted: true, ClientWinRemoved: false })
           && CustomWinnerHolder.WinnerIds.Contains(player.PlayerId);
}
