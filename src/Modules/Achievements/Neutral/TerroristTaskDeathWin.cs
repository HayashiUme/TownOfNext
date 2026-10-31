using TONX.Modules.Achievements.Core;
using TONX.Roles.Neutral;
using UnityEngine;

namespace TONX.Modules.Achievements.Neutral;

public sealed class TerroristTaskDeathWin : SingleUnlockAchievement<TerroristTaskDeathWin>
{
    public override int Id => 300401;
    public override Color TitleColor => new Color32(180, 130, 255, 255);
    public override AchievementCategory Category => AchievementCategory.Neutral;
    public override bool CheckClear(PlayerControl player)
        => player?.Data != null && !player.Data.Disconnected && player.Data.IsDead
           && player.GetRoleClass() is Terrorist { TaskDeathWinTriggered: true }
           && CustomWinnerHolder.WinnerTeam == CustomWinner.Terrorist
           && CustomWinnerHolder.WinnerIds.Contains(player.PlayerId);
}
