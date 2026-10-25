using AmongUs.GameOptions;
using Hazel;
using TONX.Modules;
using TONX.Roles.Core.Interfaces;
using UnityEngine;

namespace TONX.Roles.Neutral;

public sealed class Lawyer : RoleBase, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Lawyer),
            player => new Lawyer(player),
            CustomRoles.Lawyer,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            53000,
            SetupOptionItem,
            "law|律師|律师",
            "#788514",
            countType: CountTypes.None
        );
    public Lawyer(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        CanTargetMadmate = OptionCanTargetMadmate.GetBool();
        CanTargetJester = OptionCanTargetJester.GetBool();

        Lawyers.Add(this);
        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
    }

    public byte TargetId = byte.MaxValue;
    public string AssignedClientCode { get; private set; }
    public bool ClientWinGranted { get; private set; }
    public bool ClientWinRemoved { get; private set; }
    public string WinningClientCode { get; private set; }

    private static OptionItem OptionCanTargetMadmate;
    private static OptionItem OptionCanTargetJester;
    public static OptionItem OptionKnowTargetRole;
    public static OptionItem OptionTargetKnowsLawyer;
    public static OptionItem OptionSkillCooldown;
    public static OptionItem OptionSkillLimit;

    private static bool CanTargetMadmate;
    private static bool CanTargetJester;

    enum OptionName
    {
        LawyerCanTargetMadmate,
        LawyerCanTargetJester,
        LawyerKnowTargetRole,
        LawyerTargetKnowsLawyer,
        ProsecutorsSkillCooldown,
        ProsecutorsSkillLimit,
    }

    public static HashSet<Lawyer> Lawyers = new(15);

    private static void SetupOptionItem()
    {
        OptionCanTargetMadmate = BooleanOptionItem.Create(RoleInfo, 10, OptionName.LawyerCanTargetMadmate, false, false);
        OptionCanTargetJester = BooleanOptionItem.Create(RoleInfo, 11, OptionName.LawyerCanTargetJester, false, false);
        OptionKnowTargetRole = BooleanOptionItem.Create(RoleInfo, 12, OptionName.LawyerKnowTargetRole, false, false);
        OptionTargetKnowsLawyer = BooleanOptionItem.Create(RoleInfo, 13, OptionName.LawyerTargetKnowsLawyer, false, false);
        OptionSkillCooldown = FloatOptionItem.Create(RoleInfo, 14, OptionName.ProsecutorsSkillCooldown, new(2.5f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionSkillLimit = IntegerOptionItem.Create(RoleInfo, 15, OptionName.ProsecutorsSkillLimit, new(1, 999, 1), 3, false)
            .SetValueFormat(OptionFormat.Times);
    }
    public override void OnGameStart()
    {
        AssignTarget();
    }
    private void AssignTarget()
    {
        //ターゲット割り当て
        if (!AmongUsClient.Instance.AmHost) return;

        var playerId = Player.PlayerId;
        List<PlayerControl> targetList = new();
        var rand = IRandom.Instance;
        foreach (var target in Main.AllPlayerControls)
        {
            if (playerId == target.PlayerId) continue;
            if (target.Is(CustomRoles.Madmate))
            {
                if (!CanTargetMadmate) continue;
            }
            else if (target.Is(CustomRoleTypes.Crewmate)) continue;
            if (target.Is(CustomRoles.Jester) && !CanTargetJester) continue;
            if (target.Is(CustomRoles.Lovers)) continue;
            if (target.Is(CustomRoles.GM)) continue;

            targetList.Add(target);
        }
        if (targetList.Count == 0)
        {
            Logger.Info($"{Player.GetNameWithRole()} 没有符合条件的客户,直接转起诉人", "Lawyer");
            ChangeRole(noValidTarget: true);
            return;
        }
        var selectedTarget = targetList[rand.Next(targetList.Count)];
        TargetId = selectedTarget.PlayerId;
        AssignedClientCode = selectedTarget.FriendCode;

        NameColorManager.Add(Player.PlayerId, selectedTarget.PlayerId, RoleInfo.RoleColorCode);
        SendRPC();
    }
    public override void OnDestroy()
    {
        Lawyers.Remove(this);
    }
    public void SendRPC()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        using var sender = CreateSender();
        sender.Writer.Write(TargetId);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        byte targetId = reader.ReadByte();
        TargetId = targetId;
    }
    public override void OnPlayerDeath(PlayerControl player, CustomDeathReason deathReason, bool isOnMeeting = false)
    {
        if (player.PlayerId != TargetId) return;
        ChangeRole();
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        return TargetId == seen.PlayerId ? Utils.ColorString(RoleInfo.RoleColor, "§") : "";
    }
    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;

        if (!OptionTargetKnowsLawyer.GetBool()) return "";
        if (seer.PlayerId != seen.PlayerId) return "";

        foreach (var lawyer in Lawyers.ToArray())
        {
            if (lawyer.TargetId == seer.PlayerId)
                return Utils.ColorString(RoleInfo.RoleColor, "§");
        }
        return "";
    }
    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText)
    {
        if (OptionKnowTargetRole.GetBool() && seen.PlayerId == TargetId) enabled = true;
    }
    public bool CheckWin(ref CustomRoles winnerRole)
    {
        ClientWinGranted = false;
        ClientWinRemoved = false;
        WinningClientCode = null;
        if (TargetId == byte.MaxValue) return false;
        if (!CustomWinnerHolder.WinnerIds.Contains(TargetId)) return false;

        var client = Utils.GetPlayerById(TargetId);
        ClientWinGranted = client?.Data != null && !client.Data.Disconnected
            && !string.IsNullOrEmpty(AssignedClientCode) && client.FriendCode == AssignedClientCode;
        WinningClientCode = client?.FriendCode;
        if (Player.IsAlive())
            ClientWinRemoved = CustomWinnerHolder.WinnerIds.Remove(TargetId)
                && client != null && !CustomWinnerHolder.WinnerRoles.Contains(client.GetCustomRole());
        return true;
    }
    public void ChangeRole(bool noValidTarget = false)
    {
        var player = Player;
        player.RpcChangeRole(CustomRoles.Prosecutors);
        Utils.NotifyRoles();
        player.Notify(GetString(noValidTarget ? "LawyerNoTargetBecameProsecutors" : "LawyerBecameProsecutors"));
    }
}
