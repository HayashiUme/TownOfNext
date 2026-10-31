using AmongUs.GameOptions;
using Hazel;
using TONX.Modules;
using TONX.Modules.Achievements.Crewmate;
using TONX.Roles.Core.Interfaces;
using TONX.Roles.AddOns.Common;
using TONX.Roles.AddOns.Impostor;
using UnityEngine;

namespace TONX.Roles.Crewmate;
public sealed class Swapper : RoleBase, IMeetingButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Swapper),
            player => new Swapper(player),
            CustomRoles.Swapper,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            23200,
            SetupOptionItem,
            "swa|换票师|换票",
            "#863756"
        );
    public Swapper(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        SwapLimit = OptionSwapLimit.GetInt();
    }

    private static OptionItem OptionSwapLimit;
    private static OptionItem OptionCanUseButton;
    enum OptionName
    {
        SwapperSwapLimit,
        SwapperCanUseButton
    }

    private int SwapLimit;
    private List<byte> Targets = new();
    public bool SwapChangedImpostorExile { get; private set; }
    private Dictionary<byte, (byte Target, int Count)> VotesBeforeSwap;
    public void CaptureVotesBeforeSwap()
    {
        VotesBeforeSwap = MeetingVoteManager.Instance.AllVotes.ToDictionary(
            pair => pair.Key, pair => (pair.Value.VotedFor, pair.Value.NumVotes));
    }

    private static void SetupOptionItem()
    {
        OptionSwapLimit = IntegerOptionItem.Create(RoleInfo, 10, OptionName.SwapperSwapLimit, new(1, 99, 1), 15, false)
            .SetValueFormat(OptionFormat.Times);
        OptionCanUseButton = BooleanOptionItem.Create(RoleInfo, 11, OptionName.SwapperCanUseButton, true, false);
    }
    public override bool OnCheckReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (Is(reporter) && target == null && !OptionCanUseButton.GetBool())
        {
            Logger.Info("因禁止换票师拍灯取消会议", "Swapper.OnCheckReportDeadBody");
            return false;
        }
        return true;
    }
    public override void OnStartMeeting()
    {
        Targets.Clear();
    }
    public override void OverrideNameAsSeer(PlayerControl seen, ref string nameText, bool isForMeeting = false)
    {
        if (Player.IsAlive() && seen.IsAlive() && isForMeeting)
        {
            nameText = Utils.ColorString(RoleInfo.RoleColor, seen.PlayerId.ToString()) + " " + nameText;
        }
    }
    public override void AfterMeetingTasks()
    {
        if (Targets.Count == 2) SwapLimit--;
    }
    public override void OnVotingComplete(MeetingVoteManager.VoteResult voteResult)
    {
        var manager = MeetingVoteManager.Instance;
        if (VotesBeforeSwap == null || Targets.Count != 2
            || voteResult.Exiled?.Object?.GetCustomRole().IsImpostorTeam() != true
            || SpecialMeetingManager.GetActiveSpecialMeeting() != null
            || TicketsStealer.IsEnable
            || Main.AllPlayerControls.Any(pc => pc.Is(CustomRoles.Zombie) || pc.Is(CustomRoles.Dictator))) return;
        var swaps = manager.SwappedPlayers.Where(s =>
            Utils.GetPlayerById(s.Target1)?.IsAlive() == true
            && Utils.GetPlayerById(s.Target2)?.IsAlive() == true).ToArray();
        if (swaps.Count(s => s.Swapper == Player.PlayerId) != 1) return;
        var withoutOwn = new Dictionary<byte, (byte Target, int Count)>();
        foreach (var pair in VotesBeforeSwap)
        {
            var actualTarget = pair.Value.Target;
            var comparedTarget = actualTarget;
            foreach (var swap in swaps)
            {
                actualTarget = SwapTarget(actualTarget, swap.Target1, swap.Target2);
                if (swap.Swapper != Player.PlayerId)
                    comparedTarget = SwapTarget(comparedTarget, swap.Target1, swap.Target2);
            }
            if (!manager.AllVotes.TryGetValue(pair.Key, out var actual)
                || actual.VotedFor != actualTarget || actual.NumVotes != pair.Value.Count) return;
            withoutOwn[pair.Key] = (comparedTarget, pair.Value.Count);
        }
        var counts = voteResult.VotedCounts.ToDictionary(pair => pair.Key, _ => 0);
        foreach (var vote in withoutOwn.Values)
            if (vote.Target != MeetingVoteManager.NoVote && counts.ContainsKey(vote.Target))
                counts[vote.Target] += vote.Count;
        var highest = counts.Values.Max();
        var leaders = counts.Where(pair => pair.Value == highest).Select(pair => pair.Key).ToArray();
        if (leaders.Length == 1)
        {
            if (leaders[0] != voteResult.Exiled.PlayerId) SwapChangedImpostorExile = true;
            return;
        }
        var breakerTargets = withoutOwn.Where(pair => Tiebreaker.IsThisRole(pair.Key))
            .Select(pair => pair.Value.Target).ToHashSet();
        var chosen = leaders.Where(breakerTargets.Contains).ToArray();
        if (chosen.Length == 1)
        {
            if (chosen[0] != voteResult.Exiled.PlayerId) SwapChangedImpostorExile = true;
            return;
        }
        if (Options.VoteMode.GetBool())
        {
            var mode = (TieMode)Options.WhenTie.GetValue();
            if (mode == TieMode.Random || (mode == TieMode.All && leaders.Contains(voteResult.Exiled.PlayerId))) return;
        }
        SwapChangedImpostorExile = true;
    }
    private static byte SwapTarget(byte target, byte first, byte second)
        => target == first ? second : target == second ? first : target;

    public string ButtonName { get; private set; } = "Swapper";
    public bool ShouldShowButton() => Player.IsAlive();
    public bool ShouldShowButtonFor(PlayerControl target) => target.IsAlive();
    public override bool OnSendMessage(string msg, out MsgRecallMode recallMode)
    {
        bool isCommand = SwapMsg(Player, msg, out bool spam);
        recallMode = spam ? MsgRecallMode.Spam : MsgRecallMode.None;
        return isCommand;
    }
    public void OnClickButton(PlayerControl target)
    {
        if (!Swap(target, out var reason))
            Player.ShowPopUp(reason);
    }
    public void OnUpdateButton(MeetingHud meetingHud)
    {
        foreach (var pva in meetingHud.playerStates)
        {
            var btn = pva?.transform?.FindChild("Custom Meeting Button")?.gameObject;
            if (!btn) continue;
            if (Targets.Contains(pva.PlayerId)) btn.GetComponent<SpriteRenderer>().color = Color.green;
            else btn.GetComponent<SpriteRenderer>().color = Targets.Count == 2 ? Color.gray : Color.red;
        }
    }
    private bool Swap(PlayerControl target, out string reason)
    {
        reason = string.Empty;

        if (Targets.Remove(target.PlayerId))
        {
            string Name = target.GetRealName();

            Logger.Info($"{Player.GetNameWithRole()} => Cancel Swap {target.GetNameWithRole()}({Targets.Count})", "Swapper");

            SendRpc();

            _ = new LateTask (() =>
            {
                Utils.SendMessage(
                    string.Format(GetString("SwapSkillCancelled"), Name),
                    Player.PlayerId,
                    Utils.ColorString(Utils.GetRoleColor(CustomRoles.Swapper), GetString("SwapVoteTitle")));
            }, 0.8f, "Swap Skill Cancelled");
        }
        else
        {
            if (Targets.Count == 2)
            {
                reason = GetString("SwapUsed");
                return false;
            }
            if (SwapLimit < 1)
            {
                reason = GetString("SwapperSwapMax");
                return false;
            }

            string Name2 = target.GetRealName();

            Targets.Add(target.PlayerId);
            Logger.Info($"{Player.GetNameWithRole()} => Swap {target.GetNameWithRole()}({Targets.Count})", "Swapper");
            if (Targets.Count == 2) SwapperVoteSwap.Trigger(Player);
            
            SendRpc();

            _ = new LateTask (() =>
            {
                Utils.SendMessage(
                    string.Format(GetString("SwapSkill"), Name2),
                    Player.PlayerId,
                    Utils.ColorString(Utils.GetRoleColor(CustomRoles.Swapper), GetString("SwapVoteTitle")));
            }, 0.8f, "Swap Skill");
        }

        return true;
    }
    public bool SwapMsg(PlayerControl pc, string msg, out bool spam)
    {
        spam = false;
        if (!GameStates.IsInGame || pc == null) return false;
        if (!pc.Is(CustomRoles.Swapper)) return false;

        if (!ChatCommand.OperateRoleCommand(ref msg, "sw|swa|swap|换票|换", out int operate)) return false;

        if (!pc.IsAlive())
        {
            Utils.SendMessage(GetString("SwapperDead"), pc.PlayerId);
            return true;
        }

        if (operate == 1)
        {
            Utils.SendMessage(ChatCommand.GetFormatString(), pc.PlayerId);
            return true;
        }
        if (operate == 2)
        {
            spam = true;
            if (!AmongUsClient.Instance.AmHost) return true;

            if (!MsgToPlayer(msg, out PlayerControl target, out string error))
            {
                Utils.SendMessage(error, pc.PlayerId);
                return true;
            }

            if (!Swap(target, out var reason))
                Utils.SendMessage(reason, pc.PlayerId);
        }
        return true;
    }
    private static bool MsgToPlayer(string msg, out PlayerControl target, out string error)
    {
        error = string.Empty;

        //判断选择的玩家是否合理
        target = Utils.MsgToPlayer(ref msg, out bool multiplePlayers);
        if (target == null)
        {
            error = multiplePlayers ? GetString("SwapMultipleColor") : GetString("SwapHelp");
            return false;
        }
        if (target.Data.IsDead)
        {
            error = GetString("SwapNull");
            return false;
        }
        if (Justice.UnableToBeTargetedInJusticeMeeting(target))
        {
            error = GetString("JusticeMeetingBanAbility");
            return false;
        }
        return true;
    }
    private void SendRpc()
    {
        using var sender = CreateSender();
        sender.Writer.Write(Targets.Count);
        foreach (var target in Targets) sender.Writer.Write(target);
        RefreshTargets();
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        Targets = new();
        var num = reader.ReadInt32();
        for (var i = 0; i < num; i++) Targets.Add(reader.ReadByte());
        RefreshTargets();
    }
    private void RefreshTargets()
    {
        MeetingVoteManager.Instance?.RemoveSwappedPlayers(Player.PlayerId);
        if (Targets.Count == 2) MeetingVoteManager.Instance?.AddSwappedPlayers(Player.PlayerId, Targets[0], Targets[1], true);
    }
}
