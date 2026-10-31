using AmongUs.GameOptions;
using TONX.Modules;
using TONX.Roles.Core.Interfaces;

namespace TONX.Roles.Neutral;

public sealed class Sunnyboy : RoleBase, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
       SimpleRoleInfo.Create(
            typeof(Sunnyboy),
            player => new Sunnyboy(player),
            CustomRoles.Sunnyboy,
            () => RoleTypes.Scientist,
            CustomRoleTypes.Neutral,
            52700,
            null,
            "sb|阳光开朗大男孩|大男孩",
            "#ff9902"
#if RELEASE
            ,
            Hidden: new HiddenRoleInfo(3, CustomRoles.Jester) // For Debug
#endif
        );
    public Sunnyboy(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        
    }

    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);

    public bool DeathWinGranted { get; private set; }
    public bool DiedBeforeFirstMeeting { get; private set; }
    public int MeetingsCompletedAfterDeath { get; private set; }
    private int MeetingsStarted;
    private bool DeadAtMeetingStart;
    public override void OnPlayerDeath(PlayerControl player, CustomDeathReason deathReason, bool isOnMeeting = false)
    {
        if (player == Player && MeetingsStarted == 0 && !MeetingStates.MeetingCalled
            && !player.Data.Disconnected && deathReason != CustomDeathReason.Disconnected)
            DiedBeforeFirstMeeting = true;
    }
    public override void OnStartMeeting()
    {
        MeetingsStarted++;
        DeadAtMeetingStart = Player.Data.IsDead || MyState.IsDead;
    }
    public override void AfterMeetingTasks()
    {
        if (DeadAtMeetingStart) MeetingsCompletedAfterDeath++;
        DeadAtMeetingStart = false;
    }
    public bool CheckWin(ref CustomRoles winnerRole)
    {
        DeathWinGranted = !Player.IsAlive();
        return DeathWinGranted;
    }
}
