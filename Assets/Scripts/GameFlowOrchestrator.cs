using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Builds main menu, HUD, pause, settings, leaderboard, and tutorial UI at runtime,
/// and wires game flow (spawn enable, session reset).
/// Attach to the root <see cref="Canvas"/>.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class GameFlowOrchestrator : MonoBehaviour
{
    Canvas _canvas;
    SpawnObject _spawner;
    RectTransform _canvasRt;

    GameObject _mainMenu;
    GameObject _hud;
    GameObject _pause;
    GameObject _settings;
    GameObject _leaderboard;
    GameObject _tutorial;

    Text _hudLives;
    Text _hudLevel;
    Text _hudObjective;
    Text _hudHighScoreNum;
    Text _hudCurrentScoreNum;

    Text _settingsLangLabel;

    bool _pauseOpen;
    bool _resumePauseAfterSettings;
    bool _resumeMainMenuAfterSettings;
    bool _resumeMainMenuAfterTutorial;
    TrailRenderer _bladeTrail;

    Rect _lastSafeArea;
    RectTransform _tutorialCloseRt;

    /// <summary>Design px height of top HUD band before runtime scaling.</summary>
    const float HudBandDesignHeight = 238f;

    void Awake()
    {
        AudioSettings.LoadFromPrefs();
        _canvas = GetComponent<Canvas>();
        _canvasRt = _canvas.GetComponent<RectTransform>();
        FixCanvasScale();
        ConfigureUiCanvasSortOrder();

        _spawner = FindObjectOfType<SpawnObject>(true);
        if (_spawner != null)
            _spawner.gameObject.SetActive(false);

        BuildMainMenu();
        BuildHud();
        BuildPause();
        BuildSettingsOverlay();
        BuildLeaderboard();
        BuildTutorial();

        _lastSafeArea = Screen.safeArea;
        ApplyAllSafeAreaLayouts();

        GameSession.ScoreChanged += OnScoreHud;
        GameSession.LivesChanged += OnLivesHud;

        GameSession.SetPlaying(false);
        ShowMainMenuOnly();
        ElevateMainMenuAboveHudAndBackdrop();

        var cutter = FindObjectOfType<Cutting>();
        if (cutter != null)
            _bladeTrail = cutter.GetComponent<TrailRenderer>();
    }

    void OnDestroy()
    {
        GameSession.ScoreChanged -= OnScoreHud;
        GameSession.LivesChanged -= OnLivesHud;
    }

    void LateUpdate()
    {
        Rect sa = Screen.safeArea;
        if (SafeAreaChanged(sa, _lastSafeArea))
        {
            _lastSafeArea = sa;
            ApplyAllSafeAreaLayouts();
        }

        if (_bladeTrail == null)
            return;

        bool modalUi =
            (_settings != null && _settings.activeSelf) ||
            (_pause != null && _pause.activeSelf) ||
            (_mainMenu != null && _mainMenu.activeSelf) ||
            (_leaderboard != null && _leaderboard.activeSelf) ||
            (_tutorial != null && _tutorial.activeSelf) ||
            GameOver.IsShowing;

        if (modalUi)
        {
            _bladeTrail.emitting = false;
            _bladeTrail.Clear();
        }
        else if (GameSession.IsPlaying)
            _bladeTrail.emitting = true;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_settings.activeSelf)
            {
                CloseSettingsPanel();
                return;
            }

            if (_leaderboard.activeSelf)
            {
                _leaderboard.SetActive(false);
                return;
            }

            if (_tutorial.activeSelf)
            {
                CloseTutorialPanel();
                return;
            }

            if (!GameSession.IsPlaying || GameOver.IsShowing)
                return;

            if (_pauseOpen)
            {
                GameAudio.PlayUiTick();
                ClosePause();
            }
            else
            {
                GameAudio.PlayUiTick();
                OpenPause();
            }
            return;
        }
    }

    void FixCanvasScale()
    {
        if (_canvasRt != null && _canvasRt.localScale.sqrMagnitude < 0.01f)
            _canvasRt.localScale = Vector3.one;

        var scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }
    }

    void ConfigureUiCanvasSortOrder()
    {
        if (_canvas == null)
            return;
        _canvas.overrideSorting = true;
        _canvas.sortingOrder = 320;
    }

    void BuildMainMenu()
    {
        _mainMenu = UiFactory.Panel("MainMenu_StartScreen", transform, new Color(0.04f, 0.06f, 0.09f, 0.96f));

        var titleT = UiFactory.Text("Title", _mainMenu.transform, "FRUIT NINJA", 78, TextAnchor.UpperCenter, new Color(1f, 0.95f, 0.5f), 920f, 1f, FontStyle.Bold);
        titleT.rectTransform.anchoredPosition = UiFactory.SV(0f, 338f);
        UiFactory.ApplyReadableOutline(titleT, 3f);

        var subT = UiFactory.Text("MenuSubtitle", _mainMenu.transform, "START GAME MENU", 38, TextAnchor.UpperCenter, new Color(0.92f, 0.96f, 1f), 920f, 1f, FontStyle.Bold);
        subT.rectTransform.anchoredPosition = UiFactory.SV(0f, 268f);
        UiFactory.ApplyReadableOutline(subT, 2.35f);

        var hintT = UiFactory.Text(
                "Hint",
                _mainMenu.transform,
                "Tap START GAME below to begin.\nWhile playing: ESC or Pause opens the menu.",
                32,
                TextAnchor.UpperCenter,
                new Color(0.94f, 0.96f, 1f),
                840f,
                1.3f,
                FontStyle.Bold);
        hintT.rectTransform.anchoredPosition = UiFactory.SV(0f, 178f);
        UiFactory.ApplyReadableOutline(hintT, 2.15f);

        float y = 96f;
        float step = 118f;
        const int menuBtnFont = 36;
        UiFactory.Button("START GAME", _mainMenu.transform, new Vector2(0, y), new Vector2(620, 102), StartGame, menuBtnFont);
        y -= step;
        UiFactory.Button("SETTINGS", _mainMenu.transform, new Vector2(0, y), new Vector2(620, 94), ShowSettingsPanel, menuBtnFont);
        y -= step;
        UiFactory.Button("LEADERBOARD / HIGH SCORES", _mainMenu.transform, new Vector2(0, y), new Vector2(620, 94), ShowLeaderboardPanel, menuBtnFont);
        y -= step;
        UiFactory.Button("HELP / TUTORIAL", _mainMenu.transform, new Vector2(0, y), new Vector2(620, 94), ShowTutorialPanel, menuBtnFont);
        y -= step;
        UiFactory.Button("EXIT / QUIT", _mainMenu.transform, new Vector2(0, y), new Vector2(620, 94), QuitGame, menuBtnFont);

        ElevateMainMenuAboveHudAndBackdrop();
    }

    /// <summary>Ensures the full-screen main menu draws above gameplay backdrop/HUD siblings.</summary>
    void ElevateMainMenuAboveHudAndBackdrop()
    {
        if (_mainMenu != null)
            _mainMenu.transform.SetAsLastSibling();
    }

    void ShowSettingsPanel()
    {
        _resumeMainMenuAfterSettings = false;

        if (_pauseOpen && _pause != null && _pause.activeSelf)
        {
            _resumePauseAfterSettings = true;
            _pause.SetActive(false);
        }
        else
        {
            _resumePauseAfterSettings = false;
            if (_mainMenu != null && _mainMenu.activeSelf)
            {
                _resumeMainMenuAfterSettings = true;
                _mainMenu.SetActive(false);
            }
        }

        _settings.SetActive(true);
        _settings.transform.SetAsLastSibling();
    }

    void CloseSettingsPanel()
    {
        if (_settings == null || !_settings.activeSelf)
            return;

        _settings.SetActive(false);

        if (_resumePauseAfterSettings && _pause != null && _pauseOpen)
        {
            _pause.SetActive(true);
            _pause.transform.SetAsLastSibling();
            if (_hud != null)
                _hud.SetActive(false);
        }
        else if (_resumeMainMenuAfterSettings && _mainMenu != null)
        {
            _mainMenu.SetActive(true);
            ElevateMainMenuAboveHudAndBackdrop();
        }

        _resumePauseAfterSettings = false;
        _resumeMainMenuAfterSettings = false;
    }

    void ShowLeaderboardPanel()
    {
        _leaderboard.SetActive(true);
        _leaderboard.transform.SetAsLastSibling();
        var body = _leaderboard.transform.Find("BodyText")?.GetComponent<Text>();
        if (body != null)
            body.text = "Best score: " + LeaderboardStore.HighScore;
    }

    void ShowTutorialPanel()
    {
        _resumeMainMenuAfterTutorial = false;
        if (_mainMenu != null && _mainMenu.activeSelf)
        {
            _resumeMainMenuAfterTutorial = true;
            _mainMenu.SetActive(false);
        }

        _tutorial.SetActive(true);
        _tutorial.transform.SetAsLastSibling();
    }

    void CloseTutorialPanel()
    {
        if (_tutorial == null || !_tutorial.activeSelf)
            return;

        _tutorial.SetActive(false);

        if (_resumeMainMenuAfterTutorial && _mainMenu != null)
        {
            _mainMenu.SetActive(true);
            ElevateMainMenuAboveHudAndBackdrop();
        }

        _resumeMainMenuAfterTutorial = false;
    }

    static bool SafeAreaChanged(Rect a, Rect b)
    {
        const float eps = 0.5f;
        return Mathf.Abs(a.x - b.x) > eps || Mathf.Abs(a.y - b.y) > eps ||
               Mathf.Abs(a.width - b.width) > eps || Mathf.Abs(a.height - b.height) > eps;
    }

    void ApplyHudSafeArea()
    {
        if (_hud == null || _canvas == null)
            return;

        var rt = _hud.GetComponent<RectTransform>();
        float sf = _canvas.scaleFactor;
        float top = (Screen.height - Screen.safeArea.yMax) / sf;
        float left = Screen.safeArea.xMin / sf;
        float right = (Screen.width - Screen.safeArea.xMax) / sf;
        float hudH = UiFactory.S(HudBandDesignHeight);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(left, -hudH);
        rt.offsetMax = new Vector2(-right, -top);
    }

    void ApplyAllSafeAreaLayouts()
    {
        ApplyHudSafeArea();
        ApplyOverlayPanelsSafeArea();
        RefreshTutorialCloseButtonWidth();
    }

    void ApplyOverlayPanelsSafeArea()
    {
        if (_canvas == null)
            return;

        void Apply(GameObject go)
        {
            if (go == null)
                return;
            UiFactory.ApplySafeAreaFullBleed(go.GetComponent<RectTransform>(), _canvas);
        }

        Apply(_mainMenu);
        Apply(_pause);
        Apply(_settings);
        Apply(_leaderboard);
        Apply(_tutorial);
    }

    void RefreshTutorialCloseButtonWidth()
    {
        if (_tutorialCloseRt == null || _canvas == null)
            return;

        float pad = UiFactory.S(28f);
        float safeW = Screen.safeArea.width / _canvas.scaleFactor;
        float closeW = Mathf.Min(UiFactory.S(580f), Mathf.Max(UiFactory.S(400f), safeW - pad * 2f));
        _tutorialCloseRt.sizeDelta = new Vector2(closeW, UiFactory.S(96f));
    }

    static void AddHudOutline(Text t)
    {
        UiFactory.ApplyReadableOutline(t, 2.45f);
    }

    void BuildHud()
    {
        _hud = new GameObject("HUD", typeof(RectTransform));
        _hud.layer = 5;
        var hudRt = _hud.GetComponent<RectTransform>();
        hudRt.SetParent(transform, false);

        var hudBar = new GameObject("HudBar", typeof(RectTransform), typeof(Image));
        hudBar.layer = 5;
        hudBar.transform.SetParent(_hud.transform, false);
        var barRt = hudBar.GetComponent<RectTransform>();
        barRt.anchorMin = Vector2.zero;
        barRt.anchorMax = Vector2.one;
        float mx = UiFactory.S(18f);
        float myTop = UiFactory.S(12f);
        float myBot = UiFactory.S(14f);
        barRt.offsetMin = new Vector2(mx, myBot);
        barRt.offsetMax = new Vector2(-mx, -myTop);
        UiFactory.StyleHudBarBackground(hudBar.GetComponent<Image>());
        hudBar.GetComponent<Image>().raycastTarget = false;

        Transform hb = hudBar.transform;

        // Dedicated left column so stats never spill into center/right HUD regions.
        var hudLeft = new GameObject("HudLeft", typeof(RectTransform));
        hudLeft.layer = 5;
        hudLeft.transform.SetParent(hb, false);
        var hlRt = hudLeft.GetComponent<RectTransform>();
        hlRt.anchorMin = new Vector2(0f, 0f);
        hlRt.anchorMax = new Vector2(0.34f, 1f);
        hlRt.offsetMin = new Vector2(UiFactory.S(8f), UiFactory.S(10f));
        hlRt.offsetMax = new Vector2(UiFactory.S(-4f), UiFactory.S(-12f));

        Transform hl = hudLeft.transform;
        float iz = 24f;

        var capBest = UiFactory.Text("CapBest", hl, "High", 15, TextAnchor.UpperLeft, new Color(0.72f, 0.86f, 1f, 0.98f), 72f, 1f, FontStyle.Bold);
        capBest.rectTransform.anchorMin = new Vector2(0f, 1f);
        capBest.rectTransform.anchorMax = new Vector2(0f, 1f);
        capBest.rectTransform.pivot = new Vector2(0f, 1f);
        capBest.rectTransform.sizeDelta = UiFactory.SV(64f, 20f);
        capBest.rectTransform.anchoredPosition = UiFactory.SV(4f, -10f);
        UiFactory.ApplyReadableOutline(capBest, 1.35f);

        var sparkGo = new GameObject("HudStarBest", typeof(RectTransform), typeof(Image));
        sparkGo.layer = 5;
        sparkGo.transform.SetParent(hl, false);
        var sparkRt = sparkGo.GetComponent<RectTransform>();
        sparkRt.anchorMin = new Vector2(0f, 1f);
        sparkRt.anchorMax = new Vector2(0f, 1f);
        sparkRt.pivot = new Vector2(0f, 1f);
        sparkRt.sizeDelta = UiFactory.SV(iz, iz);
        sparkRt.anchoredPosition = UiFactory.SV(4f, -30f);
        var sparkImg = sparkGo.GetComponent<Image>();
        sparkImg.sprite = UiFactory.HudStarIconSprite();
        sparkImg.color = new Color(1f, 0.93f, 0.45f, 1f);
        sparkImg.raycastTarget = false;

        _hudHighScoreNum = UiFactory.Text("HighScoreVal", hl, "0", 27, TextAnchor.MiddleLeft, new Color(1f, 0.97f, 0.62f, 1f), 72f, 1f, FontStyle.Bold);
        var snRt = _hudHighScoreNum.rectTransform;
        snRt.anchorMin = new Vector2(0f, 1f);
        snRt.anchorMax = new Vector2(0f, 1f);
        snRt.pivot = new Vector2(0f, 1f);
        snRt.sizeDelta = UiFactory.SV(76f, 38f);
        snRt.anchoredPosition = UiFactory.SV(32f, -34f);
        AddHudOutline(_hudHighScoreNum);

        var capNow = UiFactory.Text("CapNow", hl, "Now", 15, TextAnchor.UpperLeft, new Color(0.72f, 0.86f, 1f, 0.98f), 72f, 1f, FontStyle.Bold);
        capNow.rectTransform.anchorMin = new Vector2(0f, 1f);
        capNow.rectTransform.anchorMax = new Vector2(0f, 1f);
        capNow.rectTransform.pivot = new Vector2(0f, 1f);
        capNow.rectTransform.sizeDelta = UiFactory.SV(64f, 20f);
        capNow.rectTransform.anchoredPosition = UiFactory.SV(126f, -10f);
        UiFactory.ApplyReadableOutline(capNow, 1.35f);

        var coinGo = new GameObject("HudCoinNow", typeof(RectTransform), typeof(Image));
        coinGo.layer = 5;
        coinGo.transform.SetParent(hl, false);
        var coinRt = coinGo.GetComponent<RectTransform>();
        coinRt.anchorMin = new Vector2(0f, 1f);
        coinRt.anchorMax = new Vector2(0f, 1f);
        coinRt.pivot = new Vector2(0f, 1f);
        coinRt.sizeDelta = UiFactory.SV(iz, iz);
        coinRt.anchoredPosition = UiFactory.SV(126f, -30f);
        var coinImg = coinGo.GetComponent<Image>();
        coinImg.sprite = UiFactory.HudCoinIconSprite();
        coinImg.color = Color.white;
        coinImg.raycastTarget = false;

        _hudCurrentScoreNum = UiFactory.Text("CurrentScoreVal", hl, "0", 27, TextAnchor.MiddleLeft, new Color(1f, 0.92f, 0.55f, 1f), 72f, 1f, FontStyle.Bold);
        var cnRt = _hudCurrentScoreNum.rectTransform;
        cnRt.anchorMin = new Vector2(0f, 1f);
        cnRt.anchorMax = new Vector2(0f, 1f);
        cnRt.pivot = new Vector2(0f, 1f);
        cnRt.sizeDelta = UiFactory.SV(76f, 38f);
        cnRt.anchoredPosition = UiFactory.SV(154f, -34f);
        AddHudOutline(_hudCurrentScoreNum);

        // Center column only — disjoint from lives column (previously a wide right-anchored rect overlapped this).
        _hudObjective = UiFactory.Text("ObjectiveHud", hb, GameSession.ObjectiveShort, 25, TextAnchor.MiddleCenter, new Color(0.97f, 0.99f, 1f, 1f), 420f, 1.06f, FontStyle.Bold);
        _hudObjective.horizontalOverflow = HorizontalWrapMode.Wrap;
        _hudObjective.verticalOverflow = VerticalWrapMode.Truncate;
        var objRt = _hudObjective.rectTransform;
        objRt.anchorMin = new Vector2(0.352f, 0.2f);
        objRt.anchorMax = new Vector2(0.575f, 0.9f);
        objRt.offsetMin = new Vector2(UiFactory.S(6f), UiFactory.S(8f));
        objRt.offsetMax = new Vector2(-UiFactory.S(6f), -UiFactory.S(10f));
        AddHudOutline(_hudObjective);

        // Right column: upper band = hearts only; lower band = level — both exclude pause strip at far right.
        _hudLives = UiFactory.Text("LivesHud", hb, "LIVES   ♥♥♥", 26, TextAnchor.UpperRight, new Color(1f, 0.48f, 0.58f, 1f), 260f, 1f, FontStyle.Bold);
        var livesRt = _hudLives.rectTransform;
        livesRt.anchorMin = new Vector2(0.58f, 0.52f);
        livesRt.anchorMax = new Vector2(0.795f, 0.96f);
        livesRt.offsetMin = new Vector2(UiFactory.S(4f), UiFactory.S(4f));
        livesRt.offsetMax = new Vector2(-UiFactory.S(62f), -UiFactory.S(6f));
        AddHudOutline(_hudLives);

        _hudLevel = UiFactory.Text("LevelHud", hb, "Lv 1", 22, TextAnchor.LowerRight, new Color(0.78f, 1f, 1f, 1f), 200f, 1f, FontStyle.Bold);
        var lvlRt = _hudLevel.rectTransform;
        lvlRt.anchorMin = new Vector2(0.58f, 0.06f);
        lvlRt.anchorMax = new Vector2(0.795f, 0.46f);
        lvlRt.offsetMin = new Vector2(UiFactory.S(4f), UiFactory.S(6f));
        lvlRt.offsetMax = new Vector2(-UiFactory.S(62f), -UiFactory.S(4f));
        AddHudOutline(_hudLevel);

        var pb = UiFactory.IconButton("PauseBtn", hb, Vector2.zero, new Vector2(54f, 54f), UiFactory.PauseIconSprite(), OpenPause, 0.5f);
        var prt = pb.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(1f, 0.5f);
        prt.anchorMax = new Vector2(1f, 0.5f);
        prt.pivot = new Vector2(1f, 0.5f);
        prt.sizeDelta = UiFactory.SV(54f, 54f);
        prt.anchoredPosition = new Vector2(-UiFactory.S(16f), 0f);

        _hud.SetActive(false);
    }

    void BuildPause()
    {
        _pause = UiFactory.Panel("PauseMenu", transform, new Color(0.06f, 0.08f, 0.11f, 0.94f));
        var pTitle = UiFactory.Text("PTitle", _pause.transform, "PAUSED", 56, TextAnchor.MiddleCenter, Color.white, 720f, 1f, FontStyle.Bold);
        pTitle.rectTransform.sizeDelta = UiFactory.SV(720f, 128f);
        pTitle.rectTransform.anchoredPosition = UiFactory.SV(0f, 298f);
        UiFactory.ApplyReadableOutline(pTitle, 2.75f);

        float y = 118f;
        float step = 104f;
        UiFactory.Button("Resume Game", _pause.transform, new Vector2(0, y), new Vector2(560, 94), ClosePause);
        y -= step;
        UiFactory.Button("Restart Level", _pause.transform, new Vector2(0, y), new Vector2(560, 94), RestartLevel);
        y -= step;
        UiFactory.Button("Settings", _pause.transform, new Vector2(0, y), new Vector2(560, 94), ShowSettingsPanel);
        y -= step;
        UiFactory.Button("Exit to Main Menu", _pause.transform, new Vector2(0, y), new Vector2(560, 94), ExitToMainMenu);

        _pause.SetActive(false);
    }

    void BuildSettingsOverlay()
    {
        _settings = UiFactory.Panel("Settings", transform, new Color(0.06f, 0.08f, 0.12f, 1f));

        const float contentW = 580f;
        const float sliderH = 56f;
        const float rowPitch = 122f;
        const float labelAboveSlider = 46f;
        const int titleFont = 52;
        const int rowLabelFont = 32;
        const int sectionHdrFont = 28;
        const int settingsBtnFont = 34;
        const float gapAfterSliders = 44f;
        const float gapLabelToButton = 58f;
        const float gapBetweenSections = 90f;

        var st = UiFactory.Text("STitle", _settings.transform, "SETTINGS", titleFont, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.42f), contentW + 120f, 1f, FontStyle.Bold);
        st.rectTransform.sizeDelta = UiFactory.SV(contentW + 40f, 120f);
        st.rectTransform.anchoredPosition = UiFactory.SV(0f, 332f);
        UiFactory.ApplyReadableOutline(st, 2.6f);

        float cy = 218f;

        void AudioRow(string id, string rowLabel, System.Action<float> onChanged, float initial)
        {
            var rowLbl = UiFactory.Text(
                    id + "_Lbl",
                    _settings.transform,
                    rowLabel,
                    rowLabelFont,
                    TextAnchor.MiddleLeft,
                    new Color(0.96f, 0.98f, 1f),
                    contentW,
                    1f,
                    FontStyle.Bold);
            rowLbl.rectTransform.anchoredPosition = UiFactory.SV(0f, cy + labelAboveSlider);
            UiFactory.ApplyReadableOutline(rowLbl, 2f);

            var sl = UiFactory.CreateSlider(_settings.transform, 0f, 1f, initial, new Vector2(0f, cy), new Vector2(contentW, sliderH));
            sl.onValueChanged.AddListener(v => onChanged(v));
            cy -= rowPitch;
        }

        AudioRow("Music", "Music", v => AudioSettings.MusicVolume01 = v, AudioSettings.MusicVolume01);
        AudioRow("Sfx", "Sound / SFX", v => AudioSettings.SfxVolume01 = v, AudioSettings.SfxVolume01);
        AudioRow("Master", "Master", v => AudioSettings.MasterVolume01 = v, AudioSettings.MasterVolume01);

        cy -= gapAfterSliders;

        _settingsLangLabel = UiFactory.Text(
            "LangDisp",
            _settings.transform,
            "Language: English",
            sectionHdrFont + 2,
            TextAnchor.MiddleCenter,
            new Color(0.92f, 0.94f, 1f),
            contentW,
            1.2f,
            FontStyle.Bold);
        _settingsLangLabel.rectTransform.anchoredPosition = UiFactory.SV(0f, cy);
        UiFactory.ApplyReadableOutline(_settingsLangLabel, 2.05f);

        cy -= gapLabelToButton;
        UiFactory.Button("CYCLE LANGUAGE", _settings.transform, new Vector2(0f, cy), new Vector2(contentW, 88f), CycleLanguage, settingsBtnFont);

        cy -= gapBetweenSections;

        var ctrlHdr = UiFactory.Text(
                "CtrlHdr",
                _settings.transform,
                "CONTROLS",
                sectionHdrFont,
                TextAnchor.MiddleCenter,
                new Color(0.94f, 0.96f, 1f),
                contentW,
                1.15f,
                FontStyle.Bold);
        ctrlHdr.rectTransform.anchoredPosition = UiFactory.SV(0f, cy);
        UiFactory.ApplyReadableOutline(ctrlHdr, 2.05f);

        cy -= gapLabelToButton;
        UiFactory.Button("AUTO / TOUCH / MOUSE", _settings.transform, new Vector2(0f, cy), new Vector2(contentW, 88f), CycleControls, settingsBtnFont);

        cy -= gapBetweenSections;
        UiFactory.Button("CLOSE", _settings.transform, new Vector2(0f, cy), new Vector2(contentW, 94f), CloseSettingsPanel, settingsBtnFont);

        _settings.SetActive(false);
    }

    void BuildLeaderboard()
    {
        _leaderboard = UiFactory.Panel("Leaderboard", transform, new Color(0.05f, 0.06f, 0.09f, 0.95f));
        var lbTitle = UiFactory.Text("LBTitle", _leaderboard.transform, "HIGH SCORES", 38, TextAnchor.UpperCenter, new Color(1f, 0.88f, 0.42f), 920f, 1f, FontStyle.Bold);
        lbTitle.rectTransform.anchoredPosition = UiFactory.SV(0f, 280f);
        UiFactory.ApplyReadableOutline(lbTitle, 2.55f);

        var body = UiFactory.Text("BodyText", _leaderboard.transform, "Best score: 0", 30, TextAnchor.MiddleCenter, new Color(0.98f, 0.99f, 1f), 720f, 1.15f);
        body.rectTransform.sizeDelta = UiFactory.SV(720f, 260f);
        UiFactory.ApplyReadableOutline(body, 2f);

        UiFactory.Button("Close", _leaderboard.transform, new Vector2(0f, -265f), new Vector2(280f, 62f), () => _leaderboard.SetActive(false));
        _leaderboard.SetActive(false);
    }

    void BuildTutorial()
    {
        _tutorial = UiFactory.Panel("Tutorial", transform, new Color(0.06f, 0.08f, 0.12f, 1f));

        float uiScale = UiFactory.RuntimeUiScale;
        float pad = UiFactory.S(28f);
        float padSm = UiFactory.S(18f);

        float titleDesignW = 900f;
        var title = UiFactory.Text("TutTitle", _tutorial.transform, "HOW TO PLAY", 56, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.4f), titleDesignW, 1f, FontStyle.Bold);
        var titleRt = title.rectTransform;
        titleRt.anchorMin = new Vector2(0.06f, 0.76f);
        titleRt.anchorMax = new Vector2(0.94f, 0.96f);
        titleRt.offsetMin = new Vector2(padSm, padSm);
        titleRt.offsetMax = new Vector2(-padSm, -padSm);
        UiFactory.ApplyReadableOutline(title, 2.65f);

        const string tutorialTips =
            "• Drag or hold to move the blade.\n\n" +
            "• Slice fruits to score points.\n\n" +
            "• Bombs take one life. Game over when you run out of lives.\n\n" +
            "• Hearts and score are shown at the top while you play.";

        float bodyDesignW = 880f;
        var body = UiFactory.Text("Tips", _tutorial.transform, tutorialTips, 38, TextAnchor.MiddleLeft, new Color(0.98f, 0.99f, 1f), bodyDesignW, 1.45f);
        var br = body.rectTransform;
        br.anchorMin = new Vector2(0.07f, 0.16f);
        br.anchorMax = new Vector2(0.93f, 0.74f);
        br.offsetMin = new Vector2(pad, pad * 0.75f);
        br.offsetMax = new Vector2(-pad, -pad * 0.75f);

        var shadow = body.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        shadow.effectDistance = new Vector2(1.4f, -1.4f);

        float safeW = Screen.safeArea.width / _canvas.scaleFactor;
        float closeW = Mathf.Min(UiFactory.S(580f), Mathf.Max(UiFactory.S(400f), safeW - pad * 2f));
        float closeDesignW = closeW / uiScale;

        var closeBtn = UiFactory.Button("CLOSE", _tutorial.transform, Vector2.zero, new Vector2(closeDesignW, 96f), CloseTutorialPanel, 38);
        var closeRt = closeBtn.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(0.5f, 0f);
        closeRt.anchorMax = new Vector2(0.5f, 0f);
        closeRt.pivot = new Vector2(0.5f, 0f);
        closeRt.sizeDelta = new Vector2(closeW, UiFactory.S(96f));
        closeRt.anchoredPosition = new Vector2(0f, UiFactory.S(52f));
        _tutorialCloseRt = closeRt;

        _tutorial.SetActive(false);
    }

    void CycleLanguage()
    {
        GameSession.LanguageIndex = (GameSession.LanguageIndex + 1) % 3;
        string[] names = { "English", "العربية (UI labels demo)", "Español (UI labels demo)" };
        _settingsLangLabel.text = "Language: " + names[GameSession.LanguageIndex];
    }

    void CycleControls()
    {
        GameSession.ControlScheme = (GameSession.ControlScheme + 1) % 3;
    }

    void OnScoreHud(int s)
    {
        if (_hudCurrentScoreNum != null)
            _hudCurrentScoreNum.text = s.ToString();
    }

    /// <summary>Shows persisted high score from <see cref="LeaderboardStore"/>.</summary>
    void RefreshHudHighScoreDisplay()
    {
        if (_hudHighScoreNum == null)
            return;
        _hudHighScoreNum.text = LeaderboardStore.HighScore.ToString();
    }

    void OnLivesHud(int lives)
    {
        if (_hudLives == null) return;
        string hearts = lives switch
        {
            3 => "♥♥♥",
            2 => "♥♥♡",
            1 => "♥♡♡",
            _ => "♡♡♡"
        };
        _hudLives.text = "LIVES   " + hearts;
    }

    void ShowMainMenuOnly()
    {
        GameSession.ResetRun();
        Time.timeScale = 1f;
        _mainMenu.SetActive(true);
        _hud.SetActive(false);
        _pause.SetActive(false);
        _settings.SetActive(false);
        _leaderboard.SetActive(false);
        _tutorial.SetActive(false);
        _pauseOpen = false;
        _resumePauseAfterSettings = false;
        _resumeMainMenuAfterSettings = false;
        _resumeMainMenuAfterTutorial = false;

        if (_spawner != null)
            _spawner.gameObject.SetActive(false);

        GameSession.SetPlaying(false);
        OnScoreHud(GameSession.Score);
        RefreshHudHighScoreDisplay();
        OnLivesHud(GameSession.Lives);
        if (_hudLevel != null) _hudLevel.text = "Lv " + GameSession.Level;
        if (_hudObjective != null) _hudObjective.text = GameSession.ObjectiveShort;

        ElevateMainMenuAboveHudAndBackdrop();
    }

    public void StartGame()
    {
        GameSession.ResetRun();
        GameSession.SetPlaying(true);
        _mainMenu.SetActive(false);
        _hud.SetActive(true);
        if (_spawner != null)
            _spawner.gameObject.SetActive(true);
        Time.timeScale = 1f;
        OnScoreHud(GameSession.Score);
        RefreshHudHighScoreDisplay();
        OnLivesHud(GameSession.Lives);
        if (_hudLevel != null) _hudLevel.text = "Lv " + GameSession.Level;
        if (_hudObjective != null) _hudObjective.text = GameSession.ObjectiveShort;
    }

    void OpenPause()
    {
        if (!GameSession.IsPlaying || GameOver.IsShowing) return;
        _pauseOpen = true;
        _pause.SetActive(true);
        _pause.transform.SetAsLastSibling();
        if (_hud != null)
            _hud.SetActive(false);
        Time.timeScale = 0f;
    }

    void ClosePause()
    {
        _pauseOpen = false;
        _pause.SetActive(false);
        if (_hud != null)
            _hud.SetActive(true);
        Time.timeScale = 1f;
    }

    void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void ExitToMainMenu()
    {
        ClosePause();
        GameOver.HidePanel();
        ShowMainMenuOnly();
    }

    public static void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
