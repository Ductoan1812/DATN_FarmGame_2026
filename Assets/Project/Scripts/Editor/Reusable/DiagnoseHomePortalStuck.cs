using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class DiagnoseHomePortalStuck
{
    public static void Execute()
    {
        var report = new StringBuilder();
        report.AppendLine($"scene={SceneManager.GetActiveScene().path}");

        var portals = Object.FindObjectsByType<LocalPortalTrigger2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        report.AppendLine($"localPortals={portals.Length}");

        for (int i = 0; i < portals.Length; i++)
        {
            var portal = portals[i];
            if (portal == null) continue;

            report.AppendLine($"[portal] path={BuildPath(portal.transform)} pos={portal.transform.position}");
            DumpNearbyColliders(report, portal.transform.position, 1.5f, $"portal:{portal.name}");
        }

        var entries = Object.FindObjectsByType<ScenePortalTrigger2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        report.AppendLine($"scenePortalPoints={entries.Length}");
        for (int i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            if (entry == null || !entry.IsEntryPoint) continue;

            report.AppendLine($"[entry] id={entry.SpawnPointId} path={BuildPath(entry.transform)} pos={entry.transform.position}");
            DumpNearbyColliders(report, entry.transform.position, 1.5f, $"entry:{entry.SpawnPointId}");
        }

        var logic = GameObject.Find("Tm_Logic");
        if (logic != null)
        {
            report.AppendLine($"[logic] path={BuildPath(logic.transform)}");
            var tilemap = logic.GetComponent<Tilemap>();
            var tilemapCollider = logic.GetComponent<TilemapCollider2D>();
            var composite = logic.GetComponent<CompositeCollider2D>();
            report.AppendLine($"[logic] tilemap={tilemap != null} tilemapCollider={tilemapCollider != null} composite={composite != null}");
            if (tilemapCollider != null)
            {
                report.AppendLine($"[logic] tilemapCollider.isTrigger={tilemapCollider.isTrigger} usedByComposite={tilemapCollider.usedByComposite} extrusion={tilemapCollider.extrusionFactor}");
            }

            if (composite != null)
            {
                report.AppendLine($"[logic] composite.geometryType={composite.geometryType} generationType={composite.generationType} pathCount={composite.pathCount} pointCount={composite.pointCount}");
            }
        }

        SampleWalkableArea(report, new Vector2(-47f, 106f), 6, 4, 0.5f);
        FindNearestWalkablePoint(report, new Vector2(-47.01f, 105.39f), 4f, 0.1f);
        FindNearestWalkablePoint(report, new Vector2(-47.04f, 106.67f), 4f, 0.1f);

        Debug.Log(report.ToString());
    }

    private static void SampleWalkableArea(StringBuilder report, Vector2 center, int halfWidth, int halfHeight, float step)
    {
        report.AppendLine($"[sample] center={center} halfWidth={halfWidth} halfHeight={halfHeight} step={step}");
        for (int y = halfHeight; y >= -halfHeight; y--)
        {
            var line = new StringBuilder();
            for (int x = -halfWidth; x <= halfWidth; x++)
            {
                var point = new Vector2(center.x + x * step, center.y + y * step);
                var hits = Physics2D.OverlapPointAll(point);
                bool blocked = false;
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i] != null && !hits[i].isTrigger)
                    {
                        blocked = true;
                        break;
                    }
                }

                line.Append(blocked ? "#" : ".");
            }

            report.AppendLine(line.ToString());
        }
    }

    private static void FindNearestWalkablePoint(StringBuilder report, Vector2 origin, float radius, float step)
    {
        bool found = false;
        Vector2 bestPoint = default;
        float bestDistance = float.MaxValue;

        for (float y = origin.y - radius; y <= origin.y + radius; y += step)
        {
            for (float x = origin.x - radius; x <= origin.x + radius; x += step)
            {
                var point = new Vector2(x, y);
                if (IsBlocked(point))
                    continue;

                float distance = Vector2.Distance(origin, point);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestPoint = point;
                    found = true;
                }
            }
        }

        report.AppendLine(found
            ? $"[nearestWalkable] origin={origin} point={bestPoint} distance={bestDistance:F2}"
            : $"[nearestWalkable] origin={origin} point=<none within radius {radius}>");
    }

    private static bool IsBlocked(Vector2 point)
    {
        var hits = Physics2D.OverlapPointAll(point);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] != null && !hits[i].isTrigger)
                return true;
        }

        return false;
    }

    private static void DumpNearbyColliders(StringBuilder report, Vector2 center, float radius, string label)
    {
        var hits = Physics2D.OverlapCircleAll(center, radius);
        report.AppendLine($"[{label}] nearbyColliders={hits.Length}");
        for (int i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];
            if (hit == null) continue;

            report.AppendLine(
                $"  - name={hit.name} path={BuildPath(hit.transform)} type={hit.GetType().Name} trigger={hit.isTrigger} enabled={hit.enabled} layer={LayerMask.LayerToName(hit.gameObject.layer)} overlapPoint={hit.OverlapPoint(center)} closestPoint={hit.ClosestPoint(center)} boundsMin={hit.bounds.min} boundsMax={hit.bounds.max}");
        }
    }

    private static string BuildPath(Transform current)
    {
        if (current == null)
            return "<null>";

        var path = current.name;
        while (current.parent != null)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }

        return path;
    }
}
