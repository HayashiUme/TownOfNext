using TONX.Modules.Achievements.Game;

namespace TONX.Modules.Achievements.Core.Base;

public abstract class AchievementBase<TSelf> : AchievementBase
    where TSelf : AchievementBase<TSelf>
{
    public static void Trigger(PlayerControl player = null) => TriggerFor<TSelf>(player);
}
