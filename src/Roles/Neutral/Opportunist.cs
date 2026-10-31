using AmongUs.GameOptions;
using TONX.Roles.Core.Interfaces;

namespace TONX.Roles.Neutral;

public sealed class Opportunist : RoleBase, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
       SimpleRoleInfo.Create(
            typeof(Opportunist),
            player => new Opportunist(player),
            CustomRoles.Opportunist,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            50100,
            null,
            "op|投机主义者|投机",
            "#00ff00"
        );
    public Opportunist(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    public bool SurvivalWinGranted { get; private set; }
    public int MeetingsSurvived { get; private set; }
    public int ExileMeetingsSurvived { get; private set; }
    private bool AliveAtMeetingStart;
    private bool OtherPlayerExiled;
    public override void OnStartMeeting()
    {
        AliveAtMeetingStart = Player.IsAlive();
        OtherPlayerExiled = false;
    }
    public override void OnExileWrapUp(NetworkedPlayerInfo exiled, ref bool DecidedWinner)
    {
        OtherPlayerExiled |= exiled != null && exiled.PlayerId != Player.PlayerId;
    }
    public override void AfterMeetingTasks()
    {
        if (!AliveAtMeetingStart || !Player.IsAlive()
            || Main.AfterMeetingDeathPlayers.ContainsKey(Player.PlayerId)) return;
        MeetingsSurvived++;
        if (OtherPlayerExiled || Main.AfterMeetingDeathPlayers.Any(pair =>
                pair.Key != Player.PlayerId && pair.Value == CustomDeathReason.Vote))
            ExileMeetingsSurvived++;
        AliveAtMeetingStart = false;
    }
    public bool CheckWin(ref CustomRoles winnerRole)
    {
        SurvivalWinGranted = Player.IsAlive();
        return SurvivalWinGranted;
    }
}
