using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class FillBackgroundToCamera : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool fillScreen = true;
    [SerializeField] private bool preserveAspect = false;
    [SerializeField] private float worldZPosition = 0f;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        ApplyCameraFill();
    }

    private void LateUpdate()
    {
        ApplyCameraFill();
    }

    private void ApplyCameraFill()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null || !targetCamera.orthographic)
        {
            return;
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            return;
        }

        float worldScreenHeight = targetCamera.orthographicSize * 2f;
        float worldScreenWidth = worldScreenHeight * targetCamera.aspect;

        Vector2 spriteSize = spriteRenderer.sprite.bounds.size;

        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
        {
            return;
        }

        float scaleX = worldScreenWidth / spriteSize.x;
        float scaleY = worldScreenHeight / spriteSize.y;

        Vector3 targetWorldScale;
        if (preserveAspect)
        {
            float scale = fillScreen
                ? Mathf.Max(scaleX, scaleY)
                : Mathf.Min(scaleX, scaleY);
            targetWorldScale = new Vector3(scale, scale, 1f);
        }
        else
        {
            targetWorldScale = new Vector3(scaleX, scaleY, 1f);
        }

        transform.position = new Vector3(
            targetCamera.transform.position.x,
            targetCamera.transform.position.y,
            worldZPosition
        );

        transform.localScale = GetLocalScaleForWorldScale(targetWorldScale);
    }

    private Vector3 GetLocalScaleForWorldScale(Vector3 targetWorldScale)
    {
        Transform parent = transform.parent;
        if (parent == null)
        {
            return targetWorldScale;
        }

        Vector3 parentScale = parent.lossyScale;
        return new Vector3(
            parentScale.x != 0f ? targetWorldScale.x / parentScale.x : targetWorldScale.x,
            parentScale.y != 0f ? targetWorldScale.y / parentScale.y : targetWorldScale.y,
            1f
        );
    }
}
