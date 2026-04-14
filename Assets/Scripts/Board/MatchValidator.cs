using System.Collections.Generic;
using UnityEngine;

public class MatchValidator : MonoBehaviour
{
    [SerializeField] private BoardManager boardManager;

    public bool CanTilesMatch(TileView tileA, TileView tileB)
    {
        if (tileA == null || tileB == null)
        {
            return false;
        }

        if (tileA == tileB)
        {
            return false;
        }

        if (tileA.TileTypeId != tileB.TileTypeId)
        {
            return false;
        }

        Vector2Int posA = tileA.GridPosition;
        Vector2Int posB = tileB.GridPosition;

        bool sameColumn = posA.x == posB.x;
        bool sameRow = posA.y == posB.y;

        if (!sameColumn && !sameRow)
        {
            return false;
        }

        if (sameColumn)
        {
            int minY = Mathf.Min(posA.y, posB.y);
            int maxY = Mathf.Max(posA.y, posB.y);

            for (int y = minY + 1; y < maxY; y++)
            {
                TileView tileBetween = boardManager.GetTileAt(new Vector2Int(posA.x, y));
                if (tileBetween != null)
                {
                    return false;
                }
            }

            return true;
        }

        if (sameRow)
        {
            int minX = Mathf.Min(posA.x, posB.x);
            int maxX = Mathf.Max(posA.x, posB.x);

            for (int x = minX + 1; x < maxX; x++)
            {
                TileView tileBetween = boardManager.GetTileAt(new Vector2Int(x, posA.y));
                if (tileBetween != null)
                {
                    return false;
                }
            }

            return true;
        }

        return false;
    }

    public List<TileView> GetMatchCandidates(TileView activeTile)
    {
        List<TileView> candidates = new List<TileView>();

        if (activeTile == null || boardManager == null)
        {
            return candidates;
        }

        List<TileView> sameTypeTiles = boardManager.GetTilesOfSameType(activeTile.TileTypeId, activeTile);

        foreach (TileView tile in sameTypeTiles)
        {
            if (CanTilesMatch(activeTile, tile))
            {
                candidates.Add(tile);
            }
        }

        return candidates;
    }
    
}