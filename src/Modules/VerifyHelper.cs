using System.Text;
using TONX.Modules;

namespace TONX.Modules;

public static class VerifyHelper
{
    private static readonly string ApiBase = "https://record.tonx.cc";

    public static (MsgRecallMode, string) OnVerifyCommand(MessageControl mc)
    {
        if (!mc.IsFromSelf)
            return (MsgRecallMode.Block, null);

        var friendCode = EOSManager.Instance?.friendCode ?? "";
        if (string.IsNullOrWhiteSpace(friendCode))
            return (MsgRecallMode.Block, GetString("Verify_NoFriendCode"));

        _ = SendVerifyRequestAsync(mc.Player, friendCode);
        return (MsgRecallMode.Block, GetString("Verify_Sending"));
    }

    private static async Task SendVerifyRequestAsync(PlayerControl player, string friendCode)
    {
        try
        {
            var token = await ApiTokenProvider.BuildTokenAsync().ConfigureAwait(false);
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var body = new StringContent(
                $"{{\"friend_code\":\"{friendCode}\"}}",
                Encoding.UTF8, "application/json");
            var url = $"{ApiBase}/api/verify/request?token={Uri.EscapeDataString(token)}";
            var resp = await client.PostAsync(url, body).ConfigureAwait(false);

            string msg = resp.IsSuccessStatusCode
                ? GetString("Verify_RequestSent")
                : GetString("Verify_RequestFailed");

            new LateTask(() =>
                Utils.SendMessage(msg, player.PlayerId,
                    Utils.ColorString(Main.ModColor32, GetString("Verify_Title"))),
                0f, "VerifyResponse");
        }
        catch (Exception e)
        {
            Logger.Error($"Verify request failed: {e.Message}", "VerifyHelper");
            new LateTask(() =>
                Utils.SendMessage(GetString("Verify_RequestFailed"), player.PlayerId,
                    Utils.ColorString(Main.ModColor32, GetString("Verify_Title"))),
                0f, "VerifyError");
        }
    }
}