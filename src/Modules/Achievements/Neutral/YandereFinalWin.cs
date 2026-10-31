using TONX.Modules.Achievements.Core;
using UnityEngine;

namespace TONX.Modules.Achievements.Neutral;

public sealed class YandereFinalWin : SingleUnlockAchievement<YandereFinalWin>
{
    public override int Id => 302602;
    public override Color TitleColor => new Color32(180, 130, 255, 255);
    public override AchievementCategory Category => AchievementCategory.Neutral;
    public override AchievementRarity Rarity => AchievementRarity.Epic;
    public override bool Hidden => true;
    public override bool CheckClear(PlayerControl player)
        => player?.Data != null && !player.Data.Disconnected
           && CustomWinnerHolder.WinnerTeam == CustomWinner.Yandere
           && CustomWinnerHolder.WinnerIds.Contains(player.PlayerId);
}
