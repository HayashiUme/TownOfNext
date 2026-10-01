using UnityEngine;

namespace TONX.Modules.Achievements.Core;

public enum AchievementRarity
{
    Common,
    Rare,
    Epic,
    Legendary,
}

public static class AchievementRarityExtensions
{
    public static Color GetColor(this AchievementRarity rarity) => rarity switch
    {
        AchievementRarity.Rare => new Color32(0x60, 0xA5, 0xFA, byte.MaxValue),
        AchievementRarity.Epic => new Color32(0xA7, 0x8B, 0xFA, byte.MaxValue),
        AchievementRarity.Legendary => new Color32(0xFB, 0xBF, 0x24, byte.MaxValue),
        _ => new Color32(0xC9, 0xC9, 0xD4, byte.MaxValue),
    };

    public static string GetColorCode(this AchievementRarity rarity)
        => $"#{ColorUtility.ToHtmlStringRGB(rarity.GetColor())}";

    public static string GetDisplayName(this AchievementRarity rarity)
        => GetString(rarity switch
        {
            AchievementRarity.Rare => "Achievement.Rarity.Rare",
            AchievementRarity.Epic => "Achievement.Rarity.Epic",
            AchievementRarity.Legendary => "Achievement.Rarity.Legendary",
            _ => "Achievement.Rarity.Common",
        });
}
