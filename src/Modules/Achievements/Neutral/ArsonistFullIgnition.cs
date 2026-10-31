using TONX.Modules.Achievements.Core;
using TONX.Roles.Neutral;
using UnityEngine;

namespace TONX.Modules.Achievements.Neutral;

public sealed class ArsonistFullIgnition : SingleUnlockAchievement<ArsonistFullIgnition>
{
    public override int Id => 300102;
    public override Color TitleColor => new Color32(180, 130, 255, 255);
    public override AchievementCategory Category => AchievementCategory.Neutral;
    public override AchievementRarity Rarity => AchievementRarity.Epic;
    public override bool CheckClear(PlayerControl player)
        => player?.Data != null && !player.Data.Disconnected
           && player.GetRoleClass() is Arsonist { IgnitionWinTriggered: true }
           && CustomWinnerHolder.WinnerTeam == CustomWinner.Arsonist
           && CustomWinnerHolder.WinnerIds.Contains(player.PlayerId);
}
