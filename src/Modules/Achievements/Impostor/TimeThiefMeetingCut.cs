using TONX.Modules.Achievements.Core;
using UnityEngine;

namespace TONX.Modules.Achievements.Impostor;

public sealed class TimeThiefMeetingCut : SingleUnlockAchievement<TimeThiefMeetingCut>
{
    public override int Id => 101701;
    public override Color TitleColor => new Color32(255, 25, 25, 255);
    public override AchievementCategory Category => AchievementCategory.Impostor;
}
