using TONX.Modules.Achievements.Core;
using TONX.Roles.Neutral;
using UnityEngine;

namespace TONX.Modules.Achievements.Neutral;

public sealed class JesterExileWin : SingleUnlockAchievement<JesterExileWin>
{
    public override int Id => 300201;
    public override Color TitleColor => new Color32(180, 130, 255, 255);
    public override string Category => "Neutral";

    public override bool CheckClear(PlayerControl player)
        => player?.Data != null && !player.Data.Disconnected && player.Is(CustomRoles.Jester)
           && player.GetRoleClass() is Jester { ExileWinTriggered: true }
           && (CustomWinnerHolder.WinnerIds.Contains(player.PlayerId)
               || CustomWinnerHolder.WinnerRoles.Contains(player.GetCustomRole()));
}

public sealed class JesterFirstMeetingWin : SingleUnlockAchievement<JesterFirstMeetingWin>
{
    public override int Id => 300202;
    public override Color TitleColor => new Color32(180, 130, 255, 255);
    public override string Category => "Neutral";

    public override bool CheckClear(PlayerControl player)
        => player?.Data != null && !player.Data.Disconnected && player.Is(CustomRoles.Jester)
           && player.GetRoleClass() is Jester { ExileWinTriggered: true, WonInFirstMeeting: true }
           && (CustomWinnerHolder.WinnerIds.Contains(player.PlayerId)
               || CustomWinnerHolder.WinnerRoles.Contains(player.GetCustomRole()));
}

public sealed class LawyerClientSharedWin : SingleUnlockAchievement<LawyerClientSharedWin>
{
    public override int Id => 302801;
    public override Color TitleColor => new Color32(180, 130, 255, 255);
    public override string Category => "Neutral";

    public override bool CheckClear(PlayerControl player)
    {
        if (player?.Data == null || player.Data.Disconnected || !player.Data.IsDead
            || !player.Is(CustomRoles.Lawyer)
            || player.GetRoleClass() is not Lawyer { ClientWinGranted: true } lawyer
            || string.IsNullOrEmpty(lawyer.AssignedClientCode)
            || lawyer.WinningClientCode != lawyer.AssignedClientCode
            || !(CustomWinnerHolder.WinnerIds.Contains(player.PlayerId)
                 || CustomWinnerHolder.WinnerRoles.Contains(player.GetCustomRole()))) return false;

        var client = Main.AllPlayerControls.FirstOrDefault(x => x != null && x.PlayerId == lawyer.TargetId);
        return client?.Data != null && !client.Data.Disconnected && client != player
               && client.FriendCode == lawyer.WinningClientCode
               && (CustomWinnerHolder.WinnerIds.Contains(client.PlayerId)
                   || CustomWinnerHolder.WinnerRoles.Contains(client.GetCustomRole()));
    }
}

public sealed class LawyerClientWinTaken : SingleUnlockAchievement<LawyerClientWinTaken>
{
    public override int Id => 302802;
    public override Color TitleColor => new Color32(180, 130, 255, 255);
    public override string Category => "Neutral";

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

public sealed class ProsecutorInterceptionWin : SingleUnlockAchievement<ProsecutorInterceptionWin>
{
    public override int Id => 302902;
    public override Color TitleColor => new Color32(180, 130, 255, 255);
    public override string Category => "Neutral";

    public override bool CheckClear(PlayerControl player)
        => player?.Data != null && player.IsAlive() && player.Is(CustomRoles.Prosecutors)
           && player.GetRoleClass() is Prosecutors { SurvivalWinGranted: true, InterceptedKillers: >= 2 }
           && (CustomWinnerHolder.WinnerIds.Contains(player.PlayerId)
               || CustomWinnerHolder.WinnerRoles.Contains(player.GetCustomRole()));
}
