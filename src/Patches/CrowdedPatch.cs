using System.Diagnostics.CodeAnalysis;
using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TONX.Patches;

// https://github.com/All-Of-Us-Mods/Overloaded
internal static class CrowdedPatch
{
    public static int MaxImpostors => GameOptionsManager.Instance.currentHostOptions.MaxPlayers / 2;
    private const int MaxPlayers = 127;

    private static GameOptionButton _minButton = null!;
    private static GameOptionButton _maxButton = null!;
    private static GameOptionButton _doubleMinusButton = null!;
    private static GameOptionButton _doublePlusButton = null!;

    [HarmonyPatch(typeof(CreateGameOptions), nameof(CreateGameOptions.Show))]
    public static class CreateGameOptions_Show
    {
        public static void Prefix(CreateGameOptions __instance)
        {
            var newRange = new IntRange(1, MaxPlayers);
            var newFloatRange = new FloatRange(1, MaxPlayers);
            __instance.capacitySetting.ValidRange = newRange;
            __instance.capacityOption.ValidRange = newFloatRange;
        }
    }

    [HarmonyPatch(typeof(CreateGameOptions), nameof(CreateGameOptions.Start))]
    public static class CreateGameOptions_Start
    {
        public static void Postfix(CreateGameOptions __instance)
        {
            foreach (Il2CppSystem.Object obj in __instance.capacityOption.transform)
            {
                var t = obj.Cast<Transform>();
                if (!t || t.gameObject.name is "LabelBackground" or "Title Text") continue;
                t.localPosition += new Vector3(1, 0, 0);
            }

            _doubleMinusButton = Object.Instantiate(__instance.capacityOption.MinusBtn, __instance.capacityOption.MinusBtn.transform.parent);
            var dmText = _doubleMinusButton.GetComponentInChildren<TextMeshPro>();
            dmText.text = "-5";
            dmText.fontStyle = FontStyles.Normal;
            _doubleMinusButton.transform.localPosition -= new Vector3(0.5f, 0, 0);
            _doubleMinusButton.OnClick = new Button.ButtonClickedEvent();
            _doubleMinusButton.OnClick.AddListener((UnityAction)(() =>
            {
                __instance.capacityOption.Increment = 5;
                __instance.capacityOption.Decrease();
                __instance.capacityOption.Increment = 1;
            }));

            _doublePlusButton = Object.Instantiate(__instance.capacityOption.PlusBtn, __instance.capacityOption.PlusBtn.transform.parent);
            var dpText = _doublePlusButton.GetComponentInChildren<TextMeshPro>();
            dpText.text = "+5";
            dpText.fontStyle = FontStyles.Normal;
            _doublePlusButton.transform.localPosition += new Vector3(0.5f, 0, 0);
            _doublePlusButton.OnClick = new Button.ButtonClickedEvent();
            _doublePlusButton.OnClick.AddListener((UnityAction)(() =>
            {
                __instance.capacityOption.Increment = 5;
                __instance.capacityOption.Increase();
                __instance.capacityOption.Increment = 1;
            }));

            _minButton = Object.Instantiate(__instance.capacityOption.MinusBtn, __instance.capacityOption.MinusBtn.transform.parent);
            var minText = _minButton.GetComponentInChildren<TextMeshPro>();
            minText.text = "1";
            minText.fontStyle = FontStyles.Normal;
            _minButton.SetInteractable(true);
            _minButton.transform.localPosition -= new Vector3(1, 0, 0);
            _minButton.OnClick = new Button.ButtonClickedEvent();
            _minButton.OnClick.AddListener((UnityAction)(() =>
            {
                __instance.capacityOption.Increment = int.MaxValue;
                __instance.capacityOption.Decrease();
                __instance.capacityOption.Increment = 1;
            }));

            _maxButton = Object.Instantiate(__instance.capacityOption.PlusBtn, __instance.capacityOption.PlusBtn.transform.parent);
            var maxText = _maxButton.GetComponentInChildren<TextMeshPro>();
            maxText.text = MaxPlayers.ToString();
            maxText.fontSize = maxText.fontSizeMax = 3;
            maxText.fontStyle = FontStyles.Normal;
            _maxButton.SetInteractable(true);
            _maxButton.transform.localPosition += new Vector3(1, 0, 0);
            _maxButton.OnClick = new Button.ButtonClickedEvent();
            _maxButton.OnClick.AddListener((UnityAction)(() =>
            {
                __instance.capacityOption.Increment = int.MaxValue;
                __instance.capacityOption.Increase();
                __instance.capacityOption.Increment = 1;
            }));
        }
    }

