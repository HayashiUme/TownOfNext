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
    
    public static void RpcSetSkin(PlayerControl seer)
    {
        if (!AmongUsClient.Instance.AmHost || seer == null) return;
        int colorblindClientId = seer.GetClientId();
        if (colorblindClientId < 0) return;

        foreach (var pc in Main.AllPlayerControls)
        {
            if (pc.PlayerId == seer.PlayerId || pc.Data == null || pc.NetId == 0) continue;
            var outfit = pc.Data.DefaultOutfit;
            if (outfit == null) continue;

            int wrongColor = (outfit.ColorId + 1) % Palette.PlayerColors.Length;
            if (wrongColor < 0 || wrongColor >= Palette.PlayerColors.Length) continue;

            var sender = CustomRpcSender.Create(name: $"Colorblind.RpcSetSkin({seer.Data.PlayerName}→{pc.Data.PlayerName})");

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
    }
}
