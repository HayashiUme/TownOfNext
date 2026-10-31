using AmongUs.GameOptions;
using TONX.Modules.Achievements.Impostor;
using TONX.Roles.Core.Interfaces;

namespace TONX.Roles.Impostor;
public sealed class Arrogance : RoleBase, IImpostor
{

    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Arrogance),
            player => new Arrogance(player),
            CustomRoles.Arrogance,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            3000,
            SetupOptionItem,
            "ag|狂妄殺手|狂妄"
        );
    public Arrogance(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    static OptionItem DefaultKillCooldown;
    static OptionItem ReduceKillCooldown;
    static OptionItem MinKillCooldown;
    enum OptionName
    {
        ArroganceDefaultKillCooldown,
        ArroganceReduceKillCooldown,
        ArroganceMinKillCooldown,
    }
    private float KillCooldown;
    public int Kills { get; private set; }
    public int KillsWithoutMeeting { get; private set; }
    public int MaxKillsWithoutMeeting { get; private set; }
    public bool KilledWithReducedCooldown { get; private set; }
    private static void SetupOptionItem()
    {
        DefaultKillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.ArroganceDefaultKillCooldown, new(2.5f, 180f, 2.5f), 65f, false)
            .SetValueFormat(OptionFormat.Seconds);
        ReduceKillCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.ArroganceReduceKillCooldown, new(2.5f, 180f, 2.5f), 15f, false)
            .SetValueFormat(OptionFormat.Seconds);
        MinKillCooldown = FloatOptionItem.Create(RoleInfo, 12, OptionName.ArroganceMinKillCooldown, new(2.5f, 180f, 2.5f), 2.5f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Add()
    {
        KillCooldown = DefaultKillCooldown.GetFloat();
        Kills = 0;
        KillsWithoutMeeting = 0;
        MaxKillsWithoutMeeting = 0;
        KilledWithReducedCooldown = false;
    }
    public float CalculateKillCooldown() => KillCooldown;

    public override void OnStartMeeting() => KillsWithoutMeeting = 0;

    public void BeforeMurderPlayerAsKiller(MurderInfo info)
    {
        if (info.IsSuicide) return;
        var killer = info.AttemptKiller;
        var before = KillCooldown;
        KillCooldown = Math.Clamp(KillCooldown - ReduceKillCooldown.GetFloat(), MinKillCooldown.GetFloat(), DefaultKillCooldown.GetFloat());
        if (KillCooldown < before) ArroganceCooldownReduce.Trigger(killer);
        if (killer != null && killer.PlayerId == Player.PlayerId)
        {
            Kills++;
            KillsWithoutMeeting++;
            if (KillsWithoutMeeting > MaxKillsWithoutMeeting) MaxKillsWithoutMeeting = KillsWithoutMeeting;
            if (before < DefaultKillCooldown.GetFloat()) KilledWithReducedCooldown = true;
        }
        killer.ResetKillCooldown();
        killer.SyncSettings();
    }
}