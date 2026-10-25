using AmongUs.GameOptions;
using Hazel;
using TONX.Attributes;
using TONX.Modules.Achievements.Neutral;
using TONX.Roles.Core.Interfaces;
using UnityEngine;

namespace TONX.Roles.Neutral;

public sealed class Prosecutors : RoleBase, IKiller, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Prosecutors),
            player => new Prosecutors(player),
            CustomRoles.Prosecutors,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            53100,
            null,
            "pro|起訴人|起诉人",
            "#788514",
            true,
            countType: CountTypes.None,
            Hidden: new HiddenRoleInfo(0, null)
        );
    public Prosecutors(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        ProsecutorsLimit = Lawyer.OptionSkillLimit.GetInt();

        CustomRoleManager.OnCheckMurderPlayerOthers_After.Add(OnCheckMurderPlayerOthers_After);
    }

    private int ProsecutorsLimit;
    public static HashSet<byte> BlankList = new(15);
    private static readonly Dictionary<byte, (string TargetCode, Prosecutors Owner, string OwnerCode)> BlankSources = new();
    private readonly HashSet<(byte, string)> interceptedKillers = new();
    public int InterceptedKillers => interceptedKillers.Count;
    public bool SurvivalWinGranted { get; private set; }

    [GameModuleInitializer]
    public static void Init()
    {
        BlankList.Clear();
        BlankSources.Clear();
    }

    public override void Add()
    {
        ProsecutorsLimit = Lawyer.OptionSkillLimit.GetInt();
    }
    private void SendRPC_SyncLimit()
    {
        using var sender = CreateSender();
        sender.Writer.Write(ProsecutorsLimit);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        ProsecutorsLimit = reader.ReadInt32();
    }
    public float CalculateKillCooldown() => CanUseKillButton() ? Lawyer.OptionSkillCooldown.GetFloat() : 255f;
    public bool CanUseKillButton() => Player.IsAlive() && ProsecutorsLimit >= 1;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);

    public bool OnCheckMurderAsKiller(MurderInfo info)
    {
        if (info.IsSuicide) return true;
        var (killer, target) = info.AttemptTuple;

        if (ProsecutorsLimit >= 1)
        {
            ProsecutorsLimit -= 1;
            SendRPC_SyncLimit();
            if (BlankList.Add(target.PlayerId))
                BlankSources[target.PlayerId] = (target.FriendCode, this, Player.FriendCode);

            Utils.NotifyRoles(SpecifySeer: killer);
            Logger.Info($"{killer.GetNameWithRole()} 向 {target.GetNameWithRole()} 发射了空包弹，剩余 {ProsecutorsLimit} 发", "Prosecutors");
        }

        info.CanKill = false;
        killer.RpcProtectedMurderPlayer(target);
        killer.SetKillCooldownV2();
        return false;
    }
    public static bool OnCheckMurderPlayerOthers_After(MurderInfo info)
    {
        if (info.IsSuicide) return true;
        var (killer, target) = info.AttemptTuple;
        if (!BlankList.Contains(killer.PlayerId)) return true;

        BlankList.Remove(killer.PlayerId);
        if (BlankSources.Remove(killer.PlayerId, out var source)
            && info.CanKill && info.DoKill
            && source.TargetCode == killer.FriendCode && source.Owner.Player?.Data != null
            && source.Owner.Player.FriendCode == source.OwnerCode && !source.Owner.Player.Data.Disconnected
            && source.Owner.Player.GetRoleClass() == source.Owner)
        {
            source.Owner.interceptedKillers.Add((killer.PlayerId, killer.FriendCode));
            BlankIntercepted.Trigger(source.Owner.Player);
        }
        killer.RpcProtectedMurderPlayer(target);
        killer.SetKillCooldownV2();
        Logger.Info($"{killer.GetNameWithRole()} 的空包弹生效，本次击杀失效", "Prosecutors");
        return false;
    }
    public override void OnStartMeeting() => Init();
    public bool CheckWin(ref CustomRoles winnerRole)
    {
        SurvivalWinGranted = Player.IsAlive();
        return SurvivalWinGranted;
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(CanUseKillButton() ? RoleInfo.RoleColor : Color.gray, $"({ProsecutorsLimit})");
    public bool OverrideKillButtonText(out string text)
    {
        text = GetString("ProsecutorsButtonText");
        return true;
    }
    public bool OverrideKillButtonSprite(out string buttonName)
    {
        buttonName = "Blank";
        return true;
    }
}
