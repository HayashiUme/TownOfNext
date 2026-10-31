using TONX.Modules.Achievements.Core;
using UnityEngine;

namespace TONX.Modules.Achievements.Neutral;

public sealed class CharmCompleted : SingleUnlockAchievement<CharmCompleted>
{
    public override int Id => 302101;
    public override Color TitleColor => new Color32(0xB4, 0x82, 0xFF, 0xFF);
    public override AchievementCategory Category => AchievementCategory.Neutral;
}