    [HarmonyPatch(typeof(CreateGameOptions), nameof(CreateGameOptions.ValueChanged))]
    public static class CreateGameOptions_ValueChanged
    {
        public static void Postfix(OptionBehaviour option)
        {
            var numOpt = option.Cast<NumberOption>();
            if (!numOpt) return;

            _minButton.SetInteractable(true);
            _maxButton.SetInteractable(true);
            _doubleMinusButton.SetInteractable(true);
            _doublePlusButton.SetInteractable(true);

            if (Mathf.Approximately(numOpt.Value, numOpt.ValidRange.max))
            {
                _maxButton.SetInteractable(false);
                _doublePlusButton.SetInteractable(false);
            }
            else if (Mathf.Approximately(numOpt.Value, numOpt.ValidRange.min))
            {
                _minButton.SetInteractable(false);
                _doubleMinusButton.SetInteractable(false);
            }
        }
    }

    [HarmonyPatch(typeof(NormalGameOptionsV11), nameof(NormalGameOptionsV11.AreInvalid))]
    public static class InvalidOptionsPatches
    {
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public static bool Prefix(NormalGameOptionsV11 __instance, [HarmonyArgument(0)] int maxExpectedPlayers)
        {
            return __instance.NumImpostors < 1 ||
                   __instance.KillDistance is < 0 or > 2 ||
                   __instance.PlayerSpeedMod is <= 0f or > 3f;
        }
    }

    [HarmonyPatch(typeof(NormalGameOptionsV11), nameof(NormalGameOptionsV11.TryGetIntArray))]
    public static class NormalGameOptionsV11_TryGetIntArray
    {
        private static readonly int[] ExpandedMaxImpostors;
        private static readonly int[] ExpandedMinPlayers;

        static NormalGameOptionsV11_TryGetIntArray()
        {
            ExpandedMaxImpostors = new int[128];
            ExpandedMinPlayers = new int[128];

            int[] origMaxImp = { 0, 0, 0, 0, 1, 1, 1, 2, 2, 3, 3, 3, 3, 3, 3, 3 };
            for (int i = 0; i < 128; i++)
                ExpandedMaxImpostors[i] = i < origMaxImp.Length ? origMaxImp[i] : 3;

            int[] origMinPlayers = { 4, 4, 7, 9 };
            for (int i = 0; i < 128; i++)
                ExpandedMinPlayers[i] = i < origMinPlayers.Length ? origMinPlayers[i] : 4;
        }

