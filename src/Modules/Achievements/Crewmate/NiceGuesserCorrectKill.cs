using TONX.Modules.Achievements.Core;
using UnityEngine;

namespace TONX.Modules.Achievements.Crewmate;

public sealed class NiceGuesserCorrectKill : SingleUnlockAchievement<NiceGuesserCorrectKill>
{
    public override int Id => 200101;
    public override Color TitleColor => new Color32(80, 190, 255, 255);
    public override AchievementCategory Category => AchievementCategory.Crewmate;
}
