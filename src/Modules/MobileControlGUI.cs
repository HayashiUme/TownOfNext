// 灵感来源 / Inspired by: https://github.com/Gurge44/EndlessHostRoles

using UnityEngine;
using UnityEngine.SceneManagement;

namespace TONX.Modules;

//EHR的，不知道能不能用啊
public class MobileControlGUI : MonoBehaviour
{
    public static MobileControlGUI Instance;

    public static bool HudHidden;
    public static bool NoClipEnabled;

    public bool IsOpen;

    private Vector2 _scroll;
    private float _contentH;
    private Rect _windowRect;
    private bool _dragging;
    private Vector2 _dragOffset;
    private bool _initialized;
    private float _zoomValue = 3.0f;
    
    private bool _secGeneral = true;
    private bool _secCamera = true;
    private bool _secLobby = true;
    private bool _secGame = true;

    private float _lastScale = -1f;
    private Camera _cam;
    
    private static readonly Color PinkBase = new(1.00f, 0.75f, 0.80f, 1f);
    private static readonly Color PinkDeep = new(0.88f, 0.40f, 0.55f, 1f);
    private static readonly Color PinkDark = new(0.28f, 0.10f, 0.16f, 1f);
    private static readonly Color PinkPanel = new(0.18f, 0.07f, 0.11f, 1f);
    private static readonly Color PinkMid = new(0.40f, 0.14f, 0.22f, 1f);
    private static readonly Color HostBlue = new(0.10f, 0.20f, 0.45f, 1f);
    private static readonly Color HostHover = new(0.16f, 0.32f, 0.66f, 1f);
    private static readonly Color DangerRed = new(0.50f, 0.07f, 0.07f, 1f);
    private static readonly Color DangerHover = new(0.72f, 0.12f, 0.12f, 1f);

    private static bool IsAndroid => OperatingSystem.IsAndroid();
    private static float PlatformScale => IsAndroid ? 0.62f : 0.50f;
    private static float Scale => Screen.width / 1080f * PlatformScale;
    private static int   FontSize => Mathf.Max(12, Mathf.RoundToInt(20f * Scale));
    private static float BtnH => 64f * Scale;
    private static float BtnW => (IsAndroid ? 370f : 345f) * Scale;
    private static float Pad => 10f * Scale;
    private static float SbW => (IsAndroid ? 44f : 24f) * Scale;
    private static int SmallFont => Mathf.Max(9, FontSize - 5);

    private GUIStyle _sAction, _sHost, _sDanger, _sSection, _sWindow, _sTitle, _sDragHint, _sToggle, _sCollapse;

    private void Awake()
    {
        _cam = Camera.main;
        Instance = this;
        SceneManager.add_sceneLoaded((Action<Scene, LoadSceneMode>)OnSceneLoaded);
    }

    private void OnDestroy()
    {
        SceneManager.remove_sceneLoaded((Action<Scene, LoadSceneMode>)OnSceneLoaded);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _cam = Camera.main;
        HudHidden = false;
    }

    private void RebuildStyles()
    {
        _lastScale = Scale;

        int toggleSz = Mathf.Max(1, Mathf.RoundToInt(52f * Scale));
        int toggleR  = Mathf.Max(1, toggleSz / 2);

        _sToggle = new GUIStyle
        {
            fontSize  = FontSize + 2,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { background = RoundedTex(toggleSz, toggleSz, toggleR, PinkDeep, Lift(PinkDeep, 0.12f)), textColor = Color.white },
            hover = { background = RoundedTex(toggleSz, toggleSz, toggleR, PinkBase, Lift(PinkBase, 0.08f)), textColor = PinkDark  },
            active = { background = RoundedTex(toggleSz, toggleSz, toggleR, PinkDark, Lift(PinkDark, 0.06f)), textColor = Color.white }
        };

        int winW = Mathf.Max(1, Mathf.RoundToInt(BtnW + Pad * 4f + SbW));
        int winH = Mathf.Max(1, Mathf.RoundToInt(Screen.height * (IsAndroid ? 0.84f : 0.68f)));

        _sWindow = new GUIStyle
        {
            normal = { background = RoundedTex(winW, winH, 20, PinkPanel, PinkMid) }
        };

        _sTitle = new GUIStyle
        {
            fontSize  = FontSize + 4,
            fontStyle = FontStyle.BoldAndItalic,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = PinkBase }
        };

