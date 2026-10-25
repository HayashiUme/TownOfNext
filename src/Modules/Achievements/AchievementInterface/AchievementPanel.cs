using System.Collections.Generic;
using TMPro;
using TONX.Modules.Achievements.Core;
using TONX.Modules.Achievements.Titles;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TONX.Modules.Achievements.AchievementInterface;

public static class AchievementPanel
{
    private enum ListFilter { Locked, Unlocked, All }
    
    private static readonly Vector2 PanelSize = new(9.2f, 4.9f);
    

    public static GameObject Menu { get; private set; }
    public static GameObject Slider { get; private set; }

    private static TextBoxTMP SearchBox;
    private static TextMeshPro SearchHint;

    private static ListFilter CurrentFilter = ListFilter.All;
    private static string SearchText = "";

    private static float HalfWidth = 4.6f;
    private static float HalfHeight = 2.45f;
    private static float Unit = 1f;
    private static float ListWidth = 4f;
    private static float ListHeight = 4f;

    private static readonly List<GameObject> Items = new();
    private static readonly List<ToggleButtonBehaviour> FilterButtons = new();
    private static readonly Color UnlockedColor = new(0.33f, 0.78f, 0.35f, 1f);

    private static OptionsMenuBehaviour OptionsMenu;

    // 注意：不能用 Menu?.SetActive()，?. 是纯 C# 判空，绕过了 Unity 对已销毁对象的 == 重载
    public static void Hide()
    {
        if (Menu != null) Menu.SetActive(false);
    }

    public static void Toggle(bool? on = null)
    {
        if (Menu == null) Build();
        if (Menu == null)
        {
            Hide();
            return;
        }

        on ??= !Menu.activeSelf;
        if (!GameStates.IsNotJoined || !on.Value)
        {
            Hide();
            return;
        }

        BootstrapLocalData();
        RefreshList();
        Menu.SetActive(true);
    }

    private static void BootstrapLocalData()
    {
        if (string.IsNullOrEmpty(CustomAchievementDataManager.LocalFriendCode))
            CustomAchievementDataManager.SetLocalFriendCode(EOSManager.Instance?.friendCode);

        var friendCode = CustomAchievementDataManager.LocalFriendCode;
        if (string.IsNullOrEmpty(friendCode)) return;

        CustomAchievementDataManager.GetOrCreate(friendCode, true);
    }

    public static void Update()
    {
        if (Menu == null || !Menu.activeInHierarchy) return;

        if (SearchHint != null)
            SearchHint.gameObject.SetActive(SearchBox == null || string.IsNullOrEmpty(SearchBox.text));
        
        if (Slider == null) return;
        var mask = Slider.transform.FindChild("Mask") ?? Slider.transform;
        var top = mask.position.y + ListHeight / 2f;
        var bottom = mask.position.y - ListHeight / 2f;
        foreach (var item in Items)
        {
            if (item == null) continue;
            var y = item.transform.position.y;
            item.SetActive(y <= top && y >= bottom);
        }
    }

    public static void Init(OptionsMenuBehaviour optionsMenuBehaviour) => OptionsMenu = optionsMenuBehaviour;

