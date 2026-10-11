using TONX.Modules.Achievements.AchievementInterface;
using TONX.Modules.Achievements.Core.Interfaces;
using UnityEngine;

namespace TONX.Modules.Achievements.Core;

/// <summary>
/// 成就定义的抽象基类。<b>本类不持有任何会话状态。</b>
/// </summary>
public abstract class AchievementBase : IAchievement
{
    /// <summary>成就的唯一ID</summary>
    public abstract int Id { get; }
    /// <summary>成就名称</summary>
    public abstract string Name { get; }
    /// <summary>成就获取介绍</summary>
    public abstract string Description { get; }
    /// <summary>头衔里显示的名称</summary>
    public abstract string TitleDisplay { get; }
    /// <summary>头衔颜色</summary>
    public abstract Color TitleColor { get; }

    /// <summary>稀有度</summary>
    public virtual AchievementRarity Rarity => AchievementRarity.Common;
    /// <summary>隐藏成就</summary>
    public virtual bool Hidden => false;
    /// <summary>成就所属分组</summary>
    public virtual string Category => "General";
    public virtual Sprite Icon => DefaultIcon;
    /// <summary>达成所需的进度值，1 表示单次解锁</summary>
    public virtual int RequiredValue => 1;

    /// <summary>颜色Hex码</summary>
    public string TitleColorHex =>
        $"#{(int)(TitleColor.r * 255):X2}{(int)(TitleColor.g * 255):X2}{(int)(TitleColor.b * 255):X2}";

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
        achievement.TriggerOnce(player);
    }

    /// <summary>取得指定玩家在该成就上的当前进度，无数据时为 0</summary>
    public int GetProgress(PlayerControl player = null)
    {
        var target = player ?? PlayerControl.LocalPlayer;
        return CustomAchievementDataManager.GetByPlayer(target)?.GetProgress(Id) ?? 0;
    }

    /// <summary>该成就对指定玩家是否已解锁</summary>
    public bool IsUnlockedBy(PlayerControl player = null)
    {
        var target = player ?? PlayerControl.LocalPlayer;
        return CustomAchievementDataManager.GetByPlayer(target)?.Has(Id) ?? false;
    }

    /// <summary>触发一次。单次解锁类直接解锁，计数类则推进一格进度</summary>
    public void TriggerOnce(PlayerControl player = null)
    {
        var target = player ?? PlayerControl.LocalPlayer;
        if (target == null)
        {
            Logger.Warn($"[{Id}]{Name} trigger aborted: no target player (LocalPlayer is null)", "Achievement");
            return;
        }
        
        if (IsProgressAchievement) TryProgress(GetProgress(target) + 1, target);
        else TryProgress(RequiredValue, target);
    }

    /// <summary>对指定玩家推进进度。达到上限时自动解锁</summary>
    public void TryProgress(int value, PlayerControl player = null)
    {
        var target = player ?? PlayerControl.LocalPlayer;
        var data = CustomAchievementDataManager.GetOrCreateByPlayer(target);
        if (data == null)
        {
            Logger.Warn($"[{Id}]{Name} trigger aborted: no data container (target={target?.PlayerId}, " +
                        $"friendCode='{target?.FriendCode}', amHost={AmongUsClient.Instance.AmHost})", "Achievement");
            return;
        }

        if (data.Has(Id))
        {
            Logger.Info($"[{Id}]{Name} already unlocked for {data.FriendCode}, skip", "Achievement");
            return;
        }
        
        if (data.ApplyProgress(this, value))
            AchievementToast.Enqueue(this, data.GetProgress(Id), true);
        else
            AchievementToast.Enqueue(this, data.GetProgress(Id));
    }

    /// <summary>解锁成就。写入容器并弹出提示，实际的上报由 AchievementManager 在合适的时机批量完成</summary>
    public void Unlock(PlayerControl player = null)
    {
        var target = player ?? PlayerControl.LocalPlayer;
        var data = CustomAchievementDataManager.GetOrCreateByPlayer(target);
        if (data == null)
        {
            Logger.Warn($"[{Id}]{Name} unlock aborted: no data container (friendCode='{target?.FriendCode}')", "Achievement");
            return;
        }
        if (data.Has(Id)) return;

        if (!data.ApplyUnlock(this)) return;

        AchievementToast.Enqueue(this, data.GetProgress(Id), true);
    }
}
