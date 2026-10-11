namespace TONX.Modules.Achievements.Core;

public abstract class SingleUnlockAchievement<TSelf> : AchievementBase
    where TSelf : SingleUnlockAchievement<TSelf>
{
    public sealed override int RequiredValue => 1;

    public void TryUnlock() => TryProgress(1);

    public static void Trigger(PlayerControl player = null) => TriggerFor<TSelf>(player);
}
