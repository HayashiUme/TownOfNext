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
    
    public static string GetPerceivedColorCode(PlayerControl target)
    {
        var colorId = target?.Data?.DefaultOutfit?.ColorId ?? -1;
        if (colorId < 0 || colorId >= Palette.PlayerColors.Length) return "";
        var wrongColor = Palette.PlayerColors[(colorId + 1) % Palette.PlayerColors.Length];
        return ColorUtility.ToHtmlStringRGB(wrongColor);
    }
}