    private static void Build()
    {
        if (OptionsMenu == null) return;
        var optionsMenuBehaviour = OptionsMenu;
        var mouseMoveToggle = optionsMenuBehaviour.DisableMouseMovement;

        if (CustomPopup.InfoTMP == null) CustomPopup.Init();

        var infoTextBox = AccountManager.Instance.transform.FindChild("InfoTextBox");
        if (infoTextBox == null || CustomPopup.InfoTMP == null)
        {
            Logger.Warn($"模板缺失：InfoTextBox={infoTextBox != null}, InfoTMP={CustomPopup.InfoTMP != null}", "AchievementPanel");
            return;
        }

        Menu = Object.Instantiate(infoTextBox.gameObject, optionsMenuBehaviour.transform);
        Menu.name = "Achievement Panel";
        Menu.transform.localPosition += Vector3.back * 8;
        Menu.transform.localScale = Vector3.one;

        var background = Menu.transform.FindChild("Background").GetComponent<SpriteRenderer>();
        var backgroundSize = VisibleSize(background);
        if (background.drawMode != SpriteDrawMode.Simple) background.size = PanelSize;
        else if (backgroundSize.x > 0f && backgroundSize.y > 0f)
            background.transform.localScale = new Vector3(PanelSize.x / backgroundSize.x, PanelSize.y / backgroundSize.y, 1f);

        var templateButton1 = Menu.transform.FindChild("Button1").gameObject;
        var templateButton2 = Menu.transform.FindChild("Button2").gameObject;
        templateButton1.SetActive(false);
        templateButton2.SetActive(false);
        Menu.transform.FindChild("TitleText_TMP").gameObject.SetActive(false);
        Menu.transform.FindChild("InfoText_TMP").gameObject.SetActive(false);

        HalfWidth = PanelSize.x / 2f;
        HalfHeight = PanelSize.y / 2f;
        Unit = HalfHeight / 2.32f;

        var hw = HalfWidth;
        var hh = HalfHeight;
        var u = Unit;
        var titleY = hh - 0.5f * u;
        var dividerX = hw * 0.31f;
        var listLeft = -hw + 0.5f * u;
        var listRight = dividerX - 0.35f * u;
        var columnLeft = dividerX + 0.25f * u;
        var columnRight = hw - 0.4f * u;
        var columnCenter = (columnLeft + columnRight) / 2f;

        var closeTemplate = optionsMenuBehaviour.transform.FindChild("CloseButton");
        if (closeTemplate != null)
        {
            var closeButton = Object.Instantiate(closeTemplate.gameObject, Menu.transform);
            closeButton.name = "Achievement Panel Close";
            closeButton.transform.localPosition = new Vector3(hw - 0.25f * u, hh - 0.3f * u, -6f);
            closeButton.transform.localScale = new Vector3(1f * u, 1f * u, 1f);
            var closePassive = closeButton.GetComponent<PassiveButton>();
            closePassive.OnClick = new();
            closePassive.OnClick.AddListener(new Action(Hide));

            var nav = Menu.GetComponent<ControllerNavMenu>();
            var closeElement = closeButton.GetComponent<UiElement>();
            if (nav != null && closeElement != null)
            {
                nav.BackButton = closeElement;
                nav.DefaultButtonSelected = closeElement;
            }
        }

        var titleWidth = hw - 0.5f * u;
        var title = MakeText(Menu.transform, "Title", GetString("Achievement.Title"), new Vector3(-hw + 0.5f * u + titleWidth / 2f, titleY, -5f), 1.5f * u, TextAlignmentOptions.Left, titleWidth, Color.white);
        title.fontStyle = FontStyles.Bold;

        var filterWidth = hw - dividerX;
        var filterTitle = MakeText(Menu.transform, "Filter Title", GetString("Achievement.Filter"), new Vector3(dividerX + 0.25f * u + filterWidth / 2f, titleY, -5f), 1.5f * u, TextAlignmentOptions.Left, filterWidth, Color.white);
        filterTitle.fontStyle = FontStyles.Bold;

        var divider = new GameObject("Achievement Panel Divider") { layer = 5 };
        divider.transform.SetParent(Menu.transform, false);
        divider.transform.localPosition = new Vector3(dividerX, 0f, -5f);
        var dividerRenderer = divider.AddComponent<SpriteRenderer>();
        dividerRenderer.sprite = LineSprite;
        dividerRenderer.color = new Color(1f, 1f, 1f, 0.8f);
        divider.transform.localScale = new Vector3(0.03f, hh * 1.75f, 1f);

        var sliderTemplate = AccountManager.Instance.transform.FindChild("MainSignInWindow/SignIn/AccountsMenu/Accounts/Slider");
        if (sliderTemplate != null)
        {
            var listTop = hh - 1f * u;
            var listBottom = -hh + 0.3f * u;
            Slider = Object.Instantiate(sliderTemplate.gameObject, Menu.transform);
            Slider.name = "Achievements Slider";
            Slider.transform.localPosition = new Vector3((listLeft + listRight) / 2f, (listTop + listBottom) / 2f, -1f);
            Slider.transform.localScale = new Vector3(1f, 1f, 1f);
            Slider.GetComponent<SpriteRenderer>().color = new Color(0f, 0f, 0f, 0f);
            var scroller = Slider.GetComponent<Scroller>();
            scroller.ScrollWheelSpeed = 0.3f;
            var mask = Slider.transform.FindChild("Mask");
            ListWidth = listRight - listLeft;
            ListHeight = listTop - listBottom;
            mask.transform.localScale = new Vector3(ListWidth, ListHeight, 1f);
            var maskRenderer = mask.GetComponent<SpriteRenderer>();
            if (maskRenderer != null) maskRenderer.color = new Color(0f, 0f, 0f, 0f);
        }

        BuildFilterButtons(mouseMoveToggle, columnLeft, columnRight, hh);
        BuildSearchBox(mouseMoveToggle, columnCenter, columnLeft, columnRight, hh);

        Menu.SetActive(false);
    }

