using TONX.Modules.Achievements.Core;
using UnityEngine;

namespace TONX.Modules.Achievements.Impostor;

public sealed class TrackerTargetLocked : SingleUnlockAchievement<TrackerTargetLocked>
{
    public override int Id => 102301;
    public override Color TitleColor => new Color32(0xFF, 0x19, 0x19, 0xFF);
    public override AchievementCategory Category => AchievementCategory.Impostor;
}
