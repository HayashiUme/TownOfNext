namespace TONX.Modules.Achievements.Core.Base;

public abstract class SingleUnlockAchievement : AchievementBase
{
    public sealed override int RequiredValue => 1;

    public void TryUnlock() => TryProgress(1);
}