        public static bool Prefix(Int32ArrayOptionNames optionName, ref int[] value)
        {
            if (optionName == Int32ArrayOptionNames.MaxImpostors)
            {
                value = ExpandedMaxImpostors;
                return false;
            }
            if (optionName == Int32ArrayOptionNames.MinPlayers)
            {
                value = ExpandedMinPlayers;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(NormalGameOptionsV11), nameof(NormalGameOptionsV11.SetRecommendations), typeof(int), typeof(bool))]
    public static class NormalGameOptionsV11_SetRecommendations
    {
        public static bool Prefix(NormalGameOptionsV11 __instance, int numPlayers, bool isOnline)
        {
            int safeIndex = Mathf.Clamp(numPlayers, 0, 127);

            int[] origRecImp = { 0, 0, 0, 0, 1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 3 };
            int[] origRecKill = { 0, 0, 0, 0, 45, 30, 15, 35, 30, 25, 20, 20, 20, 20, 20, 20 };

            if (!isOnline)
            {
                __instance.NumImpostors = safeIndex < origRecImp.Length ? origRecImp[safeIndex] : Mathf.Clamp(safeIndex / 4, 1, 3);
            }
            __instance.ConfirmImpostor = true;
            __instance.NumEmergencyMeetings = 1;
            __instance.EmergencyCooldown = isOnline ? 15 : 0;
            __instance.DiscussionTime = 15;
            __instance.VotingTime = 120;
            __instance.AnonymousVotes = false;
            __instance.PlayerSpeedMod = 1f;
            __instance.CrewLightMod = 1f;
            __instance.ImpostorLightMod = 1.5f;
            __instance.KillCooldown = safeIndex < origRecKill.Length ? origRecKill[safeIndex] : 20f;
            __instance.KillDistance = 1;
            __instance.VisualTasks = true;
            __instance.TaskBarMode = AmongUs.GameOptions.TaskBarMode.Normal;
            __instance.NumCommonTasks = 1;
            __instance.NumLongTasks = 1;
            __instance.NumShortTasks = 2;
            return false;
        }
    }

    [HarmonyPatch(typeof(CreateGameOptions), nameof(CreateGameOptions.Confirm))]
    public static class CreateGameOptions_Confirm
    {
        public static bool Prefix(CreateGameOptions __instance)
        {
            if (!DestroyableSingleton<MatchMaker>.Instance.Connecting<CreateGameOptions>(__instance))
                return false;

            var opts = GameOptionsManager.Instance.GameHostOptions;
            int maxPlayers = opts.MaxPlayers;

            var safeIndex = Mathf.Clamp(maxPlayers, 0, 127);

            if (opts.NumImpostors > safeIndex / 2)
            {
                opts.SetInt(Int32OptionNames.NumImpostors, Mathf.Max(safeIndex / 2, 1));
            }
            if (opts.NumImpostors == 0)
            {
                opts.SetInt(Int32OptionNames.NumImpostors, 1);
            }
            GameOptionsManager.Instance.GameHostOptions = opts;

            __instance.CoStartGame();
            return false;
        }
    }

    [HarmonyPatch(typeof(SecurityLogger), nameof(SecurityLogger.Awake))]
    public static class SecurityLoggerPatch
    {
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public static void Postfix(ref SecurityLogger __instance)
        {
            __instance.Timers = new Il2CppStructArray<float>(127);
        }
    }

    [HarmonyPatch(typeof(PlayerTab), nameof(PlayerTab.UpdateAvailableColors))]
    public static class PlayerTabUpdateAvailableColorsPatch
    {
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public static bool Prefix(PlayerTab __instance)
        {
            if (GameOptionsManager.Instance.CurrentGameOptions.MaxPlayers <= 15) return true;

            __instance.AvailableColors.Clear();

            for (var i = 0; i < Palette.PlayerColors.Count; i++)
            {
                if (!PlayerControl.LocalPlayer || PlayerControl.LocalPlayer.CurrentOutfit.ColorId != i)
                    __instance.AvailableColors.Add(i);
            }

            return false;
        }
    }

    public static class MeetingHudStartPatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            if (Main.NormalOptions.MaxPlayers <= 15) return;
            __instance.gameObject.AddComponent<MeetingHudPagingBehaviour>().meetingHud = __instance;
        }
    }

    [HarmonyPatch(typeof(ShapeshifterMinigame), nameof(ShapeshifterMinigame.Begin))]
    public static class ShapeshifterMinigameBeginPatch
    {
        public static void Postfix(ShapeshifterMinigame __instance)
        {
            if (Main.NormalOptions.MaxPlayers <= 15) return;
            __instance.gameObject.AddComponent<ShapeShifterPagingBehaviour>().shapeshifterMinigame = __instance;
        }
    }

    [HarmonyPatch(typeof(VitalsMinigame), nameof(VitalsMinigame.Begin))]
    public static class VitalsMinigameBeginPatch
    {
        public static void Postfix(VitalsMinigame __instance)
        {
            if (Main.NormalOptions.MaxPlayers <= 15) return;
            __instance.gameObject.AddComponent<VitalsPagingBehaviour>().vitalsMinigame = __instance;
        }
    }
}

