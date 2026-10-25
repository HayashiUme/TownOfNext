using TONX.Attributes;

namespace TONX.Modules.Achievements.Core;

/// <summary>Successful role events are provisional until the host settles a normal game.</summary>
public static class RoleAchievementState
{
    private static readonly Dictionary<byte, (string FriendCode, HashSet<int> Ids)> Pending = new();
    private static bool Active;

    [GameModuleInitializer]
    public static void StartRound()
    {
        Discard();
        Active = AmongUsClient.Instance?.AmHost == true && !GameStates.IsFreePlay;
    }

    public static void Discard()
    {
        Pending.Clear();
        Active = false;
    }

    public static void Record<T>(PlayerControl player) where T : SingleUnlockAchievement<T>
    {
        if (!Active || AmongUsClient.Instance?.AmHost != true || !GameStates.IsInGame || GameStates.IsFreePlay)
            return;
        if (player == null || player.Data == null || player.Data.Disconnected || string.IsNullOrEmpty(player.FriendCode))
            return;

        var definition = AchievementRegistry.Get<T>();
        if (definition == null) return;

        if (!Pending.TryGetValue(player.PlayerId, out var entry) || entry.FriendCode != player.FriendCode)
            entry = (player.FriendCode, new HashSet<int>());
        entry.Ids.Add(definition.Id);
        Pending[player.PlayerId] = entry;
    }

    public static void Commit()
    {
        if (!Active || AmongUsClient.Instance?.AmHost != true || !GameStates.IsInGame) return;

        var pending = Pending.ToArray();
        Discard(); // Settlement can be called only once per round, including aborted rounds.
        if (GameStates.IsFreePlay || CustomWinnerHolder.WinnerTeam is CustomWinner.Draw or CustomWinner.Error or CustomWinner.Default)
            return;

        foreach (var (playerId, entry) in pending)
        {
            var player = Main.AllPlayerControls.FirstOrDefault(x => x != null && x.PlayerId == playerId);
            if (player?.Data == null || player.Data.Disconnected || player.FriendCode != entry.FriendCode) continue;

            foreach (var id in entry.Ids)
                AchievementManager.TryUpdateUnit(entry.FriendCode, id);
        }
    }
}
