using UnityEngine;

namespace TONX.Modules.Achievements.Core.Interfaces;

public interface IAchievement
{
    int Id { get; }
    string Name { get; }
    string Description { get; }
    Color TitleColor { get; }
    string TitleColorHex { get; }
    Sprite Icon { get; }
    AchievementRarity Rarity { get; }
    bool Hidden { get; }
    string Category { get; }
    int RequiredValue { get; }
}
