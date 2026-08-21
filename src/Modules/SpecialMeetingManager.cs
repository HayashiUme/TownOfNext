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
        GetActiveSpecialMeeting()?.HandleSpecialMeeting(__instance);
    }
}