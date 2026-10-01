using System.Collections.Generic;
using TMPro;
using TONX.Modules.Achievements.Core;
using TONX.Modules.Achievements.Core.Base;
using TONX.Modules.Achievements.Game;
using TONX.Modules.Achievements.Player;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TONX.Modules.Achievements.AchievementInterface;

public static class AchievementPanel
{
    public static SpriteRenderer CustomBackground { get; private set; }
    public static GameObject Slider { get; private set; }

    private static readonly List<GameObject> Items = new();
    private static readonly List<GameObject> TabButtons = new();

    private static int numItems = 0;
    private static string CurrentCategory = "";

    public static void Hide()
    {
        if (CustomBackground != null)
            CustomBackground?.gameObject?.SetActive(false);
    }

    public static void Toggle(bool? on = null)
    {
        if (CustomBackground == null) on ??= false;
        else on ??= !CustomBackground.gameObject.activeSelf;

        if (!GameStates.IsNotJoined || !on.Value)
        {
            Hide();
            return;
        }

        NameTagInterface.NameTagPanel.Hide();
        NameTagInterface.NameTagEditMenu.Hide();
        RefreshList();
        CustomBackground.gameObject.SetActive(true);
    }

    public static void Init(OptionsMenuBehaviour optionsMenuBehaviour)
    {
        var mouseMoveToggle = optionsMenuBehaviour.DisableMouseMovement;

        if (CustomBackground == null)
        {
            CustomBackground = Object.Instantiate(optionsMenuBehaviour.Background, optionsMenuBehaviour.transform);
            CustomBackground.name = "Achievement Panel Background";
            CustomBackground.transform.localScale = new(0.9f, 0.9f, 1f);
            CustomBackground.transform.localPosition += Vector3.back * 8;
            CustomBackground.gameObject.SetActive(false);

            var closeButton = Object.Instantiate(mouseMoveToggle, CustomBackground.transform);
            closeButton.transform.localPosition = new(1.3f, -2.43f, -6f);
            closeButton.name = "Close";
            closeButton.Text.text = GetString("Close");
            closeButton.Background.color = Palette.DisabledGrey;
            var closePassive = closeButton.GetComponent<PassiveButton>();
            closePassive.OnClick = new();
            closePassive.OnClick.AddListener(new Action(() =>
            {
                CustomBackground.gameObject.SetActive(false);
            }));

            var sliderTemplate = AccountManager.Instance.transform.FindChild("MainSignInWindow/SignIn/AccountsMenu/Accounts/Slider").gameObject;
            if (sliderTemplate != null && Slider == null)
            {
                Slider = Object.Instantiate(sliderTemplate, CustomBackground.transform);
                Slider.name = "Achievements Slider";
                Slider.transform.localPosition = new Vector3(0f, 0.35f, -1f);
                Slider.transform.localScale = new Vector3(1f, 1f, 1f);
                Slider.GetComponent<SpriteRenderer>().size = new(5f, 3.8f);
                var scroller = Slider.GetComponent<Scroller>();
                scroller.ScrollWheelSpeed = 0.3f;
                var mask = Slider.transform.FindChild("Mask");
                mask.transform.localScale = new Vector3(4.9f, 3.72f, 1f);
            }
        }

        BuildTabs(mouseMoveToggle);
        RefreshList();
    }

    private static void BuildTabs(ToggleButtonBehaviour template)
    {
        TabButtons.Do(Object.Destroy);
        TabButtons.Clear();

        var categories = AchievementRegistry.Categories;
        if (categories == null || categories.Count == 0) return;
        if (!categories.Contains(CurrentCategory)) CurrentCategory = categories[0];

        for (int i = 0; i < categories.Count; i++)
        {
            var category = categories[i];
            var tab = Object.Instantiate(template, CustomBackground.transform);
            tab.transform.localPosition = new(-1.55f + 0.85f * i, 2.05f, -6f);
            tab.transform.localScale = new(0.6f, 0.6f, 0.6f);
            tab.name = "Achievement Tab " + category;
            tab.Text.text = GetString(category);
            tab.Background.color = category == CurrentCategory ? Main.ModColor32 : Palette.DisabledGrey;
            var passive = tab.GetComponent<PassiveButton>();
            passive.OnClick = new();
            passive.OnClick.AddListener(new Action(() =>
            {
                CurrentCategory = category;
                BuildTabs(template);
                RefreshList();
            }));
            TabButtons.Add(tab.gameObject);
        }
    }

