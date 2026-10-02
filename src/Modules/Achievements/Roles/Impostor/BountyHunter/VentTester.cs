using TONX.Modules.Achievements.Core;
using TONX.Modules.Achievements.Core.Base;
using UnityEngine;

namespace TONX.Modules.Achievements.Roles.Impostor.BountyHunter;

//仅用于测试成就
public sealed class VentTester : SingleUnlockAchievement<VentTester>
{
    public const int AchievementId = 9001;

    public override int Id => AchievementId;
    public override string Name => "管道测试";
    public override string Description => "赏金猎人跳一次管道";
    public override string TitleDisplay => "管道测试";
    public override Color TitleColor => new Color(0.45f, 0.85f, 0.55f, 1f);
    public override AchievementRarity Rarity => AchievementRarity.Common;
    public override string Category => "Impostor";
}
