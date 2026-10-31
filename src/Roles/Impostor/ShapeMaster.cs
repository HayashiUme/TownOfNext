using AmongUs.GameOptions;
using TONX.Modules.Achievements.Impostor;
using TONX.Roles.Core.Interfaces;

namespace TONX.Roles.Impostor;
public sealed class ShapeMaster : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(ShapeMaster),
            player => new ShapeMaster(player),
            CustomRoles.ShapeMaster,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            1300,
            SetupOptionItem,
            "sha|千面鬼|千面"
        );
    public ShapeMaster(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        shapeshiftDuration = OptionShapeshiftDuration.GetFloat();
    }
    private static OptionItem OptionShapeshiftDuration;

    private static float shapeshiftDuration;

    private readonly HashSet<byte> ShiftedTargets = new();
    public override void Add() => ShiftedTargets.Clear();
    public override void OnShapeshift(PlayerControl target)
    {
        if (Is(target)) return;
        ShiftedTargets.Add(target.PlayerId);
        if (ShiftedTargets.Count >= 2) ShapeMasterTwoForms.Trigger(Player);
    }

    public static void SetupOptionItem()
    {
        OptionShapeshiftDuration = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.ShapeshiftDuration, new(1, 1000, 1), 10, false)
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = 0f;
        AURoleOptions.ShapeshifterLeaveSkin = false;
        AURoleOptions.ShapeshifterDuration = shapeshiftDuration;
    }
}
