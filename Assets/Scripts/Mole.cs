using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class Mole : MonoBehaviour
{
    [SerializeField] private float popDuration = 0.22f;
    [SerializeField] private float hideDuration = 0.22f;
    [SerializeField] private float riseDistance = 1.62f;
    [SerializeField] private float shownHeightOffset = -0.06f;
    [SerializeField] private Color hitColor = new Color(1f, 0.65f, 0.65f, 1f);

    private GameManager gameManager;
    private MoleSpawner spawner;
    private SpriteRenderer spriteRenderer;
    private Collider2D hitCollider;
    private BoxCollider2D boxCollider;
    private Sprite normalSprite;
    private Sprite rewardSprite;
    private Sprite bombSprite;
    private Vector3 shownScale, shownPosition, hiddenPosition;
    private bool isHit, isVisible;
    private MoleType type;
    private Coroutine lifeRoutine;
    private static Sprite cachedHoleFrontSprite;
    private static Sprite cachedRevealMaskSprite;

    public bool IsVisible => isVisible;
    public bool CanBeHit => isVisible && !isHit && hitCollider != null && hitCollider.enabled &&
                            spriteRenderer != null && spriteRenderer.color.a >= 0.20f;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        hitCollider = GetComponent<Collider2D>();
        boxCollider = hitCollider as BoxCollider2D;
        popDuration = Mathf.Max(popDuration, 0.20f);
        hideDuration = Mathf.Max(hideDuration, 0.18f);
        riseDistance = Mathf.Max(riseDistance, 1.62f);
        spriteRenderer.sortingOrder = 2;
        normalSprite = spriteRenderer.sprite;
        shownScale = transform.localScale;
        // 兼容旧场景中序列化的偏高数值，限制最高位置，防止下排地鼠顶进上一排土堆。
        shownHeightOffset = Mathf.Min(shownHeightOffset, -0.06f);
        shownPosition = transform.localPosition + Vector3.up * shownHeightOffset;
        hiddenPosition = shownPosition + Vector3.down * riseDistance;
        CreateHoleFrontOverlay();
        HideImmediate();
    }

    public void Configure(GameManager manager, MoleSpawner owner)
    {
        gameManager = manager;
        spawner = owner;
    }

    public void Show(MoleType newType, float visibleTime)
    {
        StopAllCoroutines();
        type = newType;
        ApplyTypeSprite();
        SyncHitCollider();
        isHit = false;
        isVisible = true;
        spriteRenderer.color = new Color(1f, 1f, 1f, 0f);
        spriteRenderer.enabled = true;
        // 碰撞器跟随地鼠从洞内上升；地鼠进入可见区域后即可命中，不再丢失快速点击。
        hitCollider.enabled = true;
        transform.localPosition = hiddenPosition;
        transform.localScale = shownScale * 0.88f;
        lifeRoutine = StartCoroutine(LifeRoutine(visibleTime));
    }

    private IEnumerator LifeRoutine(float visibleTime)
    {
        yield return MoveAndScaleRoutine(hiddenPosition, shownPosition, shownScale * 0.88f, shownScale,
            popDuration, 0f, 1f);
        if (!isVisible || isHit) yield break;
        yield return new WaitForSeconds(Mathf.Max(0.1f, visibleTime - popDuration - hideDuration));
        if (!isVisible || isHit) yield break;
        hitCollider.enabled = false;
        yield return HideRoutine();
        spawner?.NotifyMoleFinished(this, false, type);
    }

    public void Hide()
    {
        if (!isVisible) return;
        hitCollider.enabled = false;
        StopAllCoroutines();
        lifeRoutine = StartCoroutine(HideRoutine());
    }

    public void HideImmediate()
    {
        StopAllCoroutines();
        lifeRoutine = null;
        isVisible = false;
        isHit = false;
        transform.localScale = shownScale == Vector3.zero ? transform.localScale : shownScale;
        transform.localPosition = hiddenPosition;
        if (spriteRenderer != null) { spriteRenderer.enabled = false; spriteRenderer.color = Color.white; }
        if (hitCollider != null) hitCollider.enabled = false;
    }

    public void Hit()
    {
        if (!isVisible || isHit || gameManager == null || !gameManager.IsPlaying) return;
        isHit = true;
        hitCollider.enabled = false;
        int delta = gameManager.RegisterMoleHit(type);
        Color feedback = type == MoleType.Bomb ? new Color(1f, 0.18f, 0.08f) :
            type == MoleType.Reward ? new Color(1f, 0.82f, 0.08f) : new Color(1f, 0.92f, 0.25f);
        string label = delta > 0 ? "+" + delta : delta < 0 ? delta.ToString() : "连击中断";
        HitFeedback.Play(transform.position + Vector3.up * 0.8f, label, feedback, type == MoleType.Bomb);
        StopAllCoroutines();
        lifeRoutine = StartCoroutine(HitRoutine());
    }

    private IEnumerator HitRoutine()
    {
        spriteRenderer.color = type == MoleType.Bomb ? new Color(1f, 0.35f, 0.28f, 1f) : hitColor;
        Vector3 pressedScale = new Vector3(shownScale.x * 1.04f, shownScale.y * 0.94f, shownScale.z);
        Vector3 pressedPosition = transform.localPosition + Vector3.down * 0.06f;
        yield return MoveAndScaleRoutine(transform.localPosition, pressedPosition, transform.localScale, pressedScale,
            0.08f, 1f, 1f);
        yield return MoveAndScaleRoutine(pressedPosition, hiddenPosition, pressedScale, shownScale * 0.88f,
            hideDuration, 1f, 1f);
        SetHidden();
        spawner?.NotifyMoleFinished(this, true, type);
    }

    private IEnumerator HideRoutine()
    {
        yield return MoveAndScaleRoutine(transform.localPosition, hiddenPosition, transform.localScale,
            shownScale * 0.88f, hideDuration, spriteRenderer.color.a, 0f);
        SetHidden();
    }

    private void ApplyTypeSprite()
    {
        if (type == MoleType.Normal) { spriteRenderer.sprite = normalSprite; return; }
        if (type == MoleType.Reward)
        {
            if (rewardSprite == null) rewardSprite = LoadRuntimeSprite("Moles/RewardMole", "RewardMoleRuntime", MoleType.Reward);
            spriteRenderer.sprite = rewardSprite != null ? rewardSprite : normalSprite;
        }
        else
        {
            if (bombSprite == null) bombSprite = LoadRuntimeSprite("Moles/BombMole", "BombMoleRuntime", MoleType.Bomb);
            spriteRenderer.sprite = bombSprite != null ? bombSprite : normalSprite;
        }
    }

    private void SyncHitCollider()
    {
        if (boxCollider == null || spriteRenderer.sprite == null) return;
        Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
        // 点击区域覆盖头部和身体，但不延伸到透明边缘、皇冠火花或炸弹引线。
        boxCollider.size = new Vector2(spriteSize.x * 0.88f, spriteSize.y * 0.66f);
        boxCollider.offset = new Vector2(0f, spriteSize.y * 0.14f);
    }

    private Sprite LoadRuntimeSprite(string path, string name, MoleType spriteType)
    {
        Texture2D texture = Resources.Load<Texture2D>(path);
        if (texture == null) return null;
        // 三张图片透明留白不同。使用实画内容宽度计算 PPU，并调整 pivot 统一脚底基线。
        float normalContentWidth = normalSprite != null ? normalSprite.bounds.size.x * (912f / 1312f) : 9.12f;
        float contentWidthPixels = spriteType == MoleType.Reward ? 940f : 1012f;
        float pixelsPerUnit = contentWidthPixels / normalContentWidth;
        Vector2 pivot = spriteType == MoleType.Reward
            ? new Vector2(650f / 1312f, 552f / 1199f)
            : new Vector2(678f / 1312f, 560f / 1199f);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
            pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect);
        sprite.name = name;
        return sprite;
    }

    private IEnumerator MoveAndScaleRoutine(Vector3 fromPosition, Vector3 toPosition, Vector3 fromScale,
        Vector3 toScale, float duration, float fromAlpha, float toAlpha)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            transform.localPosition = Vector3.LerpUnclamped(fromPosition, toPosition, eased);
            transform.localScale = Vector3.LerpUnclamped(fromScale, toScale, eased);
            Color color = spriteRenderer.color;
            color.a = Mathf.Lerp(fromAlpha, toAlpha, t);
            spriteRenderer.color = color;
            yield return null;
        }
        transform.localPosition = toPosition;
        transform.localScale = toScale;
        Color finalColor = spriteRenderer.color;
        finalColor.a = toAlpha;
        spriteRenderer.color = finalColor;
    }

    private void SetHidden()
    {
        isVisible = false;
        lifeRoutine = null;
        spriteRenderer.enabled = false;
        spriteRenderer.color = Color.white;
        transform.localScale = shownScale;
        transform.localPosition = hiddenPosition;
    }

    private void CreateHoleFrontOverlay()
    {
        Transform slot = transform.parent;
        if (slot == null) return;
        SpriteRenderer holeRenderer = null;
        foreach (SpriteRenderer candidate in slot.GetComponentsInChildren<SpriteRenderer>(true))
            if (candidate != spriteRenderer && candidate.name.StartsWith("Hole")) { holeRenderer = candidate; break; }
        if (holeRenderer == null || holeRenderer.sprite == null) return;
        const float frontHeightRatio = 0.43f;
        if (cachedHoleFrontSprite == null)
        {
            Sprite source = holeRenderer.sprite;
            Rect sourceRect = source.rect;
            float frontHeight = sourceRect.height * frontHeightRatio;
            cachedHoleFrontSprite = Sprite.Create(source.texture,
                new Rect(sourceRect.x, sourceRect.y, sourceRect.width, frontHeight),
                new Vector2(0.5f, 1f), source.pixelsPerUnit, 0, SpriteMeshType.FullRect, Vector4.zero, false);
            cachedHoleFrontSprite.name = "HoleFrontRuntime";
        }
        float frontTopOffset = holeRenderer.transform.localScale.y *
            (-holeRenderer.sprite.bounds.size.y * 0.5f + holeRenderer.sprite.bounds.size.y * frontHeightRatio);
        if (slot.Find("HoleFront") == null)
        {
            GameObject frontObject = new GameObject("HoleFront");
            frontObject.transform.SetParent(slot, false);
            frontObject.transform.localPosition = holeRenderer.transform.localPosition + Vector3.up * frontTopOffset;
            frontObject.transform.localRotation = holeRenderer.transform.localRotation;
            frontObject.transform.localScale = holeRenderer.transform.localScale;
            SpriteRenderer frontRenderer = frontObject.AddComponent<SpriteRenderer>();
            frontRenderer.sprite = cachedHoleFrontSprite;
            frontRenderer.color = holeRenderer.color;
            frontRenderer.sortingLayerID = holeRenderer.sortingLayerID;
            frontRenderer.sortingOrder = 4;
        }

        CreateRevealMask(slot, holeRenderer, frontTopOffset);
    }

    private void CreateRevealMask(Transform slot, SpriteRenderer holeRenderer, float frontTopOffset)
    {
        // 每个洞口使用独立 SortingGroup，避免同列洞口的遮罩互相影响。
        SortingGroup group = slot.GetComponent<SortingGroup>();
        if (group == null) group = slot.gameObject.AddComponent<SortingGroup>();
        group.sortingLayerID = holeRenderer.sortingLayerID;

        Transform existing = slot.Find("MoleRevealMask");
        if (existing == null)
        {
            if (cachedRevealMaskSprite == null)
            {
                Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[16];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                texture.SetPixels(pixels);
                texture.Apply();
                texture.name = "MoleRevealMaskTexture";
                cachedRevealMaskSprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f),
                    new Vector2(0.5f, 0f), 4f, 0, SpriteMeshType.FullRect);
                cachedRevealMaskSprite.name = "MoleRevealMaskSprite";
            }

            GameObject maskObject = new GameObject("MoleRevealMask");
            maskObject.transform.SetParent(slot, false);
            maskObject.transform.localPosition = holeRenderer.transform.localPosition +
                                                 Vector3.up * (frontTopOffset + 0.02f);
            float openingWidth = holeRenderer.sprite.bounds.size.x * holeRenderer.transform.localScale.x * 0.68f;
            maskObject.transform.localScale = new Vector3(openingWidth, 5.5f, 1f);
            SpriteMask mask = maskObject.AddComponent<SpriteMask>();
            mask.sprite = cachedRevealMaskSprite;
            mask.alphaCutoff = 0.1f;
            mask.isCustomRangeActive = true;
            mask.frontSortingLayerID = holeRenderer.sortingLayerID;
            mask.backSortingLayerID = holeRenderer.sortingLayerID;
            mask.frontSortingOrder = 3;
            mask.backSortingOrder = 1;
        }

        spriteRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
    }
}
