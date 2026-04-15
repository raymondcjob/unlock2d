using System.Collections.Generic;
using UnityEngine;

public class DragPreviewController : MonoBehaviour
{
    [Header("Clone Settings")]
    [SerializeField] private TileView clonePrefab;
    [SerializeField] private Transform previewContainer;

    private readonly List<TileView> activeClones = new List<TileView>();

    public void ShowPreview(
        TileView activeTile,
        List<TileView> connectedTiles,
        Vector2Int direction,
        float previewDistance,
        BoardManager boardManager)
    {
        ClearPreview();

        if (activeTile == null || activeTile.IsPath || boardManager == null || previewDistance <= 0f)
        {
            return;
        }

        List<TileView> tilesToClone = new List<TileView> { activeTile };
        tilesToClone.AddRange(connectedTiles);

        Vector3 directionOffset = new Vector3(direction.x, direction.y, 0f) * previewDistance;

        foreach (TileView sourceTile in tilesToClone)
        {
            Vector3 sourceWorldPosition = boardManager.GetWorldPosition(sourceTile.GridPosition);
            Vector3 previewWorldPosition = sourceWorldPosition + directionOffset;

            TileView clone = Instantiate(clonePrefab, previewWorldPosition, Quaternion.identity, previewContainer);
            clone.name = $"Preview_{sourceTile.name}";
            clone.Initialize(sourceTile.FaceUpSprite, sourceTile.TileTypeId, sourceTile.GridPosition);

            ApplyCloneVisual(clone);
            activeClones.Add(clone);
        }
    }

    public void ClearPreview()
    {
        for (int i = activeClones.Count - 1; i >= 0; i--)
        {
            if (activeClones[i] != null)
            {
                Destroy(activeClones[i].gameObject);
            }
        }

        activeClones.Clear();
    }

    private void ApplyCloneVisual(TileView clone)
    {
        if (clone == null)
        {
            return;
        }

        SpriteRenderer sr = clone.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder += 20;
        }
    }
}