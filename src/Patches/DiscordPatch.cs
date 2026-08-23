#if Windows
using System;
using AmongUs.Data;
using Discord;
using InnerNet;

namespace TONX.Patches;

public static class DiscordRPC
{
    private static string Lobbycode = "";
    private static string Region = "";

    [HarmonyPatch(typeof(DiscordManager), nameof(DiscordManager.SetInMenus))]
    public static class SetInMenusPatch
    {
        public static bool Prefix(DiscordManager __instance)
        {
            if (__instance.presence == null) return false;
            try
            {
                __instance.ClearPresence();
                var activity = new Activity
                {
                    State = "In Menus",
                    Details = GetDetails(),
                    Assets = new ActivityAssets
                    {
                        LargeImage = "https://i.imgur.com/947p8jb.png"
                    }
                };
                __instance.presence.GetActivityManager().UpdateActivity(activity, new Action<Result>(_ => { }));
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error("Error in SetInMenus Discord RPC", "DiscordPatch");
                Logger.Exception(ex, "DiscordPatch");
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(DiscordManager), nameof(DiscordManager.SetPlayingGame))]
    public static class SetPlayingGamePatch
    {
        public static bool Prefix(DiscordManager __instance)
        {
            if (__instance.presence == null) return false;
            try
            {
                if (__instance.StartTime == null)
                {
                    __instance.StartTime = new Il2CppSystem.Nullable<Il2CppSystem.DateTime>(Il2CppSystem.DateTime.UtcNow);
                }

                var activity = new Activity
                {
                    State = "In Game",
                    Details = GetDetails(),
                    Assets = new ActivityAssets
                    {
                        LargeImage = "https://i.imgur.com/947p8jb.png"
                    }
                };
                if (__instance.StartTime.hasValue)
                {
                    var timestamps = activity.Timestamps;
                    timestamps.Start = DiscordManager.ToUnixTime(__instance.StartTime.value);
                    activity.Timestamps = timestamps;
                }
                __instance.presence.GetActivityManager().UpdateActivity(activity, new Action<Result>(_ => { }));
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error("Error in SetPlayingGame Discord RPC", "DiscordPatch");
                Logger.Exception(ex, "DiscordPatch");
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(DiscordManager), nameof(DiscordManager.SetHowToPlay))]
    public static class SetHowToPlayPatch
    {
        public static bool Prefix(DiscordManager __instance)
        {
            if (__instance.presence == null) return false;
            try
            {
                __instance.ClearPresence();
                var activity = new Activity
                {
                    State = "In Freeplay",
                    Details = GetDetails(),
                    Assets = new ActivityAssets
                    {
                        LargeImage = "https://i.imgur.com/947p8jb.png"
                    }
                };
                __instance.presence.GetActivityManager().UpdateActivity(activity, new Action<Result>(_ => { }));
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error("Error in SetHowToPlay Discord RPC", "DiscordPatch");
                Logger.Exception(ex, "DiscordPatch");
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(DiscordManager), nameof(DiscordManager.SetInLobbyClient))]
    public static class SetInLobbyClientPatch
    {
        public static bool Prefix(DiscordManager __instance, int numPlayers, int maxPlayers, int gameId)
        {
            if (__instance.presence == null) return false;
            try
            {
                if (__instance.StartTime == null)
                {
                    __instance.StartTime = new Il2CppSystem.Nullable<Il2CppSystem.DateTime>(Il2CppSystem.DateTime.UtcNow);
                }

                string id = GameCode.IntToGameName(gameId);
                __instance.ClearPresence();
                var activity = new Activity
                {
                    State = "In Lobby",
                    Details = GetDetails(),
                    Assets = new ActivityAssets
                    {
                        LargeImage = "https://i.imgur.com/947p8jb.png"
                    }
                };
                if (__instance.StartTime.hasValue)
                {
                    var timestamps = activity.Timestamps;
                    timestamps.Start = DiscordManager.ToUnixTime(__instance.StartTime.value);
                    activity.Timestamps = timestamps;
                }
                var party = activity.Party;
                var size = party.Size;
                size.CurrentSize = numPlayers;
                size.MaxSize = maxPlayers;
                party.Size = size;
                party.Id = id;
                activity.Party = party;
                __instance.presence.GetActivityManager().UpdateActivity(activity, new Action<Result>(_ => { }));
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error("Error in SetInLobbyClient Discord RPC", "DiscordPatch");
                Logger.Exception(ex, "DiscordPatch");
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(DiscordManager), nameof(DiscordManager.SetInLobbyHost))]
    public static class SetInLobbyHostPatch
    {
        public static bool Prefix(DiscordManager __instance, int numPlayers, int maxPlayers, int gameId)
        {
            if (__instance.presence == null) return false;
            try
            {
                if (__instance.StartTime == null)
                {
                    __instance.StartTime = new Il2CppSystem.Nullable<Il2CppSystem.DateTime>(Il2CppSystem.DateTime.UtcNow);
                }

                string text = GameCode.IntToGameName(gameId);
                var activity = new Activity
                {
                    State = "In Lobby",
                    Details = GetDetails(),
                    Assets = new ActivityAssets
                    {
                        LargeImage = "https://i.imgur.com/947p8jb.png",
                        LargeText = "Ask to play!"
                    }
                };
                var party = activity.Party;
                var size = party.Size;
                size.CurrentSize = numPlayers;
                size.MaxSize = maxPlayers;
                party.Size = size;
                party.Id = text;
                activity.Party = party;

                var secrets = activity.Secrets;
                secrets.Join = "join" + DiscordManager.ReverseString(text);
                secrets.Match = "match" + DiscordManager.ReverseString(text);
                activity.Secrets = secrets;

                activity.SupportedPlatforms = 7U;
                __instance.presence.GetActivityManager().UpdateActivity(activity, new Action<Result>(_ => { }));
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error("Error in SetInLobbyHost Discord RPC", "DiscordPatch");
                Logger.Exception(ex, "DiscordPatch");
                return true;
            }
        }
    }

