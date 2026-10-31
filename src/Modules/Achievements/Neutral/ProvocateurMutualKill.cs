using TONX.Modules.Achievements.Core;
using TONX.Roles.Neutral;
using UnityEngine;

namespace TONX.Modules.Achievements.Neutral;

public sealed class ProvocateurMutualKill : SingleUnlockAchievement<ProvocateurMutualKill>
{
    public override int Id => 301801;
    public override Color TitleColor => new Color32(180, 130, 255, 255);
    public override AchievementCategory Category => AchievementCategory.Neutral;
    public override bool CheckClear(PlayerControl player)
        => player?.Data != null && !player.Data.Disconnected && !player.IsAlive()
           && player.GetRoleClass() is Provocateur { HasKilled: true };
}
