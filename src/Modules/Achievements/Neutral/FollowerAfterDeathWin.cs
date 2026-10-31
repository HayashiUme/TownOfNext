using TONX.Modules.Achievements.Core;
using TONX.Roles.Neutral;
using UnityEngine;

namespace TONX.Modules.Achievements.Neutral;

public sealed class FollowerAfterDeathWin : SingleUnlockAchievement<FollowerAfterDeathWin>
{
    public override int Id => 302002;
    public override Color TitleColor => new Color32(180, 130, 255, 255);
    public override AchievementCategory Category => AchievementCategory.Neutral;
    public override AchievementRarity Rarity => AchievementRarity.Rare;
    public override bool Hidden => true;
    public override bool CheckClear(PlayerControl player)
        => player?.Data != null && !player.Data.Disconnected && !player.IsAlive()
           && player.GetRoleClass() is Follower { BetWinGranted: true }
           && CustomWinnerHolder.WinnerIds.Contains(player.PlayerId);
}
