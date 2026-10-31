global using TONX;
global using static TONX.Translator;

namespace UnityEngine
{
    public class Sprite { }
    public struct Color
    {
        public Color(float r, float g, float b, float a) { }
    }
    public struct Color32
    {
        public Color32(byte r, byte g, byte b, byte a) { }
        public static implicit operator Color(Color32 value) => new();
    }
}

namespace TONX.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class PluginModuleInitializerAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class GameModuleInitializerAttribute : Attribute { }
}

namespace TONX
{
    public class PlayerControl
    {
        public static PlayerControl LocalPlayer;
        public bool AmOwner;
        public byte PlayerId;
        public string FriendCode;
        public PlayerData Data = new();
        public CustomRoles Role;
        public object RoleClass;
        public CountTypes Team = CountTypes.Crew;
        public HashSet<CustomRoles> SubRoles = new();
        public CustomRoles GetCustomRole() => Role;
        public object GetRoleClass() => RoleClass;
        public CountTypes GetCountTypes() => Team;
        public bool Is(CustomRoles role) => Role == role || SubRoles.Contains(role);
        public bool IsAlive() => !Data.IsDead && !Data.Disconnected;
    }
    public class PlayerData
    {
        public bool Disconnected;
        public bool IsDead;
    }
    public class AmongUsClient
    {
        public static AmongUsClient Instance = new();
        public bool AmHost = true;
    }
    public static class GameStates
    {
        public static bool IsInGame = true;
        public static bool IsFreePlay;
    }
    public static class Main
    {
        public static List<PlayerControl> AllPlayerControls = new();
    }
    public static class NameTagManager
    {
        public interface INameTagProvider
        {
            string GetPrefix(PlayerControl player);
        }
    }
    public enum CustomWinner { Default = -2, Draw = -1, Error = -4, None = -3,
        Crewmate = CustomRoles.Crewmate, Impostor = CustomRoles.Impostor, Pelican = CustomRoles.Pelican,
        Mario = CustomRoles.Mario, Arsonist = CustomRoles.Arsonist, Jester = CustomRoles.Jester,
        Terrorist = CustomRoles.Terrorist, Executioner = CustomRoles.Executioner, Workaholic = CustomRoles.Workaholic }
    public enum CountTypes { None = -1, Crew = CustomWinner.Crewmate, Impostor = CustomWinner.Impostor, Pelican = CustomWinner.Pelican }
    public enum CustomRoles { Crewmate, Impostor, KillingMachine, Mare, Bard, Madmate, Charmed, Prosecutors, Lawyer, Opportunist, Follower, Sunnyboy,
        Miner, Escapist, SoulCatcher, SchrodingerCat, Shifter, BountyHunter, Vampire, Warlock, Assassin, Sniper,
        Pelican, Mario, Arsonist, Jester, Terrorist, Executioner, Workaholic, NotAssigned }
    public static class CustomWinnerHolder
    {
        public static CustomWinner WinnerTeam = CustomWinner.Crewmate;
        public static HashSet<byte> WinnerIds = new();
        public static HashSet<CustomRoles> WinnerRoles = new();
        public static HashSet<CustomRoles> AdditionalWinnerRoles = new();
    }
    public static class Utils
    {
        public static string ColorToHex(UnityEngine.Color color) => "#FFFFFF";
        public static UnityEngine.Sprite LoadSprite(string path, float scale) => new();
    }
    public static class Translator
    {
        public static Dictionary<int, Dictionary<string, string>> translateMaps = new();
        public static int Language;
        public static string GetString(string key) => translateMaps[Language][key];
    }
    public static class Logger
    {
        public static void Info(string text, string tag) { }
        public static void Warn(string text, string tag) => throw new Exception(text);
        public static void Error(string text, string tag) => throw new Exception(text);
    }
}

namespace TONX.Roles.Neutral
{
    public sealed class Jester
    {
        public bool ExileWinTriggered { get; set; }
        public bool WonInFirstMeeting { get; set; }
    }
    public sealed class Lawyer
    {
        public bool ClientWinGranted { get; set; }
        public bool ClientWinRemoved { get; set; }
        public byte TargetId { get; set; }
        public string AssignedClientCode { get; set; }
        public string WinningClientCode { get; set; }
    }
    public sealed class Prosecutors
    {
        public bool SurvivalWinGranted { get; set; }
        public int InterceptedKillers { get; set; }
    }
    public sealed class Pelican { public int SuccessfulEats { get; set; } }
    public sealed class Mario { public bool VentWinTriggered { get; set; } }
}

namespace TONX.Roles.Impostor
{
    public sealed class BountyHunter { public int SuccessfulBounties { get; set; } }
    public sealed class Vampire { public int SuccessfulBites { get; set; } }
    public sealed class Warlock { public int SuccessfulCurseKills { get; set; } }
    public sealed class Assassin { public int SuccessfulAssassinations { get; set; } }
    public sealed class Miner { public int SuccessfulTeleports { get; set; } public bool KilledAfterTeleport { get; set; } }
    public sealed class Sniper { public int SuccessfulSnipes { get; set; } }
    public sealed class Mare { public int LightsOutKills { get; set; } }
    public sealed class Cleaner { public int CleanedBodies { get; set; } }
}

namespace TONX.Roles.Crewmate
{
    public sealed class SpeedBooster { public int SuccessfulBoosts { get; set; } }
    public sealed class Transporter { public int SuccessfulTransports { get; set; } }
}

namespace TONX.Modules.Achievements.Core
{
    public static class AchievementTitleService
    {
        public static int GetWearedTitleId(string friendCode)
            => CustomAchievementDataManager.Get(friendCode)?.WearedTitleId ?? 0;
    }
    public enum AchievementRarity { Common, Rare, Epic, Legendary }
    public static class AchievementManager
    {
        public static void TryUpdateUnit(string friendCode, int id) => AchievementProgressService.RecordUnit(friendCode, id);
    }
    public static class AchievementApiClient
    {
        public static Task<bool> TriggerUnlockAsync(string code, AchievementRecord record) => Task.FromResult(true);
        public static Task<bool> ReportProgressAsync(string code, AchievementRecord record) => Task.FromResult(true);
    }
}
