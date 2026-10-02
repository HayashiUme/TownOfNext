using System.Text;
using AmongUs.GameOptions;
using TMPro;
using TONX.Roles.GameMode;

namespace TONX.GameModes;

public sealed class DoomTag : GameModeBase
{
    public static readonly GameModeInfo ModeInfo =
        GameModeInfo.Create(
            typeof(DoomTag),
            () => new DoomTag(),
            CustomGameMode.DoomTag,
            50_000_000,
            SetupCustomOption,
            "#D9BAA5",
            () => $"<color=#D9BAA5><size=1.7>{GetString("ModeDoomTag")}</size></color>",
            (true, true)
        );

    public DoomTag() : base(ModeInfo)
    { }

    public static readonly Dictionary<byte, byte> TargetMap = new();
    public static readonly HashSet<byte> LowerVisionPlayers = new();
    public static bool FrenzyMode;

    public static OptionItem BaseKillCooldown;
    public static OptionItem FrenzyKillCooldown;
    public static OptionItem FrenzySpeedMultiplier;
    public static OptionItem LowerVisionMultiplier;
    public static OptionItem ShowTargetArrow;
    private static OptionItem PunishmentMode;
    private static OptionItem FrenzyPlayerCount;

    private static readonly string[] PunishmentModeOptions = { "DoomTag.PunishSuicide", "DoomTag.PunishLowerVision" };

    public static void SetupCustomOption()
    {
        TextOptionItem.Create(ModeInfo, 100_001, "MenuTitle.GameMode");
        BaseKillCooldown = FloatOptionItem.Create(ModeInfo, 1, "DoomTag.BaseKillCooldown", new(2.5f, 60f, 2.5f), 15f, false)
            .SetHeader(true)
            .SetValueFormat(OptionFormat.Seconds);
        PunishmentMode = StringOptionItem.Create(ModeInfo, 2, "DoomTag.PunishmentMode", PunishmentModeOptions, 0, false);
        LowerVisionMultiplier = FloatOptionItem.Create(ModeInfo, 3, "DoomTag.LowerVisionMultiplier", new(0f, 10f, 0.5f), 0.5f, false, PunishmentMode)
            .SetValueFormat(OptionFormat.Multiplier);
        FrenzyPlayerCount = IntegerOptionItem.Create(ModeInfo, 4, "DoomTag.FrenzyPlayerCount", new(1, 127, 1), 3, false)
            .SetValueFormat(OptionFormat.Players);
        FrenzyKillCooldown = FloatOptionItem.Create(ModeInfo, 5, "DoomTag.FrenzyKillCooldown", new(2.5f, 60f, 2.5f), 5f, false)
            .SetValueFormat(OptionFormat.Seconds);
        FrenzySpeedMultiplier = FloatOptionItem.Create(ModeInfo, 6, "DoomTag.FrenzySpeedMultiplier", new(0.25f, 10f, 0.25f), 2f, false)
            .SetValueFormat(OptionFormat.Multiplier);
        ShowTargetArrow = BooleanOptionItem.Create(ModeInfo, 7, "DoomTag.ShowTargetArrow", false, false);
    }

    public override void Add() => Reset();

    public override AvailableRolesData AddAvailableRoles() => default;
    public override bool ShouldAssignAddons() => false;
    public override void SelectCustomRoles(ref Dictionary<PlayerControl, CustomRoles> RoleResult, ref AvailableRolesData data)
    {
        foreach (var pc in Main.AllAlivePlayerControls)
            RoleResult.Add(pc, CustomRoles.Tagger);
    }

