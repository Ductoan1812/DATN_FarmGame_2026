using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class NavAgent2D : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float moveSpeed = 2f;
    [SerializeField, Min(0.01f)] private float defaultStopDistance = 0.05f;
    [SerializeField, Min(0.1f)] private float repathInterval = 1f;
    [SerializeField, Min(16)] private int maxExpandedNodes = 1800;
    [SerializeField] private string collisionTilemapName = "Tm_Collision";
    [SerializeField] private EntityLayer[] blockedEntityLayers = { EntityLayer.Furniture, EntityLayer.Decoration, EntityLayer.Plant };

    private readonly List<Vector2> path = new();
    private Rigidbody2D rb;
    private Tilemap collisionTilemap;
    private EntityRoot entityRoot;
    private Vector2 destination;
    private float stopDistance;
    private float nextRepathTime;
    private int pathIndex;
    private bool hasDestination;
    private Vector2 moveDirection;

    public bool HasDestination => hasDestination;
    public Vector2 Destination => destination;
    public bool IsAtDestination => !hasDestination || Vector2.Distance(rb != null ? rb.position : (Vector2)transform.position, destination) <= stopDistance;
    public bool IsMoving => moveDirection.sqrMagnitude > 0.0001f;
    public Vector2 MoveDirection => moveDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        entityRoot = GetComponent<EntityRoot>();
        AutoBindCollisionTilemap();
    }

    private void FixedUpdate()
    {
        if (!hasDestination || rb == null)
        {
            moveDirection = Vector2.zero;
            return;
        }

        if (Time.time >= nextRepathTime)
            RebuildPath();

        if (Vector2.Distance(rb.position, destination) <= stopDistance)
        {
            path.Clear();
            pathIndex = 0;
            moveDirection = Vector2.zero;
            return;
        }

        if (pathIndex >= path.Count)
        {
            moveDirection = Vector2.zero;
            return;
        }

        Vector2 target = path[pathIndex];
        if (Vector2.Distance(rb.position, target) <= 0.04f)
        {
            pathIndex++;
            moveDirection = Vector2.zero;
            return;
        }

        Vector2 next = Vector2.MoveTowards(rb.position, target, moveSpeed * Time.fixedDeltaTime);
        moveDirection = (next - rb.position).normalized;
        rb.MovePosition(next);
    }

    public void SetMoveSpeed(float value)
    {
        moveSpeed = Mathf.Max(0.1f, value);
    }

    public void SetDestination(Vector2 target, float stopDistanceOverride = -1f)
    {
        destination = target;
        stopDistance = stopDistanceOverride >= 0f ? stopDistanceOverride : defaultStopDistance;
        hasDestination = true;
        RebuildPath();
    }

    public void Stop()
    {
        hasDestination = false;
        path.Clear();
        pathIndex = 0;
        moveDirection = Vector2.zero;
    }

    public bool IsWalkable(Vector2 worldPosition)
    {
        return !IsCellBlocked(WorldToCell(worldPosition), WorldToCell(transform.position));
    }

    public bool HasLineOfSight(Vector2 worldTarget)
    {
        Vector2Int start = WorldToCell(rb != null ? rb.position : (Vector2)transform.position);
        Vector2Int goal = WorldToCell(worldTarget);
        int x = start.x;
        int y = start.y;
        int dx = Mathf.Abs(goal.x - start.x);
        int dy = Mathf.Abs(goal.y - start.y);
        int stepX = start.x < goal.x ? 1 : -1;
        int stepY = start.y < goal.y ? 1 : -1;
        int error = dx - dy;

        while (x != goal.x || y != goal.y)
        {
            int doubled = error * 2;
            if (doubled > -dy)
            {
                error -= dy;
                x += stepX;
            }
            if (doubled < dx)
            {
                error += dx;
                y += stepY;
            }

            var cell = new Vector2Int(x, y);
            if (cell != goal && IsCellBlocked(cell, start))
                return false;
        }

        return true;
    }

    private void RebuildPath()
    {
        AutoBindCollisionTilemap();
        nextRepathTime = Time.time + repathInterval;

        Vector2Int start = WorldToCell(rb != null ? rb.position : (Vector2)transform.position);
        Vector2Int goal = FindNearestWalkable(WorldToCell(destination), start);

        path.Clear();
        pathIndex = 0;

        if (start == goal)
        {
            path.Add(destination);
            return;
        }

        if (!TryFindPath(start, goal, out var cells))
            return;

        for (int i = 1; i < cells.Count; i++)
            path.Add(CellCenter(cells[i]));

        path.Add(destination);
    }

    private bool TryFindPath(Vector2Int start, Vector2Int goal, out List<Vector2Int> result)
    {
        result = null;
        var open = new List<Vector2Int> { start };
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, int> { [start] = 0 };
        var closed = new HashSet<Vector2Int>();
        int expanded = 0;

        while (open.Count > 0 && expanded < maxExpandedNodes)
        {
            Vector2Int current = PopBest(open, goal, gScore);
            if (current == goal)
            {
                result = BuildPath(cameFrom, current);
                return true;
            }

            closed.Add(current);
            expanded++;

            for (int i = 0; i < Directions.Length; i++)
            {
                Vector2Int next = current + Directions[i];
                if (closed.Contains(next) || IsCellBlocked(next, start))
                    continue;

                int tentative = gScore[current] + 1;
                if (gScore.TryGetValue(next, out int known) && tentative >= known)
                    continue;

                cameFrom[next] = current;
                gScore[next] = tentative;
                if (!open.Contains(next))
                    open.Add(next);
            }
        }

        return false;
    }

    private Vector2Int FindNearestWalkable(Vector2Int target, Vector2Int currentCell)
    {
        if (!IsCellBlocked(target, currentCell))
            return target;

        for (int radius = 1; radius <= 4; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                        continue;

                    var candidate = target + new Vector2Int(x, y);
                    if (!IsCellBlocked(candidate, currentCell))
                        return candidate;
                }
            }
        }

        return currentCell;
    }

    private bool IsCellBlocked(Vector2Int cell, Vector2Int currentCell)
    {
        if (cell == currentCell || IsOwnOccupiedCell(cell))
            return false;

        if (collisionTilemap != null && collisionTilemap.GetTile(new Vector3Int(cell.x, cell.y, 0)) != null)
            return true;

        var worldService = GameManager.Instance?.WorldService;
        if (worldService == null || blockedEntityLayers == null)
            return false;

        for (int i = 0; i < blockedEntityLayers.Length; i++)
        {
            if (worldService.HasBlockerAt(cell, blockedEntityLayers[i]))
                return true;
        }

        return false;
    }

    private bool IsOwnOccupiedCell(Vector2Int cell)
    {
        var entity = entityRoot != null ? entityRoot.GetEntity() : null;
        var position = entity != null ? GameManager.Instance?.WorldService?.GetEntityPosition(entity.id) : null;
        var cells = position?.occupiedCells;
        if (cells == null)
            return false;

        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] == cell)
                return true;
        }

        return false;
    }

    private void AutoBindCollisionTilemap()
    {
        if (collisionTilemap != null)
            return;

        var registry = SceneTilemapRegistry.Current;
        if (registry != null && registry.Collision != null)
        {
            collisionTilemap = registry.Collision;
            return;
        }

        var tilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        for (int i = 0; i < tilemaps.Length; i++)
        {
            if (tilemaps[i] != null && tilemaps[i].gameObject.name == collisionTilemapName)
            {
                collisionTilemap = tilemaps[i];
                return;
            }
        }
    }

    private static Vector2Int PopBest(List<Vector2Int> open, Vector2Int goal, Dictionary<Vector2Int, int> gScore)
    {
        int bestIndex = 0;
        int bestScore = int.MaxValue;
        for (int i = 0; i < open.Count; i++)
        {
            int score = gScore[open[i]] + Manhattan(open[i], goal);
            if (score >= bestScore)
                continue;

            bestScore = score;
            bestIndex = i;
        }

        Vector2Int best = open[bestIndex];
        open.RemoveAt(bestIndex);
        return best;
    }

    private static List<Vector2Int> BuildPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        var result = new List<Vector2Int> { current };
        while (cameFrom.TryGetValue(current, out var previous))
        {
            current = previous;
            result.Add(current);
        }

        result.Reverse();
        return result;
    }

    private static int Manhattan(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private static Vector2Int WorldToCell(Vector2 world)
    {
        return new Vector2Int(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.y));
    }

    private static Vector2 CellCenter(Vector2Int cell)
    {
        return new Vector2(cell.x + 0.5f, cell.y + 0.5f);
    }

    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };
}
