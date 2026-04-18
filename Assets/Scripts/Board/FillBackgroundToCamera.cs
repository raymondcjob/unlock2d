using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class FillBackgroundToCamera : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool fillScreen = true;

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null || !targetCamera.orthographic)
        {
            return;
        }

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
        {
            return;
        }

        float worldScreenHeight = targetCamera.orthographicSize * 2f;
        float worldScreenWidth = worldScreenHeight * targetCamera.aspect;

        Vector2 spriteSize = sr.sprite.bounds.size;

        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
        {
            return;
        }

        float scaleX = worldScreenWidth / spriteSize.x;
        float scaleY = worldScreenHeight / spriteSize.y;

        float scale = fillScreen
            ? Mathf.Max(scaleX, scaleY)
            : Mathf.Min(scaleX, scaleY);

        transform.position = new Vector3(
            targetCamera.transform.position.x,
            targetCamera.transform.position.y,
            0f
        );

        transform.localScale = new Vector3(scale, scale, 1f);
    }
}