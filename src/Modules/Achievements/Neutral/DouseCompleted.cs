using TONX.Modules.Achievements.Core;
using UnityEngine;

namespace TONX.Modules.Achievements.Neutral;

public sealed class DouseCompleted : SingleUnlockAchievement<DouseCompleted>
{
    public override int Id => 300101;
    public override Color TitleColor => new Color32(0xFF, 0x66, 0x33, 0xFF);
    public override AchievementCategory Category => AchievementCategory.Neutral;
}
