using TONX.Modules.Achievements.Core;
using UnityEngine;

namespace TONX.Modules.Achievements.Impostor;

public sealed class RoundAllImpostorsAlive : SingleUnlockAchievement<RoundAllImpostorsAlive>
{
    public override int Id => 500102;
    public override Color TitleColor => new Color32(255, 25, 25, 255);
    public override AchievementCategory Category => AchievementCategory.Impostor;
    public override AchievementRarity Rarity => AchievementRarity.Rare;
    public override bool CheckClear(PlayerControl player)
        => player?.Data != null && !player.Data.Disconnected
           && CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor
           && SelectRolesPatch.StartingImpostorIds.Count >= 2
           && SelectRolesPatch.StartingImpostorIds.All(id =>
                  PlayerState.GetByPlayerId(id) is { } state
                  && !state.IsDead
                  && state.MainRole.IsImpostor())
           && (CustomWinnerHolder.WinnerIds.Contains(player.PlayerId)
               || CustomWinnerHolder.WinnerRoles.Contains(CustomRoles.Impostor));
}
