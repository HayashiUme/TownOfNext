using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using TONX.Attributes;

namespace TONX.Modules;

public static class AmciRegister
{
    private static readonly ManualLogSource _logger = BepInEx.Logging.Logger.CreateLogSource("FilterAPI");
    private static readonly Dictionary<string, Guid> _guidByModId = new();
    private static bool _subscribed;
    public static IReadOnlyDictionary<string, Guid> Registered => _guidByModId;
    public static Guid? Primary { get; private set; }
    public static bool IsEnabled
    {
        get
        {
            var entry = Main.EnableAMCIMode;
            return entry != null && entry.Value && Primary != null;
        }
    }
    
    public static void Apply()
    {
        EnsureSubscribed();

        var primary = Primary;
        CurrentModRegistration.ModRegistrationGuidString = IsEnabled && primary != null ? primary.Value.ToString() : string.Empty;
    }

    internal static void Initialize()
    {
        foreach (var pluginInfo in IL2CPPChainloader.Instance.Plugins.Values)
        {
            if (pluginInfo.Instance != null)
            {
                Register(pluginInfo, (BasePlugin) pluginInfo.Instance);
            }
        }
        IL2CPPChainloader.Instance.PluginLoad += (pluginInfo, _, plugin) => Register(pluginInfo, plugin);

        IL2CPPChainloader.Instance.Finished += () =>
        {
            RefreshPrimary();
            Apply();
            _logger.LogInfo($"AMCI: registered={_guidByModId.Count} primary={Primary} guidString={CurrentModRegistration.ModRegistrationGuidString}");
        };
    }

    public static void Register(PluginInfo pluginInfo, BasePlugin plugin)
    {
        var guid = AmciModGuidAttribute.GetGuid(plugin.GetType());
        if (guid.HasValue)
        {
            _guidByModId[pluginInfo.Metadata.GUID] = guid.Value;
        }
    }

    public static void RefreshPrimary()
    {
        Primary = _guidByModId.Count == 0 ? null : _guidByModId.OrderBy(kv => kv.Key, StringComparer.Ordinal).First().Value;
    }

    public static void EnsureSubscribed()
    {
        if (_subscribed)
        {
            return;
        }

        var entry = Main.EnableAMCIMode;
        if (entry == null)
        {
            return;
        }

        _subscribed = true;
        entry.SettingChanged += (_, _) => Apply();
    }
}