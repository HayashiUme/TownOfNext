using TONX.Attributes;
using UnityEngine;
using static TONX.Options;

namespace TONX.Roles.AddOns.Common;

public static class Colorblind
{
    private static readonly int Id = 82200;
    private static List<byte> playerIdList = new();

    public static void SetupCustomOption()
    {
        SetupAddonOptions(Id, TabGroup.Addons, CustomRoles.Colorblind);
        AddOnsAssignData.Create(Id + 10, CustomRoles.Colorblind, true, true, true);
    }
    [GameModuleInitializer]
    public static void Init()
    {
        playerIdList = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
    public static bool IsLocalPlayer => PlayerControl.LocalPlayer != null && IsThisRole(PlayerControl.LocalPlayer.PlayerId);

    public static int GetShiftedColorId(int colorId)
    {
        int length = Palette.PlayerColors.Length;
        if (length <= 0) return colorId;
        int shifted = (colorId + 1) % length;
        return shifted < 0 ? colorId : shifted;
    }

    public static void ApplyLocalVisuals()
    {
        var seer = PlayerControl.LocalPlayer;
        if (seer == null || !IsThisRole(seer.PlayerId) || Camouflage.IsCamouflage) return;

        foreach (var pc in Main.AllPlayerControls)
        {
            if (pc == null || pc.PlayerId == seer.PlayerId || pc.Data == null || pc.cosmetics == null) continue;
            var outfit = pc.Data.DefaultOutfit;
            if (outfit == null) continue;

            int wrongColor = GetShiftedColorId(outfit.ColorId);
            pc.cosmetics.SetColor(wrongColor);
        }

        ApplyMeetingVisuals();
    }

    public static void ApplyMeetingVisuals()
    {
        if (!IsLocalPlayer || MeetingHud.Instance == null) return;

        foreach (var pva in MeetingHud.Instance.playerStates)
        {
            if (pva == null || pva.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
            var pc = Utils.GetPlayerById(pva.PlayerId);
            if (pc?.Data?.DefaultOutfit == null || pva.PlayerIcon?.cosmetics == null) continue;
            pva.PlayerIcon.cosmetics.SetColor(GetShiftedColorId(pc.Data.DefaultOutfit.ColorId));
        }
    }

    public static void RpcSetSkin(PlayerControl seer)
    {
        if (seer == null) return;
        if (seer.AmOwner) ApplyLocalVisuals();
        if (!AmongUsClient.Instance.AmHost) return;
        if (seer.AmOwner || seer.IsModClient()) return;

        int colorblindClientId = seer.GetClientId();
        if (colorblindClientId < 0) return;

        foreach (var pc in Main.AllPlayerControls)
        {
            if (pc.PlayerId == seer.PlayerId || pc.Data == null || pc.NetId == 0) continue;
            var outfit = pc.Data.DefaultOutfit;
            if (outfit == null) continue;

            int wrongColor = GetShiftedColorId(outfit.ColorId);
            if (wrongColor < 0 || wrongColor >= Palette.PlayerColors.Length) continue;

            var sender = CustomRpcSender.Create(name: "Colorblind.RpcSetSkin(" + seer.Data.PlayerName + "->" + pc.Data.PlayerName + ")");

            sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetColor, colorblindClientId)
                .Write(pc.Data.NetId)
                .Write(wrongColor)
                .EndRpc();

            sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetHatStr, colorblindClientId)
                .Write(outfit.HatId)
                .Write(pc.GetNextRpcSequenceId(RpcCalls.SetHatStr))
                .EndRpc();

            sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetSkinStr, colorblindClientId)
                .Write(outfit.SkinId)
                .Write(pc.GetNextRpcSequenceId(RpcCalls.SetSkinStr))
                .EndRpc();

            sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetVisorStr, colorblindClientId)
                .Write(outfit.VisorId)
                .Write(pc.GetNextRpcSequenceId(RpcCalls.SetVisorStr))
                .EndRpc();

            sender.SendMessage();
        }
    }
    
    public static void RpcSetSkinAll()
    {
        foreach (var pc in Main.AllPlayerControls)
            if (IsThisRole(pc.PlayerId))
                RpcSetSkin(pc);
        ApplyLocalVisuals();
    }
}
