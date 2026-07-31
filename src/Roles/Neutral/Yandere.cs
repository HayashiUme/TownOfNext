using AmongUs.GameOptions;
using Hazel;
using TONX.Roles.Core.Interfaces;
using UnityEngine;

namespace TONX.Roles.Neutral;

public sealed class Yandere : RoleBase, IKiller, IOverrideWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Yandere),
            player => new Yandere(player),
            CustomRoles.Yandere,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            52900,
            SetupOptionItem,
            "yandere|病嬌|病娇|bj",
            "#ff69b4",
            true,
            countType: CountTypes.Yandere,
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );

    public Yandere(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        DeathCooldown = OptionDeathCooldown.GetFloat();
        ProximityRange = OptionProximityRange.GetFloat();
        RivalTime = OptionRivalTime.GetFloat();
        RivalKillCdReduce = OptionRivalKillCdReduce.GetFloat();
        NonRivalKillCdIncrease = OptionNonRivalKillCdIncrease.GetFloat();
        ShowLoverArrow = OptionShowLoverArrow.GetBool();
        ShowRivalArrow = OptionShowRivalArrow.GetBool();
        ArrowUpdateInterval = OptionArrowUpdateInterval.GetFloat();
        CanVent = OptionCanVent.GetBool();
        HasImpostorVision = OptionHasImpostorVision.GetBool();

        LoverId = byte.MaxValue;
        Rivals = new();
        ProximityTimers = new();
        LoverIsDead = false;
        ArrowUpdateTimer = 0f;
    }

    private static OptionItem OptionKillCooldown;
    private static OptionItem OptionDeathCooldown;
    private static OptionItem OptionProximityRange;
    private static OptionItem OptionRivalTime;
    private static OptionItem OptionRivalKillCdReduce;
    private static OptionItem OptionNonRivalKillCdIncrease;
    private static OptionItem OptionShowLoverArrow;
    private static OptionItem OptionShowRivalArrow;
    private static OptionItem OptionArrowUpdateInterval;
    private static OptionItem OptionCanVent;
    private static OptionItem OptionHasImpostorVision;

    enum OptionName
    {
        YandereDeathCooldown,
        YandereProximityRange,
        YandereRivalTime,
        YandereRivalKillCdReduction,
        YandereNonRivalKillCdIncrease,
        YandereShowLoverArrow,
        YandereShowRivalArrow,
        YandereArrowUpdateInterval,
    }

    private static float KillCooldown;
    private static float DeathCooldown;
    private static float ProximityRange;
    private static float RivalTime;
    private static float RivalKillCdReduce;
    private static float NonRivalKillCdIncrease;
    private static bool ShowLoverArrow;
    private static bool ShowRivalArrow;
    private static float ArrowUpdateInterval;
    private static bool CanVent;
    private static bool HasImpostorVision;

    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(10f, 60f, 2.5f), 35f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionDeathCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.YandereDeathCooldown, new(10f, 60f, 2.5f), 25f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionProximityRange = FloatOptionItem.Create(RoleInfo, 12, OptionName.YandereProximityRange, new(0.5f, 20f, 0.5f), 1.5f, false);
        OptionRivalTime = FloatOptionItem.Create(RoleInfo, 13, OptionName.YandereRivalTime, new(0f, 60f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionRivalKillCdReduce = FloatOptionItem.Create(RoleInfo, 14, OptionName.YandereRivalKillCdReduction, new(0f, 60f, 2.5f), 25f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionNonRivalKillCdIncrease = FloatOptionItem.Create(RoleInfo, 15, OptionName.YandereNonRivalKillCdIncrease, new(0f, 60f, 2.5f), 10f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionShowLoverArrow = BooleanOptionItem.Create(RoleInfo, 16, OptionName.YandereShowLoverArrow, true, false);
        OptionShowRivalArrow = BooleanOptionItem.Create(RoleInfo, 17, OptionName.YandereShowRivalArrow, true, false);
        OptionArrowUpdateInterval = FloatOptionItem.Create(RoleInfo, 18, OptionName.YandereArrowUpdateInterval, new(0f, 30f, 2.5f), 10f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 19, GeneralOption.CanVent, false, false);
        OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 20, GeneralOption.ImpostorVision, false, false);
    }

    public byte LoverId;
    public PlayerControl Lover => Utils.GetPlayerById(LoverId);
    public HashSet<byte> Rivals;
    public Dictionary<byte, float> ProximityTimers;
    public bool LoverIsDead;
    private float ArrowUpdateTimer;

    public bool IsKiller { get; private set; } = true;
    public bool CanKill { get; private set; } = true;

    /// <summary>暗恋对象死亡通知计时器（秒）</summary>
    // 尚未测试完整
    private float LoverDeadNotifyTimer;

    // ===== 生命周期 =====
    public override void Add()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        // 随机选择一个非病娇玩家作为暗恋对象
        // 可能需要考虑排除的暗恋对象
        var candidates = Main.AllPlayerControls
            .Where(p => p.PlayerId != Player.PlayerId && p.IsAlive())
            .ToList();

        if (candidates.Count > 0)
        {
            var lover = candidates[IRandom.Instance.Next(candidates.Count)];
            LoverId = lover.PlayerId;
            Logger.Info($"{Player.GetNameWithRole()} 的暗恋对象是 {lover.GetNameWithRole()}", "Yandere");
        }

        SendRPC_Sync();
    }

    public override void OnGameStart()
    {
        ArrowUpdateTimer = 0f;
        LoverDeadNotifyTimer = 0f;
        if (AmongUsClient.Instance.AmHost)
            SendRPC_Sync();
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
        UpdateArrows();

        if (LoverDeadNotifyTimer > 0f)
            LoverDeadNotifyTimer -= Time.fixedDeltaTime;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!GameStates.IsInTask) return;

        var lover = Lover;
        if (lover == null) return;

        // 检测暗恋对象是否死亡
        if (!lover.IsAlive() && !LoverIsDead)
        {
            LoverIsDead = true;
            LoverDeadNotifyTimer = 5f;
            ProximityTimers.Clear();
            SendRPC_Sync();
            Utils.NotifyRoles(SpecifySeer: Player);
            Logger.Info($"{Player.GetNameWithRole()}: 暗恋对象 {lover.GetNameWithRole()} 已死亡", "Yandere");
            return;
        }

        if (LoverIsDead) return;

        var removeList = new List<byte>();
        foreach (var timerEntry in ProximityTimers)
        {
            var target = Utils.GetPlayerById(timerEntry.Key);
            if (target == null || !target.IsAlive())
            {
                removeList.Add(timerEntry.Key);
                continue;
            }
        }
        foreach (var id in removeList)
            ProximityTimers.Remove(id);

        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (pc.PlayerId == Player.PlayerId || pc.PlayerId == LoverId) continue;
            if (Rivals.Contains(pc.PlayerId)) continue; // 已是情敌，跳过

            var distance = Vector2.Distance(pc.transform.position, lover.transform.position);
            if (distance <= ProximityRange)
            {
                if (!ProximityTimers.ContainsKey(pc.PlayerId))
                    ProximityTimers[pc.PlayerId] = 0f;

                ProximityTimers[pc.PlayerId] += Time.fixedDeltaTime;

                if (ProximityTimers[pc.PlayerId] >= RivalTime)
                {
                    Rivals.Add(pc.PlayerId);
                    ProximityTimers.Remove(pc.PlayerId);
                    SendRPC_Sync();
                    Utils.NotifyRoles(SpecifySeer: Player);
                    Logger.Info($"{Player.GetNameWithRole()}: {pc.GetNameWithRole()} 已成为情敌", "Yandere");
                }
            }
        }
    }

    enum RPC_Type
    {
        SyncData
    }

    private void SendRPC_Sync()
    {
        using var sender = CreateSender();
        sender.Writer.Write((byte)RPC_Type.SyncData);
        sender.Writer.Write(LoverId);
        sender.Writer.Write(LoverIsDead);
        sender.Writer.Write(Rivals.Count);
        foreach (var r in Rivals)
            sender.Writer.Write(r);
        sender.Writer.Write(ProximityTimers.Count);
        foreach (var kv in ProximityTimers)
        {
            sender.Writer.Write(kv.Key);
            sender.Writer.Write(kv.Value);
        }
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        var rpcType = (RPC_Type)reader.ReadByte();
        if (rpcType == RPC_Type.SyncData)
        {
            LoverId = reader.ReadByte();
            var wasAlive = !LoverIsDead;
            LoverIsDead = reader.ReadBoolean();
            if (wasAlive && LoverIsDead)
                LoverDeadNotifyTimer = 5f;

            Rivals.Clear();
            var rivalCount = reader.ReadInt32();
            for (int i = 0; i < rivalCount; i++)
                Rivals.Add(reader.ReadByte());
            ProximityTimers.Clear();
            var timerCount = reader.ReadInt32();
            for (int i = 0; i < timerCount; i++)
                ProximityTimers[reader.ReadByte()] = reader.ReadSingle();

            ArrowUpdateTimer = 0f;
        }
    }

    // ===== IKiller =====
    public bool CanUseKillButton() => true;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => CanVent;
    public float CalculateKillCooldown() => LoverIsDead ? DeathCooldown : KillCooldown;

    public void BeforeMurderPlayerAsKiller(MurderInfo info)
    {
        if (info.IsSuicide) return;
        var (killer, target) = info.AttemptTuple;

        var baseCd = CalculateKillCooldown();
        if (Rivals.Contains(target.PlayerId))
        {
            // 击杀情敌——减少冷却
            var newCd = Mathf.Max(baseCd - RivalKillCdReduce, 2.5f);
            Main.AllPlayerKillCooldown[killer.PlayerId] = newCd * 2f;
            killer.SyncSettings();
        }
        else if (target.PlayerId != LoverId)
        {
            // 击杀非情敌——增加冷却
            var newCd = Mathf.Max(baseCd + NonRivalKillCdIncrease, 2.5f);
            Main.AllPlayerKillCooldown[killer.PlayerId] = newCd * 2f;
            killer.SyncSettings();
        }
    }

    public void OnMurderPlayerAsKiller(MurderInfo info)
    {
        if (info.IsSuicide) return;
        var (killer, target) = info.AttemptTuple;

        if (Rivals.Contains(target.PlayerId))
        {
            // 击杀情敌——移除情敌名单
            Rivals.Remove(target.PlayerId);
            killer.Notify(GetString("YandereRivalKilled"));
            Logger.Info($"{killer.GetNameWithRole()}: 击杀情敌 {target.GetNameWithRole()}，冷却减少 {RivalKillCdReduce}s", "Yandere");
        }
        else
        {
            Logger.Info($"{killer.GetNameWithRole()}: 击杀非情敌 {target.GetNameWithRole()}，冷却增加 {NonRivalKillCdIncrease}s", "Yandere");
        }

        // 如果击杀了暗恋对象
        if (target.PlayerId == LoverId)
        {
            LoverIsDead = true;
            LoverDeadNotifyTimer = 5f;
            Logger.Info($"{killer.GetNameWithRole()}: 击杀了自己的暗恋对象！哦~这可太残忍了！", "Yandere");
        }

        SendRPC_Sync();
        Utils.NotifyRoles(SpecifySeer: Player);
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        ProximityTimers.Clear();
        SendRPC_Sync();
    }

    public void CheckWin(ref CustomWinner WinnerTeam, ref HashSet<byte> WinnerIds)
    {
        if (!Player.IsAlive()) return;

        // 检查是否只有病娇和暗恋对象存活
        var alivePlayers = Main.AllAlivePlayerControls;
        var aliveOthers = alivePlayers.Where(p =>
            p.PlayerId != Player.PlayerId && p.PlayerId != LoverId).ToList();

        if (aliveOthers.Count == 0)
        {
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Yandere);
            CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
            if (Lover != null && Lover.IsAlive())
                CustomWinnerHolder.WinnerIds.Add(LoverId);
        }
    }

    private void UpdateArrows()
    {
        if (!Player.AmOwner) return;

        ArrowUpdateTimer -= Time.fixedDeltaTime;
        if (ArrowUpdateTimer > 0) return;
        ArrowUpdateTimer = ArrowUpdateInterval;

        TargetArrow.RemoveAllTarget(Player.PlayerId);

        // 暗恋对象箭头
        if (ShowLoverArrow && Lover != null && Lover.IsAlive())
        {
            TargetArrow.Add(Player.PlayerId, Lover.PlayerId);
        }

        // 情敌箭头
        if (ShowRivalArrow)
        {
            foreach (var rivalId in Rivals)
            {
                var rival = Utils.GetPlayerById(rivalId);
                if (rival != null && rival.IsAlive())
                {
                    TargetArrow.Add(Player.PlayerId, rivalId);
                }
            }
        }
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        seen ??= seer;

        if (LoverId == byte.MaxValue) return "";
        if (!Is(seer)) return "";

        var mark = "";

        if (seen.PlayerId == LoverId)
        {
            mark += Utils.ColorString(RoleInfo.RoleColor, "♥");
            if (ShowLoverArrow && !isForMeeting)
                mark += TargetArrow.GetArrows(seer, LoverId);
        }

        if (Rivals.Contains(seen.PlayerId))
        {
            mark += Utils.ColorString(Color.red, "♦");
            if (ShowRivalArrow && !isForMeeting)
                mark += TargetArrow.GetArrows(seer, seen.PlayerId);
        }

        return mark;
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (isForMeeting) return "";
        seen ??= seer;
        if (!Is(seer) || !Is(seen)) return "";

        if (LoverId == byte.MaxValue) return "";
        if (LoverDeadNotifyTimer > 0f)
            return Utils.ColorString(RoleInfo.RoleColor, GetString("YandereLoverDead"));

        return "";
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(HasImpostorVision);
    }
}
