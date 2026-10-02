using BepInEx.Unity.IL2CPP.Utils.Collections;
using System.Collections;
using TMPro;
using TONX.Modules.Achievements.Core;
using TONX.Modules.Achievements.Core.Base;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TONX.Modules.Achievements.AchievementInterface;

public static class AchievementToast
{
    private static readonly Queue<(AchievementBase achievement, bool unlocked)> Pending = new();
    private static bool _showing;

    private static GameObject _toast;
    private static TextMeshPro _title;
    private static TextMeshPro _body;

    private const float ShowSeconds = 2.6f;

    public static void Enqueue(AchievementBase achievement, bool unlocked = false)
    {
        if (achievement == null) return;
        Pending.Enqueue((achievement, unlocked));
        if (!_showing) AmongUsClient.Instance.StartCoroutine(CoPlayQueue().WrapToIl2Cpp());
    }

    private static IEnumerator CoPlayQueue()
    {
        _showing = true;
        try
        {
            while (Pending.Count > 0)
            {
                var (achievement, unlocked) = Pending.Dequeue();
                if (TryShow(achievement, unlocked))
                    yield return CoAnimate();
                else
                    PlayerControl.LocalPlayer?.ShowPopUp(FormatText(achievement, unlocked));
            }
        }
        finally
        {
            _showing = false;
            DestroyToast();
        }
    }

    private static string FormatText(AchievementBase achievement, bool unlocked)
    {
        string color = achievement.Rarity.GetColorCode();
        string title = unlocked ? GetString("Achievement.Toast.Unlocked") : GetString("Achievement.Toast.Progress");
        string progress = achievement.IsProgressAchievement ? $"  ({achievement.Progress}/{achievement.RequiredValue})" : "";
        return $"<color={color}>{title}</color>\n<color={achievement.TitleColorHex}>{achievement.Name}</color>{progress}";
    }

    private static bool TryShow(AchievementBase achievement, bool unlocked)
    {
        if (_toast == null) BuildToast();
        if (_toast == null) return false;

        var hud = DestroyableSingleton<HudManager>.Instance;
        if (hud == null || PlayerControl.LocalPlayer == null) return false;

        _toast.transform.SetParent(hud.transform, false);
        _toast.transform.localPosition = new Vector3(0f, -2.9f, -60f);
        _toast.SetActive(true);

        string color = achievement.Rarity.GetColorCode();
        string title = unlocked ? GetString("Achievement.Toast.Unlocked") : GetString("Achievement.Toast.Progress");
        string progress = achievement.IsProgressAchievement ? $"  ({achievement.Progress}/{achievement.RequiredValue})" : "";
        _title.text = $"<color={color}>{title}</color>  <size=70%>{achievement.Rarity.GetDisplayName()}</size>";
        _body.text = $"<color={achievement.TitleColorHex}>{achievement.Name}</color>{progress}";
        return true;
    }

    private static void BuildToast()
    {
        var source = AccountManager.Instance?.transform.FindChild("DOBEnterScreen/InfoPage");
        if (source == null) return;

        _toast = Object.Instantiate(source.gameObject);
        _toast.name = "TONX Achievement Toast";
        foreach (var button in _toast.GetComponentsInChildren<PassiveButton>(true))
            Object.Destroy(button.gameObject);
        _toast.transform.localScale = new Vector3(0.42f, 0.35f, 1f);

        _title = _toast.transform.FindChild("Title Text")?.GetComponent<TextMeshPro>();
        _body  = _toast.transform.FindChild("InfoText_TMP")?.GetComponent<TextMeshPro>();
        if (_title == null || _body == null)
        {
            Object.Destroy(_toast);
            _toast = null;
            return;
        }

        _title.DestroyTranslator();
        _body.DestroyTranslator();
        _body.GetComponent<RectTransform>().sizeDelta = new(6.4f, 0.9f);
        _toast.SetActive(false);
    }

    private static IEnumerator CoAnimate()
    {
        const float slide = 0.35f;
        var target = _toast.transform.localPosition;

        _toast.transform.localPosition = target + new Vector3(0f, -0.9f, 0f);
        for (float t = 0f; t < slide; t += Time.deltaTime)
        {
            _toast.transform.localPosition = Vector3.Lerp(_toast.transform.localPosition, target, t / slide);
            yield return null;
        }
        _toast.transform.localPosition = target;

        yield return new WaitForSeconds(ShowSeconds);

        var off = target + new Vector3(0f, -0.9f, 0f);
        for (float t = 0f; t < slide; t += Time.deltaTime)
        {
            _toast.transform.localPosition = Vector3.Lerp(_toast.transform.localPosition, off, t / slide);
            yield return null;
        }
        _toast.SetActive(false);
    }

    private static void DestroyToast()
    {
        if (_toast != null) _toast.SetActive(false);
    }
}
