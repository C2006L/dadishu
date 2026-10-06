using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InputManager : MonoBehaviour
{
    [SerializeField] private Camera gameCamera;
    [SerializeField] private LayerMask moleLayer;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private MoleSpawner spawner;

    private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();

    private void Awake()
    {
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        if (spawner == null) spawner = FindObjectOfType<MoleSpawner>();
    }

    private void Update()
    {
        if (TryGetPointerDown(out Vector2 screenPosition))
            HandleHit(screenPosition);
    }

    private bool TryGetPointerDown(out Vector2 screenPosition)
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                screenPosition = touch.position;
                return true;
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            screenPosition = Input.mousePosition;
            return true;
        }

        screenPosition = default;
        return false;
    }

    private void HandleHit(Vector2 screenPosition)
    {
        // HUD 文字和装饰图片不应吞掉对其下方地鼠的点击，只有按钮、滑条等交互控件才拦截。
        if (IsPointerOverInteractiveUi(screenPosition)) return;

        if (gameCamera == null) gameCamera = Camera.main;
        if (gameCamera == null) return;
        Vector3 world = gameCamera.ScreenToWorldPoint(screenPosition);
        // 给予约一个鼠标指针宽度的容错，避免视觉上点到边缘却因单点检测漏判。
        Collider2D[] hits = Physics2D.OverlapCircleAll(world, 0.28f, moleLayer);
        Mole nearest = null;
        float nearestDistance = float.MaxValue;
        foreach (Collider2D hit in hits)
        {
            if (!hit.TryGetComponent(out Mole mole) || !mole.CanBeHit) continue;
            float distance = ((Vector2)mole.transform.position - (Vector2)world).sqrMagnitude;
            if (distance >= nearestDistance) continue;
            nearest = mole;
            nearestDistance = distance;
        }

        if (nearest != null)
        {
            nearest.Hit();
            return;
        }

        // 已命中的地鼠下沉期间碰撞器已关闭；再次点到同一洞口不应被误判为点空。
        if (spawner != null && spawner.ShouldSuppressMiss(world, 0.18f)) return;
        gameManager?.RegisterMiss();
    }

    private bool IsPointerOverInteractiveUi(Vector2 screenPosition)
    {
        if (EventSystem.current == null) return false;
        PointerEventData pointer = new PointerEventData(EventSystem.current) { position = screenPosition };
        uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(pointer, uiRaycastResults);
        foreach (RaycastResult result in uiRaycastResults)
        {
            Selectable selectable = result.gameObject.GetComponentInParent<Selectable>();
            if (selectable != null && selectable.IsActive() && selectable.IsInteractable()) return true;
        }
        return false;
    }
}
