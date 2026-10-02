using AmongUs.GameOptions;

namespace TONX.Roles.Vanilla;

public sealed class SpiritGuide : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForVanilla(
            typeof(SpiritGuide),
            player => new SpiritGuide(player),
            RoleTypes.SpiritGuide
        );

    public SpiritGuide(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
    { }
}