using TONX.Modules.Achievements.Core;
using UnityEngine;

namespace TONX.Modules.Achievements.Crewmate;

public sealed class RepairmanOneClickFix : SingleUnlockAchievement<RepairmanOneClickFix>
{
    public override int Id => 200701;
    public override Color TitleColor => new Color32(80, 190, 255, 255);
    public override AchievementCategory Category => AchievementCategory.Crewmate;
}
