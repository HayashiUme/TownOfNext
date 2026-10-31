using TONX.Modules.Achievements.Core;
using UnityEngine;

namespace TONX.Modules.Achievements.Crewmate;

public sealed class ShieldGranted : SingleUnlockAchievement<ShieldGranted>
{
    public override int Id => 202301;
    public override Color TitleColor => new Color32(0x00, 0xA4, 0xFF, 0xFF);
    public override AchievementCategory Category => AchievementCategory.Crewmate;
}
