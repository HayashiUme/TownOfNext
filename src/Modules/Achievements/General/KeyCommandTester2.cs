using TONX.Modules.Achievements.Core;
using UnityEngine;

namespace TONX.Modules.Achievements.General;

public sealed class KeyCommandTester2 : SingleUnlockAchievement<KeyCommandTester2>
{
    public const int AchievementId = 9002;

    public override int Id => AchievementId;
    public override Color TitleColor => new Color(0.85f, 0.75f, 0.45f, 1f);
    public override AchievementRarity Rarity => AchievementRarity.Common;
}
