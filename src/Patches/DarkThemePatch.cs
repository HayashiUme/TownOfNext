using AmongUs.Data;
using TMPro;
using UnityEngine;

namespace TONX.Patches;

// https://github.com/HayashiUme/The-Other-Roles-Rework
public static class DarkThemePatch
{
    public static readonly Color32 DarkBackgroundColor = new(40, 40, 40, byte.MaxValue);

    [HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetName))]
    internal static class ChatBubbleSetNamePatch
    {
        public static void Postfix(ChatBubble __instance)
        {
            if (!Main.DarkTheme.Value) return;

            __instance.Background.color = new Color(0.1f, 0.1f, 0.1f, 1f);
            __instance.TextArea.color = Color.white;
            if (!__instance.playerInfo.Object.IsAlive() && GameStates.InGame)
                __instance.Background.color = new Color(0.1f, 0.1f, 0.1f, 0.7f);
        }
    }

    [HarmonyPatch(typeof(ChatController), nameof(ChatController.Awake))]
    internal static class ChatControllerAwakePatch
    {
        public static void Postfix(ChatController __instance)
        {
            if (!Main.DarkTheme.Value) return;

            var chatBubble = __instance.chatBubblePool.Prefab.Cast<ChatBubble>();
            chatBubble.TextArea.overrideColorTags = false;
            chatBubble.TextArea.color = Color.white;
            chatBubble.Background.color = new Color(0.1f, 0.1f, 0.1f, 1f);

            __instance.freeChatField.background.color = DarkBackgroundColor;
            __instance.freeChatField.textArea.compoText.Color(Color.white);
            __instance.freeChatField.textArea.outputText.color = Color.white;

            __instance.quickChatField.background.color = DarkBackgroundColor;
            __instance.quickChatField.text.color = Color.white;
        }
    }

    [HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
    internal static class ChatControllerUpdatePatch
    {
        public static void Postfix(ChatController __instance)
        {
            if (!Main.DarkTheme.Value) return;

            if (__instance.freeChatField.background.color != DarkBackgroundColor)
                __instance.freeChatField.background.color = DarkBackgroundColor;
            if (__instance.freeChatField.textArea.outputText.color != Color.white)
                __instance.freeChatField.textArea.outputText.color = Color.white;
            if (__instance.quickChatField.background.color != DarkBackgroundColor)
                __instance.quickChatField.background.color = DarkBackgroundColor;
        }
    }

    [HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.SetCosmetics))]
    internal static class PlayerVoteAreaSetNamePatch
    {
        public static void Postfix(PlayerVoteArea __instance)
        {
            if (!Main.DarkTheme.Value) return;

            __instance.Background.color = new Color(0.1f, 0.1f, 0.1f);
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    internal static class MeetingHudStartPatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            if (!Main.DarkTheme.Value) return;

            __instance.meetingContents.transform.FindChild("PhoneUI").FindChild("baseColor")
                .GetComponent<SpriteRenderer>().color = new Color(0.01f, 0.01f, 0.01f);
            __instance.Glass.color = new Color(0.7f, 0.7f, 0.7f, 0.3f);
            __instance.SkipVoteButton.GetComponent<SpriteRenderer>().color = new Color(0.4f, 0.4f, 0.4f);

            foreach (SpriteRenderer playerMaterialColors in __instance.PlayerColoredParts)
            {
                playerMaterialColors.color = new Color(0.25f, 0.25f, 0.25f);
                PlayerMaterial.SetColors(7, playerMaterialColors);
            }
        }
    }

    [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Start))]
    internal static class GameSettingMenuStartPatch
    {
        public static void Postfix(GameSettingMenu __instance)
        {
            if (!Main.DarkTheme.Value) return;

            __instance.ToggleLeftSideDarkener(false);
            __instance.ToggleRightSideDarkener(true);
        }
    }
}
