using UnityEngine;

public class TutorialController : MonoBehaviour
{
    private const float ClickMovementThresholdPixels = 20f;

    [SerializeField] private TutorialBoardManager tutorialBoardManager;

    private Camera mainCamera;
    private Vector2 pointerDownScreenPosition;
    private bool pointerIsDown;
    private bool dragHasStarted;

    private void Start()
    {
        if (tutorialBoardManager == null)
        {
            tutorialBoardManager = FindAnyObjectByType<TutorialBoardManager>();
        }

        mainCamera = Camera.main;

        if (tutorialBoardManager == null)
        {
            Debug.LogWarning("TutorialController could not find TutorialBoardManager.");
        }
    }

    private void Update()
    {
        if (tutorialBoardManager == null || !tutorialBoardManager.IsBoardInteractionStep())
        {
            return;
        }

        if (TryGetPointerDownPosition(out Vector2 pointerDownPosition))
        {
            HandlePointerDown(pointerDownPosition);
        }

        if (pointerIsDown && TryGetPointerHeldPosition(out Vector2 pointerHeldPosition))
        {
            HandlePointerHeld(pointerHeldPosition);
        }

        if (pointerIsDown && TryGetPointerUpPosition(out Vector2 pointerUpPosition))
        {
            HandlePointerUp(pointerUpPosition);
        }
    }

    private void HandlePointerDown(Vector2 screenPosition)
    {
        TileView tile = GetTileUnderPointer(screenPosition);
        if (!tutorialBoardManager.CanInteractWithTile(tile))
        {
            pointerIsDown = false;
            dragHasStarted = false;
            return;
        }

        pointerIsDown = true;
        dragHasStarted = false;
        pointerDownScreenPosition = screenPosition;
        tutorialBoardManager.BeginTileInteraction(tile);
    }

    private void HandlePointerHeld(Vector2 screenPosition)
    {
        if (!tutorialBoardManager.IsDragStep())
        {
            return;
        }

        if (!dragHasStarted &&
            Vector2.Distance(pointerDownScreenPosition, screenPosition) >= ClickMovementThresholdPixels)
        {
            dragHasStarted = true;
        }

        if (!dragHasStarted)
        {
            return;
        }

        tutorialBoardManager.UpdateTileInteraction(ScreenToWorld(screenPosition));
    }

    private void HandlePointerUp(Vector2 screenPosition)
    {
        TileView releasedTile = GetTileUnderPointer(screenPosition);
        tutorialBoardManager.EndTileInteraction(releasedTile);

        pointerIsDown = false;
        dragHasStarted = false;
    }

    private TileView GetTileUnderPointer(Vector2 screenPosition)
    {
        Vector3 worldPosition = ScreenToWorld(screenPosition);
        Collider2D hit = Physics2D.OverlapPoint(worldPosition);
        return hit != null ? hit.GetComponent<TileView>() : null;
    }

    private Vector3 ScreenToWorld(Vector2 screenPosition)
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        Vector3 worldPosition = mainCamera != null
            ? mainCamera.ScreenToWorldPoint(screenPosition)
            : new Vector3(screenPosition.x, screenPosition.y, 0f);

        worldPosition.z = 0f;
        return worldPosition;
    }

    private static bool TryGetPointerDownPosition(out Vector2 screenPosition)
    {
        return GameInput.TryGetPointerDownPosition(out screenPosition);
    }

    private static bool TryGetPointerHeldPosition(out Vector2 screenPosition)
    {
        return GameInput.TryGetPointerHeldPosition(out screenPosition);
    }

    private static bool TryGetPointerUpPosition(out Vector2 screenPosition)
    {
        return GameInput.TryGetPointerUpPosition(out screenPosition);
    }
}
