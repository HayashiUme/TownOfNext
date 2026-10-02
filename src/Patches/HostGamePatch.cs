using InnerNet;

namespace TONX.Patches;

// 修复一个AMCI的问题，因为本地和自由默认Tag0
[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.HostGame))]
internal class HostGameLocalPatch
{
    public static void Prefix(out string __state)
    {
        __state = null;
        if (AmongUsClient.Instance == null) return;
        if (AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame) return;
        if (!CurrentModRegistration.TryGetModRegistrationGuid(out _)) return;

        __state = CurrentModRegistration.ModRegistrationGuidString;
        CurrentModRegistration.ModRegistrationGuidString = "";
    }

    public static void Postfix(string __state)
    {
        if (__state == null) return;
        CurrentModRegistration.ModRegistrationGuidString = __state;
    }
}
