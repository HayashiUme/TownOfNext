using TONX.Modules.Achievements.Core;
using UnityEngine;

namespace TONX.Modules.Achievements.Neutral;

public sealed class BetPlaced : SingleUnlockAchievement<BetPlaced>
{
    public override int Id => 302001;
    public override Color TitleColor => new Color32(0xFF, 0x94, 0x09, 0xFF);
    public override AchievementCategory Category => AchievementCategory.Neutral;
}
