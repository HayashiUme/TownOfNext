using TONX.Modules.Achievements.Core;
using UnityEngine;

namespace TONX.Modules.Achievements.Impostor;

public sealed class BoobyTrapReportKill : SingleUnlockAchievement<BoobyTrapReportKill>
{
    public override int Id => 102201;
    public override Color TitleColor => new Color32(255, 25, 25, 255);
    public override AchievementCategory Category => AchievementCategory.Impostor;
}
