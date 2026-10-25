using AmongUs.GameOptions;
using Hazel;
using TONX.Attributes;
using TONX.Modules.Achievements.Neutral;
using TONX.Roles.Core.Interfaces;

namespace TONX.Roles.Neutral;

public sealed class Shifter : RoleBase, IKiller, IDeathReasonSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Shifter),
            player => new Shifter(player),
            CustomRoles.Shifter,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            51900,
            SetupOptionItem,
            "sf|连环交换|连环交换师",
            "#696969",
            true,
            introSound: () => GetIntroSound(RoleTypes.Shapeshifter),
            experimental: true
        );
    public Shifter(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        TargetPlayer = byte.MaxValue;
    }
    private static OptionItem OptionShiftCooldown;
    enum OptionName
    {
        ShifterSkillCooldown,
    }
    private byte TargetPlayer;
    private sealed class SwapHistory
    {
        public readonly HashSet<CustomRoles> ObtainedRoles = new();
        public bool Returned;
        public bool SwappedAfterReturn;
    }
    private static readonly Dictionary<(byte, string), SwapHistory> SwapHistories = new();
    [GameModuleInitializer]
    public static void Init() => SwapHistories.Clear();
    public static bool CompletedDistinctSwaps(PlayerControl player) =>
        player.IsAlive() && SwapHistories.TryGetValue((player.PlayerId, player.FriendCode), out var history)
        && history.SwappedAfterReturn && history.ObtainedRoles.Count >= 2;
    public static void SetupOptionItem()
    {
        OptionShiftCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.ShifterSkillCooldown, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public float CalculateKillCooldown() => CanUseKillButton() ? OptionShiftCooldown.GetFloat() : 255f;
    public bool CanUseKillButton() => TargetPlayer == byte.MaxValue;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);
    public bool OverrideKillButtonText(out string text)
    {
        text = GetString("ShifterKillButtonText");
        return true;
    }
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = (info.AttemptKiller, info.AttemptTarget);
        if (TargetPlayer == byte.MaxValue)
        {
            TargetPlayer = target.PlayerId;
            SendRPC();
        }

        killer.ResetKillCooldown();
        killer.SetKillCooldownV2();

        return false;
    }
    public override void AfterMeetingTasks()
    {
        if (TargetPlayer == byte.MaxValue) return;
        var target = Utils.GetPlayerById(TargetPlayer);
        var player = Player;
        if (target == null || (target.Data?.IsDead ?? true) || (Player.Data?.IsDead ?? true)) return;

        var obtainedRole = target.GetCustomRole();
        player.RpcChangeRole(obtainedRole);
        target.RpcChangeRole(CustomRoles.Shifter);
        if (player.GetCustomRole() == obtainedRole && target.Is(CustomRoles.Shifter))
        {
            var key = (player.PlayerId, player.FriendCode);
            if (!SwapHistories.TryGetValue(key, out var history)) SwapHistories[key] = history = new();
            history.ObtainedRoles.Add(obtainedRole);
            history.SwappedAfterReturn |= history.Returned;
            if (SwapHistories.TryGetValue((target.PlayerId, target.FriendCode), out var returned))
                returned.Returned = true;
            IdentitySwapped.Trigger(player);
        }
        Logger.Info($"连环交换师{player?.Data?.PlayerName}与{target?.Data?.PlayerName}交换了职业", "Shifter");
        Utils.NotifyRoles();
    }
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(TargetPlayer);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        TargetPlayer = reader.ReadByte();
    }
}
