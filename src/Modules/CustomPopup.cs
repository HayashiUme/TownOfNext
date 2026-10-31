using System;
using System.Linq;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

#pragma warning disable CS8602 // 解引用可能出现空引用。

namespace TONX;

#nullable enable
public static class CustomPopup
{
    public static GameObject? Fill;
    public static GameObject? InfoScreen;

    public static TextMeshPro? TitleTMP;
    public static TextMeshPro? InfoTMP;

    public static PassiveButton? ActionButtonPrefab;
    public static List<PassiveButton>? ActionButtons;

    private static bool busy;

    /// <summary>InfoScreen 的默认 localZ，与改动前的行为完全一致</summary>
    public const float DefaultLayer = -110f;

    /// <summary>Fill 相对 InfoScreen 向前偏移的量，保证遮罩压在页面下方</summary>
    private const float FillLayerOffset = 10f;

    /// <summary>
    /// 应用层级。localZ 越负越靠后，调用方传入更小的（更正的）值即可把弹窗提到前面。
    /// </summary>
    private static void ApplyLayer(float layer)
    {
        if (Fill != null) Fill.transform.SetLocalZ(layer + FillLayerOffset);
        if (InfoScreen != null) InfoScreen.transform.SetLocalZ(layer);
    }

    /// <summary>
    /// 显示一个全屏信息显示界面
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="info">内容</param>
    /// <param name="buttons">按钮（文字，点击事件）</param>
    /// <param name="layer">InfoScreen 的 localZ。默认 <see cref="DefaultLayer"/>；调用方需要弹窗盖在自己前面时传更小的值</param>
    public static void Show(string title, string info, List<(string, Action)>? buttons, float layer = DefaultLayer)
    {
        if (busy || !Fill || !InfoScreen || !ActionButtonPrefab || !TitleTMP || !InfoTMP)
        {
            Init();
            if (!Fill || !InfoScreen || !ActionButtonPrefab || !TitleTMP || !InfoTMP) return;
        }

        ApplyLayer(layer);

        busy = true;

        TitleTMP.text = title;
        InfoTMP.text = info;

        var oldButtons = ActionButtons?.ToList();
        ActionButtons = new();
        oldButtons?.Do(b => { if (b != null) Object.Destroy(b.gameObject); });

        if (buttons != null)
        {
            foreach (var buttonInfo in buttons.Where(b => b.Item1.Trim() is not null and not ""))
            {
                var (text, action) = buttonInfo;
                var button = Object.Instantiate(ActionButtonPrefab, InfoScreen.transform);
                var tmp = button.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>();
                tmp.text = text;
                button.OnClick = new();
                button.OnClick.AddListener((Action)(() =>
                {
                    InfoScreen.SetActive(false);
                    Fill.SetActive(false);
                }));
                if (action != null) button.OnClick.AddListener(action);
                button.transform.SetLocalX(0);
                button.gameObject.SetActive(true);
                ActionButtons.Add(button);
            }
        }

        if (ActionButtons.Count > 1)
        {
            float widthSum = ActionButtons.Count * ActionButtonPrefab.gameObject.GetComponent<BoxCollider2D>().size.x;
            widthSum += (ActionButtons.Count - 1) * 0.1f;
            float start = -Math.Abs(widthSum / 2);
            float each = widthSum / ActionButtons.Count;
            int index = 0;
            foreach (var button in ActionButtons)
            {
                button.transform.SetLocalX(start + each * (index + 0.5f));
                index++;
            }
        }

        Fill.SetActive(true);
        InfoScreen.SetActive(true);

        busy = false;
    }
    private static (string title, string info, List<(string, Action)>? buttons, float layer)? waitToShow;
    public static void ShowLater(string title, string info, List<(string, Action)>? buttons, float layer = DefaultLayer)
        => waitToShow = (title, info, buttons, layer);
    private static string waitToUpdateText = string.Empty;
    public static void UpdateTextLater(string info) => waitToUpdateText = info;
    public static void Update()
    {
        if (waitToShow != null)
        {
            Show(waitToShow.Value.title, waitToShow.Value.info, waitToShow.Value.buttons, waitToShow.Value.layer);
            waitToShow = null;
        }
        if (!string.IsNullOrEmpty(waitToUpdateText))
        {
            InfoTMP?.SetText(waitToUpdateText);
            waitToUpdateText = string.Empty;
        }
    }
    public static void Init()
    {
        var DOBScreen = AccountManager.Instance.transform.FindChild("DOBEnterScreen");
        if (DOBScreen != null && (Fill == null || InfoScreen == null || ActionButtons == null))
        {
            Fill = Object.Instantiate(DOBScreen.FindChild("Fill").gameObject);
            Fill.name = "TONX Info Popup Fill";
            Fill.SetActive(false);

            InfoScreen = Object.Instantiate(DOBScreen.FindChild("InfoPage").gameObject);
            InfoScreen.name = "TONX Info Popup Page";
            InfoScreen.SetActive(false);

            ApplyLayer(DefaultLayer);

            TitleTMP = InfoScreen.transform.FindChild("Title Text").GetComponent<TextMeshPro>();
            TitleTMP.transform.localPosition = new(0f, 2.3f, 3f);
            TitleTMP.DestroyTranslator();
            TitleTMP.text = "";

            InfoTMP = InfoScreen.transform.FindChild("InfoText_TMP").GetComponent<TextMeshPro>();
            InfoTMP.GetComponent<RectTransform>().sizeDelta = new(7f, 1.3f);
            InfoTMP.transform.localScale = new(1f, 1f, 1f);
            InfoTMP.DestroyTranslator();
            InfoTMP.text = "";

            ActionButtonPrefab = InfoScreen.transform.FindChild("BackButton").GetComponent<PassiveButton>();
            ActionButtonPrefab.gameObject.name = "ActionButtonPrefab";
            ActionButtonPrefab.transform.localScale = new(0.66f, 0.66f, 0.66f);
            ActionButtonPrefab.transform.localPosition = new(0f, -0.65f, 3f);
            ActionButtonPrefab.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>().DestroyTranslator();
            ActionButtonPrefab.gameObject.SetActive(false);
        }
    }
}