using AmongUs.GameOptions;
using TONX.Modules.Achievements.Crewmate;

namespace TONX.Roles.Crewmate;
public sealed class LazyGuy : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(LazyGuy),
            player => new LazyGuy(player),
            CustomRoles.LazyGuy,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            20200,
            null,
            "lg|擺爛人|摆烂",
            "#a4dffe"
        );
    public LazyGuy(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    public bool TaskDone { get; private set; }
    public int MeetingsSurvived { get; private set; }
    public override bool OnCompleteTask(out bool cancel)
    {
        if (!TaskDone)
        {
            TaskDone = true;
            LazyGuyTaskDone.Trigger(Player);
        }
        cancel = false;
        return false;
    }
    public override void OnStartMeeting()
    {
        if (Player.IsAlive()) MeetingsSurvived++;
    }
}