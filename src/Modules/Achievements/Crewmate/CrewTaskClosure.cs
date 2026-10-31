using TONX.Modules.Achievements.Core;
using UnityEngine;

namespace TONX.Modules.Achievements.Crewmate;

public sealed class CrewTaskClosure : SingleUnlockAchievement<CrewTaskClosure>
{
    public override int Id => 500201;
    public override Color TitleColor => new Color32(80, 190, 255, 255);
    public override AchievementCategory Category => AchievementCategory.Crewmate;
    public override AchievementRarity Rarity => AchievementRarity.Rare;
    public override bool CheckClear(PlayerControl player)
        => player?.Data != null && !player.Data.Disconnected && player.GetCustomRole().IsCrewmate()
           && !player.Is(CustomRoles.Madmate) && !player.Is(CustomRoles.Charmed)
           && GameEndChecker.TaskWinTriggered
           && CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate
           && (CustomWinnerHolder.WinnerIds.Contains(player.PlayerId)
               || CustomWinnerHolder.WinnerRoles.Contains(player.GetCustomRole()));
}
