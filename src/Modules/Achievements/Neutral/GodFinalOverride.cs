using TONX.Modules.Achievements.Core;
using TONX.Roles.Neutral;
using UnityEngine;

namespace TONX.Modules.Achievements.Neutral;

public sealed class GodFinalOverride : SingleUnlockAchievement<GodFinalOverride>
{
    public override int Id => 300802;
    public override Color TitleColor => new Color32(180, 130, 255, 255);
    public override AchievementCategory Category => AchievementCategory.Neutral;
    public override AchievementRarity Rarity => AchievementRarity.Epic;
    public override bool CheckClear(PlayerControl player)
        => player?.Data != null && !player.Data.Disconnected && player.IsAlive()
           && player.GetRoleClass() is God { MeetingsSurvived: >= 3, OverrideWinTriggered: true }
           && CustomWinnerHolder.WinnerTeam == CustomWinner.God
           && (CustomWinnerHolder.WinnerIds.Contains(player.PlayerId)
               || CustomWinnerHolder.WinnerRoles.Contains(CustomRoles.God));
}
