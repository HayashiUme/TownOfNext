using TONX.Modules.Achievements.Core;
using UnityEngine;

namespace TONX.Modules.Achievements.Crewmate;

public sealed class VentLocked : SingleUnlockAchievement<VentLocked>
{
    public override int Id => 203401;
    public override Color TitleColor => new Color32(0x4B, 0x6E, 0x8C, 0xFF);
    public override AchievementCategory Category => AchievementCategory.Crewmate;
}
