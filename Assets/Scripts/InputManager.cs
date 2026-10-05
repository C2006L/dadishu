using UnityEngine;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour
{
    [SerializeField] private Camera gameCamera;
    [SerializeField] private LayerMask moleLayer;
    [SerializeField] private GameManager gameManager;

    private void Awake()
    {
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
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
        if (EventSystem.current != null)
        {
            bool overUi = Input.touchCount > 0
                ? EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)
                : EventSystem.current.IsPointerOverGameObject();
            if (overUi) return;
        }

        Vector3 world = gameCamera.ScreenToWorldPoint(screenPosition);
        Collider2D hit = Physics2D.OverlapPoint(world, moleLayer);
        if (hit != null && hit.TryGetComponent(out Mole mole))
            mole.Hit();
        else
            gameManager?.RegisterMiss();
    }
}
