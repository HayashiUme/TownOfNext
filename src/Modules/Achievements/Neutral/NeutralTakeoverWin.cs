using TONX.Modules.Achievements.Core;
using UnityEngine;

namespace TONX.Modules.Achievements.Neutral;

public sealed class NeutralTakeoverWin : SingleUnlockAchievement<NeutralTakeoverWin>
{
    public override int Id => 500307;
    public override Color TitleColor => new Color32(180, 130, 255, 255);
    public override AchievementCategory Category => AchievementCategory.Neutral;
    public override AchievementRarity Rarity => AchievementRarity.Epic;
    public override bool CheckClear(PlayerControl player)
        => player?.Data != null && !player.Data.Disconnected && player.GetCustomRole().IsNeutral()
           && GameEndChecker.NeutralOverrideSources.TryGetValue(player.PlayerId, out var source)
           && source == (CustomWinner)player.GetCustomRole() && CustomWinnerHolder.WinnerTeam == source
           && (CustomWinnerHolder.WinnerIds.Contains(player.PlayerId)
               || CustomWinnerHolder.WinnerRoles.Contains(player.GetCustomRole()));
}
