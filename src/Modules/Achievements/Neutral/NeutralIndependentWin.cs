using TONX.Modules.Achievements.Core;
using TONX.Roles.Neutral;
using UnityEngine;

namespace TONX.Modules.Achievements.Neutral;

public sealed class NeutralIndependentWin : SingleUnlockAchievement<NeutralIndependentWin>
{
    public override int Id => 500301;
    public override Color TitleColor => new Color32(180, 130, 255, 255);
    public override AchievementCategory Category => AchievementCategory.Neutral;
    public override AchievementRarity Rarity => AchievementRarity.Rare;
    public override bool CheckClear(PlayerControl player)
        => player?.Data != null && !player.Data.Disconnected && HasBaseWinSource(player)
           && (CustomWinnerHolder.WinnerIds.Contains(player.PlayerId)
               || CustomWinnerHolder.WinnerRoles.Contains(player.GetCustomRole()));

    internal static bool HasBaseWinSource(PlayerControl player)
        => (player.GetRoleClass(), CustomWinnerHolder.WinnerTeam) switch
        {
            (Jester { BaseWinTriggered: true }, CustomWinner.Jester) => true,
            (Executioner { BaseWinTriggered: true }, CustomWinner.Executioner) => true,
            (Innocent { BaseWinTriggered: true }, CustomWinner.Innocent) => true,
            (Terrorist { BaseWinTriggered: true }, CustomWinner.Terrorist) => true,
            (Workaholic { BaseWinTriggered: true }, CustomWinner.Workaholic) => true,
            (Arsonist { BaseWinTriggered: true }, CustomWinner.Arsonist) => true,
            (Collector { BaseWinTriggered: true }, CustomWinner.Collector) => true,
            (Doomsayer { BaseWinTriggered: true }, CustomWinner.Doomsayer) => true,
            (PlagueDoctor { InfectionWinTriggered: true }, CustomWinner.PlagueDoctor) => true,
            _ => false
        };
}
