using AmongUs.GameOptions;
using TONX.Modules;
using TONX.Modules.Achievements.Crewmate;

namespace TONX.Roles.Crewmate;
public sealed class DetectiveTONX : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(DetectiveTONX),
            player => new DetectiveTONX(player),
            CustomRoles.DetectiveTONX,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            21300,
            SetupOptionItem,
            "de|偵探",
            "#7160e8"
        );
    public DetectiveTONX(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        MsgToSend = null;
    }

    static OptionItem OptionKnowKiller;
    enum OptionName
    {
        DetectiveCanknowKiller,
    }

    private string MsgToSend;
    public int CorrectKillersExiled { get; private set; }
    private byte CluedKillerId = byte.MaxValue;
    private static void SetupOptionItem()
    {
        OptionKnowKiller = BooleanOptionItem.Create(RoleInfo, 10, OptionName.DetectiveCanknowKiller, true, false);
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        var tpc = target.Object;
        if (reporter == null || !Is(reporter) || target == null || tpc == null || reporter.PlayerId == target.PlayerId) return;
        {
            string msg;
            msg = string.Format(GetString("DetectiveNoticeVictim"), tpc.GetRealName(), tpc.GetTrueRoleName());
            if (OptionKnowKiller.GetBool())
            {
                var realKiller = tpc.GetRealKiller();
                if (realKiller == null) msg += "；" + GetString("DetectiveNoticeKillerNotFound");
                else
                {
                    CluedKillerId = realKiller.PlayerId;
                    msg += "；" + string.Format(GetString("DetectiveNoticeKiller"), realKiller.GetTrueRoleName());
                }
            }
            MsgToSend = msg;
        }
    }
    public override void NotifyOnMeetingStart(ref List<(string, byte, string)> msgToSend)
    {
        if (MsgToSend != null)
        {
            msgToSend.Add((MsgToSend, Player.PlayerId, Utils.ColorString(RoleInfo.RoleColor, GetString("DetectiveNoticeTitle"))));
            DetectiveClue.Trigger(Player);
        }
        MsgToSend = null;
    }
    public override void OnVotingComplete(MeetingVoteManager.VoteResult voteResult)
    {
        if (CluedKillerId != byte.MaxValue && voteResult.Exiled?.PlayerId == CluedKillerId
            && voteResult.Exiled.Object?.GetCustomRole().IsImpostor() == true
            && MeetingVoteManager.Instance.AllVotes.TryGetValue(Player.PlayerId, out var ownVote)
            && ownVote.VotedFor == CluedKillerId)
            CorrectKillersExiled++;
        CluedKillerId = byte.MaxValue;
    }
}
