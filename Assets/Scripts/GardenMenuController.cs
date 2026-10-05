using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DefaultExecutionOrder(-1100)]
public class GardenMenuController : MonoBehaviour
{
    private Font font;
    private GameManager manager;
    private AudioManager audioManager;
    private GameObject hudRoot;
    private GameObject pauseOverlay;
    private Text comboText;
    private Text muteButtonText;
    private Text musicButtonText;
    private Text sfxButtonText;
    private Text finalStatsText;
    private Coroutine comboPulse;
    private Sprite roundedSprite;

    private static readonly Color Ink = new Color(0.10f, 0.16f, 0.10f, 1f);
    private static readonly Color DeepGreen = new Color(0.055f, 0.20f, 0.12f, 0.98f);
    private static readonly Color Cream = new Color(1f, 0.95f, 0.78f, 1f);
    private static readonly Color Gold = new Color(1f, 0.76f, 0.16f, 1f);

    private void Awake()
    {
        font = Resources.Load<Font>("Fonts/NotoSansSC-VF");
        if (font == null)
            font = Font.CreateDynamicFontFromOSFont(
                new[] { "Noto Sans SC", "Microsoft YaHei UI", "Microsoft YaHei", "SimHei" }, 56);
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        manager = FindObjectOfType<GameManager>();
        audioManager = FindObjectOfType<AudioManager>();
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler != null) { scaler.referenceResolution = new Vector2(1920f, 1080f); scaler.matchWidthOrHeight = 0.5f; }
        ApplyFonts();
        BuildStartMenu();
        BuildHud();
        BuildPausePanel();
        BuildGameOverMenu();
        ShowGameplayHud(false);
    }

    public void ShowGameplayHud(bool visible)
    {
        if (hudRoot != null) hudRoot.SetActive(visible);
        SetActive("ScoreText", visible);
        SetActive("TimeText", visible);
        SetActive("StatusText", visible);
        if (!visible) ShowPausePanel(false);
    }

    public void UpdateCombo(int combo, float multiplier)
    {
        if (comboText == null) return;
        comboText.text = combo <= 0 ? "连击  —" : $"连击  {combo}     ×{multiplier:0.#}";
        comboText.color = combo >= 10 ? new Color(1f, 0.38f, 0.16f) : combo >= 6 ? Gold : combo >= 3 ? new Color(0.70f, 1f, 0.44f) : Color.white;
    }

    public void PulseComboBreak()
    {
        if (comboText == null) return;
        if (comboPulse != null) StopCoroutine(comboPulse);
        comboPulse = StartCoroutine(ComboBreakRoutine());
    }

    public void SetFinalStats(string value) { if (finalStatsText != null) finalStatsText.text = value; }

    public void ShowPausePanel(bool show)
    {
        if (pauseOverlay != null) pauseOverlay.SetActive(show);
        RefreshAudioLabels();
    }

    private void BuildStartMenu()
    {
        Transform panel = Find("StartPanel");
        if (panel == null) return;
        foreach (Transform child in panel) child.gameObject.SetActive(false);
        RectTransform rootRect = panel.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f); rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(1580f, 870f); rootRect.anchoredPosition = new Vector2(0f, -4f);
        Image rootImage = panel.GetComponent<Image>();
        rootImage.color = Color.clear;
        rootImage.raycastTarget = false;
        Image board = Image(panel, "GardenBoard", LoadSprite("UI/MenuBoardV2"));
        Place(board.rectTransform, Vector2.zero, new Vector2(1580f, 870f));
        board.preserveAspect = true;
        board.raycastTarget = false;
        board.transform.SetAsFirstSibling();

        Text title = Text(panel, "RefinedTitle", "地鼠大作战", 68, new Color(0.30f, 0.13f, 0.035f), FontStyle.Bold);
        Place(title.rectTransform, new Vector2(0f, 326f), new Vector2(900f, 82f));
        Text subtitle = Text(panel, "Subtitle", "选择模式  ·  60 秒挑战最高连击", 23, new Color(0.39f, 0.22f, 0.08f));
        Place(subtitle.rectTransform, new Vector2(0f, 254f), new Vector2(900f, 42f));

        CreateModeCard(panel, "EasyCard", new Vector2(-455f, -36f), "简单", "轻松热身", "6 个洞口 · 单只地鼠\n速度舒缓 · 无特殊鼠", "UI/EasyBadge",
            new Color(1f, 0.96f, 0.76f), new Color(0.32f, 0.67f, 0.19f), manager.StartEasyMode);
        CreateModeCard(panel, "NormalCard", new Vector2(0f, -36f), "中等", "奖励抉择", "九宫格 · 最多 2 只\n金色奖励鼠 +30 分", "UI/NormalBadge",
            new Color(1f, 0.94f, 0.72f), new Color(0.95f, 0.58f, 0.08f), manager.StartNormalMode);
        CreateModeCard(panel, "HardCard", new Vector2(455f, -36f), "困难", "避开炸弹", "九宫格 · 最多 3 只\n奖励鼠 +30 · 炸弹 -20", "UI/HardBadge",
            new Color(1f, 0.90f, 0.70f), new Color(0.86f, 0.22f, 0.12f), manager.StartHardMode);

        Text footer = Text(panel, "Footer", "连续命中可提升倍率：×1.5  →  ×2  →  ×3", 21, new Color(0.36f, 0.20f, 0.07f));
        Place(footer.rectTransform, new Vector2(0f, -354f), new Vector2(1100f, 42f));
    }

    private void CreateModeCard(Transform parent, string name, Vector2 position, string mode, string tagline,
        string rules, string iconPath, Color baseColor, Color accent, UnityAction action)
    {
        GameObject card = Panel(parent, name, baseColor);
        Place(card.GetComponent<RectTransform>(), position, new Vector2(388f, 432f));
        Roundify(card);
        AddOutline(card, new Color(accent.r * 0.68f, accent.g * 0.68f, accent.b * 0.68f), new Vector2(4f, -4f));
        AddShadow(card, new Color(0.28f, 0.13f, 0.025f, 0.48f), new Vector2(9f, -11f));
        Button button = card.AddComponent<Button>();
        ColorBlock cb = button.colors;
        cb.normalColor = Color.white; cb.highlightedColor = new Color(1.12f, 1.12f, 1.12f);
        cb.pressedColor = new Color(0.80f, 0.80f, 0.80f); cb.fadeDuration = 0.10f; button.colors = cb;
        button.onClick.AddListener(action);

        Image icon = Image(card.transform, "Badge", LoadSprite(iconPath));
        Place(icon.rectTransform, new Vector2(0f, 107f), new Vector2(145f, 145f));
        icon.preserveAspect = true; icon.raycastTarget = false;
        Text heading = Text(card.transform, "Mode", mode + "模式", 41, new Color(0.27f, 0.13f, 0.035f), FontStyle.Bold);
        Place(heading.rectTransform, new Vector2(0f, 8f), new Vector2(340f, 58f));
        Text tag = Text(card.transform, "Tagline", tagline, 22, accent, FontStyle.Normal);
        Place(tag.rectTransform, new Vector2(0f, -42f), new Vector2(340f, 38f));
        Text detail = Text(card.transform, "Rules", rules, 19, new Color(0.31f, 0.20f, 0.09f));
        detail.lineSpacing = 1.2f;
        Place(detail.rectTransform, new Vector2(0f, -105f), new Vector2(350f, 74f));
        GameObject choose = Panel(card.transform, "Choose", accent);
        Place(choose.GetComponent<RectTransform>(), new Vector2(0f, -176f), new Vector2(250f, 55f));
        Roundify(choose);
        Text chooseText = Text(choose.transform, "Label", "开始挑战  ›", 24, Color.white, FontStyle.Bold);
        Place(chooseText.rectTransform, Vector2.zero, new Vector2(240f, 50f));
    }

    private void BuildHud()
    {
        hudRoot = new GameObject("RefinedHud", typeof(RectTransform));
        hudRoot.transform.SetParent(transform, false);
        RectTransform root = hudRoot.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero;

        GameObject top = Panel(hudRoot.transform, "TopBar", Color.clear);
        RectTransform tr = top.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0f, 1f); tr.anchorMax = new Vector2(1f, 1f); tr.pivot = new Vector2(0.5f, 1f);
        tr.sizeDelta = new Vector2(0f, 126f); tr.anchoredPosition = Vector2.zero;

        GameObject scorePill = CreateHudPill(top.transform, "ScorePill", new Vector2(-770f, -52f), new Vector2(300f, 72f), new Color(0.25f, 0.49f, 0.15f, 0.94f));
        StyleExistingHudText("ScoreText", scorePill.transform, Vector2.zero, new Vector2(276f, 62f), TextAnchor.MiddleCenter, new Color(1f, 0.93f, 0.42f));

        GameObject comboPill = CreateHudPill(top.transform, "ComboPill", new Vector2(-435f, -52f), new Vector2(300f, 72f), new Color(0.42f, 0.27f, 0.08f, 0.94f));
        comboText = Text(comboPill.transform, "ComboText", "连击  —", 27, Color.white, FontStyle.Bold);
        Place(comboText.rectTransform, Vector2.zero, new Vector2(276f, 62f));

        GameObject statusPill = CreateHudPill(top.transform, "StatusPill", new Vector2(0f, -52f), new Vector2(520f, 72f), new Color(0.96f, 0.82f, 0.43f, 0.95f));
        StyleExistingHudText("StatusText", statusPill.transform, Vector2.zero, new Vector2(490f, 62f), TextAnchor.MiddleCenter, new Color(0.25f, 0.14f, 0.035f));

        GameObject timePill = CreateHudPill(top.transform, "TimePill", new Vector2(420f, -52f), new Vector2(250f, 72f), new Color(0.19f, 0.48f, 0.58f, 0.95f));
        StyleExistingHudText("TimeText", timePill.transform, Vector2.zero, new Vector2(228f, 62f), TextAnchor.MiddleCenter, Color.white);

        CreateSmallButton(top.transform, "MuteButton", new Vector2(665f, -52f), new Vector2(130f, 72f), "音效", () => {
            audioManager.ToggleSfx(); RefreshAudioLabels();
        }, out muteButtonText);
        CreateSmallButton(top.transform, "PauseButton", new Vector2(835f, -52f), new Vector2(160f, 72f), "暂停 Ⅱ", manager.PauseGame, out _);
    }

    private void BuildPausePanel()
    {
        pauseOverlay = Panel(transform, "PauseOverlay", new Color(0.05f, 0.11f, 0.055f, 0.72f));
        RectTransform overlay = pauseOverlay.GetComponent<RectTransform>();
        overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one; overlay.offsetMin = overlay.offsetMax = Vector2.zero;
        pauseOverlay.transform.SetAsLastSibling();
        GameObject panel = Panel(pauseOverlay.transform, "PauseCard", new Color(1f, 0.91f, 0.66f, 1f));
        Place(panel.GetComponent<RectTransform>(), Vector2.zero, new Vector2(720f, 720f));
        Roundify(panel);
        AddOutline(panel, new Color(0.45f, 0.23f, 0.07f), new Vector2(6f, -6f)); AddShadow(panel, Color.black, new Vector2(15f, -18f));
        Text title = Text(panel.transform, "Title", "游戏暂停", 60, new Color(0.35f, 0.17f, 0.04f), FontStyle.Bold);
        Place(title.rectTransform, new Vector2(0f, 276f), new Vector2(600f, 82f));
        Text hint = Text(panel.transform, "Hint", "休息一下，调整设置后继续挑战", 24, new Color(0.43f, 0.28f, 0.11f));
        Place(hint.rectTransform, new Vector2(0f, 220f), new Vector2(600f, 38f));

        Text volumeLabel = Text(panel.transform, "VolumeLabel", "总音量", 28, new Color(0.30f, 0.16f, 0.05f), FontStyle.Bold);
        volumeLabel.alignment = TextAnchor.MiddleLeft;
        Place(volumeLabel.rectTransform, new Vector2(-210f, 137f), new Vector2(180f, 42f));
        Slider slider = CreateSlider(panel.transform, new Vector2(75f, 137f));
        slider.value = audioManager != null ? audioManager.MasterVolume : 0.8f;
        slider.onValueChanged.AddListener(v => audioManager.SetMasterVolume(v));

        CreateSmallButton(panel.transform, "MusicToggle", new Vector2(-160f, 48f), new Vector2(280f, 68f), "背景音乐", () => {
            audioManager.SetMusicEnabled(!audioManager.MusicEnabled); RefreshAudioLabels();
        }, out musicButtonText);
        CreateSmallButton(panel.transform, "SfxToggle", new Vector2(160f, 48f), new Vector2(280f, 68f), "游戏音效", () => {
            audioManager.SetSfxEnabled(!audioManager.SfxEnabled); RefreshAudioLabels();
        }, out sfxButtonText);
        CreateWideButton(panel.transform, "Resume", "继续游戏", new Vector2(0f, -55f), new Color(0.38f, 0.73f, 0.20f), manager.ResumeGame);
        CreateWideButton(panel.transform, "Restart", "重新开始", new Vector2(0f, -150f), new Color(0.88f, 0.53f, 0.12f), manager.RestartGame);
        CreateWideButton(panel.transform, "Menu", "返回模式选择", new Vector2(0f, -245f), new Color(0.55f, 0.29f, 0.12f), manager.ReturnToMenu);
        RefreshAudioLabels();
        pauseOverlay.SetActive(false);
    }

    private void BuildGameOverMenu()
    {
        Transform panel = Find("GameOverPanel");
        if (panel == null) return;
        RectTransform pr = panel.GetComponent<RectTransform>();
        pr.sizeDelta = new Vector2(820f, 610f); pr.anchoredPosition = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(1f, 0.91f, 0.66f, 0.99f);
        Roundify(panel.gameObject);
        AddOutline(panel.gameObject, new Color(0.45f, 0.23f, 0.07f), new Vector2(6f, -6f)); AddShadow(panel.gameObject, Color.black, new Vector2(15f, -18f));
        ConfigureExisting("GameOverTitle", "挑战完成", 58, new Color(0.36f, 0.17f, 0.035f), new Vector2(0f, 205f), new Vector2(650f, 80f));
        ConfigureExisting("FinalScoreText", "简单模式  ·  0 分", 39, new Color(0.23f, 0.31f, 0.12f), new Vector2(0f, 120f), new Vector2(650f, 70f));
        finalStatsText = Text(panel, "FinalStats", "最高连击  0", 27, new Color(0.37f, 0.25f, 0.10f));
        finalStatsText.lineSpacing = 1.35f;
        Place(finalStatsText.rectTransform, new Vector2(0f, 34f), new Vector2(650f, 100f));
        Transform restart = Find("RestartButton");
        if (restart != null)
        {
            Place(restart.GetComponent<RectTransform>(), new Vector2(0f, -92f), new Vector2(500f, 72f));
            StyleButton(restart.gameObject, "再来一局", new Color(0.38f, 0.73f, 0.20f));
        }
        Transform oldBack = panel.Find("BackToMenuButton");
        if (oldBack != null) oldBack.gameObject.SetActive(false);
        CreateWideButton(panel, "RefinedBack", "返回模式选择", new Vector2(0f, -190f), new Color(0.68f, 0.36f, 0.11f), manager.ReturnToMenu);
    }

    private IEnumerator ComboBreakRoutine()
    {
        comboText.text = "连击中断"; comboText.color = new Color(1f, 0.35f, 0.20f);
        float elapsed = 0f;
        while (elapsed < 0.45f)
        {
            elapsed += Time.unscaledDeltaTime;
            comboText.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(elapsed * 22f) * 0.08f);
            yield return null;
        }
        comboText.rectTransform.localScale = Vector3.one;
        comboText.text = "连击  —"; comboText.color = Color.white;
    }

    private void RefreshAudioLabels()
    {
        if (audioManager == null) return;
        if (muteButtonText != null) muteButtonText.text = audioManager.SfxEnabled ? "音效 ✓" : "静音 ×";
        if (musicButtonText != null) musicButtonText.text = audioManager.MusicEnabled ? "背景音乐  开" : "背景音乐  关";
        if (sfxButtonText != null) sfxButtonText.text = audioManager.SfxEnabled ? "游戏音效  开" : "游戏音效  关";
    }

    private Slider CreateSlider(Transform parent, Vector2 position)
    {
        GameObject root = new GameObject("MasterVolume", typeof(RectTransform), typeof(Slider));
        root.transform.SetParent(parent, false); Place(root.GetComponent<RectTransform>(), position, new Vector2(390f, 46f));
        GameObject bg = Panel(root.transform, "Background", new Color(0.02f, 0.07f, 0.04f));
        RectTransform br = bg.GetComponent<RectTransform>(); br.anchorMin = new Vector2(0f, 0.5f); br.anchorMax = new Vector2(1f, 0.5f); br.sizeDelta = new Vector2(0f, 16f);
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform)); fillArea.transform.SetParent(root.transform, false);
        RectTransform fa = fillArea.GetComponent<RectTransform>(); fa.anchorMin = new Vector2(0f, 0.25f); fa.anchorMax = new Vector2(1f, 0.75f); fa.offsetMin = new Vector2(8f, 0f); fa.offsetMax = new Vector2(-8f, 0f);
        GameObject fill = Panel(fillArea.transform, "Fill", Gold); RectTransform fr = fill.GetComponent<RectTransform>(); fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one; fr.offsetMin = fr.offsetMax = Vector2.zero;
        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform)); handleArea.transform.SetParent(root.transform, false);
        RectTransform ha = handleArea.GetComponent<RectTransform>(); ha.anchorMin = Vector2.zero; ha.anchorMax = Vector2.one; ha.offsetMin = new Vector2(10f, 0f); ha.offsetMax = new Vector2(-10f, 0f);
        GameObject handle = Panel(handleArea.transform, "Handle", Color.white); Place(handle.GetComponent<RectTransform>(), Vector2.zero, new Vector2(34f, 34f));
        Slider slider = root.GetComponent<Slider>(); slider.fillRect = fr; slider.handleRect = handle.GetComponent<RectTransform>(); slider.targetGraphic = handle.GetComponent<Image>(); slider.minValue = 0f; slider.maxValue = 1f;
        return slider;
    }

    private void CreateSmallButton(Transform parent, string name, Vector2 position, Vector2 size, string label,
        UnityAction action, out Text labelText)
    {
        GameObject obj = Panel(parent, name, new Color(0.22f, 0.48f, 0.17f, 0.96f));
        Place(obj.GetComponent<RectTransform>(), position, size); Roundify(obj); AddOutline(obj, new Color(0.48f, 0.72f, 0.27f), new Vector2(2f, -2f));
        Button button = obj.AddComponent<Button>(); button.onClick.AddListener(action);
        labelText = Text(obj.transform, "Label", label, 22, Color.white, FontStyle.Bold); Place(labelText.rectTransform, Vector2.zero, size - new Vector2(8f, 8f));
    }

    private void CreateWideButton(Transform parent, string name, string label, Vector2 position, Color color, UnityAction action)
    {
        GameObject obj = Panel(parent, name, color); Place(obj.GetComponent<RectTransform>(), position, new Vector2(500f, 72f));
        Roundify(obj);
        AddOutline(obj, new Color(0.12f, 0.08f, 0.02f), new Vector2(3f, -3f)); AddShadow(obj, Color.black, new Vector2(5f, -6f));
        Button button = obj.AddComponent<Button>(); button.onClick.AddListener(action);
        Text text = Text(obj.transform, "Label", label, 28, Color.white, FontStyle.Bold); Place(text.rectTransform, Vector2.zero, new Vector2(480f, 64f));
    }

    private GameObject CreateHudPill(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        GameObject pill = Panel(parent, name, color);
        Place(pill.GetComponent<RectTransform>(), position, size);
        Roundify(pill);
        AddOutline(pill, new Color(1f, 0.92f, 0.60f, 0.58f), new Vector2(2f, -2f));
        AddShadow(pill, new Color(0.12f, 0.08f, 0.02f, 0.48f), new Vector2(4f, -5f));
        return pill;
    }

    private void Roundify(GameObject obj)
    {
        Image image = obj.GetComponent<Image>();
        if (image == null) return;
        if (roundedSprite == null) roundedSprite = CreateRoundedSprite();
        image.sprite = roundedSprite;
        image.type = UnityEngine.UI.Image.Type.Sliced;
    }

    private static Sprite CreateRoundedSprite()
    {
        const int size = 64;
        const float radius = 18f;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "RoundedUiRuntime";
        texture.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = Mathf.Max(radius - x, 0f) + Mathf.Max(x - (size - 1 - radius), 0f);
            float dy = Mathf.Max(radius - y, 0f) + Mathf.Max(y - (size - 1 - radius), 0f);
            float alpha = 1f - Mathf.Clamp01((Mathf.Sqrt(dx * dx + dy * dy) - radius + 1f) / 2f);
            pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
        }
        texture.SetPixels(pixels); texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(20f, 20f, 20f, 20f));
    }

    private void StyleExistingHudText(string name, Transform newParent, Vector2 position, Vector2 size, TextAnchor alignment, Color color)
    {
        Transform t = Find(name); if (t == null) return;
        t.SetParent(newParent, false);
        // 场景旧版 HUD 自带黑色 Outline。更换字体后继续叠加会形成粗重影，先禁用旧效果。
        foreach (Shadow effect in t.GetComponents<Shadow>())
            effect.enabled = false;
        Text text = t.GetComponent<Text>(); text.font = font; text.fontSize = 32; text.fontStyle = FontStyle.Bold; text.color = color; text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        if (color.grayscale > 0.58f)
        {
            Shadow subtle = t.gameObject.AddComponent<Shadow>();
            subtle.effectColor = new Color(0f, 0f, 0f, 0.34f);
            subtle.effectDistance = new Vector2(1f, -1f);
            subtle.useGraphicAlpha = true;
        }
        Place(text.rectTransform, position, size); t.SetAsLastSibling();
    }

    private void ConfigureExisting(string name, string value, int size, Color color, Vector2 position, Vector2 rectSize)
    {
        Transform t = Find(name); if (t == null) return;
        Text text = t.GetComponent<Text>(); if (text == null) return;
        text.font = font; text.fontStyle = FontStyle.Bold; text.fontSize = size; text.text = value; text.color = color; Place(text.rectTransform, position, rectSize);
    }

    private void StyleButton(GameObject obj, string label, Color color)
    {
        Image image = obj.GetComponent<Image>(); if (image != null) image.color = color;
        AddOutline(obj, Ink, new Vector2(3f, -3f));
        Text text = obj.GetComponentInChildren<Text>(true); if (text != null) { text.font = font; text.fontStyle = FontStyle.Bold; text.fontSize = 28; text.text = label; text.color = Color.white; }
    }

    private void ApplyFonts()
    {
        foreach (Text text in GetComponentsInChildren<Text>(true))
        {
            text.font = font;
            text.fontStyle = FontStyle.Normal;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
        }
    }

    private Text Text(Transform parent, string name, string value, int size, Color color, FontStyle style = FontStyle.Normal)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); obj.transform.SetParent(parent, false);
        Text text = obj.GetComponent<Text>(); text.font = font; text.text = value; text.fontSize = size; text.fontStyle = style; text.color = color; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
        if (color.grayscale > 0.58f)
            AddShadow(obj, new Color(0f, 0f, 0f, 0.38f), new Vector2(1f, -1f));
        return text;
    }

    private static GameObject Panel(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); obj.transform.SetParent(parent, false); obj.GetComponent<Image>().color = color; return obj;
    }

    private static Image Image(Transform parent, string name, Sprite sprite)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); obj.transform.SetParent(parent, false); Image image = obj.GetComponent<Image>(); image.sprite = sprite; image.color = Color.white; return image;
    }

    private static Sprite LoadSprite(string path)
    {
        Texture2D texture = Resources.Load<Texture2D>(path); if (texture == null) return null;
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.pivot = new Vector2(0.5f, 0.5f); rect.anchoredPosition = position; rect.sizeDelta = size;
    }

    private static void AddOutline(GameObject obj, Color color, Vector2 distance)
    {
        Outline outline = obj.GetComponent<Outline>(); if (outline == null) outline = obj.AddComponent<Outline>(); outline.effectColor = color; outline.effectDistance = distance; outline.useGraphicAlpha = true;
    }

    private static void AddShadow(GameObject obj, Color color, Vector2 distance)
    {
        Shadow shadow = null; foreach (Shadow s in obj.GetComponents<Shadow>()) if (!(s is Outline)) { shadow = s; break; }
        if (shadow == null) shadow = obj.AddComponent<Shadow>(); shadow.effectColor = color; shadow.effectDistance = distance; shadow.useGraphicAlpha = true;
    }

    private Transform Find(string name)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true)) if (child.name == name) return child; return null;
    }

    private void SetActive(string name, bool active) { Transform t = Find(name); if (t != null) t.gameObject.SetActive(active); }
}