    public static string GetDetails()
    {
        var details = $"TONX v{Main.PluginVersion}";
#if DEBUG
        details += $" {Main.GitBranch}({Main.GitCommit})";
#endif
        try
        {
            if (DataManager.Settings != null && DataManager.Settings.Gameplay != null && !DataManager.Settings.Gameplay.StreamerMode)
            {
                if (GameStates.IsLobby && GameStartManager.Instance?.GameRoomNameCode != null)
                {
                    Lobbycode = GameStartManager.Instance.GameRoomNameCode.text;
                    Region = ServerManager.Instance?.CurrentRegion?.Name ?? "";
                }

                if (!string.IsNullOrEmpty(Lobbycode) && !string.IsNullOrEmpty(Region))
                {
                    details = $"TONX - {Lobbycode} ({Region})";
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Error in getting discord rpc details", "DiscordPatch");
            Logger.Exception(ex, "DiscordPatch");
        }

        return details;
    }

    public static void UpdateLobbyPresence(int currentPlayers, int maxPlayers, int gameId)
    {
        try
        {
            if (!DestroyableSingleton<DiscordManager>.InstanceExists) return;

            var manager = DestroyableSingleton<DiscordManager>.Instance;
            if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost && AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame)
            {
                manager.SetInLobbyHost(currentPlayers, maxPlayers, gameId);
            }
            else if (AmongUsClient.Instance != null)
            {
                manager.SetInLobbyClient(currentPlayers, maxPlayers, gameId);
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Error in updating lobby presence", "DiscordPatch");
            Logger.Exception(ex, "DiscordPatch");
        }
    }
}
#else
namespace TONX.Patches;

public static class DiscordRPC
{
    public static void UpdateLobbyPresence(int currentPlayers, int maxPlayers, int gameId) { }
}
#endif





