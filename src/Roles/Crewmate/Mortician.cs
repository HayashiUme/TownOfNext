using AmongUs.GameOptions;
using Hazel;
using TONX.Modules;
using TONX.Modules.Achievements.Crewmate;
using UnityEngine;

namespace TONX.Roles.Crewmate;
public sealed class Mortician : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Mortician),
            player => new Mortician(player),
            CustomRoles.Mortician,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            22400,
            null,
            "mo|入殮師|入检师|入殓",
            "#333c49"
        );
    public Mortician(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        LastPlayerName = new();
        MsgToSend = null;
    }

    private static Dictionary<byte, string> LastPlayerName;
    private static Dictionary<byte, byte> LastContactId = new();
    private string MsgToSend;
    public bool KillerExiledAfterContact { get; private set; }
    private byte ContactedKillerId = byte.MaxValue;
    private int ContactMeetingsRemaining;
    private void SendRPC(bool add, Vector3 loc = new())
    {
        using var sender = CreateSender();
        sender.Writer.Write(add);
        if (add)
        {
            sender.Writer.Write(loc.x);
            sender.Writer.Write(loc.y);
            sender.Writer.Write(loc.z);
        }
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        
        if (reader.ReadBoolean())
            LocateArrow.Add(Player.PlayerId, new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
        else LocateArrow.RemoveAllTarget(Player.PlayerId);
    }
    public override void OnPlayerDeath(PlayerControl player, CustomDeathReason deathReason, bool isOnMeeting = false)
    {
        var target = player;
        var pos = target.GetTruePosition();
        float minDis = float.MaxValue;
        string minName = string.Empty;
        byte minPlayerId = byte.MaxValue;
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (pc.PlayerId == target.PlayerId) continue;
            var dis = Vector2.Distance(pc.GetTruePosition(), pos);
            if (dis < minDis && dis < 1.5f)
            {
                minDis = dis;
                minName = pc.GetRealName();
                minPlayerId = pc.PlayerId;
            }
        }

        LastPlayerName.TryAdd(target.PlayerId, minName);
        LastContactId[target.PlayerId] = minPlayerId;
        LocateArrow.Add(Player.PlayerId, target.transform.position);
        SendRPC(true, target.GetTruePosition());
    }
    public override void OnStartMeeting()
    {
        LocateArrow.RemoveAllTarget(Player.PlayerId);
        SendRPC(false);
    }
    public override void OnVotingComplete(MeetingVoteManager.VoteResult voteResult)
    {
        if (ContactedKillerId != byte.MaxValue && voteResult.Exiled?.PlayerId == ContactedKillerId
            && MeetingVoteManager.Instance.AllVotes.TryGetValue(Player.PlayerId, out var ownVote)
            && ownVote.VotedFor == ContactedKillerId)
            KillerExiledAfterContact = true;
        else if (ContactMeetingsRemaining > 0 && --ContactMeetingsRemaining == 0)
            ContactedKillerId = byte.MaxValue;
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (reporter == null || !Is(reporter) || target == null || reporter.PlayerId == target.PlayerId) return;
        var targetObject = target.Object;
        var killer = targetObject?.GetRealKiller();
        if (killer != null && LastContactId.TryGetValue(target.PlayerId, out var lastContactId)
            && lastContactId == killer.PlayerId)
        {
            ContactedKillerId = killer.PlayerId;
            ContactMeetingsRemaining = 2;
        }
        MsgToSend = LastPlayerName.TryGetValue(target.PlayerId, out var name)
            ? string.Format(GetString("MorticianGetInfo"), target.PlayerName, name)
            : string.Format(GetString("MorticianGetNoInfo"), target.PlayerName);
    }
    public override void NotifyOnMeetingStart(ref List<(string, byte, string)> msgToSend)
    {
        if (MsgToSend != null)
        {
            msgToSend.Add((MsgToSend, Player.PlayerId, Utils.ColorString(RoleInfo.RoleColor, GetString("MorticianCheckTitle"))));
            MorticianLastContact.Trigger(Player);
        }
        MsgToSend = null;
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (!Is(seer) || !(seen) || isForMeeting) return "";
        return (Utils.ColorString(Color.white, LocateArrow.GetArrows(seer)));
    }
}
