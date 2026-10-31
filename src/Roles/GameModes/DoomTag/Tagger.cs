using AmongUs.GameOptions;
using Hazel;
using TONX.Roles.Core.Interfaces;
using static TONX.GameModes.DoomTag;

namespace TONX.Roles.GameMode;

public sealed class Tagger : RoleBase, IKiller
{
    private enum RpcType
    {
        SyncState,
    }

    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Tagger),
            player => new Tagger(player),
            CustomRoles.Tagger,
            () => RoleTypes.Impostor,
            CustomRoleTypes.GameMode,
            100006,
            null,
            "dt|猎人",
            "#D9BAA5",
            true,
            Hidden: new HiddenRoleInfo(0, null)
        );

    public Tagger(PlayerControl player)
        : base(RoleInfo, player, () => HasTask.False)
    { }

    public bool CanUseKillButton() => true;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;
    public float CalculateKillCooldown() => FrenzyMode ? FrenzyKillCooldown.GetFloat() : BaseKillCooldown.GetFloat();

    public bool OnCheckMurderAsKiller(MurderInfo info) => OnCheckMurder(info);

    public override void ApplyGameOptions(IGameOptions opt)
    {
        Main.AllPlayerSpeed[Player.PlayerId] = DefaultSpeed * (FrenzyMode ? FrenzySpeedMultiplier.GetFloat() : 1f);

        if (!IsLowerVision(Player.PlayerId)) return;

        opt.SetVision(false);
        opt.SetFloat(FloatOptionNames.CrewLightMod, LowerVisionMultiplier.GetFloat());
        opt.SetFloat(FloatOptionNames.ImpostorLightMod, LowerVisionMultiplier.GetFloat());
    }
    
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (!FrenzyMode || isForMeeting || seer.PlayerId != seen.PlayerId) return "";

        return $"<color=#ff6600>{GetString("DoomTag.FrenzyModeHUD")}</color>";
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (isForMeeting || seer.PlayerId != seen.PlayerId || !seer.IsAlive()) return "";

        var targetId = GetTarget(seer.PlayerId);
        if (targetId == byte.MaxValue) return "";

        return $"{GetString("DoomTag.CurrentTarget")}: <b>{Utils.ColorString(Main.PlayerColors[targetId], Main.AllPlayerNames[targetId])}</b>";
    }

    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (isForMeeting || !ShowTargetArrow.GetBool() || seer.PlayerId != seen.PlayerId || !seer.IsAlive()) return "";

        var targetId = GetTarget(seer.PlayerId);
        return targetId == byte.MaxValue ? "" : TargetArrow.GetArrows(seer, targetId);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        switch ((RpcType)reader.ReadByte())
        {
            case RpcType.SyncState:
                FrenzyMode = reader.ReadBoolean();
                TargetMap.Clear();
                var count = reader.ReadInt32();
                for (var i = 0; i < count; i++)
                    TargetMap[reader.ReadByte()] = reader.ReadByte();
                RefreshArrows();
                break;
        }
    }

    public static void SyncState()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (PlayerControl.LocalPlayer.GetRoleClass() is not Tagger tagger) return;

        using var sender = tagger.CreateSender();
        sender.Writer.Write((byte)RpcType.SyncState);
        sender.Writer.Write(FrenzyMode);
        sender.Writer.Write(TargetMap.Count);
        foreach (var (hunterId, targetId) in TargetMap)
        {
            sender.Writer.Write(hunterId);
            sender.Writer.Write(targetId);
        }
    }
}