    public static void RefreshList()
    {
        if (Slider == null) return;

        var scroller = Slider.GetComponent<Scroller>();
        scroller.Inner.gameObject.ForEachChild((Action<GameObject>)(DestroyObj));
        static void DestroyObj(GameObject obj)
        {
            if (obj.name.StartsWith("Achievement Item")) Object.Destroy(obj);
        }

        var numberSetter = AccountManager.Instance.transform.FindChild("DOBEnterScreen/EnterAgePage/MonthMenu/Months").GetComponent<NumberSetter>();
        var buttonPrefab = numberSetter.ButtonPrefab.gameObject;

        Items.Do(Object.Destroy);
        Items.Clear();
        numItems = 0;

        var list = AchievementRegistry.GetByCategory(CurrentCategory)
            .OrderBy(a => a.Unlocked ? 0 : 1)
            .ThenBy(a => (int)a.Rarity * -1)
            .ThenBy(a => a.Id);

        foreach (var achievement in list)
        {
            numItems++;
            var button = Object.Instantiate(buttonPrefab, scroller.Inner);
            button.transform.localPosition = new(-1f, 1.5f - 0.6f * numItems, -0.5f);
            button.transform.localScale = new(1.2f, 1.2f, 1.2f);
            button.name = "Achievement Item " + achievement.Id;
            Object.Destroy(button.GetComponent<UIScrollbarHelper>());
            Object.Destroy(button.GetComponent<NumberButton>());

            var label = button.transform.GetChild(0).GetComponent<TextMeshPro>();
            label.text = achievement.Unlocked || !achievement.Hidden
                ? $"{achievement.Name}"
                : GetString("Achievement.HiddenName");

            var renderer = button.GetComponent<SpriteRenderer>();
            renderer.color = achievement.Unlocked ? achievement.Rarity.GetColor() : Palette.DisabledGrey;
            var rollover = button.GetComponent<ButtonRolloverHandler>();
            rollover.OverColor = achievement.Unlocked ? achievement.Rarity.GetColor() : Palette.DisabledGrey;
            rollover.OutColor = achievement.Unlocked ? achievement.Rarity.GetColor() : Palette.DisabledGrey;

            var subText = Object.Instantiate(label, button.transform);
            subText.transform.SetLocalX(1.9f);
            subText.fontSize = 0.9f;

            var localPlayer = PlayerControl.LocalPlayer;
            int equippedId = localPlayer == null ? 0 : PlayerAchievementData.GetEquippedTitle(localPlayer.PlayerId);
            string progressText = achievement.IsProgressAchievement ? $"{achievement.Progress}/{achievement.RequiredValue}" : null;

            if (achievement.Unlocked)
            {
                subText.text = equippedId == achievement.Id
                    ? GetString("Achievement.Equipped")
                    : progressText ?? GetString("Achievement.UnlockedMark");
            }
            else
            {
                subText.text = achievement.Hidden
                    ? GetString("Achievement.HiddenDescription")
                    : progressText ?? GetString("Achievement.LockedMark");
            }

            if (achievement.IsProgressAchievement) BuildProgressBar(button, achievement);

            var passive = button.GetComponent<PassiveButton>();
            passive.OnClick = new();
            var captured = achievement;
            passive.OnClick.AddListener(new Action(() =>
            {
                _ = TONX.Modules.Achievements.Game.AchievementManager.WearTitleAsync(PlayerControl.LocalPlayer, captured.Id);
                _ = new LateTask(RefreshList, 0.6f, "RefreshAchievementPanel");
            }));

            Items.Add(button);
        }

        scroller.SetYBoundsMin(0f);
        scroller.SetYBoundsMax(0.6f * numItems);
    }

    private static void BuildProgressBar(GameObject button, AchievementBase achievement)
    {
        float ratio = achievement.RequiredValue <= 0 ? 0f : Mathf.Clamp01((float)achievement.Progress / achievement.RequiredValue);

        var bar = Object.Instantiate(button.GetComponent<SpriteRenderer>(), button.transform);
        bar.name = "Progress Bar";
        bar.transform.localPosition = new(1.05f, -0.28f, -0.2f);
        bar.size = new(1.6f * ratio, 0.1f);
        bar.color = achievement.Unlocked ? achievement.Rarity.GetColor() : Palette.DisabledGrey;
    }
}
