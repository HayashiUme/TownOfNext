using AmongUs.GameOptions;
using TONX.Roles.Core.Interfaces;
using UnityEngine;

namespace TONX.Roles.GameMode;

public sealed class ColdPotato : PotatoBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(ColdPotato),
            player => new ColdPotato(player),
            CustomRoles.ColdPotato,
            () => RoleTypes.Impostor,
            CustomRoleTypes.GameMode,
            100004,
            null,
            "cp|冷土豆",
            "#66ffff",
            true,
            Hidden: new HiddenRoleInfo(0, null)
        );

    public ColdPotato(PlayerControl player)
        : base(RoleInfo, player, () => HasTask.False)
    { }

    public bool CanUseKillButton() => false;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;

    public override void OverrideDisplayRoleNameAsSeen(PlayerControl seer, ref bool enabled, ref Color roleColor, ref string roleText)
        => enabled = true;
}
