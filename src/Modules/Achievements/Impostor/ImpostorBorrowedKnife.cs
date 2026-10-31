using TONX.Modules.Achievements.Core;
using TONX.Roles.Impostor;
using UnityEngine;

namespace TONX.Modules.Achievements.Impostor;

public sealed class ImpostorBorrowedKnife : SingleUnlockAchievement<ImpostorBorrowedKnife>
{
    public override int Id => 500106;
    public override Color TitleColor => new Color32(255, 25, 25, 255);
    public override AchievementCategory Category => AchievementCategory.Impostor;
    public override AchievementRarity Rarity => AchievementRarity.Rare;
    public override bool CheckClear(PlayerControl player)
        => player?.Data != null && !player.Data.Disconnected && !player.Is(CustomRoles.Charmed)
           && player.GetCustomRole().IsImpostor()
           && player.GetRoleClass() is (Warlock { NonImpostorPuppetKill: true }
               or Puppeteer { NonImpostorPuppetKill: true })
           && CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor
           && (CustomWinnerHolder.WinnerIds.Contains(player.PlayerId)
               || CustomWinnerHolder.WinnerRoles.Contains(player.GetCustomRole()));
}
