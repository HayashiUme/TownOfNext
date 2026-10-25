using TONX.Modules.Achievements.Core.Interfaces;
using UnityEngine;

namespace TONX.Modules.Achievements.Core;

public abstract class AchievementBase : IAchievement
{
    public abstract int Id { get; }
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string TitleDisplay { get; }
    public abstract Color TitleColor { get; }

    public virtual AchievementRarity Rarity => AchievementRarity.Common;
    public virtual bool Hidden => false;
    public virtual string Category => "General";
    public virtual Sprite Icon => DefaultIcon;
    public virtual int RequiredValue => 1;

    public string TitleColorHex => Utils.ColorToHex(TitleColor);

    public bool IsProgressAchievement => RequiredValue > 1;

    private static Sprite _defaultIcon;
    public static Sprite DefaultIcon => _defaultIcon ??= Utils.LoadSprite("TONX.Resources.Images.UI.CheckMark.png", 100f);

    public static void TriggerFor<T>(PlayerControl player = null) where T : AchievementBase
    {
        var achievement = AchievementRegistry.GetByType(typeof(T));
        if (achievement == null)
        {
            Logger.Warn($"achievement not registered: {typeof(T).Name}", "Achievement");
            return;
        }
        _ = new StaticAchievementToken(achievement.Id, player);
    }

    public virtual bool CheckClear(PlayerControl player) => false;

    public int GetProgress(PlayerControl player = null)
    {
        var target = player ?? PlayerControl.LocalPlayer;
        return CustomAchievementDataManager.GetByPlayer(target)?.GetProgress(Id) ?? 0;
    }

    public bool IsUnlockedBy(PlayerControl player = null)
    {
        var target = player ?? PlayerControl.LocalPlayer;
        return CustomAchievementDataManager.GetByPlayer(target)?.Has(Id) ?? false;
    }
}
