using AmongUs.GameOptions;
using Hazel;
using TONX.Modules;
using TONX.Modules.Achievements.Crewmate;
using TONX.Roles.AddOns.Common;

namespace TONX.Roles.Crewmate;
public sealed class Mayor : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Mayor),
            player => new Mayor(player),
            CustomRoles.Mayor,
            () => OptionHasPortableButton.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            20500,
            SetupOptionItem,
            "my|市长",
            "#204d42"
        );
    public Mayor(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        AdditionalVote = OptionAdditionalVote.GetInt();
        HasPortableButton = OptionHasPortableButton.GetBool();
        NumOfUseButton = OptionNumOfUseButton.GetInt();

        LeftButtonCount = NumOfUseButton;
    }

    private static OptionItem OptionAdditionalVote;
    private static OptionItem OptionHasPortableButton;
    private static OptionItem OptionNumOfUseButton;
    enum OptionName
    {
        MayorAdditionalVote,
        MayorHasPortableButton,
        MayorNumOfUseButton
    }
    public static int AdditionalVote;
    public static bool HasPortableButton;
    public static int NumOfUseButton;
    public static bool HideVote;

    public int LeftButtonCount;
    public bool ExtraVoteChangedImpostorExile { get; private set; }
    private static void SetupOptionItem()
    {
        OptionAdditionalVote = IntegerOptionItem.Create(RoleInfo, 10, OptionName.MayorAdditionalVote, new(1, 15, 1), 4, false)
            .SetValueFormat(OptionFormat.Votes);
        OptionHasPortableButton = BooleanOptionItem.Create(RoleInfo, 11, OptionName.MayorHasPortableButton, false, false);
        OptionNumOfUseButton = IntegerOptionItem.Create(RoleInfo, 12, OptionName.MayorNumOfUseButton, new(1, 99, 1), 3, false, OptionHasPortableButton)
            .SetValueFormat(OptionFormat.Times);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown =
            LeftButtonCount <= 0
            ? 255f
            : opt.GetInt(Int32OptionNames.EmergencyCooldown);
        AURoleOptions.EngineerInVentMaxTime = 1;
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (LeftButtonCount > 0)
        {
            var user = physics.myPlayer;
            //ホスト視点、vent処理中に会議を呼ぶとベントの矢印が残るので遅延させる
            _ = new LateTask(() =>
            {
                user?.ReportDeadBody(null);
                if (GameStates.IsMeeting)
                {
                    LeftButtonCount--;
                    SendRpc();
                }
            }, 0.1f, "MayorPortableButton");

            //ポータブルボタン時はベントから追い出す必要はない
            return true;
        }
        return false;
    }
    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
    {
        // 既定値
        var (votedForId, numVotes, doVote) = base.ModifyVote(voterId, sourceVotedForId, isIntentional);
        if (voterId == Player.PlayerId)
        {
            numVotes = AdditionalVote + 1;
            if (isIntentional && numVotes > 1) MayorExtraVote.Trigger(Player);
        }
        return (votedForId, numVotes, doVote);
    }
    public override void OnVotingComplete(MeetingVoteManager.VoteResult voteResult)
    {
        var exileId = voteResult.Exiled?.PlayerId;
        if (exileId == null || Utils.GetPlayerById(exileId.Value)?.GetCustomRole().IsImpostor() != true
            || !MeetingVoteManager.Instance.AllVotes.TryGetValue(Player.PlayerId, out var ownVote)
            || ownVote.VotedFor != exileId.Value || ownVote.NumVotes <= AdditionalVote
            || SpecialMeetingManager.GetActiveSpecialMeeting() != null) return;

        var counts = voteResult.VotedCounts.ToDictionary(pair => pair.Key, pair => pair.Value);
        counts[exileId.Value] -= AdditionalVote;
        var highest = counts.Values.Max();
        var leaders = counts.Where(pair => pair.Value == highest).Select(pair => pair.Key).ToArray();
        if (leaders.Length == 1)
        {
            if (leaders[0] != exileId.Value) ExtraVoteChangedImpostorExile = true;
            return;
        }
        if (Tiebreaker.ChooseExileTarget(leaders, out var chosen))
        {
            if (chosen != exileId.Value) ExtraVoteChangedImpostorExile = true;
            return;
        }
        if (Options.VoteMode.GetBool())
        {
            var mode = (TieMode)Options.WhenTie.GetValue();
            if (mode == TieMode.Random || (mode == TieMode.All && leaders.Contains(exileId.Value))) return;
        }
        ExtraVoteChangedImpostorExile = true;
    }
    public override int OverrideAbilityButtonUsesRemaining() => LeftButtonCount;
    private void SendRpc()
    {
        using var sender = CreateSender();
        sender.Writer.Write(LeftButtonCount);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        LeftButtonCount = reader.ReadInt32();
    }
}
