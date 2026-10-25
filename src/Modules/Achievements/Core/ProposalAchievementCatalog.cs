using System.Reflection;
using System.Text.Json;
using UnityEngine;

namespace TONX.Modules.Achievements.Core;

/// <summary>Display metadata for the proposal. Only concrete achievement classes can award progress.</summary>
public static class ProposalAchievementCatalog
{
    public sealed class Entry
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Rarity { get; set; }
        public bool Hidden { get; set; }
        public string Category { get; set; }
        public string Role { get; set; }
    }

    public static IEnumerable<AchievementBase> CreateDefinitions()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var name = assembly.GetManifestResourceNames().Single(x => x.EndsWith("Achievements.ProposalCatalog.json", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(name);
        var entries = JsonSerializer.Deserialize<Entry[]>(stream);
        if (entries == null || entries.Length != 292 || entries.Select(x => x.Id).Distinct().Count() != 292)
            throw new InvalidOperationException("Invalid reviewed achievement catalog");
        return entries.Select(x => (AchievementBase)new ProposalAchievement(x)).ToArray();
    }
}

public sealed class ProposalAchievement : AchievementBase
{
    private readonly ProposalAchievementCatalog.Entry entry;
    public ProposalAchievement(ProposalAchievementCatalog.Entry entry) => this.entry = entry;
    public override int Id => entry.Id;
    public override Color TitleColor => entry.Category switch
    {
        "Impostor" => new Color32(255, 25, 25, 255),
        "Crewmate" => new Color32(80, 190, 255, 255),
        "Neutral" => new Color32(180, 130, 255, 255),
        _ => new Color32(255, 200, 80, 255),
    };
    public override string Category => entry.Category;
    public override AchievementRarity Rarity => Enum.Parse<AchievementRarity>(entry.Rarity);
    public override bool Hidden => entry.Hidden;
}
