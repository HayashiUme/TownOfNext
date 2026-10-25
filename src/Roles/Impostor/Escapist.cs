using AmongUs.GameOptions;
using Hazel;
using TONX.Modules;
using TONX.Modules.Achievements.Impostor;
using TONX.Roles.Core.Interfaces;
using UnityEngine;

namespace TONX.Roles.Impostor;
public sealed class Escapist : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Escapist),
            player => new Escapist(player),
            CustomRoles.Escapist,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            2000,
            null,
            "ec|逃逸"
        );
    public Escapist(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Marked = false;
    }

    private bool Shapeshifting;
    private bool Marked;
    private Vector2 MarkedPosition;
    private float lastTeleport = float.NegativeInfinity;
    public bool KilledAfterTeleport { get; private set; }
    public override void OnStartMeeting() => lastTeleport = float.NegativeInfinity;
    public void OnMurderPlayerAsKiller(MurderInfo info)
    {
        if (!info.IsSuicide && info.AttemptTarget.Data.IsDead && Time.time - lastTeleport <= 10f)
            KilledAfterTeleport = true;
    }
    public override void Add()
    {
        Marked = false;
        Shapeshifting = false;
    }
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(Marked);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        
        Marked = reader.ReadBoolean();
    }
    public override bool GetAbilityButtonText(out string text)
    {
        text = Marked ? GetString("EscapistTeleportButtonText") : GetString("EscapistMarkButtonText");
        return !Shapeshifting;
    }
    public override void OnShapeshift(PlayerControl target)
    {
        Shapeshifting = !Is(target);

        if (!AmongUsClient.Instance.AmHost) return;

        if (!Shapeshifting) return;

        if (Marked)
        {
            Marked = false;
            var distance = Vector2.Distance(Player.GetTruePosition(), MarkedPosition);
            Player.RPCPlayCustomSound("Teleport");
            Utils.TP(Player.NetTransform, MarkedPosition);
            lastTeleport = Time.time;
            if (distance >= 8f) EscapistReturned.Trigger(Player);
            Logger.Msg($"{Player.GetNameWithRole()}：{MarkedPosition}", "Escapist.OnShapeshift");
        }
        else
        {
            MarkedPosition = Player.GetTruePosition();
            Marked = true;
            SendRPC();
        }
    }
}
