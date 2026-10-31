using TONX.Modules.Achievements.Core;
using TONX.Roles.Neutral;
using UnityEngine;

namespace TONX.Modules.Achievements.Neutral;

public sealed class MarioVentWin : SingleUnlockAchievement<MarioVentWin>
{
    public override int Id => 300902;
    public override Color TitleColor => new Color32(180, 130, 255, 255);
    public override AchievementCategory Category => AchievementCategory.Neutral;
    public override AchievementRarity Rarity => AchievementRarity.Rare;
    public override bool CheckClear(PlayerControl player)
        => player?.Data != null && !player.Data.Disconnected
           && player.GetRoleClass() is Mario { VentWinTriggered: true }
           && CustomWinnerHolder.WinnerTeam == CustomWinner.Mario
           && CustomWinnerHolder.WinnerIds.Contains(player.PlayerId);
}
