using AmongUs.GameOptions;
using Hazel;
using UnityEngine;
using TONX.Modules;
using TONX.Roles.Core.Interfaces;

namespace TONX.Roles.Crewmate;

public class Justice : RoleBase, IMeetingButton, IMeetingTimeAlterable, ISpecialMeeting
{
    public static readonly SimpleRoleInfo RoleInfo = SimpleRoleInfo.Create(
        typeof(Justice),
        player => new Justice(player),
        CustomRoles.Justice,
        () => RoleTypes.Crewmate,
        CustomRoleTypes.Crewmate,
        23400,
        SetupOptionItem,
        "jus|大神官",
        "#FFD700"
    );
    
    public Justice(PlayerControl player) : base(RoleInfo, player)
    {
        SpecialMeetingPlayers = new List<byte>(2);
    }

    private static OptionItem OptionCanUseTimes;
    private static OptionItem OptionMeetingVotingTime;
    enum OptionName
    {
        JusticeCanUseTimes,
        JusticeMeetingVotingTime
    }
    
    private int SkillLimits;
    public List<byte> SelectedPlayes;
    public bool IsSpecialMeetingActive { get; set; }
    public List<byte> SpecialMeetingPlayers { get; set; }
    public bool AllowSkip => false;
    public bool RevertOnDie => true;

    private static void SetupOptionItem()
    {
        OptionCanUseTimes = IntegerOptionItem.Create(RoleInfo, 10, OptionName.JusticeCanUseTimes, new(1, 15, 1), 3, false)
            .SetValueFormat(OptionFormat.Times);
        OptionMeetingVotingTime = IntegerOptionItem.Create(RoleInfo, 11, OptionName.JusticeMeetingVotingTime, new(0, 300, 15), 120, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Add()
    {
        SkillLimits = OptionCanUseTimes.GetInt();
        IsSpecialMeetingActive = false;
    }
    public bool CheckSpecialMeetingVote(PlayerControl voter, PlayerControl voted)
    {
        if (voted == null) return false;
        if (SpecialMeetingPlayers.Contains(voted.PlayerId)) return true;
        Utils.SendMessage(GetString("JusticeMeetingInvalidVote"), voter.PlayerId);
        return false;
    }
    public override NetworkedPlayerInfo GetVoteOverride(MeetingVoteManager.VoteResult result)
    {
        if (!IsSpecialMeetingActive || result.IsTie || result.Exiled == null) return null;
        return result.Exiled;
    }

    public bool OnSpecialMeetingVotingComplete(MeetingVoteManager.VoteResult voteResult)
    {
        if (!voteResult.IsTie || !voteResult.NoVotes) return false;
        var (target1, target2) = (SpecialMeetingPlayers[0], SpecialMeetingPlayers[1]);
        MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Vote, target1);
        MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Vote, target2);
        Logger.Info($"Double Exile => {Utils.GetPlayerById(target1)?.GetNameWithRole()} & {Utils.GetPlayerById(target2)?.GetNameWithRole()}", "Justice");
        return true; 
    }
    
    public int CalculateMeetingTimeDelta() => OptionMeetingVotingTime.GetInt() - Main.RealOptionsData.GetInt(Int32OptionNames.VotingTime);

    public override void NotifyOnMeetingStart(ref List<(string, byte, string)> msgToSend)
    {
        if (Player.IsAlive())
        {
            msgToSend.Add((string.Format(GetString("JusticeUsesRemaining"), SkillLimits),
            Player.PlayerId,
            Utils.ColorString(Utils.GetRoleColor(CustomRoles.Justice), GetString("JusticeBalanceTitle"))));
        }
    }
    public override void OnStartMeeting()
    {
        Logger.Info($"Justice.OnStartMeeting: active={IsSpecialMeetingActive}, players=[{string.Join(",", SpecialMeetingPlayers)}]", "Justice");
        if (!IsSpecialMeetingActive) return;
        var target1 = Utils.GetPlayerById(SpecialMeetingPlayers[0]);
        var target2 = Utils.GetPlayerById(SpecialMeetingPlayers[1]);
        Utils.SendMessage(
            string.Format(GetString("JusticeMeetingStart"), target1.GetRealName(), target2.GetRealName()),
            255,
            Utils.ColorString(Utils.GetRoleColor(CustomRoles.Justice), GetString("JusticeBalanceTitle"))
        );
        foreach (var player in Main.AllPlayerControls) player.KillFlash();
    }
    public void OnSpecialMeetingEnd()
    {
        if (!IsSpecialMeetingActive) return;
        SpecialMeetingPlayers.Clear();
        IsSpecialMeetingActive = false;
        SendRPC();
    }

