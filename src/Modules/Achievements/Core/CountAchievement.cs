namespace TONX.Modules.Achievements.Core;

public abstract class CountAchievement<TSelf> : AchievementBase
    where TSelf : CountAchievement<TSelf>
{
    public abstract override int RequiredValue { get; }

    public void Increment(int count = 1) => TryProgress(GetProgress() + count, PlayerControl.LocalPlayer);

    public void SetProgress(int value) => TryProgress(value, PlayerControl.LocalPlayer);

    public static void Trigger(PlayerControl player = null) => TriggerFor<TSelf>(player);
}
