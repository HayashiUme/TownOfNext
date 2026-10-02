using TONX.Modules.Achievements.Core.Interfaces;
using TONX.Modules.Achievements.Game;
using TONX.Modules.Achievements.Player;
using TONX.Modules.Achievements.AchievementInterface;
using UnityEngine;

namespace TONX.Modules.Achievements.Core.Base;

public abstract class AchievementBase : IAchievement
{
    private static readonly List<(string friendCode, int id, string name, string description)> PendingUnlocks = new();

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
    /// <summary>进度值</summary>
    public virtual int RequiredValue => 1;

    public int Progress { get; protected set; }
    public bool Unlocked { get; protected set; }

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

    public void TriggerOnce(PlayerControl player = null)
    {
        var target = player ?? PlayerControl.LocalPlayer;
        if (target == null || !target.AmOwner) return;
        if (Unlocked) return;

        if (IsProgressAchievement) TryProgress(Progress + 1);
        else Unlock();
    }

    public void TryProgress(int value)
    {
        if (Unlocked || value <= Progress) return;

        Progress = Math.Min(value, RequiredValue);
        TONX.Modules.Achievements.Game.AchievementManager.MarkDirty(this);

        if (Progress >= RequiredValue) Unlock();
        else AchievementToast.Enqueue(this);
    }
    
    public void Unlock()
    {
        if (Unlocked) return;

        var player = PlayerControl.LocalPlayer;
        if (GameStates.IsLocalGame || player == null || string.IsNullOrEmpty(player.FriendCode)) return;

        Unlocked = true;
        Progress = RequiredValue;
        Logger.Info($"{player.GetRealName()} unlocked [{Id}]{Name}", "Achievement");

        if (PlayerAchievementData.HasAchievement(player.FriendCode, Id)) return;

        PlayerAchievementData.AddToCache(player.FriendCode, new AchievementRecord
        {
            Id = Id,
            Name = Name,
            Description = Description,
            UnlockedAt = DateTime.UtcNow.ToString("o"),
            Progress = RequiredValue,
            Required = RequiredValue,
        });

        PendingUnlocks.Add((player.FriendCode, Id, Name, Description));
        AchievementToast.Enqueue(this, true);
    }
    public void ApplyServerRecord(AchievementRecord record)
    {
        if (record == null) return;
        if (record.Unlocked) Unlocked = true;
        Progress = Unlocked ? RequiredValue : Math.Max(Progress, record.Progress);
    }

    public static async Task FlushPendingUnlocks()
    {
        if (PendingUnlocks.Count == 0) return;

        var toSend = new List<(string friendCode, int id, string name, string description)>(PendingUnlocks);
        PendingUnlocks.Clear();

        foreach (var (friendCode, id, name, description) in toSend)
        {
            await TONX.Modules.Achievements.Game.AchievementManager.TriggerUnlockAsync(friendCode, id, name, description);

            var player = Main.AllPlayerControls.FirstOrDefault(p => p.FriendCode == friendCode);
            if (player == null) continue;

            var achievement = AchievementRegistry.GetById(id);
            string colorHex = achievement?.TitleColorHex ?? "#FFD700";
            string msg = string.Format(GetString("Achievement.Unlocked"), colorHex, name);
            Utils.SendMessage(msg, player.PlayerId, $"<color=#FFD700>{GetString("AchievementMsgTitle")}</color>");
        }
    }
}
