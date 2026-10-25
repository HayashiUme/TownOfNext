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

    public static void Open(string info, Action onConfirm)
    {
        if (Window == null) Init();
        if (Window == null) return;

        if (Info != null) Info.GetComponent<TextMeshPro>().text = info;
        onConfirmed = onConfirm;

        Window.transform.SetLocalZ(-30f);
        Window.SetActive(true);
    }

    public static void Close()
    {
        onConfirmed = null;
        if (Window != null) Window.SetActive(false);
    }

    public static void Init()
    {
        if (GameStates.IsInGame && !GameStates.IsLobby) return;

        Window = Object.Instantiate(AccountManager.Instance.transform.FindChild("InfoTextBox").gameObject, TONX.Modules.NameTagInterface.NameTagPanel.CustomBackground.transform.parent);
        Window.name = "Achievement Confirm Window";
        Window.transform.FindChild("Background").localScale *= 0.7f;
        
        Window.transform.FindChild("Button2").gameObject.SetActive(false);

        var closeButton = Object.Instantiate(Window.transform.parent.FindChild("CloseButton"), Window.transform);
        closeButton.transform.localPosition = new Vector3(2.4f, 1.2f, -1f);
        closeButton.transform.localScale = new Vector3(1f, 1f, 1f);
        closeButton.GetComponent<PassiveButton>().OnClick = new();
        closeButton.GetComponent<PassiveButton>().OnClick.AddListener(new Action(Close));

        var titlePrefab = Window.transform.FindChild("TitleText_TMP").gameObject;
        titlePrefab.name = "Title Prefab";
        var infoPrefab = Window.transform.FindChild("InfoText_TMP").gameObject;
        infoPrefab.name = "Info Prefab";
        var buttonPrefab = Window.transform.FindChild("Button1").gameObject;
        buttonPrefab.name = "Button Prefab";
        buttonPrefab.GetComponent<PassiveButton>().OnClick = new();

        Info = Object.Instantiate(infoPrefab, Window.transform);
        Info.name = "Confirm Description";
        Info.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        var infoTmp = Info.GetComponent<TextMeshPro>();
        infoTmp.text = string.Empty;

        ConfirmButton = Object.Instantiate(buttonPrefab, Window.transform);
        ConfirmButton.name = "Confirm Button";
        ConfirmButton.transform.localPosition = new Vector3(0, -0.8f, 0f);
        ConfirmButton.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() =>
        {
            var action = onConfirmed;
            Close();
            action?.Invoke();
        }));
        var upperButtonTmp = ConfirmButton.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>();
        upperButtonTmp.text = GetString(StringNames.Confirm);

        titlePrefab.SetActive(false);
        infoPrefab.SetActive(false);
        buttonPrefab.SetActive(false);

        Window.SetActive(false);
    }
}
