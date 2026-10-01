using System.Collections.Generic;
using TMPro;
using TONX.Modules.Achievements.Core;
using TONX.Modules.Achievements.Core.Base;
using TONX.Modules.Achievements.Game;
using TONX.Modules.Achievements.Player;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TONX.Modules.Achievements.AchievementInterface;

public static class AchievementTitleMenu
{
    public static GameObject Menu { get; private set; }
    public static GameObject Slider { get; private set; }

    private static readonly List<GameObject> Items = new();
    private static int numItems = 0;

    public static void Hide()
    {
        if (Menu != null) Menu?.SetActive(false);
    }

    public static void Toggle(bool? on = null)
    {
        if (Menu == null) on ??= true;
        else on ??= !Menu.activeSelf;

        if (!GameStates.IsNotJoined || !on.Value)
        {
            Hide();
            return;
        }
        if (Menu == null) Init();
        if (Menu == null) return;

        Menu.SetActive(true);
        RefreshList();
        _ = CoSyncThenRefresh();
    }

    private static async Task CoSyncThenRefresh()
    {
        await TONX.Modules.Achievements.Game.AchievementManager.SyncAchievementsAsync(PlayerControl.LocalPlayer);
        RefreshList();
    }

    private static void Init()
    {
        var template = AccountManager.Instance?.transform.FindChild("InfoTextBox");
        if (template == null) return;

        Menu = Object.Instantiate(template.gameObject, NameTagInterface.NameTagPanel.CustomBackground.transform.parent);
        Menu.name = "TONX Achievement Title Menu";
        Menu.transform.SetLocalZ(-30f);
        Menu.transform.FindChild("Background").localScale *= 1.4f;

        Object.Destroy(Menu.transform.FindChild("Button1").gameObject);
        Object.Destroy(Menu.transform.FindChild("Button2").gameObject);

        var titleText = Menu.transform.FindChild("TitleText_TMP").GetComponent<TextMeshPro>();
        titleText.DestroyTranslator();
        titleText.text = GetString("AchievementTitle");
        var infoText = Menu.transform.FindChild("InfoText_TMP").GetComponent<TextMeshPro>();
        infoText.DestroyTranslator();
        infoText.text = GetString("Achievement.SelectTitleHint");

        var closeButton = Object.Instantiate(Menu.transform.parent.FindChild("CloseButton").gameObject, Menu.transform);
        closeButton.name = "Title Menu Close";
        closeButton.transform.localPosition = new Vector3(4.9f, 2.5f, -1f);
        closeButton.transform.localScale = new Vector3(1f, 1f, 1f);
        var closePassive = closeButton.GetComponent<PassiveButton>();
        closePassive.OnClick = new();
        closePassive.OnClick.AddListener(new Action(Hide));

        var sliderTemplate = AccountManager.Instance.transform.FindChild("MainSignInWindow/SignIn/AccountsMenu/Accounts/Slider").gameObject;
        if (sliderTemplate != null && Slider == null)
        {
            Slider = Object.Instantiate(sliderTemplate, Menu.transform);
            Slider.name = "Achievement Titles Slider";
            Slider.transform.localPosition = new Vector3(0f, 0.35f, -1f);
            Slider.transform.localScale = new Vector3(1f, 1f, 1f);
            Slider.GetComponent<SpriteRenderer>().size = new(5f, 3.8f);
            var scroller = Slider.GetComponent<Scroller>();
            scroller.ScrollWheelSpeed = 0.3f;
            Slider.transform.FindChild("Mask").transform.localScale = new Vector3(4.9f, 3.72f, 1f);
        }

        var newTagTemplate = NameTagInterface.NameTagPanel.CustomBackground.transform.FindChild("New Tag").gameObject;
        var browseButton = Object.Instantiate(newTagTemplate, Menu.transform);
        browseButton.name = "Browse Achievements";
        browseButton.transform.localPosition = new Vector3(0f, -2.3f, -5f);
        var browseToggle = browseButton.GetComponent<ToggleButtonBehaviour>();
        if (browseToggle != null) browseToggle.Text.text = GetString("Achievements");
        var browsePassive = browseButton.GetComponent<PassiveButton>();
        browsePassive.OnClick = new();
        browsePassive.OnClick.AddListener(new Action(() =>
        {
            Hide();
            AchievementPanel.Toggle(true);
        }));
    }

    public static void RefreshList()
    {
        if (Slider == null) return;

        var scroller = Slider.GetComponent<Scroller>();
        scroller.Inner.gameObject.ForEachChild((Action<GameObject>)(DestroyObj));
        static void DestroyObj(GameObject obj)
        {
            if (obj.name.StartsWith("Title Item")) Object.Destroy(obj);
        }

        var numberSetter = AccountManager.Instance.transform.FindChild("DOBEnterScreen/EnterAgePage/MonthMenu/Months").GetComponent<NumberSetter>();
        var buttonPrefab = numberSetter.ButtonPrefab.gameObject;

        Items.Do(Object.Destroy);
        Items.Clear();
        numItems = 0;

        foreach (var achievement in AchievementRegistry.GetAll().Where(a => a.Unlocked).OrderBy(a => a.Id))
        {
            numItems++;
            var button = Object.Instantiate(buttonPrefab, scroller.Inner);
            button.transform.localPosition = new(-1f, 1.5f - 0.6f * numItems, -0.5f);
            button.transform.localScale = new(1.2f, 1.2f, 1.2f);
            button.name = "Title Item " + achievement.Id;
            Object.Destroy(button.GetComponent<UIScrollbarHelper>());
            Object.Destroy(button.GetComponent<NumberButton>());

            var label = button.transform.GetChild(0).GetComponent<TextMeshPro>();
            label.text = achievement.TitleDisplay;
            label.color = achievement.TitleColor;

            var renderer = button.GetComponent<SpriteRenderer>();
            renderer.color = achievement.Rarity.GetColor();

            var passive = button.GetComponent<PassiveButton>();
            passive.OnClick = new();
            var captured = achievement;
            passive.OnClick.AddListener(new Action(() =>
            {
                _ = TONX.Modules.Achievements.Game.AchievementManager.WearTitleAsync(PlayerControl.LocalPlayer, captured.Id);
                Hide();
            }));
            Items.Add(button);
        }

        numItems++;
        var removeButton = Object.Instantiate(buttonPrefab, scroller.Inner);
        removeButton.transform.localPosition = new(-1f, 1.5f - 0.6f * numItems, -0.5f);
        removeButton.transform.localScale = new(1.2f, 1.2f, 1.2f);
        removeButton.name = "Title Item Remove";
        Object.Destroy(removeButton.GetComponent<UIScrollbarHelper>());
        Object.Destroy(removeButton.GetComponent<NumberButton>());
        removeButton.transform.GetChild(0).GetComponent<TextMeshPro>().text = GetString("Achievement.Unequip");
        removeButton.GetComponent<SpriteRenderer>().color = Palette.DisabledGrey;
        var removePassive = removeButton.GetComponent<PassiveButton>();
        removePassive.OnClick = new();
        removePassive.OnClick.AddListener(new Action(() =>
        {
            _ = TONX.Modules.Achievements.Game.AchievementManager.WearTitleAsync(PlayerControl.LocalPlayer, 0);
            Hide();
        }));
        Items.Add(removeButton);

        scroller.SetYBoundsMin(0f);
        scroller.SetYBoundsMax(0.6f * numItems);
    }
}
