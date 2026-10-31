using TONX.Modules.Achievements.Core;
using TONX.Roles.Neutral;
using UnityEngine;

namespace TONX.Modules.Achievements.Neutral;

public sealed class LawyerClientWinTaken : SingleUnlockAchievement<LawyerClientWinTaken>
{
    public override int Id => 302802;
    public override Color TitleColor => new Color32(180, 130, 255, 255);
    public override AchievementCategory Category => AchievementCategory.Neutral;

    public override bool CheckClear(PlayerControl player)
    {
        if (player?.Data == null || !player.IsAlive() || !player.Is(CustomRoles.Lawyer)
            || player.GetRoleClass() is not Lawyer { ClientWinGranted: true, ClientWinRemoved: true } lawyer
            || string.IsNullOrEmpty(lawyer.AssignedClientCode)
            || lawyer.WinningClientCode != lawyer.AssignedClientCode
            || !(CustomWinnerHolder.WinnerIds.Contains(player.PlayerId)
                 || CustomWinnerHolder.WinnerRoles.Contains(player.GetCustomRole()))) return false;

        var client = Main.AllPlayerControls.FirstOrDefault(x => x != null && x.PlayerId == lawyer.TargetId);
        return client?.Data != null && !client.Data.Disconnected && client != player
               && client.FriendCode == lawyer.WinningClientCode
               && !CustomWinnerHolder.WinnerIds.Contains(client.PlayerId)
               && !CustomWinnerHolder.WinnerRoles.Contains(client.GetCustomRole());
    }
}
