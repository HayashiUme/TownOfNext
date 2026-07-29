using AmongUs.GameOptions;
using Hazel;
using TONX.Modules;

namespace TONX.Roles.Crewmate;

public sealed class SecurityGuard : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(SecurityGuard),
            player => new SecurityGuard(player),
            CustomRoles.SecurityGuard,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            23500,
            SetupOptionItem,
            "sg|保安|保安",
            "#4B6E8C"
        );

    public SecurityGuard(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    private static OptionItem OptionVentLockMaxCount;
    private static OptionItem OptionVentLockCooldown;
    enum OptionName
    {
        SecurityGuardVentLockMaxCount,
        SecurityGuardVentLockCooldown,
    }

    public static HashSet<int> LockedVents = new();

    private static void SetupOptionItem()
    {
        OptionVentLockMaxCount = IntegerOptionItem.Create(RoleInfo, 10, OptionName.SecurityGuardVentLockMaxCount, new(1, 15, 1), 3, false)
            .SetValueFormat(OptionFormat.Times);
        OptionVentLockCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.SecurityGuardVentLockCooldown, new(5f, 60f, 2.5f), 25f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Add()
    {
        LockedVents.Clear();
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = OptionVentLockCooldown.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = 1f;
    }

    public override bool GetAbilityButtonText(out string text)
    {
        text = GetString("SecurityGuardVentButtonText");
        return true;
    }

    /// <summary>检查通风管是否已被保安封锁</summary>
    public static bool IsVentLocked(int ventId)
    {
        return LockedVents.Contains(ventId);
    }

    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (!AmongUsClient.Instance.AmHost) return false;

        var maxCount = (int)OptionVentLockMaxCount.GetFloat();

        if (LockedVents.Count >= maxCount)
        {
            // 已达封锁上限，不能继续封锁，也不能使用管道
            Player.Notify(string.Format(GetString("SecurityGuardVentLockMaxReached"), maxCount));
            physics.RpcBootFromVent(ventId);
            Logger.Info($"{Player.GetNameWithRole()}: 尝试封锁通风管 {ventId} 但已达上限 {maxCount}", "SecurityGuard");
            return false;
        }

        // 封锁该通风管
        LockedVents.Add(ventId);
        SendRPC_SyncLockedVents();
        Player.Notify(string.Format(GetString("SecurityGuardVentLocked"), ventId));
        Logger.Info($"{Player.GetNameWithRole()}: 封锁了通风管 {ventId}，当前已封锁: {LockedVents.Count}", "SecurityGuard");
        Utils.MarkEveryoneDirtySettings();
        Player.SyncSettings();
        Player.RpcResetAbilityCooldown();
        return false;
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        LockedVents.Clear();
        var count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
            LockedVents.Add(reader.ReadInt32());
    }

    private void SendRPC_SyncLockedVents()
    {
        using var sender = CreateSender();
        sender.Writer.Write(LockedVents.Count);
        foreach (var ventId in LockedVents)
            sender.Writer.Write(ventId);
    }

    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (!Is(seen) || isForMeeting) return "";
        var maxCount = (int)OptionVentLockMaxCount.GetFloat();
        return string.Format(GetString("SecurityGuardHudText"), LockedVents.Count, maxCount);
    }
}
