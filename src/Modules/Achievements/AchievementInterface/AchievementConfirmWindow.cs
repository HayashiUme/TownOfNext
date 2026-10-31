using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TONX.Modules.Achievements.AchievementInterface;

public static class AchievementConfirmWindow
{
    public static GameObject Window { get; private set; }
    public static GameObject Info { get; private set; }
    public static GameObject ConfirmButton { get; private set; }

    private static Action onConfirmed;
    private static PassiveButton confirmPassiveButton;
    private static bool isConfirming;

    public static void Open(string info, Action onConfirm)
    {
        if (Window == null) Init();
        if (Window == null) return;

        isConfirming = false;
        if (confirmPassiveButton != null) confirmPassiveButton.enabled = true;

        if (Info != null && Info.TryGetComponent<TextMeshPro>(out var infoText))
            infoText.text = info;
        onConfirmed = onConfirm;

        Window.transform.SetLocalZ(-30f);
        Window.SetActive(true);
    }

    public static void Close()
    {
        onConfirmed = null;
        isConfirming = false;
        if (confirmPassiveButton != null) confirmPassiveButton.enabled = true;
        if (Window != null) Window.SetActive(false);
    }

    public static void Init()
    {
        if (GameStates.IsInGame && !GameStates.IsLobby) return;

        if (AccountManager.Instance == null)
        {
            Logger.Warn("找不到 AccountManager，确认窗口初始化跳过", "AchievementConfirmWindow");
            return;
        }

        var infoTemplate = AccountManager.Instance.transform.FindChild("InfoTextBox");
        if (infoTemplate == null)
        {
            Logger.Warn("找不到 InfoTextBox 模板，确认窗口初始化跳过", "AchievementConfirmWindow");
            return;
        }

        var parent = TONX.Modules.NameTagInterface.NameTagPanel.CustomBackground;
        if (parent == null)
        {
            Logger.Warn("找不到成就面板父对象，确认窗口初始化跳过", "AchievementConfirmWindow");
            return;
        }

        Window = Object.Instantiate(infoTemplate.gameObject, parent.transform.parent);
        if (Window == null) return;

        Window.name = "Achievement Confirm Window";
        var background = Window.transform.FindChild("Background");
        var button2 = Window.transform.FindChild("Button2");
        var closeTemplate = Window.transform.parent?.FindChild("CloseButton");
        var titlePrefab = Window.transform.FindChild("TitleText_TMP");
        var infoPrefab = Window.transform.FindChild("InfoText_TMP");
        var buttonPrefab = Window.transform.FindChild("Button1");
        if (background == null || button2 == null || closeTemplate == null || titlePrefab == null ||
            infoPrefab == null || buttonPrefab == null)
        {
            Logger.Warn("确认窗口模板组件不完整", "AchievementConfirmWindow");
            Object.Destroy(Window);
            Window = null;
            return;
        }

        background.localScale *= 0.7f;
        
        button2.gameObject.SetActive(false);

        var closeButton = Object.Instantiate(closeTemplate, Window.transform);
        if (closeButton == null || !closeButton.TryGetComponent<PassiveButton>(out var closePassiveButton))
        {
            Logger.Warn("确认窗口关闭按钮组件缺失", "AchievementConfirmWindow");
            Object.Destroy(Window);
            Window = null;
            return;
        }
        closeButton.transform.localPosition = new Vector3(2.4f, 1.2f, -1f);
        closeButton.transform.localScale = new Vector3(1f, 1f, 1f);
        closePassiveButton.OnClick = new();
        closePassiveButton.OnClick.AddListener(new Action(Close));

        var titleObject = titlePrefab.gameObject;
        titlePrefab.name = "Title Prefab";
        var infoObject = infoPrefab.gameObject;
        infoPrefab.name = "Info Prefab";
        var buttonObject = buttonPrefab.gameObject;
        buttonPrefab.name = "Button Prefab";
        if (!buttonObject.TryGetComponent<PassiveButton>(out var buttonPassiveButton))
        {
            Logger.Warn("确认窗口确认按钮组件缺失", "AchievementConfirmWindow");
            Object.Destroy(Window);
            Window = null;
            return;
        }
        buttonPassiveButton.OnClick = new();

        Info = Object.Instantiate(infoObject, Window.transform);
        if (Info == null || !Info.TryGetComponent<TextMeshPro>(out var infoTmp))
        {
            Logger.Warn("确认窗口说明文本组件缺失", "AchievementConfirmWindow");
            Object.Destroy(Window);
            Window = null;
            Info = null;
            return;
        }
        Info.name = "Confirm Description";
        Info.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        infoTmp.text = string.Empty;

        ConfirmButton = Object.Instantiate(buttonObject, Window.transform);
        if (ConfirmButton == null || !ConfirmButton.TryGetComponent<PassiveButton>(out confirmPassiveButton))
        {
            Logger.Warn("确认窗口按钮实例化失败", "AchievementConfirmWindow");
            Object.Destroy(Window);
            Window = null;
            Info = null;
            ConfirmButton = null;
            return;
        }
        ConfirmButton.name = "Confirm Button";
        ConfirmButton.transform.localPosition = new Vector3(0, -0.8f, 0f);
        confirmPassiveButton.OnClick.AddListener((Action)(() =>
        {
            if (isConfirming) return;
            isConfirming = true;
            confirmPassiveButton.enabled = false;
            var action = onConfirmed;
            onConfirmed = null;
            if (Window != null) Window.SetActive(false);
            action?.Invoke();
        }));
        var upperButtonText = ConfirmButton.transform.FindChild("Text_TMP");
        if (upperButtonText != null && upperButtonText.TryGetComponent<TextMeshPro>(out var upperButtonTmp))
            upperButtonTmp.text = GetString(StringNames.Confirm);

        titleObject.SetActive(false);
        infoObject.SetActive(false);
        buttonObject.SetActive(false);

        Window.SetActive(false);
    }
}
