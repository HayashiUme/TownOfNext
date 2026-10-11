using TONX.Modules.Achievements.Titles;
using UnityEngine;
using Object = UnityEngine.Object;
using AchManager = TONX.Modules.Achievements.Core.AchievementManager;

namespace TONX;

[HarmonyPatch(typeof(LobbyBehaviour), nameof(LobbyBehaviour.Start))]
public class LobbyStartPatch
{
    private static GameObject Paint;

    private static bool _wasInGame = false;

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.ExitGame))]
    public static class MarkGameExit
    {
        public static void Prefix() => _wasInGame = GameStates.IsInGame;
    }

    public static void Postfix(LobbyBehaviour __instance)
    {
        SetPaint(__instance);
        
        if (_wasInGame)
        {
            _ = AchManager.FlushAllAsync();
            _wasInGame = false;
        }
    }

    public static void SetPaint(LobbyBehaviour __instance)
    {
        if (Paint != null) return;
        Paint = Object.Instantiate(__instance.transform.FindChild("Leftbox").gameObject, __instance.transform);
        Paint.name = "TONX Lobby Paint";
        Paint.transform.localPosition = new Vector3(0.042f, -2.59f, -10.5f);
        SpriteRenderer renderer = Paint.GetComponent<SpriteRenderer>();
        renderer.sprite = Utils.LoadSprite("TONX.Resources.Images.LobbyPaint.png", 290f);
    }
}