    private static void BuildFilterButtons(ToggleButtonBehaviour template, float columnLeft, float columnRight, float halfHeight)
    {
        var old = FilterButtons.ToList();
        FilterButtons.Clear();
        old.Do(x => { if (x != null) Object.Destroy(x.gameObject); });

        var filters = new List<ListFilter> { ListFilter.Locked, ListFilter.Unlocked, ListFilter.All };
        var width = (columnRight - columnLeft) * 0.5f;

        for (int i = 0; i < filters.Count; i++)
        {
            var filter = filters[i];
            var y = halfHeight - 1.3f * Unit - 0.62f * Unit * i;
            var button = MakeButton("Achievement Filter " + filter, GetFilterText(filter), new Vector3(columnRight - width / 2f, y, -5f), width,
                filter == CurrentFilter ? Main.ModColor32 : Palette.DisabledGrey, template);
            button.GetComponent<PassiveButton>().OnClick.AddListener(new Action(() =>
            {
                CurrentFilter = filter;
                BuildFilterButtons(template, columnLeft, columnRight, halfHeight);
                RefreshList();
            }));
            FilterButtons.Add(button);
        }
    }

    private static void BuildSearchBox(ToggleButtonBehaviour template, float columnCenter, float columnLeft, float columnRight, float halfHeight)
    {
        if (SearchBox == null)
        {
            var boxTemplate = AccountManager.Instance.transform.FindChild("PremissionRequestWindow/GuardianEmailConfirm");
            if (boxTemplate == null)
            {
                Logger.Warn("找不到 GuardianEmailConfirm 模板，搜索框跳过", "AchievementPanel");
                return;
            }
            var box = Object.Instantiate(boxTemplate.gameObject, Menu.transform);
            box.name = "Achievement Search Box";
            SearchBox = box.GetComponent<TextBoxTMP>();
            Object.Destroy(box.GetComponent<EmailTextBehaviour>());
            SearchBox.allowAllCharacters = true;
            SearchBox.AllowEmail = false;
            SearchBox.AllowSymbols = true;
            SearchBox.AllowPaste = true;

            var widest = 0f;
            foreach (var child in box.GetComponentsInChildren<SpriteRenderer>(true))
                widest = Mathf.Max(widest, VisibleSize(child).x);
            var boxScale = widest > 0f ? (columnRight - columnLeft) / widest : 1f;
            box.transform.localScale = new Vector3(boxScale, boxScale, 1f);
            box.transform.localPosition = new Vector3(columnCenter, -halfHeight + 1.1f * Unit, -5f);

            SearchHint = MakeText(Menu.transform, "Search Hint", GetString("Achievement.Search"),
                box.transform.localPosition + Vector3.back * 0.5f, 1f * Unit, TextAlignmentOptions.Center, columnRight - columnLeft, new Color(1f, 1f, 1f, 0.45f));
        }

        MakeButton("Achievement Confirm", GetString(StringNames.Confirm), new Vector3(columnCenter, -halfHeight + 0.42f * Unit, -5f), (columnRight - columnLeft) * 0.5f,
            new Color(1f, 1f, 1f, 0.3f), template).GetComponent<PassiveButton>().OnClick.AddListener(new Action(() =>
        {
            SearchText = SearchBox?.text?.Trim() ?? "";
            RefreshList();
        }));
    }

