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
        ProximityRange = OptionProximityRange.GetFloat();
        RivalTime = OptionRivalTime.GetFloat();
        RivalKillCdReduce = OptionRivalKillCdReduce.GetFloat();
        NonRivalKillCdIncrease = OptionNonRivalKillCdIncrease.GetFloat();
        HasImpostorVision = OptionHasImpostorVision.GetBool();

        LoverId = byte.MaxValue;
        Rivals = new();
        ProximityTimers = new();
        LoverIsDead = false;
    }

    private static OptionItem OptionKillCooldown;
    private static OptionItem OptionDeathCooldown;
    private static OptionItem OptionProximityRange;
    private static OptionItem OptionRivalTime;
    private static OptionItem OptionRivalKillCdReduce;
    private static OptionItem OptionNonRivalKillCdIncrease;
    private static OptionItem OptionShowLoverArrow;
    private static OptionItem OptionShowRivalArrow;
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
    }

    private static float KillCooldown;
    private static float ProximityRange;
    private static float RivalTime;
    private static float RivalKillCdReduce;
    private static float NonRivalKillCdIncrease;
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
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 18, GeneralOption.CanVent, true, false);
        OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 19, GeneralOption.ImpostorVision, false, false);
    }

    public byte LoverId;
    public PlayerControl Lover => Utils.GetPlayerById(LoverId);
    public HashSet<byte> Rivals;
    public Dictionary<byte, float> ProximityTimers;
    public bool LoverIsDead;
    private float LoverDeadNotifyTimer;

    public bool IsKiller { get; private set; } = true;
    public bool CanKill { get; private set; } = true;

    public override void Add()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        // 随机选择一个非病娇玩家作为暗恋对象
        // 可能需要考虑排除特定 职业/附加职业 的暗恋对象
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
        LoverDeadNotifyTimer = 0f;
        if (LoverId == byte.MaxValue) return;
        RefreshArrows();
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
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
            // 暗恋对象死后，切换到死亡击杀冷却
            var deathCd = OptionDeathCooldown.GetFloat();
            Main.AllPlayerKillCooldown[Player.PlayerId] = deathCd;
            Player.SyncSettings();
            SendRPC_Sync();
            Utils.NotifyRoles(SpecifySeer: Player);
            Logger.Info($"{Player.GetNameWithRole()}: 暗恋对象 {lover.GetNameWithRole()} 已死亡，冷却变更为 {deathCd}s", "Yandere");
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

            // 状态同步时注册箭头（与 OnGameStart 共用同一逻辑）
            RefreshArrows();
        }
    }

    public bool CanUseKillButton() => true;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => OptionCanVent.GetBool();
    public float CalculateKillCooldown() => LoverIsDead ? OptionDeathCooldown.GetFloat() : KillCooldown;

    public void BeforeMurderPlayerAsKiller(MurderInfo info)
    {
        if (info.IsSuicide) return;
        var (killer, target) = info.AttemptTuple;

        // 禁止击杀暗恋对象
        if (target.PlayerId == LoverId)
        {
            info.CanKill = false;
            killer.Notify(Utils.ColorString(RoleInfo.RoleColor, GetString("YandereCannotKillLover")));
            Logger.Info($"{killer.GetNameWithRole()}: 尝试击杀暗恋对象 {target.GetNameWithRole()}，已阻止", "Yandere");
            return;
        }

        var baseCd = CalculateKillCooldown();

        // 暗恋对象死后，击杀冷却保持恒定值，不再根据击杀目标调整
        if (LoverIsDead)
        {
            Main.AllPlayerKillCooldown[killer.PlayerId] = baseCd;
            killer.SyncSettings();
            return;
        }

        if (Rivals.Contains(target.PlayerId))
        {
            // 击杀情敌——减少冷却
            var newCd = Mathf.Max(baseCd - RivalKillCdReduce, 2.5f);
            Main.AllPlayerKillCooldown[killer.PlayerId] = newCd;
            killer.SyncSettings();
        }
        else
        {
            // 击杀非情敌——增加冷却
            var newCd = Mathf.Max(baseCd + NonRivalKillCdIncrease, 2.5f);
            Main.AllPlayerKillCooldown[killer.PlayerId] = newCd;
            killer.SyncSettings();
        }
    }

    public void OnMurderPlayerAsKiller(MurderInfo info)
    {
        if (info.IsSuicide) return;
        var (killer, target) = info.AttemptTuple;

        if (Rivals.Remove(target.PlayerId))
        {
            // 击杀情敌后从名单中移除
            killer.Notify(GetString("YandereRivalKilled"));
            Logger.Info($"{killer.GetNameWithRole()}: 击杀情敌 {target.GetNameWithRole()}，冷却减少 {RivalKillCdReduce}s", "Yandere");
        }
        else
        {
            Logger.Info($"{killer.GetNameWithRole()}: 击杀非情敌 {target.GetNameWithRole()}，冷却增加 {NonRivalKillCdIncrease}s", "Yandere");
        }

        // 如果击杀了暗恋对象
        // 需要考虑不是病娇想击杀的
        if (target.PlayerId == LoverId)
        {
            LoverIsDead = true;
            LoverDeadNotifyTimer = 5f;
            Main.AllPlayerKillCooldown[killer.PlayerId] = OptionDeathCooldown.GetFloat();
            killer.SyncSettings();
            Logger.Info($"{killer.GetNameWithRole()}: 击杀了自己的暗恋对象！", "Yandere");
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
        if (Main.AllAlivePlayerControls.All(p =>
            p.PlayerId == Player.PlayerId || p.PlayerId == LoverId))
        {
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Yandere);
            CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
            if (Lover != null && Lover.IsAlive())
                CustomWinnerHolder.WinnerIds.Add(LoverId);
        }
    }

    private void RefreshArrows()
    {
        if (OptionShowLoverArrow.GetBool() && Lover != null && Lover.IsAlive())
            TargetArrow.Add(Player.PlayerId, Lover.PlayerId);

        if (OptionShowRivalArrow.GetBool())
        {
            foreach (var rivalId in Rivals)
            {
                var rival = Utils.GetPlayerById(rivalId);
                if (rival != null && rival.IsAlive())
                    TargetArrow.Add(Player.PlayerId, rivalId);
            }
        }
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        seen ??= seer;

        if (LoverId == byte.MaxValue) return "";
        if (!Is(seer)) return "";

        var mark = "";

        if (Is(seen) && !isForMeeting)
        {
            RefreshArrows();

            if (OptionShowLoverArrow.GetBool() && Lover != null && Lover.IsAlive())
                mark += Utils.ColorString(RoleInfo.RoleColor, TargetArrow.GetArrows(seer, LoverId));
            if (OptionShowRivalArrow.GetBool())
            {
                foreach (var rivalId in Rivals)
                {
                    var rival = Utils.GetPlayerById(rivalId);
                    if (rival != null && rival.IsAlive())
                        mark += Utils.ColorString(Color.red, TargetArrow.GetArrows(seer, rivalId));
                }
            }
            return mark;
        }

        if (seen.PlayerId == LoverId)
            mark += Utils.ColorString(RoleInfo.RoleColor, "♥");
        if (Rivals.Contains(seen.PlayerId))
            mark += Utils.ColorString(Color.red, "♦");

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
