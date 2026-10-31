using AmongUs.GameOptions;
using TONX.Modules;
using TONX.Modules.Achievements.Crewmate;
using TONX.Roles.Core.Interfaces;

namespace TONX.Roles.Crewmate;
public sealed class Doctor : RoleBase, IDeathReasonSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Doctor),
            player => new Doctor(player),
            CustomRoles.Doctor,
            () => RoleTypes.Scientist,
            CustomRoleTypes.Crewmate,
            21100,
            SetupOptionItem,
            "doc|法医",
            "#80ffdd"
        );
    public Doctor(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        TaskCompletedBatteryCharge = OptionTaskCompletedBatteryCharge.GetFloat();
    }
    private static OptionItem OptionTaskCompletedBatteryCharge;
    enum OptionName
    {
        DoctorTaskCompletedBatteryCharge
    }
    private static float TaskCompletedBatteryCharge;
    public HashSet<byte> RevealedDeathReasons { get; } = new();
    public int RevealedDeathReasonCount => RevealedDeathReasons.Count;
    public bool VotedForRevealedImpostor { get; private set; }
    public bool RecordDeathReason(PlayerControl seen)
    {
        if (seen == null || PlayerState.GetByPlayerId(seen.PlayerId)?.IsDead != true) return false;
        RevealedDeathReasons.Add(seen.PlayerId);
        DoctorDeathReasonConfirmed.Trigger(Player);
        return true;
    }
    public override void OnVotingComplete(MeetingVoteManager.VoteResult voteResult)
    {
        if (RevealedDeathReasons.Count >= 2
            && voteResult.Exiled?.Object?.GetCustomRole().IsImpostor() == true
            && MeetingVoteManager.Instance.AllVotes.TryGetValue(Player.PlayerId, out var vote)
            && vote.VotedFor == voteResult.Exiled.PlayerId)
        {
            VotedForRevealedImpostor = true;
            DoctorTwoCasesSolved.Trigger(Player);
        }
    }
    private static void SetupOptionItem()
    {
        OptionTaskCompletedBatteryCharge = FloatOptionItem.Create(RoleInfo, 10, OptionName.DoctorTaskCompletedBatteryCharge, new(0f, 10f, 1f), 5f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ScientistCooldown = 0f;
        AURoleOptions.ScientistBatteryCharge = TaskCompletedBatteryCharge;
    }
}
