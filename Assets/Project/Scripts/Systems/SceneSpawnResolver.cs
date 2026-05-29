using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;

public static class SceneSpawnResolver
{
    public const string DefaultPlayerSpawnPointId = "player_start";

    public static Vector2 Resolve(string spawnPointId, Vector2 fallback)
    {
        var context = SceneContext.Current ?? UnityEngine.Object.FindAnyObjectByType<SceneContext>();
        if (context != null && context.TryFindSpawnPosition(spawnPointId, out var position))
            return position;

        if (TryFindSpawnPointComponent(spawnPointId, out var point))
            return point.transform.position;

        if (!string.IsNullOrWhiteSpace(spawnPointId))
            Debug.LogWarning($"[SceneSpawnResolver] Spawn point '{spawnPointId}' was not found in active scene '{SceneManager.GetActiveScene().name}'. Falling back to {fallback}.");

        return fallback;
    }

    public static bool TryResolve(string spawnPointId, Tilemap markerMap, out Vector2 position)
    {
        if (TryFindSpawnPointComponent(spawnPointId, out var point))
        {
            position = point.transform.position;
            return true;
        }

        if (TryFindSpawnPointTile(spawnPointId, markerMap, out position))
            return true;

        position = default;
        return false;
    }

    public static bool TryFindSpawnPointComponent(string spawnPointId, out ScenePortalTrigger2D chosen)
    {
        chosen = null;
        var points = UnityEngine.Object.FindObjectsByType<ScenePortalTrigger2D>(FindObjectsSortMode.None);
        if (points == null || points.Length == 0) return false;

        string targetId = Normalize(spawnPointId);
        var activeScene = SceneManager.GetActiveScene();
        foreach (var point in points)
        {
            if (point == null || !point.IsEntryPoint || point.gameObject.scene != activeScene) continue;
            if (!string.IsNullOrEmpty(targetId) &&
                string.Equals(Normalize(point.SpawnPointId), targetId, StringComparison.OrdinalIgnoreCase))
            {
                chosen = point;
                return true;
            }
        }

        if (!string.IsNullOrEmpty(targetId))
            return false;

        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] == null || !points[i].IsEntryPoint || points[i].gameObject.scene != activeScene)
                continue;

            chosen = points[i];
            return true;
        }

        return false;
    }

    private static bool TryFindSpawnPointTile(string spawnPointId, Tilemap markerMap, out Vector2 position)
    {
        position = default;
        if (markerMap == null) return false;

        string targetId = Normalize(spawnPointId);
        bool hasTarget = !string.IsNullOrEmpty(targetId);
        bool foundFallback = false;
        Vector2 fallback = default;

        foreach (var cell in markerMap.cellBounds.allPositionsWithin)
        {
            var tile = markerMap.GetTile<SceneSpawnTile>(cell);
            if (tile == null || tile.markerKind != SceneMarkerKind.PlayerSpawn)
                continue;

            Vector3 world = markerMap.GetCellCenterWorld(cell);
            var tilePosition = new Vector2(world.x, world.y);
            string tileId = Normalize(tile.spawnPointId);

            if (hasTarget && string.Equals(tileId, targetId, StringComparison.OrdinalIgnoreCase))
            {
                position = tilePosition;
                return true;
            }

            if (!foundFallback)
            {
                fallback = tilePosition;
                foundFallback = true;
            }
        }

        if (!foundFallback) return false;
        position = fallback;
        return true;
    }

    private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}
