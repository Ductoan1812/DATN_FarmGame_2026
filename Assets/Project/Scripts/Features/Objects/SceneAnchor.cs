using System;
using UnityEngine;

[DisallowMultipleComponent]
public class SceneAnchor : MonoBehaviour
{
    public string anchorId;

    public static bool TryResolve(string id, out Vector2 position)
    {
        position = default;
        string targetId = Normalize(id);
        if (string.IsNullOrEmpty(targetId))
            return false;

        var anchors = FindObjectsByType<SceneAnchor>(FindObjectsSortMode.None);
        for (int i = 0; i < anchors.Length; i++)
        {
            var anchor = anchors[i];
            if (anchor == null)
                continue;

            if (!string.Equals(Normalize(anchor.anchorId), targetId, StringComparison.OrdinalIgnoreCase))
                continue;

            position = anchor.transform.position;
            return true;
        }

        return false;
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
        Gizmos.DrawSphere(transform.position, 0.15f);
    }
}
