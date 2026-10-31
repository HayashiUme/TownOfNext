namespace TONX.Modules.Achievements.Core;

public abstract class CountAchievement<TSelf> : AchievementBase
    where TSelf : CountAchievement<TSelf>
{
    public abstract override int RequiredValue { get; }

    public void Increment(int count = 1, PlayerControl player = null)
    {
        if (count <= 0) return;
        count = Math.Min(count, Math.Max(1, RequiredValue));

        for (int i = 0; i < count; i++) _ = new StaticAchievementToken(Id, player);
    }

    public static void Trigger(PlayerControl player = null) => TriggerFor<TSelf>(player);
}
