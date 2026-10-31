using TONX.Modules.Achievements.Core;
using UnityEngine;

namespace TONX.Modules.Achievements.Neutral;

public sealed class NeutralSoleWinner : SingleUnlockAchievement<NeutralSoleWinner>
{
    public override int Id => 500303;
    public override Color TitleColor => new Color32(180, 130, 255, 255);
    public override AchievementCategory Category => AchievementCategory.Neutral;
    public override AchievementRarity Rarity => AchievementRarity.Legendary;
    public override bool CheckClear(PlayerControl player)
        => player?.Data != null && !player.Data.Disconnected && NeutralIndependentWin.HasBaseWinSource(player)
           && (CustomWinnerHolder.WinnerIds.Contains(player.PlayerId)
               || CustomWinnerHolder.WinnerRoles.Contains(player.GetCustomRole()))
           && CustomWinnerHolder.WinnerIds.All(id => id == player.PlayerId)
           && Main.AllPlayerControls.All(pc => pc.PlayerId == player.PlayerId
               || !CustomWinnerHolder.WinnerRoles.Contains(pc.GetCustomRole()));
}
