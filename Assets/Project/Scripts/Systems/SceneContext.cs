using System;
using UnityEngine;
using UnityEngine.Tilemaps;

[DefaultExecutionOrder(-150)]
public class SceneContext : MonoBehaviour
{
    public const string RuntimeMarkersTilemapName = "Tm_RuntimeMarkers";

    [SerializeField] private Tilemap runtimeMarkers;
    [SerializeField] private bool hideRuntimeMarkersOnPlay = true;

    public static SceneContext Current { get; private set; }
    public Tilemap RuntimeMarkers => runtimeMarkers;

    private void Awake()
    {
        Current = this;
        AutoBind();
        HideRuntimeMarkerRenderer();
    }

    private void OnDestroy()
    {
        if (Current == this)
            Current = null;
    }

    public void AutoBind()
    {
        if (runtimeMarkers != null) return;

        var tilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        foreach (var tilemap in tilemaps)
        {
            if (tilemap != null && string.Equals(tilemap.gameObject.name, RuntimeMarkersTilemapName, StringComparison.Ordinal))
            {
                runtimeMarkers = tilemap;
                break;
            }
        }
    }

    public SceneSpawnPoint FindSpawnPoint(string spawnPointId)
    {
        if (SceneSpawnResolver.TryFindSpawnPointComponent(spawnPointId, out var point))
            return point;

        return null;
    }

    public bool TryFindSpawnPosition(string spawnPointId, out Vector2 position)
    {
        AutoBind();
        return SceneSpawnResolver.TryResolve(spawnPointId, runtimeMarkers, out position);
    }

    private void HideRuntimeMarkerRenderer()
    {
        if (!hideRuntimeMarkersOnPlay || runtimeMarkers == null) return;
        var renderer = runtimeMarkers.GetComponent<TilemapRenderer>();
        if (renderer != null) renderer.enabled = false;
    }
}
