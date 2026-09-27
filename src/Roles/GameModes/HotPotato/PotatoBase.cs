using AmongUs.GameOptions;
using Hazel;
using static TONX.GameModes.HotPotato;

namespace TONX.Roles.GameMode;

public abstract class PotatoBase : RoleBase
{
    private enum RpcType
    {
        SyncExplosionTime,
    }

    protected PotatoBase(SimpleRoleInfo roleInfo, PlayerControl player, Func<HasTask> hasTasks = null)
        : base(roleInfo, player, hasTasks)
    {
        CustomRoleManager.MarkOthers.Add(GetPotatoMark);
    }

    private static string GetPotatoMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        return seen.Is(CustomRoles.HotPotato) ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.HotPotato), "●") : "";
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        Main.AllPlayerSpeed[Player.PlayerId] = ModeSpeed;
        opt.SetVision(true);
        opt.SetFloat(FloatOptionNames.CrewLightMod, 1f);
        opt.SetFloat(FloatOptionNames.ImpostorLightMod, 1f);
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (isForMeeting || seer.PlayerId != seen.PlayerId) return "";

        return string.Format(GetString("HotPotatoTimeRemain"), RemainExplosionTime);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        switch ((RpcType)reader.ReadByte())
        {
            case RpcType.SyncExplosionTime:
                RemainExplosionTime = reader.ReadInt32();
                break;
        }
    }

    public static void SyncExplosionTime(int time)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (PlayerControl.LocalPlayer.GetRoleClass() is not PotatoBase potato) return;

        using var sender = potato.CreateSender();
        sender.Writer.Write((byte)RpcType.SyncExplosionTime);
        sender.Writer.Write(time);
    }
}
