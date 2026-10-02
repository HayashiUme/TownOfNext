namespace TONX.Modules.Achievements.Core.Base;

public abstract class CountAchievement<TSelf> : CountAchievement
    where TSelf : CountAchievement<TSelf>
{
    public static void Trigger(PlayerControl player = null) => TriggerFor<TSelf>(player);
}
