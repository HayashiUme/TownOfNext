using System.Reflection;
using TONX.Attributes;

namespace TONX.Modules.Achievements.Core;

public static class AchievementRegistry
{
    private static readonly Dictionary<int, AchievementBase> All = new();
    private static readonly Dictionary<Type, AchievementBase> ByType = new();
    private static readonly Dictionary<string, List<AchievementBase>> ByCategory = new();

    public static IReadOnlyList<string> Categories { get; private set; } = new List<string>();

    [PluginModuleInitializer]
    public static void Initialize()
    {
        All.Clear();
        ByType.Clear();
        ByCategory.Clear();

        var types = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.ContainsGenericParameters
                        && typeof(AchievementBase).IsAssignableFrom(t))
            .ToArray();

        foreach (var type in types)
        {
            try
            {
                Register((AchievementBase)Activator.CreateInstance(type));
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to register {type.Name}：{ex.Message}", "AchievementRegistry");
            }
        }

        Categories = ByCategory.Keys.OrderBy(x => x, StringComparer.Ordinal).ToList().AsReadOnly();
        Logger.Info($"{All.Count} achievements in {Categories.Count} categories", "AchievementRegistry");
    }

    public static AchievementBase GetById(int id)
        => All.TryGetValue(id, out var a) ? a : null;

    public static AchievementBase GetByType(Type type)
        => ByType.TryGetValue(type, out var a) ? a : null;

    public static T Get<T>() where T : AchievementBase
        => GetByType(typeof(T)) as T;

    public static IReadOnlyCollection<AchievementBase> GetAll()
        => All.Values;

    private static void Register(AchievementBase achievement)
    {
        if (All.ContainsKey(achievement.Id))
        {
            Logger.Error($"Checked the same id：{achievement.Id}({achievement.Name}), Skip.", "AchievementRegistry");
            return;
        }

        All[achievement.Id] = achievement;
        ByType[achievement.GetType()] = achievement;
        if (!ByCategory.ContainsKey(achievement.Category)) ByCategory[achievement.Category] = new();
        ByCategory[achievement.Category].Add(achievement);
        Logger.Info($"Registry successfully [{achievement.Id}] {achievement.Name}", "AchievementRegistry");
    }
}
