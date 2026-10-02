namespace TONX.Modules.Achievements.Core.Base;

public abstract class CountAchievement : AchievementBase
{
    public abstract override int RequiredValue { get; }

    public void Increment(int count = 1) => SetProgress(Progress + count);

    public void SetProgress(int value) => TryProgress(value);
}
