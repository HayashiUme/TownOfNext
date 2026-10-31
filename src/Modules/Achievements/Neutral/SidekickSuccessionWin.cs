using TONX.Modules.Achievements.Core;
using TONX.Roles.Neutral;
using UnityEngine;

namespace TONX.Modules.Achievements.Neutral;

public sealed class SidekickSuccessionWin : SingleUnlockAchievement<SidekickSuccessionWin>
{
    public override int Id => 300702;
    public override Color TitleColor => new Color32(180, 130, 255, 255);
    public override AchievementCategory Category => AchievementCategory.Neutral;
    public override AchievementRarity Rarity => AchievementRarity.Epic;
    public override bool CheckClear(PlayerControl player)
        => player?.Data != null && !player.Data.Disconnected
           && player.GetRoleClass() is Jackal { SucceededFromSidekick: true }
           && player.GetCountTypes() == CountTypes.Jackal
           && CustomWinnerHolder.WinnerTeam == CustomWinner.Jackal
           && (CustomWinnerHolder.WinnerIds.Contains(player.PlayerId)
               || CustomWinnerHolder.WinnerRoles.Contains(CustomRoles.Jackal));
}
