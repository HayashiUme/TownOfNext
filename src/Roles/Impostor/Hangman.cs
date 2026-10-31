using AmongUs.GameOptions;
using TONX.Roles.AddOns.Common;
using TONX.Roles.Core.Interfaces;
using TONX.Roles.Crewmate;

namespace TONX.Roles.Impostor;
public sealed class Hangman : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Hangman),
            player => new Hangman(player),
            CustomRoles.Hangman,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            3800,
            SetupOptionItem,
            "ha|劊子手|筷子手|侩子手|柜子手"
        );
    public Hangman(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    static OptionItem OptionShapeshiftCooldown;
    static OptionItem OptionShapeshiftDuration;

    public bool IsKiller { get; private set; } = false;
    private readonly HashSet<byte> strangledTargets = new();
    public bool StrangledProtectedTarget { get; private set; }
    public int DistinctStrangleTargets { get; private set; }
    public bool BypassedDeathPassive { get; private set; }

    public override void Add()
    {
        strangledTargets.Clear();
        StrangledProtectedTarget = false;
        DistinctStrangleTargets = 0;
        BypassedDeathPassive = false;
    }

    private static void SetupOptionItem()
    {
        OptionShapeshiftCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.ShapeshiftCooldown, new(2.5f, 180f, 2.5f), 25f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionShapeshiftDuration = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.ShapeshiftDuration, new(2.5f, 180f, 2.5f), 15f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = OptionShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = OptionShapeshiftDuration.GetFloat();
    }
    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (Main.CheckShapeshift.TryGetValue(killer.PlayerId, out var s) && s)
        {
            if (HasProtectionState(target)) StrangledProtectedTarget = true;
            if (HasBypassedDeathPassive(target)) BypassedDeathPassive = true;
            if (strangledTargets.Add(target.PlayerId)) DistinctStrangleTargets = strangledTargets.Count;

            Utils.TP(killer.NetTransform, target.GetTruePosition());

            target.Data.IsDead = true;
            target.SetRealKiller(killer);
            target.SetDeathReason(CustomDeathReason.LossOfHead);
            target.RpcExileV2();
            PlayerState.GetByPlayerId(target.PlayerId)?.SetDead();

            killer.SetKillCooldownV2();
            RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);

            return false;
        }
        return true;
    }
    /// <summary>
    /// 一次绞杀前目标是否处于常规防护状态（白名单，均读取实时游戏状态）
    /// </summary>
    private static bool HasProtectionState(PlayerControl target)
        => target != null
           && (Main.ShieldPlayer == target.PlayerId
               || Medic.InProtect(target.PlayerId)
               || target.GetRoleClass() is Veteran { IsOnGuard: true });
    /// <summary>
    /// 一次绞杀是否绕过了死亡被动（绞杀直接置死，不经过 OnMurderPlayer）
    /// </summary>
    private static bool HasBypassedDeathPassive(PlayerControl target)
        => target != null
           && (Bait.IsThisRole(target.PlayerId)
               || Beartrap.IsThisRole(target.PlayerId)
               || Avenger.IsThisRole(target.PlayerId)
               || target.GetRoleClass() is Luckey);
    public override bool GetAbilityButtonSprite(out string buttonName)
    {
        buttonName = "Hangman";
        return true;
    }
}