using TONX.Modules;
using TONX.Roles.Core.Interfaces;

namespace TONX.Modules;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
public static class SpecialMeetingManager
{
    /// <summary>
    /// 获取当前正在主持特殊会议的实现
    /// </summary>
    public static ISpecialMeeting GetActiveSpecialMeeting()
    {
        foreach (var role in CustomRoleManager.AllActiveRoles.Values)
        {
            if (role is ISpecialMeeting specialMeeting && specialMeeting.IsSpecialMeetingActive)
                return specialMeeting;
        }
        // if (Options.CurrentGameMode.GetModeClass() is ISpecialMeeting modeSpecialMeeting && modeSpecialMeeting.IsSpecialMeetingActive)
            // return modeSpecialMeeting;
        return null;
    }

    [HarmonyPostfix]
    public static void Postfix(MeetingHud __instance)
    {
        foreach (var role in CustomRoleManager.AllActiveRoles.Values)
        {
            if (role is ISpecialMeeting debugSm)
                Logger.Info($"SpecialMeeting.Postfix: role={role.Player?.GetNameWithRole()} active={debugSm.IsSpecialMeetingActive} players=[{(debugSm.SpecialMeetingPlayers != null ? string.Join(",", debugSm.SpecialMeetingPlayers) : "null")}]", "SpecialMeeting");
        }
        var active = GetActiveSpecialMeeting();
        Logger.Info($"SpecialMeeting.Postfix: active={(active == null ? "NULL" : active.GetType().Name)}", "SpecialMeeting");
        active?.HandleSpecialMeeting(__instance);
    }
}