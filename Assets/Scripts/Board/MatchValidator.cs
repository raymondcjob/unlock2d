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

        if (tileA.IsPath || tileB.IsPath)
        {
            return false;
        }

        if (tileA.TileTypeId != tileB.TileTypeId)
        {
            return false;
        }

        return CanPositionsMatch(
            tileA.TileTypeId,
            tileA.GridPosition,
            tileB,
            null,
            null
        );
    }

    public List<TileView> GetMatchCandidates(TileView activeTile)
    {
        List<TileView> candidates = new List<TileView>();

        if (activeTile == null || activeTile.IsPath || boardManager == null)
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

    public List<TileView> GetProjectedMatchCandidates(
        TileView activeTile,
        Vector2Int projectedPosition,
        List<TileView> movedGroup,
        Vector2Int direction,
        int steps)
    {
        List<TileView> candidates = new List<TileView>();

        if (activeTile == null || activeTile.IsPath || boardManager == null || steps <= 0)
        {
            return candidates;
        }

        HashSet<Vector2Int> vacatedPositions = new HashSet<Vector2Int>();
        HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>();

        foreach (TileView tile in movedGroup)
        {
            if (tile == null)
            {
                continue;
            }

            vacatedPositions.Add(tile.GridPosition);
            occupiedPositions.Add(tile.GridPosition + direction * steps);
        }

        List<TileView> sameTypeTiles = boardManager.GetTilesOfSameType(activeTile.TileTypeId, activeTile);

        foreach (TileView tile in sameTypeTiles)
        {
            if (tile == null || tile.IsPath || movedGroup.Contains(tile))
            {
                continue;
            }

            if (CanPositionsMatch(
                activeTile.TileTypeId,
                projectedPosition,
                tile,
                vacatedPositions,
                occupiedPositions))
            {
                candidates.Add(tile);
            }
        }

        return candidates;
    }

    private bool CanPositionsMatch(
        int movingTileTypeId,
        Vector2Int movingTilePosition,
        TileView otherTile,
        HashSet<Vector2Int> vacatedPositions,
        HashSet<Vector2Int> occupiedPositions)
    {
        if (otherTile == null || otherTile.IsPath)
        {
            return false;
        }

        if (otherTile.TileTypeId != movingTileTypeId)
        {
            return false;
        }

        Vector2Int otherPosition = otherTile.GridPosition;

        bool sameColumn = movingTilePosition.x == otherPosition.x;
        bool sameRow = movingTilePosition.y == otherPosition.y;

        if (!sameColumn && !sameRow)
        {
            return false;
        }

        if (sameColumn)
        {
            int minY = Mathf.Min(movingTilePosition.y, otherPosition.y);
            int maxY = Mathf.Max(movingTilePosition.y, otherPosition.y);

            for (int y = minY + 1; y < maxY; y++)
            {
                if (IsOccupiedForProjectedCheck(
                    new Vector2Int(movingTilePosition.x, y),
                    vacatedPositions,
                    occupiedPositions))
                {
                    return false;
                }
            }

            return true;
        }

        int minX = Mathf.Min(movingTilePosition.x, otherPosition.x);
        int maxX = Mathf.Max(movingTilePosition.x, otherPosition.x);

        for (int x = minX + 1; x < maxX; x++)
        {
            if (IsOccupiedForProjectedCheck(
                new Vector2Int(x, movingTilePosition.y),
                vacatedPositions,
                occupiedPositions))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsOccupiedForProjectedCheck(
        Vector2Int position,
        HashSet<Vector2Int> vacatedPositions,
        HashSet<Vector2Int> occupiedPositions)
    {
        if (occupiedPositions != null && occupiedPositions.Contains(position))
        {
            return true;
        }

        if (vacatedPositions != null && vacatedPositions.Contains(position))
        {
            return false;
        }

        return boardManager.HasTileAt(position);
    }
}