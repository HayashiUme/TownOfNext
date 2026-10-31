using TONX.Modules.Achievements.Core;
using UnityEngine;

namespace TONX.Modules.Achievements.Neutral;

public sealed class SidekickSurvivalWin : SingleUnlockAchievement<SidekickSurvivalWin>
{
    public override int Id => 300701;
    public override Color TitleColor => new Color32(180, 130, 255, 255);
    public override AchievementCategory Category => AchievementCategory.Neutral;
    public override bool CheckClear(PlayerControl player)
        => player?.Data != null && !player.Data.Disconnected && player.IsAlive()
           && player.Is(CustomRoles.Sidekick)
           && CustomWinnerHolder.WinnerTeam == CustomWinner.Jackal
           && CustomWinnerHolder.WinnerIds.Contains(player.PlayerId);
}
