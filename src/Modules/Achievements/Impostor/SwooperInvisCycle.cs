using TONX.Modules.Achievements.Core;
using UnityEngine;

namespace TONX.Modules.Achievements.Impostor;

public sealed class SwooperInvisCycle : SingleUnlockAchievement<SwooperInvisCycle>
{
    public override int Id => 103801;
    public override Color TitleColor => new Color32(255, 25, 25, 255);
    public override AchievementCategory Category => AchievementCategory.Impostor;
}