    public static void RefreshList()
    {
        if (Slider == null || Menu == null) return;

        var scroller = Slider.GetComponent<Scroller>();
        scroller.Inner.gameObject.ForEachChild((Action<GameObject>)(DestroyObj));
        static void DestroyObj(GameObject obj)
        {
            if (obj.name.StartsWith("Achievement Item") || obj.name.StartsWith("Achievement Row")) Object.Destroy(obj);
        }

        var numberSetter = AccountManager.Instance.transform.FindChild("DOBEnterScreen/EnterAgePage/MonthMenu/Months").GetComponent<NumberSetter>();
        var buttonPrefab = numberSetter.ButtonPrefab.gameObject;
        var rowPrefabSize = VisibleSize(buttonPrefab.GetComponent<SpriteRenderer>());

        var oldItems = Items.ToList();
        Items.Clear();
        oldItems.Do(x => { if (x != null) Object.Destroy(x); });

        var mask = Slider.transform.FindChild("Mask") ?? Slider.transform;
        var inner = scroller.Inner;

        var equippedId = TONX.Modules.Achievements.Core.AchievementManager.GetWearedTitleId(CustomAchievementDataManager.LocalFriendCode);
        var localData = CustomAchievementDataManager.GetLocal();

        var list = AchievementRegistry.GetAll()
            .Where(a => Matches(a, localData))
            .OrderBy(a => IsUnlocked(a, localData) ? 0 : 1)
            .ThenBy(a => (int)a.Rarity * -1)
            .ThenBy(a => a.Id);

        var listTop = mask.position.y + ListHeight / 2f;
        var rowPitch = 0.72f * Unit;
        var titleGap = 0.26f * Unit;
        var lineGap = 0.32f * Unit;

        int rows = 0;
        foreach (var achievement in list)
        {
            rows++;
            var rowCenterY = listTop - rowPitch * (rows - 0.5f);
            var titleY = rowCenterY + titleGap;
            var rowPoint = inner.InverseTransformPoint(new Vector3(mask.position.x, rowCenterY, mask.position.z));

            var row = Object.Instantiate(buttonPrefab, inner);
            row.name = "Achievement Item " + achievement.Id;
            Object.Destroy(row.GetComponent<UIScrollbarHelper>());
            Object.Destroy(row.GetComponent<NumberButton>());
            Object.Destroy(row.transform.GetChild(0).gameObject);
            var renderer = row.GetComponent<SpriteRenderer>();
            row.transform.localScale = new Vector3(
                rowPrefabSize.x > 0f ? ListWidth / rowPrefabSize.x : 1f,
                rowPrefabSize.y > 0f ? rowPitch * 0.94f / rowPrefabSize.y : 1f, 1f);
            row.transform.localPosition = new Vector3(rowPoint.x, rowPoint.y, -0.5f);
            renderer.color = Color.clear;
            var rollover = row.GetComponent<ButtonRolloverHandler>();
            rollover.OverColor = Color.clear;
            rollover.OutColor = Color.clear;

            var passive = row.GetComponent<PassiveButton>();
            passive.OnClick = new();
            var captured = achievement;
            passive.OnClick.AddListener(new Action(() => OnClicked(captured)));
            Items.Add(row);

            var visible = IsUnlocked(achievement, localData) || !achievement.Hidden;
            var title = MakeText(inner, "Achievement Row Title " + achievement.Id, titleY, 1f * Unit, TextAlignmentOptions.Left, Color.white);
            title.text = $"{GetAchievementName(achievement, localData)}  <size=72%><color={achievement.Rarity.GetColorCode()}>{achievement.Rarity.GetDisplayName()}</color></size>";
            Items.Add(title.gameObject);

            var description = MakeText(inner, "Achievement Row Description " + achievement.Id, titleY - lineGap, 0.72f * Unit, TextAlignmentOptions.Left, new Color(1f, 1f, 1f, 0.6f));
            description.text = visible
                ? $"{achievement.Description}{(achievement.IsProgressAchievement ? $"  <size=80%>{GetString("Achievement.Progress")}（{GetProgress(achievement, localData)}/{achievement.RequiredValue}）</size>" : "")}"
                : GetString("Achievement.HiddenDescription");
            Items.Add(description.gameObject);

            var state = MakeText(inner, "Achievement Row State " + achievement.Id, titleY, 0.9f * Unit, TextAlignmentOptions.Right, StateColor(achievement, equippedId, localData));
            state.text = StateText(achievement, equippedId, localData);
            Items.Add(state.gameObject);
        }

        scroller.SetYBoundsMin(0f);
        scroller.SetYBoundsMax(Mathf.Max(0f, rowPitch * rows - ListHeight));
    }

    private static bool IsUnlocked(AchievementBase achievement, CustomAchievementData data)
        => data != null && data.Has(achievement.Id);

    private static int GetProgress(AchievementBase achievement, CustomAchievementData data)
        => data == null ? 0 : data.GetProgress(achievement.Id);

    private static string GetAchievementName(AchievementBase achievement, CustomAchievementData data)
        => IsUnlocked(achievement, data) || !achievement.Hidden ? achievement.Name : GetString("Achievement.HiddenName");