public class AbstractPagingBehaviour : MonoBehaviour
{
    public AbstractPagingBehaviour(IntPtr ptr) : base(ptr) { }

    protected const string PageIndexGameObjectName = "CrowdedMod_PageIndex";

    private int _page;

    protected static int MaxPerPage => 15;

    public virtual int PageIndex
    {
        get => _page;
        set
        {
            _page = value;
            OnPageChanged();
        }
    }

    protected virtual int MaxPageIndex => throw new("MaxPageIndex must be overridden");

    public virtual void Start()
    {
        OnPageChanged();
    }

    public virtual void Update()
    {
        bool chatIsOpen = HudManager.Instance.Chat.IsOpenOrOpening;
        bool gameMenuIsOpen = HudManager.Instance.GameMenu.IsOpen;
        
        if (Input.touchSupported)
        {
            foreach (Touch touch in Input.touches)
            {
                if (touch.phase != TouchPhase.Moved) continue;
                if (chatIsOpen || gameMenuIsOpen) break;

                if (touch.deltaPosition.y > 0f)
                {
                    Cycle(false);
                    break;
                }
                if (touch.deltaPosition.y < 0f)
                {
                    Cycle(true);
                    break;
                }
            }
        }

        if (!chatIsOpen && !gameMenuIsOpen && (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.mouseScrollDelta.y > 0f))
            Cycle(false);
        else if (!chatIsOpen && !gameMenuIsOpen && (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.RightArrow) || Input.mouseScrollDelta.y < 0f))
            Cycle(true);
    }

    public virtual void OnPageChanged()
    {
        throw new("OnPageChanged must be overridden");
    }

    public virtual void Cycle(bool increment)
    {
        int change = increment ? 1 : -1;
        PageIndex = Mathf.Clamp(PageIndex + change, 0, MaxPageIndex);
    }
}

public class MeetingHudPagingBehaviour : AbstractPagingBehaviour
{
    public MeetingHudPagingBehaviour(IntPtr ptr) : base(ptr) { }

    internal MeetingHud meetingHud = null!;
    [HideFromIl2Cpp] private IEnumerable<PlayerVoteArea> Targets => meetingHud.playerStates.OrderBy(p => p.AmDead);

    protected override int MaxPageIndex => (Targets.Count() - 1) / MaxPerPage;

    public override void Start()
    {
        OnPageChanged();
    }

    public override void Update()
    {
        base.Update();
        if (meetingHud.state is MeetingHud.MeetingStates.Animating or MeetingHud.MeetingStates.Proceeding || meetingHud.TimerText.text.Contains($" ({PageIndex + 1}/{MaxPageIndex + 1})")) return;
        meetingHud.TimerText.text += $" ({PageIndex + 1}/{MaxPageIndex + 1})";
    }

    public override void OnPageChanged()
    {
        var i = 0;

        foreach (PlayerVoteArea button in Targets)
        {
            if (i >= PageIndex * MaxPerPage && i < (PageIndex + 1) * MaxPerPage)
            {
                button.gameObject.SetActive(true);
                int relativeIndex = i % MaxPerPage;
                int row = relativeIndex / 3;
                int col = relativeIndex % 3;
                Transform buttonTransform = button.transform;

                buttonTransform.localPosition = meetingHud.VoteOrigin +
                                                new Vector3(
                                                    meetingHud.VoteButtonOffsets.x * col,
                                                    meetingHud.VoteButtonOffsets.y * row,
                                                    buttonTransform.localPosition.z
                                                );
            }
            else button.gameObject.SetActive(false);

            i++;
        }
    }
}

