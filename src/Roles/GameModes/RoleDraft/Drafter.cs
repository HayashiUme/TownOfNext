using AmongUs.GameOptions;
using TONX.Modules;
using TONX.Roles.Core.Interfaces;
using UnityEngine;

namespace TONX.Roles.Crewmate;
public sealed class Drafter : RoleBase, ISpecialMeeting
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Drafter),
            player => new Drafter(player),
            CustomRoles.Drafter,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.GameMode,
            100001,
            null,
            "dra|起草者",
            "#ffffff",
            Hidden: new HiddenRoleInfo(0, null)
        );
    public Drafter(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
    
    public bool IsSpecialMeetingActive => !CustomRoleSelector.RoleAssigned;
    public List<byte> SpecialMeetingPlayers => new();
    public bool AllowSkip => false;
}
