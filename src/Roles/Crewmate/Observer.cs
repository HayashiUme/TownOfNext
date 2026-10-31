using AmongUs.GameOptions;
using TONX.Modules;
using TONX.Modules.Achievements.Crewmate;

namespace TONX.Roles.Crewmate;
public sealed class Observer : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Observer),
            player => new Observer(player),
            CustomRoles.Observer,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            22600,
            null,
            "ob|觀察者|观察",
            "#a8e0fa"
        );
    public Observer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }

    public int ShieldNoticeCount { get; private set; }
    public bool ShieldNoticeFollowedByEvilExile { get; private set; }
    private readonly HashSet<MurderInfo> NotifiedAttacks = new();

    public static void NotifyBlockedAttack(MurderInfo info)
    {
        if (!AmongUsClient.Instance.AmHost || info.IsSuicide || !info.CanKill || !info.DoKill) return;
        var target = info.AttemptTarget;
        foreach (var player in Main.AllAlivePlayerControls)
        {
            if (player.PlayerId == target.PlayerId || player.GetRoleClass() is not Observer observer
                || !observer.NotifiedAttacks.Add(info)) continue;
            player.RpcProtectedMurderPlayer(target);
            observer.ShieldNoticeCount++;
            ObserverShieldNotice.Trigger(player);
        }
    }

    public override void OnVotingComplete(MeetingVoteManager.VoteResult voteResult)
    {
        var role = voteResult.Exiled?.Object?.GetCustomRole();
        if (ShieldNoticeCount >= 3 && role.HasValue
            && (role.Value.IsImpostorTeam() || role.Value.IsNeutralEvil())
            && MeetingVoteManager.Instance.AllVotes.TryGetValue(Player.PlayerId, out var vote)
            && vote.VotedFor == voteResult.Exiled.PlayerId)
            ShieldNoticeFollowedByEvilExile = true;
    }
}