        _sDragHint = new GUIStyle
        {
            fontSize = SmallFont,
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.90f, 0.65f, 0.72f, 0.80f) }
        };

        _sSection = new GUIStyle
        {
            fontSize = FontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = PinkBase }
        };

        _sCollapse = new GUIStyle
        {
            fontSize = SmallFont + 2,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { background = RoundedTex(
                              Mathf.Max(1, Mathf.RoundToInt(BtnH * 0.55f)),
                              Mathf.Max(1, Mathf.RoundToInt(BtnH * 0.55f)),
                              Mathf.Max(1, Mathf.RoundToInt(BtnH * 0.28f)),
                              PinkMid, Lift(PinkMid, 0.10f)),
                          textColor = PinkBase },
            hover = { background = RoundedTex(
                           Mathf.Max(1, Mathf.RoundToInt(BtnH * 0.55f)),
                           Mathf.Max(1, Mathf.RoundToInt(BtnH * 0.55f)),
                           Mathf.Max(1, Mathf.RoundToInt(BtnH * 0.28f)),
                           PinkDeep, Lift(PinkDeep, 0.10f)),
                       textColor = Color.white }
        };

        _sAction = MakeBtn(PinkDark, PinkMid, new Color(0.12f, 0.04f, 0.08f, 1f));
        _sHost = MakeBtn(HostBlue, HostHover, new Color(0.06f, 0.12f, 0.30f, 1f));
        _sDanger = MakeBtn(DangerRed, DangerHover, new Color(0.30f, 0.04f, 0.04f, 1f));
    }

    private static GUIStyle MakeBtn(Color normal, Color hover, Color active)
    {
        int w = Mathf.Max(1, Mathf.RoundToInt(BtnW));
        int h = Mathf.Max(1, Mathf.RoundToInt(BtnH));
        int r = Mathf.Max(1, Mathf.RoundToInt(BtnH * 0.36f));
        return new GUIStyle
        {
            fontSize = FontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            richText = true,
            normal = { background = RoundedTex(w, h, r, normal, Lift(normal, 0.10f)), textColor = Color.white },
            hover = { background = RoundedTex(w, h, r, hover,  Lift(hover,  0.10f)), textColor = Color.white },
            active = { background = RoundedTex(w, h, r, active, Lift(active, 0.06f)), textColor = Color.white }
        };
    }

    private static Color Lift(Color c, float a) =>
        new(Mathf.Clamp01(c.r + a), Mathf.Clamp01(c.g + a), Mathf.Clamp01(c.b + a), 1f);

    private static Texture2D RoundedTex(int w, int h, int r, Color fill, Color edge)
    {
        w = Mathf.Max(1, w); h = Mathf.Max(1, h);
        r = Mathf.Clamp(r, 0, Mathf.Min(w, h) / 2);
        var tex = new Texture2D(w, h, TextureFormat.ARGB32, false) { filterMode = FilterMode.Bilinear };
        for (int py = 0; py < h; py++)
        for (int px = 0; px < w; px++)
        {
            float a = CornerAlpha(px, py, w, h, r);
            tex.SetPixel(px, py, a <= 0f ? Color.clear : a >= 1f ? fill : Color.Lerp(edge, fill, a));
        }
        tex.Apply();
        tex.hideFlags = HideFlags.HideAndDontSave;
        return tex;
    }

    private static float CornerAlpha(int px, int py, int w, int h, int r)
    {
        int cx, cy;
        if (px < r && py < r ) { cx = r; cy = r; }
        else if (px >= w-r && py < r) { cx = w - r; cy = r;}
        else if (px < r && py >= h-r) { cx = r; cy = h - r;}
        else if (px >= w-r && py >= h-r) { cx = w - r; cy = h - r;}
        else return 1f;
        float d = Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
        if (d >= r + 1f) return 0f;
        if (d <= r - 1f) return 1f;
        return r + 0.5f - d;
    }

    private void InitRect()
    {
        float w = BtnW + Pad * 4f + SbW;
        float h = Screen.height * (IsAndroid ? 0.84f : 0.68f);
        _windowRect = new Rect(18f * Scale, (Screen.height - h) * 0.5f, w, h);
        _initialized = true;
    }

    private void OnGUI()
    {
        if (!HudManager.InstanceExists) return;
        if (!_initialized) InitRect();
        if (Math.Abs(_lastScale - Scale) > 0.01f) RebuildStyles();

        HandleDrag();
        DrawToggle();
        if (IsOpen) DrawWindow();
    }

    private void DrawToggle()
    {
        float sz = 52f * Scale;
        float x, y;
        if (IsOpen)
        {
            x = _windowRect.x + _windowRect.width + 6f * Scale;
            y = _windowRect.y + (_windowRect.height - sz) * 0.5f;
        }
        else
        {
            x = Screen.width * 0.28f - sz * 0.5f;
            y = Screen.height - sz - 8f * Scale;
        }

        bool fade = !IsOpen && (GameStates.IsInGame || GameSettingMenu.Instance);
        Color prev = GUI.color;
        if (fade) GUI.color = new Color(1f, 1f, 1f, 0.12f);

        if (GUI.Button(new Rect(x, y, sz, sz), IsOpen ? "✕" : "≡", _sToggle))
            IsOpen = !IsOpen;

        if (fade) GUI.color = prev;
    }

    private void HandleDrag()
    {
        if (!IsOpen) return;
        Event e = Event.current;
        float titleH = BtnH * 0.78f + Pad;
        var titleRect = new Rect(_windowRect.x, _windowRect.y, _windowRect.width, titleH);

        switch (e.type)
        {
            case EventType.MouseDown when titleRect.Contains(e.mousePosition):
                _dragging = true;
                _dragOffset = e.mousePosition - new Vector2(_windowRect.x, _windowRect.y);
                e.Use();
                break;
            case EventType.MouseDrag when _dragging:
                _windowRect.x = Mathf.Clamp(e.mousePosition.x - _dragOffset.x, 0, Screen.width  - _windowRect.width);
                _windowRect.y = Mathf.Clamp(e.mousePosition.y - _dragOffset.y, 0, Screen.height - _windowRect.height);
                e.Use();
                break;
            case EventType.MouseUp:
                _dragging = false;
                break;
        }
    }

    private void DrawWindow()
    {
        GUI.Box(_windowRect, "", _sWindow);

        float titleH = BtnH * 0.78f + Pad;

        GUI.Label(
            new Rect(_windowRect.x, _windowRect.y + Pad * 0.5f, _windowRect.width, BtnH * 0.52f),
            GetString("MobileGUI.Title"),
            _sTitle
        );
        GUI.Label(
            new Rect(_windowRect.x, _windowRect.y + BtnH * 0.55f + Pad * 0.3f, _windowRect.width, BtnH * 0.34f),
            GetString("MobileGUI.DragHint"),
            _sDragHint
        );

        float scrollY  = _windowRect.y + titleH + Pad * 0.3f;
        float scrollH  = _windowRect.height - titleH - Pad;
        float visibleW = _windowRect.width - Pad * 2f;
        float contentW = visibleW - SbW - 1f;

        GUI.skin.verticalScrollbar.fixedWidth = SbW;
        GUI.skin.verticalScrollbarThumb.fixedWidth = SbW;

        _scroll = GUI.BeginScrollView(
            new Rect(_windowRect.x + Pad, scrollY, visibleW, scrollH),
            _scroll,
            new Rect(0, 0, contentW, _contentH),
            false, false
        );

        float y = Pad * 0.4f;
        DrawButtons(ref y, contentW);
        _contentH = y + Pad;

        GUI.EndScrollView();
    }

    private void DrawButtons(ref float y, float w)
    {
        bool amHost = AmongUsClient.Instance && AmongUsClient.Instance.AmHost;
        bool inGame = GameStates.IsInGame;
        bool inLobby = GameStates.IsLobby;
        bool inMeeting = GameStates.IsMeeting;
        bool countdown = GameStates.IsCountDown;
        bool notJoined = GameStates.IsNotJoined;
        bool alive = PlayerControl.LocalPlayer && PlayerControl.LocalPlayer.IsAlive();
        bool canMove = GameStates.IsCanMove;
        bool noGameEnd = Options.NoGameEnd.GetBool();
        bool canZoom = (GameStates.IsShip || inLobby) && !inMeeting && canMove;
        bool canNoClip = canMove && (!AmongUsClient.Instance.IsGameStarted || !GameStates.IsOnlineGame);
        
        Section(ref y, w, GetString("MobileGUI.Section.General"), ref _secGeneral);
        if (_secGeneral)
        {
            Btn(ref y, w, GetString("MobileGUI.DumpLog"), _sAction, () =>
            {
                Utils.DumpLog();
            });
            Btn(ref y, w, GetString("MobileGUI.ReloadLangs"), _sAction, () =>
            {
                Logger.Info("Reload translation via MobileControlGUI", "MobileControlGUI");
                LoadLangs();
                Logger.SendInGame(GetString("MobileGUI.ReloadLangs_Done"));
            });
            if (!notJoined)
                Btn(ref y, w, GetString("MobileGUI.CopySettings"), _sAction, Utils.CopyCurrentSettings);
            Btn(ref y, w, GetString("MobileGUI.OpenDir"), _sAction, () =>
                Utils.OpenDirectory(Environment.CurrentDirectory)
            );
            if (inGame || inMeeting)
                Btn(ref y, w, GetString("MobileGUI.FixBlackscreen"), _sAction, () =>
                    ExileController.Instance?.ReEnableGameplay()
                );
            if (inGame && (canMove || inMeeting))
                Btn(ref y, w,
                    InGameRoleInfoMenu.Showing ? GetString("MobileGUI.HideRoleInfo") : GetString("MobileGUI.ShowRoleInfo"),
                    _sAction, () =>
                    {
                        if (InGameRoleInfoMenu.Showing) InGameRoleInfoMenu.Hide();
                        else
                        {
                            InGameRoleInfoMenu.SetRoleInfoRef(PlayerControl.LocalPlayer);
                            InGameRoleInfoMenu.Show();
                        }
                        HudManagerInitializePatch.RoleInfoButton?.SelectButton(InGameRoleInfoMenu.Showing);
                    });
        }

        if (canZoom || canNoClip)
        {
            Section(ref y, w, GetString("MobileGUI.Section_Camera"), ref _secCamera);
            if (_secCamera)
            {
                if (canZoom)
                {
                    if (_cam) _zoomValue = _cam.orthographicSize;

                    GUI.Label(new Rect(0, y, w, BtnH * 0.42f),
                        $"{GetString("MobileGUI.Zoom")}  {_zoomValue:F1}×", _sSection);
                    y += BtnH * 0.44f;

                    float newZoom = GUI.HorizontalSlider(new Rect(0, y, w, BtnH * 0.50f), _zoomValue, 3.0f, 18.0f);
                    y += BtnH * 0.52f + Pad * 0.6f;

                    if (Mathf.Abs(newZoom - _zoomValue) > 0.01f)
                    {
                        _zoomValue = newZoom;
                        if (_cam) _cam.orthographicSize = _zoomValue;
                        if (HudManager.InstanceExists) HudManager.Instance.UICamera.orthographicSize = _zoomValue;
                    }

                    Btn(ref y, w, GetString("MobileGUI.ResetZoom"), _sAction, () =>
                    {
                        Zoom.SetZoomSize(reset: true);
                        _zoomValue = 3.0f;
                    });
                }
                else if (!Mathf.Approximately(_zoomValue, 3.0f))
                {
                    Zoom.SetZoomSize(reset: true);
                    _zoomValue = 3.0f;
                }

                if (canNoClip)
                {
                    bool ncOn = NoClipEnabled;
                    Btn(ref y, w,
                        ncOn ? GetString("MobileGUI.NoClip_On") : GetString("MobileGUI.NoClip_Off"),
                        ncOn ? _sHost : _sAction, () =>
                        {
                            NoClipEnabled = !NoClipEnabled;
                            if (PlayerControl.LocalPlayer)
                                PlayerControl.LocalPlayer.Collider.offset = NoClipEnabled
                                    ? new Vector2(0f, 127f)
                                    : new Vector2(0f, -0.3636f);
                        });
                }
                else if (NoClipEnabled && PlayerControl.LocalPlayer)
                {
                    NoClipEnabled = false;
                    PlayerControl.LocalPlayer.Collider.offset = new Vector2(0f, -0.3636f);
                }

                bool canHud = IntroCutscene.Instance == null && !inMeeting && !ExileController.Instance;
                if (canHud)
                {
                    Btn(ref y, w,
                        HudHidden ? GetString("MobileGUI.ShowHud") : GetString("MobileGUI.HideHud"),
                        HudHidden ? _sHost : _sAction, () =>
                        {
                            HudHidden = !HudHidden;
                            if (HudManager.InstanceExists)
                                HudManager.Instance.gameObject.SetActive(!HudHidden);
                        });
                }
                else if (HudHidden)
                {
                    HudHidden = false;
                    if (HudManager.InstanceExists) HudManager.Instance.gameObject.SetActive(true);
                }
            }
        }
        
        if (inLobby)
        {
            Section(ref y, w, GetString("MobileGUI.Section_Lobby"), ref _secLobby);
            if (_secLobby)
            {
                if (amHost && !countdown)
                    Btn(ref y, w, GetString("MobileGUI.StartGame"), _sHost, () =>
                    {
                        if (GameStartManager.InstanceExists)
                        {
                            Logger.Info("Start game via MobileControlGUI", "MobileControlGUI");
                            GameStartManager.Instance.BeginGame();
                        }
                    });

                if (amHost && countdown)
                {
                    Btn(ref y, w, GetString("MobileGUI.StartNow"), _sHost, () =>
                        GameStartManager.Instance.countDownTimer = 0);
                    Btn(ref y, w, GetString("MobileGUI.CancelCountdown"), _sDanger, () =>
                    {
                        GameStartManager.Instance.ResetStartState();
                        Logger.SendInGame(GetString("CancelStartCountDown"));
                    });
                }

                if (amHost)
                {
                    Btn(ref y, w, GetString("MobileGUI.ShowSettings"), _sHost, () =>
                    {
                        Utils.ShowActiveSettings();
                    });
                    Btn(ref y, w, GetString("MobileGUI.ShowSettingsHelp"), _sHost, () =>
                    {
                        Utils.ShowActiveSettingsHelp();
                    });
                    Btn(ref y, w, GetString("MobileGUI.SaveOptions"), _sHost, OptionSerializer.SaveToClipboard);
                    Btn(ref y, w, GetString("MobileGUI.LoadOptions"), _sHost, OptionSerializer.LoadFromClipboard);
                    Btn(ref y, w, GetString("MobileGUI.ResetOptions"), _sDanger, () =>
                    {
                        OptionItem.AllOptions.ToArray()
                            .Where(x => x.Id > 0)
                            .Do(x => x.SetValue(x.DefaultValue, false));
                        Logger.SendInGame(GetString("RestTONXSetting"));
                    });
                }
            }
        }
        
        if (inGame)
        {
            Section(ref y, w, GetString("MobileGUI.Section_Game"), ref _secGame);
            if (_secGame)
            {
                if (amHost && alive)
                    Btn(ref y, w, GetString("MobileGUI.KillSelf"), _sDanger, () =>
                    {
                        var state = PlayerState.GetByPlayerId(PlayerControl.LocalPlayer.PlayerId);
                        PlayerControl.LocalPlayer.Data.IsDead = true;
                        state.DeathReason = CustomDeathReason.etc;
                        PlayerControl.LocalPlayer.RpcExileV2();
                        state.SetDead();
                        Utils.SendMessage(GetString("HostKillSelfByCommand"),
                            title: $"<color=#ff0000>{GetString("DefaultSystemMessageTitle")}</color>");
                        _ = new LateTask(() => Utils.NotifyRoles(), 0.1f, "HostKillSelfNotify");
                    });

                if (amHost)
                {
                    if (!inMeeting)
                        Btn(ref y, w, GetString("MobileGUI.CallMeeting"), _sHost, () =>
                            PlayerControl.LocalPlayer.NoCheckStartMeeting(null, true));
                    else
                    {
                        Btn(ref y, w, GetString("MobileGUI.EndMeeting"), _sHost, () =>
                            MeetingHud.Instance.RpcForceEndMeeting());
                        Btn(ref y, w, GetString("MobileGUI.EndByVotes"), _sHost, () =>
                        {
                            MeetingHud.Instance.CheckForEndVoting();
                        });
                    }

                    Btn(ref y, w, GetString("MobileGUI.OpenChat"), _sHost, () =>
                        HudManager.Instance.Chat.SetVisible(true));
                    
                    if (noGameEnd)
                        Btn(ref y, w, GetString("MobileGUI.ForceEnd"), _sDanger, () =>
                        {
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Draw);
                            GameManager.Instance.LogicFlow.CheckEndCriteria();
                        });
                }
            }
        }

        return;

        void Section(ref float sy, float sw, string title, ref bool expanded)
        {
            sy += Pad * 1.6f;
            float headerH = BtnH * 0.60f;
            float collapseW = headerH;
            float labelW = sw - collapseW - Pad;

            GUI.Label(new Rect(0, sy, labelW, headerH), $"◆ {title}", _sSection);

            string chevron = expanded ? "▲" : "▼";
            if (GUI.Button(new Rect(sw - collapseW, sy, collapseW, headerH), chevron, _sCollapse))
                expanded = !expanded;

            sy += headerH + Pad * 0.5f;
            
            Color prev = GUI.color;
            GUI.color = new Color(PinkDeep.r, PinkDeep.g, PinkDeep.b, 0.55f);
            GUI.DrawTexture(new Rect(0, sy, sw, 1.5f * Scale), Texture2D.whiteTexture);
            GUI.color = prev;
            sy += 2f * Scale + Pad * 0.4f;
        }

        void Btn(ref float by, float bw, string label, GUIStyle style, Action action)
        {
            if (GUI.Button(new Rect(0, by, bw, BtnH), label, style))
            {
                try { action(); }
                catch (Exception e) { Logger.Error(e.ToString(), "MobileControlGUI"); }
            }
            by += BtnH + Pad * 0.65f;
        }
    }
}
