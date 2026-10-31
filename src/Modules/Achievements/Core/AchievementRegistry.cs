using System.Reflection;
using TONX.Attributes;

namespace TONX.Modules.Achievements.Core;

public static class AchievementRegistry
{
    private static readonly Dictionary<int, AchievementBase> All = new();
    private static readonly Dictionary<Type, AchievementBase> ByType = new();
    private static readonly Dictionary<AchievementCategory, List<AchievementBase>> ByCategory = new();

    public static IReadOnlyList<AchievementCategory> Categories { get; private set; } = new List<AchievementCategory>();

    [PluginModuleInitializer]
    public static void Initialize()
    {
        All.Clear();
        ByType.Clear();
        ByCategory.Clear();

        var types = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.ContainsGenericParameters
                        && typeof(AchievementBase).IsAssignableFrom(t)
                        && t.GetConstructor(Type.EmptyTypes) != null)
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

        foreach (var definition in ProposalAchievementCatalog.CreateDefinitions())
            if (!All.ContainsKey(definition.Id)) Register(definition);

        Categories = ByCategory.Keys.OrderBy(x => x).ToList().AsReadOnly();
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
        if (achievement == null)
        {
            Logger.Error("Attempted to register a null achievement", "AchievementRegistry");
            return;
        }

        if (achievement.Id <= 0 || achievement.Id > 1_000_000 ||
            achievement.RequiredValue <= 0 || achievement.RequiredValue > 1_000_000 ||
            string.IsNullOrWhiteSpace(achievement.Name) ||
            string.IsNullOrWhiteSpace(achievement.Description) ||
            !Enum.IsDefined(typeof(AchievementCategory), achievement.Category))
        {
            Logger.Error($"Invalid achievement definition: {achievement.GetType().Name}", "AchievementRegistry");
            return;
        }

        if (All.ContainsKey(achievement.Id))
        {
            Logger.Error($"Checked the same id：{achievement.Id}({achievement.Name}), Skip.", "AchievementRegistry");
            return;
        }

        foreach (var key in new[] { achievement.Name, achievement.Description })
        {
            if (!Translator.translateMaps.Values.Any(map =>
                    map.TryGetValue(key, out var text) && !string.IsNullOrWhiteSpace(text)))
                Logger.Warn($"Missing achievement translation: {key}", "AchievementRegistry");
        }

        All[achievement.Id] = achievement;
        ByType[achievement.GetType()] = achievement;
        if (!ByCategory.ContainsKey(achievement.Category)) ByCategory[achievement.Category] = new();
        ByCategory[achievement.Category].Add(achievement);
        Logger.Info($"Registry successfully [{achievement.Id}] nameKey={achievement.Name}", "AchievementRegistry");
    }
}