    private static bool Matches(AchievementBase achievement, CustomAchievementData data)
    {
        bool unlocked = IsUnlocked(achievement, data);
        var passFilter = CurrentFilter switch
        {
            ListFilter.Locked => !unlocked,
            ListFilter.Unlocked => unlocked,
            _ => true
        };
        if (!passFilter) return false;
        if (string.IsNullOrEmpty(SearchText)) return true;
        return achievement.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || achievement.TitleDisplay.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetFilterText(ListFilter filter) => filter switch
    {
        ListFilter.Locked => GetString("Achievement.LockedMark"),
        ListFilter.Unlocked => GetString("Achievement.UnlockedMark"),
        _ => GetString("Achievement.Filter.All")
    };

    private static string StateText(AchievementBase achievement, int equippedId, CustomAchievementData data)
    {
        if (!IsUnlocked(achievement, data)) return GetString("Achievement.LockedMark");
        return equippedId == achievement.Id ? GetString("Achievement.Equipped") : GetString("Achievement.UnlockedMark");
    }

    private static Color StateColor(AchievementBase achievement, int equippedId, CustomAchievementData data)
    {
        if (!IsUnlocked(achievement, data)) return Palette.DisabledGrey;
        if (equippedId == achievement.Id) return (Color)Main.ModColor32;
        return UnlockedColor;
    }

    private static void OnClicked(AchievementBase achievement)
    {
        var localData = CustomAchievementDataManager.GetLocal();
        if (!IsUnlocked(achievement, localData)) return;

        var equippedId = TONX.Modules.Achievements.Core.AchievementManager.GetWearedTitleId(CustomAchievementDataManager.LocalFriendCode);
        var equipped = equippedId == achievement.Id;

        var body = equipped
            ? GetString("Achievement.UnequipConfirm")
            : string.Format(GetString("Achievement.EquipConfirm"), $"<color={achievement.TitleColorHex}>{achievement.TitleDisplay}</color>");

        AchievementConfirmWindow.Open(body, () => Wear(achievement, !equipped));
    }

        private static void Wear(AchievementBase achievement, bool equip)
        => _ = WearAsync(achievement, equip);

    private static async Task WearAsync(AchievementBase achievement, bool equip)
    {
        var friendCode = CustomAchievementDataManager.LocalFriendCode;
        int titleId = equip ? achievement.Id : 0;

        if (!await TONX.Modules.Achievements.Core.AchievementManager.WearPendingAchievementTitle(friendCode, titleId))
            Logger.Warn($"头衔上传失败：{friendCode} -> {titleId}", "AchievementPanel");

        if (NameTagInterface.NameTagPanel.Slider != null) NameTagInterface.NameTagPanel.RefreshTagList();
        _ = new LateTask(()=>
        {
            RefreshList();
        }, 0f, "RefreshAchievementPanel");
    }
    
    private static Vector2 VisibleSize(SpriteRenderer renderer)
    {
        if (renderer == null) return Vector2.zero;
        if (renderer.drawMode != SpriteDrawMode.Simple && renderer.size.x > 0f && renderer.size.y > 0f)
            return renderer.size;
        return renderer.sprite == null ? Vector2.zero : (Vector2)renderer.sprite.bounds.size;
    }

    private static Sprite _lineSprite;
    private static Sprite LineSprite
    {
        get
        {
            if (_lineSprite != null) return _lineSprite;
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            texture.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
            _lineSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            _lineSprite.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
            return _lineSprite;
        }
    }
    
    private static TextMeshPro MakeText(Transform parent, string name, float y, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        var mask = Slider.transform.FindChild("Mask") ?? Slider.transform;
        var point = parent.InverseTransformPoint(new Vector3(mask.position.x, y, mask.position.z));
        return MakeText(parent, name, "", new Vector3(point.x, point.y, -0.5f), fontSize, alignment, ListWidth, color);
    }

    private static TextMeshPro MakeText(Transform parent, string name, string text, Vector3 position, float fontSize, TextAlignmentOptions alignment, float width, Color color)
    {
        var obj = Object.Instantiate(CustomPopup.InfoTMP.gameObject, parent);
        obj.name = name;
        obj.transform.localPosition = position;
        obj.transform.localScale = Vector3.one;
        obj.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 1f);
        var tmp = obj.GetComponent<TextMeshPro>();
        tmp.DestroyTranslator();
        tmp.text = text;
        tmp.alignment = alignment;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.enableWordWrapping = false;
        return tmp;
    }

    private static ToggleButtonBehaviour MakeButton(string name, string text, Vector3 position, float width, Color color, ToggleButtonBehaviour template)
    {
        var button = Object.Instantiate(template, Menu.transform);
        button.name = name;
        button.transform.localPosition = position;
        button.transform.localScale = Vector3.one;
        button.Text.DestroyTranslator();
        button.Text.text = text;
        button.Background.color = color;
        button.GetComponent<PassiveButton>().OnClick = new();
        var buttonSize = VisibleSize(button.Background);
        if (buttonSize.x > 0f)
        {
            var scale = width / buttonSize.x;
            button.transform.localScale = new Vector3(scale, scale, 1f);
        }
        return button;
    }
}
