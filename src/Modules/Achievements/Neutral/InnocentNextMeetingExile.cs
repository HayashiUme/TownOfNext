using TONX.Modules.Achievements.Core;
using TONX.Roles.Neutral;
using UnityEngine;

namespace TONX.Modules.Achievements.Neutral;

public sealed class InnocentNextMeetingExile : SingleUnlockAchievement<InnocentNextMeetingExile>
{
    public override int Id => 301002;
    public override Color TitleColor => new Color32(180, 130, 255, 255);
    public override AchievementCategory Category => AchievementCategory.Neutral;
    public override AchievementRarity Rarity => AchievementRarity.Epic;
    public override bool Hidden => true;
    public override bool CheckClear(PlayerControl player)
        => player?.Data != null && !player.Data.Disconnected
           && player.GetRoleClass() is Innocent { FrameWinTriggered: true, NextMeetingFrameExile: true }
           && CustomWinnerHolder.WinnerIds.Contains(player.PlayerId);
}
