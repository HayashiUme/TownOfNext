namespace TONX.Modules.Achievements.Core.Base;

public abstract class SingleUnlockAchievement<TSelf> : SingleUnlockAchievement
    where TSelf : SingleUnlockAchievement<TSelf>
{
    public static void Trigger(PlayerControl player = null) => TriggerFor<TSelf>(player);
}
