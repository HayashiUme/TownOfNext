using TONX.Modules.Achievements.Core;
using TONX.Roles.Impostor;
using UnityEngine;

namespace TONX.Modules.Achievements.Impostor;

public sealed class InsiderFullPicture : SingleUnlockAchievement<InsiderFullPicture>
{
    public override int Id => 104301;
    public override Color TitleColor => new Color32(255, 25, 25, 255);
    public override AchievementCategory Category => AchievementCategory.Impostor;
    public override bool CheckClear(PlayerControl player)
        => player?.Data != null && !player.Data.Disconnected
           && player.GetRoleClass() is Insider { RevealedRoleInfo: true };
}
