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

    /// <summary>
    /// 色盲玩家看到其他玩家的颜色：所有颜色都偏移一位（红→蓝→绿…），保证与真实颜色不同
    /// </summary>
    public static string GetPerceivedColorCode(PlayerControl target)
    {
        var colorId = target?.Data?.DefaultOutfit?.ColorId ?? -1;
        if (colorId < 0 || colorId >= Palette.PlayerColors.Length) return "";
        var wrongColor = Palette.PlayerColors[(colorId + 1) % Palette.PlayerColors.Length];
        return ColorUtility.ToHtmlStringRGB(wrongColor);
    }
}
