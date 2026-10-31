using TONX.Modules.Achievements.Core;
using UnityEngine;

namespace TONX.Modules.Achievements.Crewmate;

public sealed class CrewNobodyLeftBehind : SingleUnlockAchievement<CrewNobodyLeftBehind>
{
    public override int Id => 500208;
    public override Color TitleColor => new Color32(80, 190, 255, 255);
    public override AchievementCategory Category => AchievementCategory.Crewmate;
    public override AchievementRarity Rarity => AchievementRarity.Legendary;
    public override bool CheckClear(PlayerControl player)
        => player?.Data != null && !player.Data.Disconnected && player.GetCustomRole().IsCrewmate()
           && !player.Is(CustomRoles.Madmate) && !player.Is(CustomRoles.Charmed)
           && CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate
           && (CustomWinnerHolder.WinnerIds.Contains(player.PlayerId)
               || CustomWinnerHolder.WinnerRoles.Contains(player.GetCustomRole()))
           && SelectRolesPatch.StartingCrewmates.Count >= 4
           && SelectRolesPatch.DepartedStartingCrewmates.Count == 0
           && SelectRolesPatch.StartingCrewmates.All(pair =>
               Main.AllPlayerControls.Any(pc => pc.PlayerId == pair.Key && pc.FriendCode == pair.Value
                   && pc.Data != null && !pc.Data.Disconnected && !pc.Data.IsDead
                   && pc.GetCustomRole().IsCrewmate() && pc.GetCountTypes() == CountTypes.Crew
                   && !pc.Is(CustomRoles.Madmate) && !pc.Is(CustomRoles.Charmed)
                   && PlayerState.GetByPlayerId(pc.PlayerId) is { IsDead: false }));
}
