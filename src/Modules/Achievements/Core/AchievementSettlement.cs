namespace TONX.Modules.Achievements.Core;

public sealed class GameAchievementResult
{
    public string FriendCode = string.Empty;
    public byte PlayerId = byte.MaxValue;
    public List<AchievementBase> Unlocked = new();
}

public static class AchievementSettlement
{
    public static Task<bool> SettleAsync()
    {
        if (AmongUsClient.Instance?.AmHost != true || !GameStates.IsInGame || GameStates.IsFreePlay
            || CustomWinnerHolder.WinnerTeam is CustomWinner.Draw or CustomWinner.Error or CustomWinner.Default)
            return Task.FromResult(false);

        var players = Main.AllPlayerControls
            .Where(x => x?.Data != null && !x.Data.Disconnected && !string.IsNullOrEmpty(x.FriendCode))
            .ToList();

        foreach (var player in players)
        {
            var data = CustomAchievementDataManager.Get(player.FriendCode);
            foreach (var definition in AchievementRegistry.GetAll())
            {
                if (data?.Has(definition.Id) != true && definition.CheckClear(player))
                    AchievementManager.TryUpdateUnit(player.FriendCode, definition.Id);
            }
        }

        Logger.Info("Local achievement settlement completed", "AchievementSettlement");
        return Task.FromResult(true);
    }
}