public class ShapeShifterPagingBehaviour : AbstractPagingBehaviour
{
    public ShapeShifterPagingBehaviour(IntPtr ptr) : base(ptr) { }

    public ShapeshifterMinigame shapeshifterMinigame = null!;
    private TextMeshPro PageText = null!;
    [HideFromIl2Cpp] private IEnumerable<ShapeshifterPanel> Targets => shapeshifterMinigame.potentialVictims.ToArray();

    protected override int MaxPageIndex => (Targets.Count() - 1) / MaxPerPage;

    public override void Start()
    {
        PageText = Object.Instantiate(HudManager.Instance.KillButton.cooldownTimerText, shapeshifterMinigame.transform);
        PageText.name = PageIndexGameObjectName;
        PageText.enableWordWrapping = false;
        PageText.gameObject.SetActive(true);
        PageText.transform.localPosition = new(4.1f, -2.36f, -1f);
        PageText.transform.localScale *= 0.5f;
        OnPageChanged();
    }

    public override void OnPageChanged()
    {
        PageText.text = $"({PageIndex + 1}/{MaxPageIndex + 1})";
        var i = 0;

        foreach (ShapeshifterPanel panel in Targets)
        {
            if (i >= PageIndex * MaxPerPage && i < (PageIndex + 1) * MaxPerPage)
            {
                panel.gameObject.SetActive(true);
                int relativeIndex = i % MaxPerPage;
                int row = relativeIndex / 3;
                int col = relativeIndex % 3;
                Transform buttonTransform = panel.transform;

                buttonTransform.localPosition =
                    new(
                        shapeshifterMinigame.XStart + (shapeshifterMinigame.XOffset * col),
                        shapeshifterMinigame.YStart + (shapeshifterMinigame.YOffset * row),
                        buttonTransform.localPosition.z
                    );
            }
            else panel.gameObject.SetActive(false);

            i++;
        }
    }
}

public class VitalsPagingBehaviour : AbstractPagingBehaviour
{
    public VitalsPagingBehaviour(IntPtr ptr) : base(ptr) { }

    public VitalsMinigame vitalsMinigame = null!;
    private TextMeshPro PageText = null!;
    [HideFromIl2Cpp] private IEnumerable<VitalsPanel> Targets => vitalsMinigame.vitals.ToArray();

    protected override int MaxPageIndex => (Targets.Count() - 1) / MaxPerPage;

    public override void Start()
    {
        PageText = Object.Instantiate(HudManager.Instance.KillButton.cooldownTimerText, vitalsMinigame.transform);
        PageText.name = PageIndexGameObjectName;
        PageText.enableWordWrapping = false;
        PageText.gameObject.SetActive(true);
        PageText.transform.localPosition = new(2.7f, -2f, -1f);
        PageText.transform.localScale *= 0.5f;
        OnPageChanged();
    }

    public override void OnPageChanged()
    {
        if (PlayerTask.PlayerHasTaskOfType<HudOverrideTask>(PlayerControl.LocalPlayer))
            return;

        PageText.text = $"({PageIndex + 1}/{MaxPageIndex + 1})";
        var i = 0;

        foreach (VitalsPanel panel in Targets)
        {
            if (i >= PageIndex * MaxPerPage && i < (PageIndex + 1) * MaxPerPage)
            {
                panel.gameObject.SetActive(true);
                int relativeIndex = i % MaxPerPage;
                int row = relativeIndex / 3;
                int col = relativeIndex % 3;
                Transform panelTransform = panel.transform;

                panelTransform.localPosition =
                    new(
                        vitalsMinigame.XStart + (vitalsMinigame.XOffset * col),
                        vitalsMinigame.YStart + (vitalsMinigame.YOffset * row),
                        panelTransform.localPosition.z
                    );
            }
            else panel.gameObject.SetActive(false);

            i++;
        }
    }
}
