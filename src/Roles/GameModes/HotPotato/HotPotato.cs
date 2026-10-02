using AmongUs.GameOptions;
using TONX.Roles.Core.Interfaces;
using UnityEngine;

namespace TONX.Roles.GameMode;

public sealed class HotPotato : PotatoBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(HotPotato),
            player => new HotPotato(player),
            CustomRoles.HotPotato,
            () => RoleTypes.Impostor,
            CustomRoleTypes.GameMode,
            100002,
            null,
            "hp|热土豆",
            "#ff9900",
            true,
            Hidden: new HiddenRoleInfo(0, null)
        );

    public HotPotato(PlayerControl player)
        : base(RoleInfo, player, () => HasTask.False)
    { }

    public bool IsKiller => false;
    public bool CanUseKillButton() => true;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;
    public float CalculateKillCooldown() => 0f;

    public override void OverrideDisplayRoleNameAsSeen(PlayerControl seer, ref bool enabled, ref Color roleColor, ref string roleText)
        => enabled = true;

    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        info.CanKill = false;

        if (Options.CurrentGameMode != CustomGameMode.HotPotato || target.Is(CustomRoles.HotPotato)) return false;

        target.RpcChangeRole(CustomRoles.HotPotato, refreshSeen: false);
        killer.RpcChangeRole(CustomRoles.ColdPotato, refreshSeen: false);
        RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
        RPC.PlaySoundRPC(target.PlayerId, Sounds.KillSound);
        _ = new LateTask(() => target.SetKillCooldownV2(0f), 0.1f, "HotPotato");
        Utils.NotifyRoles(SpecifySeer: killer);
        Utils.NotifyRoles(SpecifySeer: target);
        return false;
    }
}
