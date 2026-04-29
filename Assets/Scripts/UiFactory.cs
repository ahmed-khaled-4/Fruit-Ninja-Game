using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helpers to build legacy UI at runtime (menus not authored in the scene).
/// Uses procedural rounded-rect sprites so buttons/panels look modern without extra assets.
/// </summary>
public static class UiFactory
{
    /// <summary>
    /// Layout multiplier for positions/sizes (fonts use <see cref="Font"/> separately — avoid double-huge text).
    /// </summary>
    public const float RuntimeUiScale = 1.35f;

    const int RoundedAtlasSize = 128;
    const float RoundedCornerPx = 28f;

    /// <summary>Horizontal / vertical inset for label text inside rounded buttons (design px before scaling).</summary>
    const float BtnPadXDesign = 32f;
    const float BtnPadYDesign = 26f;

    static Sprite _roundedSprite;
    static Sprite _whiteKnobSprite;
    static Sprite _pauseIconSprite;
    static Sprite _shareIconSprite;

    static Sprite WhiteKnobSprite()
    {
        if (_whiteKnobSprite == null)
        {
            var tex = Texture2D.whiteTexture;
            _whiteKnobSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        return _whiteKnobSprite;
    }

    static bool InsideRoundedRect(float x, float y, float w, float h, float rad)
    {
        if (x < 0f || x > w || y < 0f || y > h)
            return false;
        float r = rad;
        if (x >= r && x <= w - r)
            return true;
        if (y >= r && y <= h - r)
            return true;
        if (x < r && y < r)
            return Vector2.Distance(new Vector2(x, y), new Vector2(r, r)) <= r;
        if (x > w - r && y < r)
            return Vector2.Distance(new Vector2(x, y), new Vector2(w - r, r)) <= r;
        if (x < r && y > h - r)
            return Vector2.Distance(new Vector2(x, y), new Vector2(r, h - r)) <= r;
        if (x > w - r && y > h - r)
            return Vector2.Distance(new Vector2(x, y), new Vector2(w - r, h - r)) <= r;
        return false;
    }

    /// <summary>White RGBA rounded quad for 9-sliced buttons, panels, tracks.</summary>
    public static Sprite RoundedRectSprite()
    {
        if (_roundedSprite != null)
            return _roundedSprite;

        var tex = new Texture2D(RoundedAtlasSize, RoundedAtlasSize, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        float w = RoundedAtlasSize;
        float h = RoundedAtlasSize;
        float r = RoundedCornerPx;

        for (int py = 0; py < RoundedAtlasSize; py++)
        {
            for (int px = 0; px < RoundedAtlasSize; px++)
            {
                float x = px + 0.5f;
                float y = py + 0.5f;
                bool inside = InsideRoundedRect(x, y, w, h, r);
                tex.SetPixel(px, py, inside ? Color.white : Color.clear);
            }
        }

        tex.Apply(false);
        float border = Mathf.Clamp(r - 2f, 10f, r);
        _roundedSprite = Sprite.Create(
            tex,
            new Rect(0f, 0f, w, h),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(border, border, border, border));

        return _roundedSprite;
    }

    const int IconTexSize = 64;

    static void IconFillRect(Texture2D tex, int x0, int y0, int x1, int y1, Color c)
    {
        x0 = Mathf.Clamp(x0, 0, tex.width - 1);
        x1 = Mathf.Clamp(x1, 0, tex.width - 1);
        y0 = Mathf.Clamp(y0, 0, tex.height - 1);
        y1 = Mathf.Clamp(y1, 0, tex.height - 1);
        if (x1 < x0 || y1 < y0)
            return;
        for (int py = y0; py <= y1; py++)
        {
            for (int px = x0; px <= x1; px++)
                tex.SetPixel(px, py, c);
        }
    }

    static void IconDisk(Texture2D tex, float cx, float cy, float r, Color c)
    {
        int ri = Mathf.CeilToInt(r) + 1;
        int icx = Mathf.RoundToInt(cx);
        int icy = Mathf.RoundToInt(cy);
        for (int py = -ri; py <= ri; py++)
        {
            for (int px = -ri; px <= ri; px++)
            {
                if (px * px + py * py <= r * r)
                {
                    int x = icx + px;
                    int y = icy + py;
                    if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                        tex.SetPixel(x, y, c);
                }
            }
        }
    }

    static float IconDistPointSegment(float px, float py, float ax, float ay, float bx, float by)
    {
        float abx = bx - ax;
        float aby = by - ay;
        float apx = px - ax;
        float apy = py - ay;
        float abLen2 = abx * abx + aby * aby;
        float t = abLen2 > 1e-6f ? Mathf.Clamp01((apx * abx + apy * aby) / abLen2) : 0f;
        float qx = ax + abx * t;
        float qy = ay + aby * t;
        float dx = px - qx;
        float dy = py - qy;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    static void IconStrokeSegment(Texture2D tex, float x0, float y0, float x1, float y1, float halfWidth, Color c)
    {
        float minX = Mathf.Min(x0, x1) - halfWidth - 1f;
        float maxX = Mathf.Max(x0, x1) + halfWidth + 1f;
        float minY = Mathf.Min(y0, y1) - halfWidth - 1f;
        float maxY = Mathf.Max(y0, y1) + halfWidth + 1f;
        int ix0 = Mathf.Max(0, Mathf.FloorToInt(minX));
        int ix1 = Mathf.Min(tex.width - 1, Mathf.CeilToInt(maxX));
        int iy0 = Mathf.Max(0, Mathf.FloorToInt(minY));
        int iy1 = Mathf.Min(tex.height - 1, Mathf.CeilToInt(maxY));
        for (int py = iy0; py <= iy1; py++)
        {
            for (int px = ix0; px <= ix1; px++)
            {
                float fx = px + 0.5f;
                float fy = py + 0.5f;
                if (IconDistPointSegment(fx, fy, x0, y0, x1, y1) <= halfWidth)
                    tex.SetPixel(px, py, c);
            }
        }
    }

    /// <summary>Two-bar pause glyph — white on transparency; consistent line weight with UI chrome.</summary>
    public static Sprite PauseIconSprite()
    {
        if (_pauseIconSprite != null)
            return _pauseIconSprite;

        var tex = new Texture2D(IconTexSize, IconTexSize, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color clear = Color.clear;
        for (int py = 0; py < IconTexSize; py++)
        {
            for (int px = 0; px < IconTexSize; px++)
                tex.SetPixel(px, py, clear);
        }

        Color w = Color.white;
        IconFillRect(tex, 14, 14, 23, 49, w);
        IconFillRect(tex, 41, 14, 50, 49, w);

        tex.Apply(false);
        _pauseIconSprite = Sprite.Create(
            tex,
            new Rect(0f, 0f, IconTexSize, IconTexSize),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect);

        return _pauseIconSprite;
    }

    /// <summary>Share / export glyph (three nodes + stems) — readable without labels.</summary>
    public static Sprite ShareIconSprite()
    {
        if (_shareIconSprite != null)
            return _shareIconSprite;

        var tex = new Texture2D(IconTexSize, IconTexSize, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color clear = Color.clear;
        for (int py = 0; py < IconTexSize; py++)
        {
            for (int px = 0; px < IconTexSize; px++)
                tex.SetPixel(px, py, clear);
        }

        Color w = Color.white;
        const float r = 5.5f;
        IconDisk(tex, 14f, 22f, r, w);
        IconDisk(tex, 50f, 22f, r, w);
        IconDisk(tex, 32f, 42f, r, w);
        float hw = 2.2f;
        IconStrokeSegment(tex, 14f, 22f, 32f, 42f, hw, w);
        IconStrokeSegment(tex, 50f, 22f, 32f, 42f, hw, w);

        tex.Apply(false);
        _shareIconSprite = Sprite.Create(
            tex,
            new Rect(0f, 0f, IconTexSize, IconTexSize),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect);

        return _shareIconSprite;
    }

    const int HudGlyphTexSize = 48;
    static Sprite _hudStarSprite;
    static Sprite _hudCoinSprite;

    /// <summary>Compact sparkle used for score — reads clearly at small HUD sizes.</summary>
    public static Sprite HudStarIconSprite()
    {
        if (_hudStarSprite != null)
            return _hudStarSprite;

        var tex = new Texture2D(HudGlyphTexSize, HudGlyphTexSize, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color clear = Color.clear;
        for (int py = 0; py < HudGlyphTexSize; py++)
        {
            for (int px = 0; px < HudGlyphTexSize; px++)
                tex.SetPixel(px, py, clear);
        }

        Color w = Color.white;
        IconFillRect(tex, 20, 8, 27, 39, w);
        IconFillRect(tex, 8, 20, 39, 27, w);

        tex.Apply(false);
        _hudStarSprite = Sprite.Create(
            tex,
            new Rect(0f, 0f, HudGlyphTexSize, HudGlyphTexSize),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect);

        return _hudStarSprite;
    }

    /// <summary>Coin glyph for coins column — matches HUD star weight.</summary>
    public static Sprite HudCoinIconSprite()
    {
        if (_hudCoinSprite != null)
            return _hudCoinSprite;

        var tex = new Texture2D(HudGlyphTexSize, HudGlyphTexSize, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color clear = Color.clear;
        for (int py = 0; py < HudGlyphTexSize; py++)
        {
            for (int px = 0; px < HudGlyphTexSize; px++)
                tex.SetPixel(px, py, clear);
        }

        Color outer = new Color(1f, 0.84f, 0.35f, 1f);
        Color inner = new Color(0.62f, 0.44f, 0.08f, 1f);
        IconDisk(tex, 24f, 24f, 17.5f, outer);
        IconDisk(tex, 24f, 24f, 9f, inner);

        tex.Apply(false);
        _hudCoinSprite = Sprite.Create(
            tex,
            new Rect(0f, 0f, HudGlyphTexSize, HudGlyphTexSize),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect);

        return _hudCoinSprite;
    }

    /// <summary>Rounded top HUD strip — glassy navy panel + cyan edge glow + soft drop shadow.</summary>
    public static void StyleHudBarBackground(Image img)
    {
        ApplyRoundedGraphic(img);
        img.color = new Color(0.07f, 0.11f, 0.22f, 0.93f);

        var outline = img.gameObject.GetComponent<Outline>() ?? img.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.38f, 0.82f, 1f, 0.32f);
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = true;

        if (img.gameObject.GetComponent<Shadow>() == null)
        {
            var sh = img.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.42f);
            sh.effectDistance = new Vector2(0f, -6f);
            sh.useGraphicAlpha = true;
        }
    }

    /// <summary>
    /// Insets a full-stretch panel (anchors 0–1) so content stays inside <see cref="Screen.safeArea"/> on notched / home-indicator devices.
    /// </summary>
    public static void ApplySafeAreaFullBleed(RectTransform stretchFullCanvasChild, Canvas canvas)
    {
        if (stretchFullCanvasChild == null || canvas == null)
            return;

        float sf = canvas.scaleFactor;
        float top = (Screen.height - Screen.safeArea.yMax) / sf;
        float bottom = Screen.safeArea.yMin / sf;
        float left = Screen.safeArea.xMin / sf;
        float right = (Screen.width - Screen.safeArea.xMax) / sf;
        stretchFullCanvasChild.anchorMin = Vector2.zero;
        stretchFullCanvasChild.anchorMax = Vector2.one;
        stretchFullCanvasChild.pivot = new Vector2(0.5f, 0.5f);
        stretchFullCanvasChild.offsetMin = new Vector2(left, bottom);
        stretchFullCanvasChild.offsetMax = new Vector2(-right, -top);
    }

    /// <summary>Rounded HUD/menu button showing only a procedural icon (no caption).</summary>
    public static Button IconButton(string objectName, Transform parent, Vector2 anchoredPositionDesign, Vector2 sizeDesign, Sprite icon, Action onClick, float iconFill = 0.52f, bool playUiClickSound = true)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        go.layer = 5;
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = SV(sizeDesign);
        rt.anchoredPosition = SV(anchoredPositionDesign);

        var img = go.GetComponent<Image>();
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        ApplyHudCompactButton(img, btn);
        btn.onClick.AddListener(() =>
        {
            if (playUiClickSound)
                GameAudio.PlayUiTick();
            onClick?.Invoke();
        });

        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.layer = 5;
        iconGo.transform.SetParent(go.transform, false);
        float dim = Mathf.Min(sizeDesign.x, sizeDesign.y) * iconFill;
        var iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.sizeDelta = SV(dim, dim);
        iconRt.anchoredPosition = Vector2.zero;
        var iconImg = iconGo.GetComponent<Image>();
        iconImg.sprite = icon;
        iconImg.type = Image.Type.Simple;
        iconImg.color = new Color(0.93f, 0.97f, 1f, 1f);
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;

        return btn;
    }

    static void ApplyRoundedGraphic(Image img)
    {
        img.sprite = RoundedRectSprite();
        img.type = Image.Type.Sliced;
    }

    /// <summary>Pulse / HUD compact buttons — same chrome as <see cref="Button"/>.</summary>
    public static void ApplyHudCompactButton(Image img, Button btn)
    {
        ApplyRoundedGraphic(img);
        img.color = BtnNormal;
        var colors = btn.colors;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        colors.highlightedColor = BtnHighlight;
        colors.pressedColor = BtnPressed;
        colors.selectedColor = BtnHighlight;
        colors.disabledColor = BtnDisabled;
        btn.colors = colors;
        if (img.GetComponent<Shadow>() == null)
        {
            var sh = img.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.28f);
            sh.effectDistance = new Vector2(4f, -4f);
            sh.useGraphicAlpha = true;
        }
    }

    static readonly Color BtnNormal = new Color(0.22f, 0.34f, 0.52f, 0.98f);
    static readonly Color BtnHighlight = new Color(0.34f, 0.52f, 0.72f, 1f);
    static readonly Color BtnPressed = new Color(0.16f, 0.26f, 0.42f, 1f);
    static readonly Color BtnDisabled = new Color(0.35f, 0.38f, 0.44f, 0.55f);

    public static Font DefaultFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    public static float S(float designPixels) => designPixels * RuntimeUiScale;

    public static Vector2 SV(float x, float y) => new Vector2(S(x), S(y));

    public static Vector2 SV(Vector2 design) => design * RuntimeUiScale;

    /// <summary>Logical pt size at reference resolution; scaled modestly so Text fits boxes.</summary>
    public static int Font(int designSize) => Mathf.Clamp(Mathf.RoundToInt(designSize * 1.22f), 12, 96);

    /// <summary>
    /// Strong outline for legacy <see cref="Text"/> so labels stay crisp on wood backdrops, gradients, and overlays.
    /// </summary>
    public static void ApplyReadableOutline(Text t, float spread = 2.25f)
    {
        if (t == null)
            return;
        var o = t.GetComponent<Outline>();
        if (o == null)
            o = t.gameObject.AddComponent<Outline>();
        o.effectColor = new Color(0f, 0f, 0f, 0.92f);
        o.effectDistance = new Vector2(spread, -spread);
        o.useGraphicAlpha = true;
    }

    public static GameObject Panel(string name, Transform parent, Color? bg = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = 5;
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        ApplyRoundedGraphic(img);
        img.color = bg ?? new Color(0.03f, 0.05f, 0.1f, 0.92f);
        img.raycastTarget = true;
        if (go.GetComponent<Outline>() == null)
        {
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.35f, 0.65f, 0.95f, 0.12f);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
        }

        return go;
    }

    public static Text Text(string name, Transform parent, string value, int fontSize, TextAnchor align, Color color, float widthDesign = 920f, float lineSpacing = 1f, FontStyle fontStyle = FontStyle.Normal)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.layer = 5;
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = DefaultFont;
        t.text = value;
        int fs = Font(fontSize);
        t.fontSize = fs;
        t.color = color;
        t.alignment = align;
        t.fontStyle = fontStyle;
        t.lineSpacing = lineSpacing;
        t.raycastTarget = false;
        t.resizeTextForBestFit = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        int lines = Mathf.Max(1, value.Split('\n').Length);
        float boxW = S(widthDesign);
        float lineH = (fs * lineSpacing * 1.28f) + 10f;
        float boxH = Mathf.Max(S(52f), lineH * lines + S(22f));
        rt.sizeDelta = new Vector2(boxW, boxH);

        return t;
    }

    public static UnityEngine.UI.Slider CreateSlider(Transform parent, float min, float max, float value, Vector2 anchoredPosition, Vector2 size)
    {
        var root = new GameObject("Slider", typeof(RectTransform));
        root.layer = 5;
        var rt = root.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = SV(size);
        rt.anchoredPosition = SV(anchoredPosition);

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.layer = 5;
        bg.transform.SetParent(root.transform, false);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        var bgImg = bg.GetComponent<Image>();
        ApplyRoundedGraphic(bgImg);
        bgImg.color = new Color(0.12f, 0.16f, 0.24f, 1f);

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.layer = 5;
        fillArea.transform.SetParent(root.transform, false);
        var faRt = fillArea.GetComponent<RectTransform>();
        faRt.anchorMin = Vector2.zero;
        faRt.anchorMax = Vector2.one;
        float pad = 14f * RuntimeUiScale;
        float py = 11f * RuntimeUiScale;
        faRt.offsetMin = new Vector2(pad, py);
        faRt.offsetMax = new Vector2(-pad, -py);

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.layer = 5;
        fill.transform.SetParent(fillArea.transform, false);
        var fRt = fill.GetComponent<RectTransform>();
        fRt.anchorMin = Vector2.zero;
        fRt.anchorMax = Vector2.one;
        fRt.offsetMin = Vector2.zero;
        fRt.offsetMax = Vector2.zero;
        var fillImg = fill.GetComponent<Image>();
        ApplyRoundedGraphic(fillImg);
        fillImg.color = new Color(0.45f, 0.95f, 0.78f, 1f);
        fillImg.raycastTarget = false;

        var handleSlide = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleSlide.layer = 5;
        handleSlide.transform.SetParent(root.transform, false);
        var hsRt = handleSlide.GetComponent<RectTransform>();
        hsRt.anchorMin = Vector2.zero;
        hsRt.anchorMax = Vector2.one;
        hsRt.offsetMin = Vector2.zero;
        hsRt.offsetMax = Vector2.zero;

        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.layer = 5;
        handle.transform.SetParent(handleSlide.transform, false);
        var hRt = handle.GetComponent<RectTransform>();
        float knob = S(32f);
        hRt.anchorMin = new Vector2(0f, 0.5f);
        hRt.anchorMax = new Vector2(0f, 0.5f);
        hRt.pivot = new Vector2(0.5f, 0.5f);
        hRt.sizeDelta = new Vector2(knob, knob);
        hRt.anchoredPosition = Vector2.zero;
        var handleImg = handle.GetComponent<Image>();
        ApplyRoundedGraphic(handleImg);
        handleImg.color = new Color(0.92f, 0.96f, 1f, 1f);
        handleImg.raycastTarget = true;

        var slider = root.AddComponent<UnityEngine.UI.Slider>();
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = hRt;
        slider.targetGraphic = handleImg;
        slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = false;
        slider.value = value;
        return slider;
    }

    public static Dropdown Dropdown(Transform parent, Vector2 anchoredPosition, Vector2 size, string[] options)
    {
        var root = new GameObject("Dropdown", typeof(RectTransform), typeof(Image), typeof(Dropdown));
        root.layer = 5;
        var rt = root.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = SV(size);
        rt.anchoredPosition = SV(anchoredPosition);
        var ddImg = root.GetComponent<Image>();
        ApplyRoundedGraphic(ddImg);
        ddImg.color = new Color(0.14f, 0.2f, 0.3f, 1f);

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelGo.layer = 5;
        labelGo.transform.SetParent(root.transform, false);
        var lRt = labelGo.GetComponent<RectTransform>();
        lRt.anchorMin = Vector2.zero;
        lRt.anchorMax = Vector2.one;
        lRt.offsetMin = SV(18, 12);
        lRt.offsetMax = SV(-38, -12);
        var label = labelGo.GetComponent<Text>();
        label.font = DefaultFont;
        label.fontSize = Font(22);
        label.fontStyle = FontStyle.Bold;
        label.color = new Color(0.98f, 0.99f, 1f, 1f);
        label.alignment = TextAnchor.MiddleLeft;
        ApplyReadableOutline(label, 2f);

        var dd = root.GetComponent<Dropdown>();
        dd.targetGraphic = ddImg;
        dd.captionText = label;
        dd.options.Clear();
        foreach (var o in options)
            dd.options.Add(new Dropdown.OptionData(o));
        dd.value = 0;
        dd.RefreshShownValue();
        return dd;
    }

    public static Button Button(string label, Transform parent, Vector2 anchoredPosition, Vector2 size, System.Action onClick, int labelFontDesign = 28)
    {
        var go = new GameObject(label + "_Btn", typeof(RectTransform), typeof(Image), typeof(Button));
        go.layer = 5;
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = SV(size);
        rt.anchoredPosition = SV(anchoredPosition);
        var img = go.GetComponent<Image>();
        ApplyRoundedGraphic(img);
        img.color = BtnNormal;
        img.raycastTarget = true;
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() =>
        {
            GameAudio.PlayUiTick();
            onClick?.Invoke();
        });

        var colors = btn.colors;
        colors.fadeDuration = 0.08f;
        colors.highlightedColor = BtnHighlight;
        colors.pressedColor = BtnPressed;
        colors.selectedColor = BtnHighlight;
        colors.disabledColor = BtnDisabled;
        btn.colors = colors;

        var shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
        shadow.effectDistance = new Vector2(4f, -4f);
        shadow.useGraphicAlpha = true;

        var txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        txtGo.layer = 5;
        txtGo.transform.SetParent(go.transform, false);
        var tr = txtGo.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        float padX = S(BtnPadXDesign);
        float padY = S(BtnPadYDesign);
        tr.offsetMin = new Vector2(padX, padY);
        tr.offsetMax = new Vector2(-padX, -padY);
        var tx = txtGo.GetComponent<Text>();
        tx.font = DefaultFont;
        tx.text = label;
        tx.fontSize = Font(labelFontDesign);
        tx.fontStyle = FontStyle.Bold;
        tx.color = new Color(0.98f, 0.995f, 1f, 1f);
        tx.alignment = TextAnchor.MiddleCenter;
        tx.resizeTextForBestFit = false;
        tx.horizontalOverflow = HorizontalWrapMode.Wrap;
        tx.verticalOverflow = VerticalWrapMode.Overflow;
        tx.lineSpacing = 1.1f;
        tx.supportRichText = false;
        ApplyReadableOutline(tx, 2.15f);
        return btn;
    }
}
