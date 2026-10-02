using AmongUs.GameOptions;

namespace TONX.Roles.Vanilla;

public sealed class Judge : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForVanilla(
            typeof(Judge),
            player => new Judge(player),
            RoleTypes.Judge,
            "#8cffff"
        );
    public Judge(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
}
