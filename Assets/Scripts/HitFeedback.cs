using UnityEngine;

public static class HitFeedback
{
    private static Sprite sparkSprite;

    public static void Play(Vector3 worldPosition, string label, Color color, bool explosive = false)
    {
        CreateScoreText(worldPosition, label, color);
        CreateSparks(worldPosition, color, explosive ? 16 : 8);
        if (explosive && Camera.main != null)
            Camera.main.gameObject.AddComponent<CameraShakeEffect>();
    }

    private static void CreateScoreText(Vector3 position, string label, Color color)
    {
        GameObject obj = new GameObject("HitScoreFeedback");
        obj.transform.position = position;
        TextMesh text = obj.AddComponent<TextMesh>();
        text.text = label;
        text.font = Resources.Load<Font>("Fonts/NotoSansSC-VF");
        if (text.font == null)
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontStyle = FontStyle.Bold;
        text.fontSize = 72;
        text.characterSize = 0.055f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = color;
        MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
        renderer.material = text.font.material;
        renderer.sortingOrder = 20;
        obj.AddComponent<FloatingScoreEffect>();
    }

    private static void CreateSparks(Vector3 position, Color primary, int count)
    {
        if (sparkSprite == null)
        {
            Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Color[] colors = new Color[16];
            for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
            texture.SetPixels(colors);
            texture.Apply();
            sparkSprite = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            sparkSprite.name = "HitSparkRuntime";
        }

        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / count;
            Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            GameObject spark = new GameObject("HitSpark");
            spark.transform.position = position;
            spark.transform.localScale = new Vector3(0.13f, 0.13f, 1f);
            SpriteRenderer renderer = spark.AddComponent<SpriteRenderer>();
            renderer.sprite = sparkSprite;
            renderer.color = i % 2 == 0 ? primary : Color.Lerp(primary, Color.white, 0.55f);
            renderer.sortingOrder = 19;
            HitSparkEffect effect = spark.AddComponent<HitSparkEffect>();
            effect.Initialize(direction);
        }
    }
}

public class CameraShakeEffect : MonoBehaviour
{
    private Vector3 origin;
    private float elapsed;
    private const float Duration = 0.22f;
    private void Awake() { origin = transform.localPosition; }
    private void LateUpdate()
    {
        elapsed += Time.unscaledDeltaTime;
        float strength = Mathf.Lerp(0.16f, 0f, elapsed / Duration);
        transform.localPosition = origin + (Vector3)Random.insideUnitCircle * strength;
        if (elapsed >= Duration) { transform.localPosition = origin; Destroy(this); }
    }
}

public class FloatingScoreEffect : MonoBehaviour
{
    private TextMesh text;
    private float elapsed;
    private const float Duration = 0.72f;

    private void Awake()
    {
        text = GetComponent<TextMesh>();
        transform.localScale = Vector3.one * 0.65f;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / Duration);
        transform.position += Vector3.up * (1.15f * Time.deltaTime);
        transform.localScale = Vector3.one * Mathf.Lerp(0.65f, 1.05f, Mathf.Sin(t * Mathf.PI));
        Color color = text.color;
        color.a = 1f - t;
        text.color = color;
        if (elapsed >= Duration) Destroy(gameObject);
    }
}

public class HitSparkEffect : MonoBehaviour
{
    private Vector3 direction;
    private SpriteRenderer spriteRenderer;
    private float elapsed;
    private const float Duration = 0.38f;

    public void Initialize(Vector3 moveDirection)
    {
        direction = moveDirection.normalized;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / Duration);
        transform.position += direction * (2.4f * (1f - t) * Time.deltaTime);
        transform.Rotate(0f, 0f, 300f * Time.deltaTime);
        transform.localScale = Vector3.one * Mathf.Lerp(0.13f, 0.02f, t);
        Color color = spriteRenderer.color;
        color.a = 1f - t;
        spriteRenderer.color = color;
        if (elapsed >= Duration) Destroy(gameObject);
    }
}
