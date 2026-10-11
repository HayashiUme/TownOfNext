using TONX.Modules.Achievements.Core;
using UnityEngine;

namespace TONX.Modules.Achievements.General;

//仅用于测试成就
public sealed class KeyCommandTester : SingleUnlockAchievement<KeyCommandTester>
{
    public const int AchievementId = 9002;

    public override int Id => AchievementId;
    public override string Name => "按键测试";
    public override string Description => "按下 F5+A";
    public override string TitleDisplay => "按键测试";
    public override Color TitleColor => new Color(0.85f, 0.75f, 0.45f, 1f);
    public override AchievementRarity Rarity => AchievementRarity.Common;
}
