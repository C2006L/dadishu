using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-1000)]
public class UIThemeController : MonoBehaviour
{
    private Font chineseFont;

    private void Awake()
    {
        GardenMenuController gardenMenu = GetComponent<GardenMenuController>();
        if (gardenMenu == null) gardenMenu = gameObject.AddComponent<GardenMenuController>();
        if (gardenMenu != null) return;

        chineseFont = Font.CreateDynamicFontFromOSFont(
            new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Arial" }, 48);
        if (chineseFont == null)
            chineseFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        CreateHudBackground();
        ApplyTextTheme();
        ApplyPanelTheme("StartPanel", new Color(0.06f, 0.16f, 0.12f, 0.94f));
        ApplyPanelTheme("GameOverPanel", new Color(0.15f, 0.07f, 0.06f, 0.95f));
        ApplyButtonTheme("StartButton");
        ApplyButtonTheme("RestartButton");
    }

    public void ShowGameplayHud(bool visible)
    {
        GardenMenuController gardenMenu = GetComponent<GardenMenuController>();
        if (gardenMenu != null)
        {
            gardenMenu.ShowGameplayHud(visible);
            return;
        }

        Transform bar = transform.Find("HudBackground");
        if (bar != null) bar.gameObject.SetActive(visible);
        SetNamedObjectActive("ScoreText", visible);
        SetNamedObjectActive("TimeText", visible);
        SetNamedObjectActive("StatusText", visible);
    }

    private void SetNamedObjectActive(string name, bool active)
    {
        Transform target = FindDeepChild(transform, name);
        if (target != null) target.gameObject.SetActive(active);
    }

    private void CreateHudBackground()
    {
        if (transform.Find("HudBackground") != null) return;

        GameObject bar = new GameObject("HudBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bar.transform.SetParent(transform, false);
        bar.transform.SetAsFirstSibling();
        RectTransform rect = bar.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, 108f);
        Image image = bar.GetComponent<Image>();
        image.color = new Color(0.025f, 0.10f, 0.075f, 0.66f);
        image.raycastTarget = false;
    }

    private void ApplyTextTheme()
    {
        foreach (Text text in GetComponentsInChildren<Text>(true))
        {
            text.font = chineseFont;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
        }

        ConfigureText("ScoreText", "得分：0", 42, new Color(1f, 0.88f, 0.22f));
        ConfigureText("TimeText", "剩余：60 秒", 42, new Color(0.42f, 0.92f, 1f));
        ConfigureText("StatusText", "准备好了吗？", 42, Color.white);
        ConfigureText("Title", "欢乐打地鼠", 72, new Color(1f, 0.79f, 0.16f));
        ConfigureText("Instructions", "点击地鼠，每次获得 10 分\n在 60 秒内挑战更高分！", 32, Color.white);
        ConfigureText("GameOverTitle", "游戏结束", 64, new Color(1f, 0.74f, 0.18f));
        ConfigureText("FinalScoreText", "最终得分：0", 44, Color.white);
        ConfigureText("StartButton/Label", "开始游戏", 36, new Color(0.10f, 0.08f, 0.03f));
        ConfigureText("RestartButton/Label", "再玩一次", 36, new Color(0.10f, 0.08f, 0.03f));
    }

    private void ConfigureText(string path, string value, int fontSize, Color color)
    {
        Transform target = FindDeepChild(transform, path);
        if (target == null) return;
        Text text = target.GetComponent<Text>();
        if (text == null) return;
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;

        if (text.GetComponent<Shadow>() == null)
        {
            Shadow shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
            shadow.effectDistance = new Vector2(2f, -2f);
        }
    }

    private void ApplyPanelTheme(string panelName, Color color)
    {
        Transform panel = transform.Find(panelName);
        if (panel == null) return;
        Image image = panel.GetComponent<Image>();
        if (image != null) image.color = color;
    }

    private void ApplyButtonTheme(string buttonName)
    {
        Transform target = FindDeepChild(transform, buttonName);
        if (target == null) return;
        Button button = target.GetComponent<Button>();
        Image image = target.GetComponent<Image>();
        if (button == null || image == null) return;

        image.color = new Color(1f, 0.63f, 0.12f, 1f);
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.91f, 0.62f, 1f);
        colors.pressedColor = new Color(0.93f, 0.66f, 0.28f, 1f);
        button.colors = colors;
    }

    private static Transform FindDeepChild(Transform root, string path)
    {
        string[] parts = path.Split('/');
        Transform current = root;
        foreach (string part in parts)
        {
            Transform direct = current.Find(part);
            if (direct != null)
            {
                current = direct;
                continue;
            }

            Transform found = null;
            foreach (Transform child in current.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == part)
                {
                    found = child;
                    break;
                }
            }
            if (found == null) return null;
            current = found;
        }
        return current;
    }
}
