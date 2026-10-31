using TONX.Modules.Achievements.Core;
using TONX.Roles.Neutral;
using UnityEngine;

namespace TONX.Modules.Achievements.Neutral;

public sealed class ProvocateurTargetLoses : SingleUnlockAchievement<ProvocateurTargetLoses>
{
    public override int Id => 301802;
    public override Color TitleColor => new Color32(180, 130, 255, 255);
    public override AchievementCategory Category => AchievementCategory.Neutral;
    public override AchievementRarity Rarity => AchievementRarity.Rare;
    public override bool Hidden => true;
    public override bool CheckClear(PlayerControl player)
    {
        if (player?.Data == null || player.Data.Disconnected
            || player.GetRoleClass() is not Provocateur { ExplosionWinGranted: true } role
            || !CustomWinnerHolder.WinnerIds.Contains(player.PlayerId)) return false;
        var target = Main.AllPlayerControls.FirstOrDefault(pc => pc.PlayerId == role.ExplosionTargetId);
        return target?.Data != null && !CustomWinnerHolder.WinnerIds.Contains(target.PlayerId)
            && !CustomWinnerHolder.WinnerRoles.Contains(target.GetCustomRole());
    }
}
