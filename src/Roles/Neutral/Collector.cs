using AmongUs.GameOptions;
using Hazel;
using TONX.Modules;
using TONX.Modules.Achievements.Neutral;

namespace TONX.Roles.Neutral;

public sealed class Collector : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
       SimpleRoleInfo.Create(
            typeof(Collector),
            player => new Collector(player),
            CustomRoles.Collector,
            () => RoleTypes.Scientist,
            CustomRoleTypes.Neutral,
            52100,
            SetupOptionItem,
            "colle|集票者|集票",
            "#9d8892"
        );
    public Collector(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CollectVote = 0;
        CollectorVoteFor = null;
    }
    private int CollectVote;
    private PlayerControl CollectorVoteFor;
    public bool VoteWinTriggered { get; private set; }
    public bool BaseWinTriggered { get; private set; }
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(CollectVote);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        int Num = reader.ReadInt32();
        CollectVote = Num;
    }
    enum OptionName
    {
        CollectorCollectAmount,
    }
    public static OptionItem CollectorCollectAmount;
    private static void SetupOptionItem()
    {
        CollectorCollectAmount = IntegerOptionItem.Create(RoleInfo, 10, OptionName.CollectorCollectAmount, new(1, 999, 1), 20, false)
            .SetValueFormat(OptionFormat.Votes);
    }

    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);

    public override string GetProgressText(bool comms = false)
    {
        int VoteAmount = CollectVote;
        int CollectNum = CollectorCollectAmount.GetInt();
        return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Collector).ShadeColor(0.25f), $"({VoteAmount}/{CollectNum})");
    }
    public override bool CheckVoteAsVoter(PlayerControl votedFor)
    {
        if (votedFor == null || !Player.IsAlive()) return true;
        CollectorVoteFor = votedFor;
        return true;
    }

    public override void OnVotingComplete(MeetingVoteManager.VoteResult voteResult)
    {
        if (!AmongUsClient.Instance.AmHost || !Player.IsAlive() || CollectorVoteFor == null
            || MeetingVoteManager.Instance?.AllVotes.TryGetValue(Player.PlayerId, out var vote) != true
            || vote.NumVotes <= 0 || vote.VotedFor != CollectorVoteFor.PlayerId
            || !voteResult.VotedCounts.TryGetValue(CollectorVoteFor.PlayerId, out var collected)
            || collected <= 0) return;
        CollectVote += collected;
        CollectorVoteCollected.Trigger(Player);
        SendRPC();
    }
    public override void AfterMeetingTasks()
    {
        CollectorVoteFor = null;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost || CollectVote < CollectorCollectAmount.GetInt()) return;
        BaseWinTriggered |= CustomWinnerHolder.WinnerTeam == CustomWinner.Default;
        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Collector);
        CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
        VoteWinTriggered = true;
    }
}