    public override void OnGameStart()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        Reset();
        AssignTargets();
    }

    private static float _defaultSpeed = -1f;
    public static float DefaultSpeed
    {
        get
        {
            if (_defaultSpeed <= 0f) _defaultSpeed = Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod);
            return _defaultSpeed;
        }
    }

    private static void Reset()
    {
        TargetMap.Clear();
        LowerVisionPlayers.Clear();
        FrenzyMode = false;
        _defaultSpeed = -1f;
    }

    public static byte GetTarget(byte playerId) => TargetMap.TryGetValue(playerId, out var id) ? id : byte.MaxValue;
    public static bool IsLowerVision(byte playerId) => LowerVisionPlayers.Contains(playerId);

    public static void AssignTargets()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        TargetMap.Clear();
        var ids = Main.AllAlivePlayerControls
            .OrderBy(_ => IRandom.Instance.Next(0, 1_000_000))
            .Select(x => x.PlayerId)
            .ToList();

        if (ids.Count < 2)
        {
            Logger.Warn("Not enough players for DoomTag", "DoomTag");
            return;
        }

        for (var i = 0; i < ids.Count; i++)
            SetTarget(ids[i], ids[(i + 1) % ids.Count]);

        RefreshArrows();
        Tagger.SyncState();
        Utils.NotifyRoles();
    }

    private static void SetTarget(byte hunterId, byte targetId)
    {
        if (hunterId == targetId) return;

        var oldHunter = GetHunterOf(targetId);
        if (oldHunter != byte.MaxValue && oldHunter != hunterId) TargetMap.Remove(oldHunter);

        TargetMap[hunterId] = targetId;
        Logger.Info($"{hunterId} => {targetId}", "DoomTag");
    }

    private static byte GetHunterOf(byte targetId)
    {
        foreach (var kvp in TargetMap)
            if (kvp.Value == targetId) return kvp.Key;
        return byte.MaxValue;
    }

    public static void RefreshArrows()
    {
        if (Main.AllPlayerControls == null) return;

        foreach (var pc in Main.AllPlayerControls)
            TargetArrow.RemoveAllTarget(pc.PlayerId);

        if (!ShowTargetArrow.GetBool()) return;
        foreach (var (hunterId, targetId) in TargetMap)
            TargetArrow.Add(hunterId, targetId);
    }

    /// <summary>
    /// 由 Tagger 职业调用，返回 true 表示允许本次击杀
    /// </summary>
    public static bool OnCheckMurder(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        info.CanKill = false;

        if (Options.CurrentGameMode != CustomGameMode.DoomTag || !AmongUsClient.Instance.AmHost) return false;

        var killerId = killer.PlayerId;
        var targetId = target.PlayerId;

        if (GetTarget(killerId) != targetId)
        {
            ApplyPunishment(killer);
            if (PunishmentMode.GetInt() != 0) return false;

            info.CanKill = true;
            RefreshSuffix(killerId);
            return true;
        }

        var nextTarget = GetTarget(targetId);
        TargetMap.Remove(targetId);
        TargetArrow.RemoveAllTarget(targetId);

        if (nextTarget != byte.MaxValue && nextTarget != killerId) SetTarget(killerId, nextTarget);
        else TargetMap.Remove(killerId);

        RefreshArrows();
        CheckFrenzyMode();
        Tagger.SyncState();
        RefreshSuffix(killerId);

        info.CanKill = true;
        return true;
    }

    /// <summary>
    /// 目标变更后刷新该玩家名字下方的猎物提示
    /// </summary>
    private static void RefreshSuffix(byte playerId)
    {
        var pc = Utils.GetPlayerById(playerId);
        if (pc != null) Utils.NotifyRoles(SpecifySeer: pc);
    }

    private static void ApplyPunishment(PlayerControl killer)
    {
        if (PunishmentMode.GetInt() == 1)
        {
            LowerVisionPlayers.Add(killer.PlayerId);
            killer.MarkDirtySettings();
            killer.Notify(string.Format(GetString("DoomTag.WrongKillLowerVision"), LowerVisionMultiplier.GetFloat()));
            return;
        }

        killer.Notify(GetString("DoomTag.WrongKillSuicide"));

        var killerId = killer.PlayerId;
        _ = new LateTask(() =>
        {
            var pc = Utils.GetPlayerById(killerId);
            if (pc == null || pc.Data == null || !pc.IsAlive() || GameStates.IsEnded) return;

            pc.RpcMurderPlayerV2(pc);
            HandleDeadPlayer(killerId);
        }, 0.5f, "DoomTag");
    }

    private static void HandleDeadPlayer(byte deadPlayerId)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!TargetMap.Remove(deadPlayerId)) return;

        var hunterId = GetHunterOf(deadPlayerId);
        var nextTarget = GetTarget(deadPlayerId);
        LowerVisionPlayers.Remove(deadPlayerId);
        TargetArrow.RemoveAllTarget(deadPlayerId);

        if (hunterId != byte.MaxValue && nextTarget != byte.MaxValue && nextTarget != hunterId)
            SetTarget(hunterId, nextTarget);
        else if (hunterId != byte.MaxValue)
            TargetMap.Remove(hunterId);

        RefreshArrows();
        CheckFrenzyMode();
        Tagger.SyncState();
        if (hunterId != byte.MaxValue) RefreshSuffix(hunterId);
    }

    public static void CheckFrenzyMode()
    {
        if (!AmongUsClient.Instance.AmHost || FrenzyMode) return;

        var aliveCount = Main.AllAlivePlayerControls.Count();
        if (aliveCount > FrenzyPlayerCount.GetInt() || aliveCount <= 1) return;

        FrenzyMode = true;
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            pc.MarkDirtySettings();
            pc.Notify(GetString("DoomTag.FrenzyModeActivated"));
        }
        Tagger.SyncState();
    }

    private static long LastSecondUpdate;
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost || player != PlayerControl.LocalPlayer) return;
        if (!GameStates.IsInTask) return;

        var now = Utils.GetTimeStamp();
        if (LastSecondUpdate == now) return;
        LastSecondUpdate = now;

        foreach (var id in TargetMap.Keys.Concat(TargetMap.Values).Distinct().ToList())
        {
            var pc = Utils.GetPlayerById(id);
            if (pc == null || pc.Data == null || !pc.IsAlive()) HandleDeadPlayer(id);
        }
        CheckFrenzyMode();
    }

    public override void EditIntroFormat(ref IntroCutscene intro)
    {
        intro.TeamTitle.text = GetString("ModeDoomTag");
        intro.TeamTitle.color = ModeInfo.ModeColor;
        // Subtitle is hidden on purpose: only the mode name is shown.
        intro.ImpostorText.gameObject.SetActive(false);
        intro.BackgroundBar.material.color = ModeInfo.ModeColor;
    }
    public override void EditOutroFormat(ref EndGameManager outro, ref TextMeshPro winnerText, ref string cwText, ref StringBuilder awText, ref string cwColor)
    {
        var winnerId = CustomWinnerHolder.WinnerIds.FirstOrDefault();
        outro.WinText.text = Main.AllPlayerNames[winnerId] + GetString("Win");
        outro.WinText.fontSize -= 5f;
        outro.WinText.color = Main.PlayerColors[winnerId];
        outro.BackgroundBar.material.color = ModeInfo.ModeColor;
        winnerText.text = $"<color=#D9BAA5>{GetString("ModeDoomTag")}</color>";
        winnerText.color = ModeInfo.ModeColor;
    }

    public override void AfterCheckForGameEnd(GameOverReason reason, ref GameEndPredicate predicate)
    {
        if (CustomWinnerHolder.WinnerIds.Count > 0 || CustomWinnerHolder.WinnerTeam != CustomWinner.Default)
        {
            ShipStatus.Instance.enabled = false;
            GameEndChecker.StartEndGame(reason);
            predicate = null;
        }
    }
    public override GameEndPredicate Predicate() => new DoomTagGameEndPredicate();

    private sealed class DoomTagGameEndPredicate : GameEndPredicate
    {
        public override bool CheckForEndGame(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorsByKill;

            var alive = Main.AllAlivePlayerControls.ToList();
            if (alive.Count > 1) return false;

            if (alive.Count == 1) CustomWinnerHolder.WinnerIds = new() { alive[0].PlayerId };
            else CustomWinnerHolder.ResetAndSetWinner(CustomWinner.None);
            Main.DoBlockNameChange = true;
            return true;
        }
    }
}
