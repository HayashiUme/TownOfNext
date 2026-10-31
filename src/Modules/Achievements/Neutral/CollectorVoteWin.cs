using TONX.Modules.Achievements.Core;
using UnityEngine;

namespace TONX.Modules.Achievements.Neutral;

public sealed class CollectorVoteWin : SingleUnlockAchievement<CollectorVoteWin>
{
    public override int Id => 301702;
    public override Color TitleColor => new Color32(180, 130, 255, 255);
    public override AchievementCategory Category => AchievementCategory.Neutral;
    public override AchievementRarity Rarity => AchievementRarity.Rare;
    public override bool Hidden => true;
    public override bool CheckClear(PlayerControl player)
        => player?.Data != null && !player.Data.Disconnected
           && CustomWinnerHolder.WinnerTeam == CustomWinner.Collector
           && CustomWinnerHolder.WinnerIds.Contains(player.PlayerId);
}