    public override void AfterMeetingTasks()
    {
        Logger.Info($"Justice.AfterMeetingTasks: active={IsSpecialMeetingActive}, players=[{string.Join(",", SpecialMeetingPlayers)}]", "Justice");
        if (IsSpecialMeetingActive) return;
        if (SpecialMeetingPlayers.Count == 2)
        {
            SkillLimits--;
            IsSpecialMeetingActive = true;
            Logger.Info($"Justice.AfterMeetingTasks: 天平会议已激活, players=[{string.Join(",", SpecialMeetingPlayers)}]", "Justice");
            _ = new LateTask(() =>
            {
                PlayerControl.LocalPlayer.NoCheckStartMeeting(null, true);
            }, 0.1f, "StartJusticeMeeting");
        }
        else SpecialMeetingPlayers.Clear();
        SendRPC();
    }


    public override void OverrideNameAsSeer(PlayerControl seen, ref string nameText, bool isForMeeting = false)
    {
        if (Player.IsAlive() && seen.IsAlive() && isForMeeting)
        {
            nameText = Utils.ColorString(RoleInfo.RoleColor, seen.PlayerId.ToString()) + " " + nameText;
        }
    }

    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(IsSpecialMeetingActive);
        sender.Writer.Write(SpecialMeetingPlayers.Count);
        foreach (var smpId in SpecialMeetingPlayers) sender.Writer.Write(smpId);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        IsSpecialMeetingActive = reader.ReadBoolean();
        var count = reader.ReadInt32();
        SpecialMeetingPlayers = new List<byte>(count);
        for (var i = 0; i < count; i++) SpecialMeetingPlayers.Add(reader.ReadByte());
    }

    public string ButtonName => "Scale";
    public bool ShouldShowButton() => Player.IsAlive() && !IsJusticeMeeting();
    public bool ShouldShowButtonFor(PlayerControl target) => target.IsAlive();
    public override bool OnSendMessage(string msg, out MsgRecallMode recallMode)
    {
        var isCommand = JusticeMsg(Player, msg, out var spam);
        recallMode = spam ? MsgRecallMode.Spam : MsgRecallMode.None;
        return isCommand;
    }
    public void OnClickButton(PlayerControl target)
    {
        if (!Scale(target, out var reason))
        {
            Player.ShowPopUp(reason);
            return;
        }
    }
    public void OnUpdateButton(MeetingHud meetingHud)
    {
        foreach (var pva in meetingHud.playerStates)
        {
            var btn = pva?.transform?.FindChild("Custom Meeting Button")?.gameObject;
            if (!btn) continue;

            if (SpecialMeetingPlayers.Contains(pva.PlayerId))
                btn.GetComponent<SpriteRenderer>().color = Color.yellow;
            else
                btn.GetComponent<SpriteRenderer>().color = Color.white;
        }
    }

    private bool Scale(PlayerControl target, out string reason)
    {
        reason = string.Empty;

        if (SpecialMeetingPlayers.Remove(target.PlayerId))
        {
            string Name = target.GetRealName();

            Logger.Info($"{Player.GetNameWithRole()} => Cancel Scale {target.GetNameWithRole()}({SpecialMeetingPlayers.Count})", "Justice");

            SendRPC();

            _ = new LateTask (() =>
            {
                Utils.SendMessage(
                    string.Format(GetString("BalanceSkillCancelled"), Name),
                    Player.PlayerId,
                    Utils.ColorString(Utils.GetRoleColor(CustomRoles.Justice), GetString("JusticeBalanceTitle")));
            }, 0.8f, "Balance Skill Cancelled");

            return true;
        }

        if (SkillLimits < 1)
        {
            reason = GetString("JusticeBalanceMax");
            return false;
        }
        if (SpecialMeetingPlayers.Count == 2)
        {
            reason = GetString("BalanceUsed");
            return false;
        }

        string Name2 = target.GetRealName();

        SpecialMeetingPlayers.Add(target.PlayerId);
        Logger.Info($"{Player.GetNameWithRole()} => Scale {target.GetNameWithRole()}({SpecialMeetingPlayers.Count})", "Justice");

        SendRPC();

        _ = new LateTask (() =>
        {
            Utils.SendMessage(
                string.Format(GetString("BalanceSkill"), Name2),
                Player.PlayerId,
                Utils.ColorString(Utils.GetRoleColor(CustomRoles.Justice), GetString("JusticeBalanceTitle")));
        }, 0.8f, "Balance Skill");

        if (SpecialMeetingPlayers.Count == 2) MeetingHud.Instance.RpcForceEndMeeting();

        return true;
    }

    private bool JusticeMsg(PlayerControl pc, string msg, out bool spam)
    {
        spam = false;
        if (!GameStates.IsInGame || pc == null) return false;
        if (!pc.Is(CustomRoles.Justice)) return false;

        if (!ChatCommand.OperateRoleCommand(ref msg, "bl|balance|scale|天平|审判|jtc|sc", out int operate)) return false;

        if (!pc.IsAlive())
        {
            Utils.SendMessage(GetString("JusticeDead"), pc.PlayerId);
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

            if (!Scale(target, out var reason))
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
            error = multiplePlayers ? GetString("BalanceMultipleColor") : GetString("BalanceHelp");
            return false;
        }
        if (target.Data.IsDead)
        {
            error = GetString("BalanceNull");
            return false;
        }
        if (IsJusticeMeeting())
        {
            error = GetString("JusticeMeeting");
            return false;
        }
        return true;
    }

    public override void OnPlayerDeath(PlayerControl player, CustomDeathReason deathReason, bool isOnMeeting = false)
    {
        Logger.Info($"OnPlayerDeath enter: dyingPlayer={player.GetNameWithRole()}, deathReason={deathReason}, isOnMeeting={isOnMeeting}, MyPlayer={Player.GetNameWithRole()}, HostingJusticeMeeting={IsSpecialMeetingActive}, SelectedPlayers=[{string.Join(",", SpecialMeetingPlayers)}]", "Justice");
        if (deathReason is CustomDeathReason.Vote || !isOnMeeting || player == null) return;
        if (!IsSpecialMeetingActive)
        {
            if (SpecialMeetingPlayers.Remove(player.PlayerId)) SendRPC();
            return;
        }

        // Justice 死亡或掉线时清除天平会议状态，让当前会议正常继续
        if (player.PlayerId == Player.PlayerId && deathReason == CustomDeathReason.Disconnected)
        {
            Logger.Info($"Justice 死亡或掉线，清除天平会议状态", "Justice");
            IsSpecialMeetingActive = false;
            SpecialMeetingPlayers.Clear();
            SendRPC();
            return;
        }

        // 被选中的目标在会议期间死亡 — 将其移除并放逐存活者
        if (SpecialMeetingPlayers.Remove(player.PlayerId)) SendRPC();
        if (SpecialMeetingPlayers.Count == 0 || MeetingVoteManager.Instance == null) return;

        var survivorId = SpecialMeetingPlayers.Find(x => x != player.PlayerId);
        if (Utils.GetPlayerById(survivorId) == null) return;
        MeetingVoteManager.Instance.ClearAndExile(player.PlayerId, survivorId);
        Utils.SendMessage(
            string.Format(GetString("JusticeMeetingForceExile"), player.GetRealName(), Utils.GetPlayerById(survivorId).GetRealName()),
            255,
            Utils.ColorString(Utils.GetRoleColor(CustomRoles.Justice), GetString("JusticeBalanceTitle"))
        );
    }

    public static bool UnableToBeTargetedInJusticeMeeting(PlayerControl target) => IsJusticeMeeting() && (!GetHostingJustice()?.SpecialMeetingPlayers?.Contains(target.PlayerId) ?? true);
    public static bool IsJusticeMeeting() => GetHostingJustice() != null;
    public static Justice GetHostingJustice()
    {
        foreach (var pc in Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Justice)))
        {
            if (pc.GetRoleClass() is not Justice roleClass) continue;
            if (roleClass.IsSpecialMeetingActive) return roleClass;
        }
        return null;
    }
}