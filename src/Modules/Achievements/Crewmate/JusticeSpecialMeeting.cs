using TONX.Modules.Achievements.Core;
using UnityEngine;

namespace TONX.Modules.Achievements.Crewmate;

public sealed class JusticeSpecialMeeting : SingleUnlockAchievement<JusticeSpecialMeeting>
{
    public override int Id => 203301;
    public override Color TitleColor => new Color32(80, 190, 255, 255);
    public override AchievementCategory Category => AchievementCategory.Crewmate;
}
