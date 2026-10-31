using TONX.Modules.Achievements.Core;
using UnityEngine;

namespace TONX.Modules.Achievements.Neutral;

public sealed class CollectorVoteCollected : SingleUnlockAchievement<CollectorVoteCollected>
{
    public override int Id => 301701;
    public override Color TitleColor => new Color32(180, 130, 255, 255);
    public override AchievementCategory Category => AchievementCategory.Neutral;
}
