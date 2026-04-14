using System.Collections.Generic;
using UnityEngine;

public class DragPreviewController : MonoBehaviour
{
    [Header("Clone Settings")]
    [SerializeField] private TileView clonePrefab;
    [SerializeField] private Transform previewContainer;
    [SerializeField, Range(0f, 1f)] private float cloneAlpha = 0.45f;

    private readonly List<TileView> activeClones = new List<TileView>();

    public void ShowPreview(TileView activeTile, List<TileView> connectedTiles, Vector2Int direction, int previewStep, BoardManager boardManager)
    {
        ClearPreview();

        if (activeTile == null || boardManager == null || previewStep <= 0)
        {
            return;
        }

        List<TileView> tilesToClone = new List<TileView> { activeTile };
        tilesToClone.AddRange(connectedTiles);

        foreach (TileView sourceTile in tilesToClone)
        {
            Vector2Int previewGridPosition = sourceTile.GridPosition + direction * previewStep;
            Vector3 previewWorldPosition = boardManager.GetWorldPosition(previewGridPosition);

            TileView clone = Instantiate(clonePrefab, previewWorldPosition, Quaternion.identity, previewContainer);
            clone.name = $"Preview_{sourceTile.name}";
            clone.Initialize(sourceTile.GetSprite(), sourceTile.TileTypeId, previewGridPosition);

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
        SpriteRenderer sr = clone.GetComponent<SpriteRenderer>();

        if (sr != null)
        {
            Color c = Color.white;
            c.a = cloneAlpha;
            sr.color = c;
        }
    }
}