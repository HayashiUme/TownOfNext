using TONX.Modules.Achievements.Core;
using TONX.Roles.Neutral;
using UnityEngine;

namespace TONX.Modules.Achievements.Neutral;

public sealed class SuccubusCharmMajority : SingleUnlockAchievement<SuccubusCharmMajority>
{
    public override int Id => 302102;
    public override Color TitleColor => new Color32(180, 130, 255, 255);
    public override AchievementCategory Category => AchievementCategory.Neutral;
    public override AchievementRarity Rarity => AchievementRarity.Epic;
    public override bool CheckClear(PlayerControl player)
        => player?.Data != null && !player.Data.Disconnected
           && player.GetRoleClass() is Succubus { CharmedCount: >= 2 }
           && CustomWinnerHolder.WinnerTeam == CustomWinner.Succubus
           && CustomWinnerHolder.WinnerIds.Contains(player.PlayerId);
}
