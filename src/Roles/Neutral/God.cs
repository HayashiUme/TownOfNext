using AmongUs.GameOptions;
using TONX.Modules.Achievements.Neutral;
using TONX.Roles.Core.Interfaces;
using UnityEngine;

namespace TONX.Roles.Neutral;
public sealed class God : RoleBase, IOverrideWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(God),
            player => new God(player),
            CustomRoles.God,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            50300,
            SetupOptionItem,
            "go|神|上帝",
            "#f96464",
            experimental: true
        );
    public God(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    { }

    private static OptionItem OptionNotifyGodAlive;
    public int MeetingsSurvived { get; private set; }
    public bool RoleInformationProvided { get; private set; }
    public bool OverrideWinTriggered { get; private set; }
    private bool AliveAtMeetingStart;
    public override void OnStartMeeting() => AliveAtMeetingStart = Player.IsAlive();
    public override void AfterMeetingTasks()
    {
        if (AliveAtMeetingStart && Player.IsAlive()
            && !Main.AfterMeetingDeathPlayers.ContainsKey(Player.PlayerId))
        {
            MeetingsSurvived++;
            if (RoleInformationProvided) GodKnowsAll.Trigger(Player);
        }
        AliveAtMeetingStart = false;
    }
    public void RecordRoleInformationProvided(PlayerControl seen)
    {
        if (!AmongUsClient.Instance.AmHost || !Player.IsAlive() || seen == null
            || seen.PlayerId == Player.PlayerId || seen.Data.Disconnected) return;
        RoleInformationProvided = true;
        if (MeetingsSurvived > 0) GodKnowsAll.Trigger(Player);
    }
    enum OptionName
    {
        NotifyGodAlive
    }

    private static void SetupOptionItem()
    {
        OptionNotifyGodAlive = BooleanOptionItem.Create(RoleInfo, 10, OptionName.NotifyGodAlive, true, false);
    }
    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText)
    {
        enabled = true;
    }
    public override void NotifyOnMeetingStart(ref List<(string, byte, string)> msgToSend)
    {
        if (OptionNotifyGodAlive.GetBool())
            msgToSend.Add((GetString("GodNoticeAlive"), 255, Utils.ColorString(RoleInfo.RoleColor, GetString("GodAliveTitle"))));
    }
    public void CheckWin(ref CustomWinner WinnerTeam, ref HashSet<byte> WinnerIds)
    {
        if (Player.IsAlive() && WinnerTeam != CustomWinner.God)
        {
            OverrideWinTriggered = WinnerTeam is not CustomWinner.Default and not CustomWinner.Draw
                and not CustomWinner.Error and not CustomWinner.None;
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.God);
            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.God);
        }
    }
}
