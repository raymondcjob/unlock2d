using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class BoardMoveAnalyzer
{
    public enum MoveKind
    {
        Direct,
        Drag
    }

    public struct HintResult
    {
        public TileView SourceTile;
        public TileView TargetTile;
        public MoveKind Kind;
        public Vector2Int Direction;
        public int Steps;
    }

    private sealed class AnalyzerState
    {
        public int Width;
        public int Height;
        public int?[,] TileTypeIds;
        public TileView[,] TileViews;
    }

    private struct DirectMatchPair
    {
        public Vector2Int A;
        public Vector2Int B;

        public DirectMatchPair(Vector2Int a, Vector2Int b)
        {
            if (ComparePositions(a, b) <= 0)
            {
                A = a;
                B = b;
            }
            else
            {
                A = b;
                B = a;
            }
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + A.x;
                hash = hash * 31 + A.y;
                hash = hash * 31 + B.x;
                hash = hash * 31 + B.y;
                return hash;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is not DirectMatchPair other)
            {
                return false;
            }

            return A == other.A && B == other.B;
        }
    }

    private static readonly Vector2Int[] CardinalDirections =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    public static bool HasAnyAvailableMove(BoardManager boardManager)
    {
        if (boardManager == null)
        {
            return false;
        }

        AnalyzerState state = CreateStateFromBoard(boardManager);
        return TryFindAnyMove(state, out _);
    }

    public static bool TryFindHint(BoardManager boardManager, out HintResult hint)
    {
        hint = default;

        if (boardManager == null)
        {
            return false;
        }

        AnalyzerState state = CreateStateFromBoard(boardManager);
        return TryFindAnyMove(state, out hint);
    }

    public static List<HintResult> GetAllHints(BoardManager boardManager)
    {
        List<HintResult> hints = new List<HintResult>();

        if (boardManager == null)
        {
            return hints;
        }

        AnalyzerState state = CreateStateFromBoard(boardManager);
        CollectAllHints(state, hints);
        return hints;
    }

    public static bool ShouldRegenerateInitialLayout(int[,] tileTypeLayout)
    {
        if (tileTypeLayout == null)
        {
            return true;
        }

        AnalyzerState initialState = CreateStateFromLayout(tileTypeLayout);

        // Rule 1:
        // Reject if the generated board has no opening move at all.
        if (!TryFindAnyMove(initialState, out _))
        {
            return true;
        }

        // Rule 2:
        // If opening direct matches exist, search all legal direct-match removal orders.
        // Accept the board as soon as any branch produces a drag-created move.
        List<DirectMatchPair> openingDirectMatches = GetDirectMatchPairs(initialState);

        if (openingDirectMatches.Count == 0)
        {
            return false;
        }

        HashSet<string> visitedStates = new HashSet<string>();
        bool canReachDragMove = CanReachDragMoveViaDirectMatchOrder(initialState, visitedStates);

        return !canReachDragMove;
    }

    private static AnalyzerState CreateStateFromBoard(BoardManager boardManager)
    {
        int width = boardManager.GetBoardWidth();
        int height = boardManager.GetBoardHeight();

        AnalyzerState state = new AnalyzerState
        {
            Width = width,
            Height = height,
            TileTypeIds = new int?[width, height],
            TileViews = new TileView[width, height]
        };

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int position = new Vector2Int(x, y);
                TileView tile = boardManager.GetTileAt(position);

                if (tile == null || tile.IsPath)
                {
                    state.TileTypeIds[x, y] = null;
                    state.TileViews[x, y] = null;
                    continue;
                }

                state.TileTypeIds[x, y] = tile.TileTypeId;
                state.TileViews[x, y] = tile;
            }
        }

        return state;
    }

    private static AnalyzerState CreateStateFromLayout(int[,] tileTypeLayout)
    {
        int width = tileTypeLayout.GetLength(0);
        int height = tileTypeLayout.GetLength(1);

        AnalyzerState state = new AnalyzerState
        {
            Width = width,
            Height = height,
            TileTypeIds = new int?[width, height],
            TileViews = new TileView[width, height]
        };

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                state.TileTypeIds[x, y] = tileTypeLayout[x, y];
                state.TileViews[x, y] = null;
            }
        }

        return state;
    }

    private static AnalyzerState CloneState(AnalyzerState source)
    {
        AnalyzerState clone = new AnalyzerState
        {
            Width = source.Width,
            Height = source.Height,
            TileTypeIds = new int?[source.Width, source.Height],
            TileViews = new TileView[source.Width, source.Height]
        };

        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                clone.TileTypeIds[x, y] = source.TileTypeIds[x, y];
                clone.TileViews[x, y] = source.TileViews[x, y];
            }
        }

        return clone;
    }

    private static bool CanReachDragMoveViaDirectMatchOrder(
        AnalyzerState state,
        HashSet<string> visitedStates)
    {
        string stateKey = BuildStateKey(state);

        if (!visitedStates.Add(stateKey))
        {
            return false;
        }

        if (TryFindAnyDragMove(state, out _))
        {
            return true;
        }

        List<DirectMatchPair> directMatches = GetDirectMatchPairs(state);

        if (directMatches.Count == 0)
        {
            return false;
        }

        foreach (DirectMatchPair directMatch in directMatches)
        {
            AnalyzerState nextState = CloneState(state);
            RemoveDirectMatchPair(nextState, directMatch);

            if (CanReachDragMoveViaDirectMatchOrder(nextState, visitedStates))
            {
                return true;
            }
        }

        return false;
    }

    private static string BuildStateKey(AnalyzerState state)
    {
        StringBuilder sb = new StringBuilder(state.Width * state.Height * 3);

        for (int y = 0; y < state.Height; y++)
        {
            for (int x = 0; x < state.Width; x++)
            {
                int? tileTypeId = state.TileTypeIds[x, y];

                if (tileTypeId.HasValue)
                {
                    sb.Append(tileTypeId.Value);
                }
                else
                {
                    sb.Append('X');
                }

                sb.Append(',');
            }
        }

        return sb.ToString();
    }

    private static void RemoveDirectMatchPair(AnalyzerState state, DirectMatchPair pair)
    {
        state.TileTypeIds[pair.A.x, pair.A.y] = null;
        state.TileViews[pair.A.x, pair.A.y] = null;

        state.TileTypeIds[pair.B.x, pair.B.y] = null;
        state.TileViews[pair.B.x, pair.B.y] = null;
    }

    private static List<DirectMatchPair> GetDirectMatchPairs(AnalyzerState state)
    {
        HashSet<DirectMatchPair> uniquePairs = new HashSet<DirectMatchPair>();

        for (int y = 0; y < state.Height; y++)
        {
            for (int x = 0; x < state.Width; x++)
            {
                Vector2Int sourcePosition = new Vector2Int(x, y);

                if (!HasActiveTile(state, sourcePosition))
                {
                    continue;
                }

                List<Vector2Int> directMatches = GetDirectMatchPositions(state, sourcePosition);

                foreach (Vector2Int targetPosition in directMatches)
                {
                    uniquePairs.Add(new DirectMatchPair(sourcePosition, targetPosition));
                }
            }
        }

        return new List<DirectMatchPair>(uniquePairs);
    }

    private static bool TryFindAnyMove(AnalyzerState state, out HintResult hint)
    {
        hint = default;

        // Prefer direct matches first for clearer hints.
        for (int y = 0; y < state.Height; y++)
        {
            for (int x = 0; x < state.Width; x++)
            {
                Vector2Int sourcePosition = new Vector2Int(x, y);

                if (!HasActiveTile(state, sourcePosition))
                {
                    continue;
                }

                if (TryFindFirstDirectMatch(state, sourcePosition, out Vector2Int targetPosition))
                {
                    hint = new HintResult
                    {
                        SourceTile = state.TileViews[sourcePosition.x, sourcePosition.y],
                        TargetTile = state.TileViews[targetPosition.x, targetPosition.y],
                        Kind = MoveKind.Direct,
                        Direction = Vector2Int.zero,
                        Steps = 0
                    };

                    return true;
                }
            }
        }

        if (TryFindAnyDragMove(state, out hint))
        {
            return true;
        }

        return false;
    }

    private static void CollectAllHints(AnalyzerState state, List<HintResult> hints)
    {
        HashSet<DirectMatchPair> seenPairs = new HashSet<DirectMatchPair>();

        for (int y = 0; y < state.Height; y++)
        {
            for (int x = 0; x < state.Width; x++)
            {
                Vector2Int sourcePosition = new Vector2Int(x, y);

                if (!HasActiveTile(state, sourcePosition))
                {
                    continue;
                }

                List<Vector2Int> directMatches = GetDirectMatchPositions(state, sourcePosition);

                foreach (Vector2Int targetPosition in directMatches)
                {
                    DirectMatchPair pair = new DirectMatchPair(sourcePosition, targetPosition);

                    if (!seenPairs.Add(pair))
                    {
                        continue;
                    }

                    hints.Add(new HintResult
                    {
                        SourceTile = state.TileViews[sourcePosition.x, sourcePosition.y],
                        TargetTile = state.TileViews[targetPosition.x, targetPosition.y],
                        Kind = MoveKind.Direct,
                        Direction = Vector2Int.zero,
                        Steps = 0
                    });
                }
            }
        }

        for (int y = 0; y < state.Height; y++)
        {
            for (int x = 0; x < state.Width; x++)
            {
                Vector2Int sourcePosition = new Vector2Int(x, y);

                if (!HasActiveTile(state, sourcePosition))
                {
                    continue;
                }

                List<HintResult> dragHints = GetDragHintResults(state, sourcePosition);

                foreach (HintResult dragHint in dragHints)
                {
                    if (dragHint.SourceTile == null || dragHint.TargetTile == null)
                    {
                        continue;
                    }

                    DirectMatchPair pair = new DirectMatchPair(
                        dragHint.SourceTile.GridPosition,
                        dragHint.TargetTile.GridPosition);

                    if (!seenPairs.Add(pair))
                    {
                        continue;
                    }

                    hints.Add(dragHint);
                }
            }
        }
    }

    private static bool TryFindAnyDragMove(AnalyzerState state, out HintResult hint)
    {
        hint = default;

        for (int y = 0; y < state.Height; y++)
        {
            for (int x = 0; x < state.Width; x++)
            {
                Vector2Int sourcePosition = new Vector2Int(x, y);

                if (!HasActiveTile(state, sourcePosition))
                {
                    continue;
                }

                if (TryFindFirstDragMatch(
                    state,
                    sourcePosition,
                    out Vector2Int targetPosition,
                    out Vector2Int direction,
                    out int steps))
                {
                    hint = new HintResult
                    {
                        SourceTile = state.TileViews[sourcePosition.x, sourcePosition.y],
                        TargetTile = state.TileViews[targetPosition.x, targetPosition.y],
                        Kind = MoveKind.Drag,
                        Direction = direction,
                        Steps = steps
                    };

                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryFindFirstDirectMatch(
        AnalyzerState state,
        Vector2Int sourcePosition,
        out Vector2Int targetPosition)
    {
        targetPosition = Vector2Int.zero;

        List<Vector2Int> directMatches = GetDirectMatchPositions(state, sourcePosition);

        if (directMatches.Count == 0)
        {
            return false;
        }

        targetPosition = directMatches[0];
        return true;
    }

    private static bool TryFindFirstDragMatch(
        AnalyzerState state,
        Vector2Int activePosition,
        out Vector2Int targetPosition,
        out Vector2Int direction,
        out int steps)
    {
        targetPosition = Vector2Int.zero;
        direction = Vector2Int.zero;
        steps = 0;

        int? activeTypeId = state.TileTypeIds[activePosition.x, activePosition.y];

        if (!activeTypeId.HasValue)
        {
            return false;
        }

        HashSet<Vector2Int> currentDirectMatches = new HashSet<Vector2Int>(
            GetDirectMatchPositions(state, activePosition));

        foreach (Vector2Int testDirection in CardinalDirections)
        {
            List<Vector2Int> movedGroup = GetMovedGroup(state, activePosition, testDirection);
            HashSet<Vector2Int> movedGroupSet = new HashSet<Vector2Int>(movedGroup);

            int maxMoveSteps = GetMaxMoveSteps(state, movedGroup, testDirection);

            if (maxMoveSteps <= 0)
            {
                continue;
            }

            for (int testSteps = 1; testSteps <= maxMoveSteps; testSteps++)
            {
                Vector2Int projectedPosition = activePosition + testDirection * testSteps;
                HashSet<Vector2Int> vacatedPositions = new HashSet<Vector2Int>(movedGroup);
                HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>();

                foreach (Vector2Int movedPosition in movedGroup)
                {
                    occupiedPositions.Add(movedPosition + testDirection * testSteps);
                }

                for (int y = 0; y < state.Height; y++)
                {
                    for (int x = 0; x < state.Width; x++)
                    {
                        Vector2Int otherPosition = new Vector2Int(x, y);

                        if (!HasActiveTile(state, otherPosition))
                        {
                            continue;
                        }

                        if (otherPosition == activePosition)
                        {
                            continue;
                        }

                        if (movedGroupSet.Contains(otherPosition))
                        {
                            continue;
                        }

                        if (state.TileTypeIds[otherPosition.x, otherPosition.y] != activeTypeId)
                        {
                            continue;
                        }

                        if (currentDirectMatches.Contains(otherPosition))
                        {
                            continue;
                        }

                        if (CanPositionsMatch(
                            state,
                            projectedPosition,
                            otherPosition,
                            vacatedPositions,
                            occupiedPositions))
                        {
                            targetPosition = otherPosition;
                            direction = testDirection;
                            steps = testSteps;
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    private static List<HintResult> GetDragHintResults(AnalyzerState state, Vector2Int activePosition)
    {
        List<HintResult> results = new List<HintResult>();
        int? activeTypeId = state.TileTypeIds[activePosition.x, activePosition.y];

        if (!activeTypeId.HasValue)
        {
            return results;
        }

        HashSet<Vector2Int> currentDirectMatches = new HashSet<Vector2Int>(
            GetDirectMatchPositions(state, activePosition));
        HashSet<DirectMatchPair> seenPairs = new HashSet<DirectMatchPair>();

        foreach (Vector2Int testDirection in CardinalDirections)
        {
            List<Vector2Int> movedGroup = GetMovedGroup(state, activePosition, testDirection);
            HashSet<Vector2Int> movedGroupSet = new HashSet<Vector2Int>(movedGroup);

            int maxMoveSteps = GetMaxMoveSteps(state, movedGroup, testDirection);

            if (maxMoveSteps <= 0)
            {
                continue;
            }

            for (int testSteps = 1; testSteps <= maxMoveSteps; testSteps++)
            {
                Vector2Int projectedPosition = activePosition + testDirection * testSteps;
                HashSet<Vector2Int> vacatedPositions = new HashSet<Vector2Int>(movedGroup);
                HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>();

                foreach (Vector2Int movedPosition in movedGroup)
                {
                    occupiedPositions.Add(movedPosition + testDirection * testSteps);
                }

                for (int y = 0; y < state.Height; y++)
                {
                    for (int x = 0; x < state.Width; x++)
                    {
                        Vector2Int otherPosition = new Vector2Int(x, y);

                        if (!HasActiveTile(state, otherPosition))
                        {
                            continue;
                        }

                        if (otherPosition == activePosition)
                        {
                            continue;
                        }

                        if (movedGroupSet.Contains(otherPosition))
                        {
                            continue;
                        }

                        if (state.TileTypeIds[otherPosition.x, otherPosition.y] != activeTypeId)
                        {
                            continue;
                        }

                        if (currentDirectMatches.Contains(otherPosition))
                        {
                            continue;
                        }

                        if (!CanPositionsMatch(
                            state,
                            projectedPosition,
                            otherPosition,
                            vacatedPositions,
                            occupiedPositions))
                        {
                            continue;
                        }

                        DirectMatchPair pair = new DirectMatchPair(activePosition, otherPosition);

                        if (!seenPairs.Add(pair))
                        {
                            continue;
                        }

                        results.Add(new HintResult
                        {
                            SourceTile = state.TileViews[activePosition.x, activePosition.y],
                            TargetTile = state.TileViews[otherPosition.x, otherPosition.y],
                            Kind = MoveKind.Drag,
                            Direction = testDirection,
                            Steps = testSteps
                        });
                    }
                }
            }
        }

        return results;
    }

    private static List<Vector2Int> GetDirectMatchPositions(AnalyzerState state, Vector2Int sourcePosition)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        int? sourceTypeId = state.TileTypeIds[sourcePosition.x, sourcePosition.y];

        if (!sourceTypeId.HasValue)
        {
            return result;
        }

        for (int y = 0; y < state.Height; y++)
        {
            for (int x = 0; x < state.Width; x++)
            {
                Vector2Int otherPosition = new Vector2Int(x, y);

                if (otherPosition == sourcePosition)
                {
                    continue;
                }

                if (!HasActiveTile(state, otherPosition))
                {
                    continue;
                }

                if (state.TileTypeIds[otherPosition.x, otherPosition.y] != sourceTypeId)
                {
                    continue;
                }

                if (CanPositionsMatch(state, sourcePosition, otherPosition, null, null))
                {
                    result.Add(otherPosition);
                }
            }
        }

        return result;
    }

    private static List<Vector2Int> GetMovedGroup(
        AnalyzerState state,
        Vector2Int activePosition,
        Vector2Int direction)
    {
        List<Vector2Int> movedGroup = new List<Vector2Int> { activePosition };

        Vector2Int current = activePosition + direction;

        while (IsInsideBoard(state, current) && HasActiveTile(state, current))
        {
            movedGroup.Add(current);
            current += direction;
        }

        return movedGroup;
    }

    private static int GetMaxMoveSteps(
        AnalyzerState state,
        List<Vector2Int> movedGroup,
        Vector2Int direction)
    {
        if (movedGroup == null || movedGroup.Count == 0)
        {
            return 0;
        }

        Vector2Int frontPosition = movedGroup[movedGroup.Count - 1];
        Vector2Int checkPosition = frontPosition + direction;
        int steps = 0;

        while (IsInsideBoard(state, checkPosition) && !HasActiveTile(state, checkPosition))
        {
            steps++;
            checkPosition += direction;
        }

        return steps;
    }

    private static bool CanPositionsMatch(
        AnalyzerState state,
        Vector2Int movingPosition,
        Vector2Int otherPosition,
        HashSet<Vector2Int> vacatedPositions,
        HashSet<Vector2Int> occupiedPositions)
    {
        int? movingTypeId = state.TileTypeIds[movingPosition.x, movingPosition.y];

        if (!movingTypeId.HasValue && vacatedPositions == null && occupiedPositions == null)
        {
            return false;
        }

        int? otherTypeId = state.TileTypeIds[otherPosition.x, otherPosition.y];

        if (!otherTypeId.HasValue)
        {
            return false;
        }

        if (vacatedPositions == null && occupiedPositions == null)
        {
            movingTypeId = state.TileTypeIds[movingPosition.x, movingPosition.y];
        }
        else
        {
            movingTypeId = otherTypeId;
        }

        if (movingTypeId != otherTypeId)
        {
            return false;
        }

        bool sameColumn = movingPosition.x == otherPosition.x;
        bool sameRow = movingPosition.y == otherPosition.y;

        if (!sameColumn && !sameRow)
        {
            return false;
        }

        if (sameColumn)
        {
            int minY = Mathf.Min(movingPosition.y, otherPosition.y);
            int maxY = Mathf.Max(movingPosition.y, otherPosition.y);

            for (int y = minY + 1; y < maxY; y++)
            {
                Vector2Int testPosition = new Vector2Int(movingPosition.x, y);

                if (IsOccupiedForCheck(state, testPosition, vacatedPositions, occupiedPositions))
                {
                    return false;
                }
            }

            return true;
        }

        int minX = Mathf.Min(movingPosition.x, otherPosition.x);
        int maxX = Mathf.Max(movingPosition.x, otherPosition.x);

        for (int x = minX + 1; x < maxX; x++)
        {
            Vector2Int testPosition = new Vector2Int(x, movingPosition.y);

            if (IsOccupiedForCheck(state, testPosition, vacatedPositions, occupiedPositions))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsOccupiedForCheck(
        AnalyzerState state,
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

        return HasActiveTile(state, position);
    }

    private static bool HasActiveTile(AnalyzerState state, Vector2Int position)
    {
        return IsInsideBoard(state, position) && state.TileTypeIds[position.x, position.y].HasValue;
    }

    private static bool IsInsideBoard(AnalyzerState state, Vector2Int position)
    {
        return position.x >= 0 &&
               position.x < state.Width &&
               position.y >= 0 &&
               position.y < state.Height;
    }

    private static int ComparePositions(Vector2Int a, Vector2Int b)
    {
        if (a.y != b.y)
        {
            return a.y.CompareTo(b.y);
        }

        return a.x.CompareTo(b.x);
    }
}
