using System.Reflection;
using TONX.Attributes;
using TONX.Modules.Achievements.Core.Base;
using TONX.Modules.Achievements.Core.Interfaces;
using TONX.Modules.Achievements.Player;

namespace TONX.Modules.Achievements.Game;

public static class AchievementRegistry
{
    private static readonly Dictionary<int, AchievementBase> All = new();
    private static readonly Dictionary<string, List<AchievementBase>> ByCategory = new();

    public static IReadOnlyList<string> Categories { get; private set; } = new List<string>();

    [PluginModuleInitializer]
    public static void Initialize()
    {
        All.Clear();
        ByCategory.Clear();

        var types = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.ContainsGenericParameters
                        && typeof(AchievementBase).IsAssignableFrom(t))
            .ToArray();

        foreach (var type in types)
            Register((AchievementBase)Activator.CreateInstance(type));
        
        foreach (var type in types.Where(t => typeof(IAchievementTracker).IsAssignableFrom(t)))
        {
            var tracker = (IAchievementTracker)Activator.CreateInstance(type);
            foreach (var achievement in tracker.TrackedAchievements)
                if (achievement is AchievementBase tracked) Register(tracked);
        }

        Categories = ByCategory.Keys.OrderBy(x => x, StringComparer.Ordinal).ToList().AsReadOnly();
        Logger.Info($"{All.Count} achievements in {Categories.Count} categories", "AchievementRegistry");
    }

    public static AchievementBase GetById(int id)
        => All.TryGetValue(id, out var a) ? a : null;

    public static AchievementBase GetByType(Type type)
        => All.Values.FirstOrDefault(a => a.GetType() == type);

    public static T Get<T>() where T : AchievementBase
        => All.Values.OfType<T>().FirstOrDefault();

    public static IReadOnlyCollection<AchievementBase> GetAll()
        => All.Values.ToList().AsReadOnly();

    public static IReadOnlyList<AchievementBase> GetByCategory(string category)
        => ByCategory.TryGetValue(category, out var list) ? list : new List<AchievementBase>();
    
    public static void SyncLocalState(List<AchievementRecord> records)
    {
        if (records == null) return;
        foreach (var record in records) GetById(record.Id)?.ApplyServerRecord(record);
    }

    private static void Register(AchievementBase achievement)
    {
        if (All.ContainsKey(achievement.Id))
        {
            Logger.Error($"Checked the same id：{achievement.Id}({achievement.Name}), Skip.", "AchievementRegistry");
            return;
        }

        All[achievement.Id] = achievement;
        if (!ByCategory.ContainsKey(achievement.Category)) ByCategory[achievement.Category] = new();
        ByCategory[achievement.Category].Add(achievement);
        Logger.Info($"Registry successfully [{achievement.Id}] {achievement.Name}", "AchievementRegistry");
    }
}
