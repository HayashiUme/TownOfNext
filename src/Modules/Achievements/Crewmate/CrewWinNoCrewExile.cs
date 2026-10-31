using TONX.Modules.Achievements.Core;
using UnityEngine;

namespace TONX.Modules.Achievements.Crewmate;

public sealed class CrewWinNoCrewExile : SingleUnlockAchievement<CrewWinNoCrewExile>
{
    public override int Id => 500202;
    public override Color TitleColor => new Color32(80, 190, 255, 255);
    public override AchievementCategory Category => AchievementCategory.Crewmate;
    public override AchievementRarity Rarity => AchievementRarity.Rare;
    public override bool CheckClear(PlayerControl player)
        => player?.Data != null && !player.Data.Disconnected
           && ExileControllerWrapUpPatch.ExiledImpostorCount > 0
           && ExileControllerWrapUpPatch.ExiledCrewCount == 0
           && CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate
           && (CustomWinnerHolder.WinnerIds.Contains(player.PlayerId)
               || CustomWinnerHolder.WinnerRoles.Contains(CustomRoles.Crewmate));
}
