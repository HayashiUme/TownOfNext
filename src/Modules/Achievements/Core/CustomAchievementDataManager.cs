using TONX.Modules.Achievements.Core;

namespace TONX.Modules.Achievements.Core;

public static class CustomAchievementDataManager
{
    private static readonly object _lock = new();
    private static readonly Dictionary<string, CustomAchievementData> ByFriendCode = new();

    public static string LocalFriendCode { get; private set; } = "";

    public static IReadOnlyList<CustomAchievementData> GetAll()
    {
        lock (_lock) return ByFriendCode.Values.ToList();
    }

    public static int Count
    {
        get { lock (_lock) return ByFriendCode.Count; }
    }

    public static void SetLocalFriendCode(string friendCode)
    {
        LocalFriendCode = friendCode;
    }

    public static CustomAchievementData Get(string friendCode)
    {
        if (string.IsNullOrEmpty(friendCode)) return null;

        lock (_lock) return ByFriendCode.TryGetValue(friendCode, out var data) ? data : null;
    }

    public static CustomAchievementData GetLocal() => Get(LocalFriendCode);

    public static CustomAchievementData GetByPlayer(PlayerControl player)
        => player == null ? null : Get(player.FriendCode);

    public static CustomAchievementData GetOrCreate(string friendCode, bool isLocal = false)
    {
        if (string.IsNullOrEmpty(friendCode)) return null;

        lock (_lock)
        {
            if (ByFriendCode.TryGetValue(friendCode, out var data)) return data;

            data = new CustomAchievementData(friendCode, isLocal);
            ByFriendCode[friendCode] = data;
            return data;
        }
    }

    public static CustomAchievementData GetOrCreateByPlayer(PlayerControl player)
        => player == null ? null : GetOrCreate(player.FriendCode, player.AmOwner);

    public static void Remove(string friendCode)
    {
        if (string.IsNullOrEmpty(friendCode)) return;

        lock (_lock) ByFriendCode.Remove(friendCode);
    }

    public static void Clear()
    {
        lock (_lock) ByFriendCode.Clear();
    }

}
