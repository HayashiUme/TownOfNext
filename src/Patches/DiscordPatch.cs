#if Windows
using System;
using AmongUs.Data;
using Discord;
using InnerNet;

namespace TONX.Patches;

public static class DiscordRPC
{
    private static string _lobbyCode = "";
    private static string _region = "";

    [HarmonyPatch(typeof(DiscordManager), nameof(DiscordManager.SetInMenus))]
    public static class SetInMenusPatch
    {
        public static bool Prefix(DiscordManager __instance)
        {
            if (__instance.presence == null) return false;
            try
            {
                _lobbyCode = "";
                _region = "";
                __instance.ClearPresence();
                var activity = new Activity
                {
                    State = "In Menus",
                    Details = $"TONX v{Main.PluginVersion}",
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
                _lobbyCode = "";
                _region = "";

                var activity = new Activity
                {
                    State = "In Game",
                    Details = $"TONX v{Main.PluginVersion}",
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
                    Details = $"TONX v{Main.PluginVersion}",
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

            // 不是大厅状态就不处理，防止游戏中被覆盖
            if (AmongUsClient.Instance == null || AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Joined)
                return false;

            try
            {
                string id = GameCode.IntToGameName(gameId);
                _lobbyCode = id;
                _region = ServerManager.Instance?.CurrentRegion?.Name ?? "";

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

            // 不是大厅状态就不处理，防止游戏中被覆盖
            if (AmongUsClient.Instance == null || AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Joined)
                return false;

            try
            {
                string text = GameCode.IntToGameName(gameId);
                _lobbyCode = text;
                _region = ServerManager.Instance?.CurrentRegion?.Name ?? "";

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

    private static string GetDetails()
    {
        var details = $"TONX v{Main.PluginVersion}";
#if DEBUG
        details += $" {Main.GitBranch}({Main.GitCommit})";
#endif
        try
        {
            if (!DataManager.Settings.Gameplay.StreamerMode && !string.IsNullOrEmpty(_lobbyCode) && !string.IsNullOrEmpty(_region))
                details = $"TONX - {_lobbyCode} ({_region})";
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
            if (AmongUsClient.Instance == null || AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Joined) return;

            var manager = DestroyableSingleton<DiscordManager>.Instance;
            if (AmongUsClient.Instance.AmHost && AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame)
                manager.SetInLobbyHost(currentPlayers, maxPlayers, gameId);
            else
                manager.SetInLobbyClient(currentPlayers, maxPlayers, gameId);
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
