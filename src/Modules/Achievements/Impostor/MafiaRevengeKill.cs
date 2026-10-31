using TONX.Modules.Achievements.Core;
using UnityEngine;

namespace TONX.Modules.Achievements.Impostor;

public sealed class MafiaRevengeKill : SingleUnlockAchievement<MafiaRevengeKill>
{
    public override int Id => 101201;
    public override Color TitleColor => new Color32(255, 25, 25, 255);
    public override AchievementCategory Category => AchievementCategory.Impostor;
    public override AchievementRarity Rarity => AchievementRarity.Rare;
}
