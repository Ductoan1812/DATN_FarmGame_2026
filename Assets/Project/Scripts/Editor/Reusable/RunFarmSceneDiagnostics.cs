using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class RunFarmSceneDiagnostics
{
    public static string Execute()
    {
        var report = new StringBuilder();
        var activeScene = SceneManager.GetActiveScene();
        report.AppendLine($"scene={activeScene.name}");
        report.AppendLine($"isLoaded={activeScene.isLoaded}");

        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            report.AppendLine("camera=null");
            Debug.Log(report.ToString());
            return report.ToString();
        }

        report.AppendLine($"camera.name={mainCamera.name}");
        report.AppendLine($"camera.pos={mainCamera.transform.position}");
        report.AppendLine($"camera.ortho={mainCamera.orthographic}");
        report.AppendLine($"camera.size={mainCamera.orthographicSize}");
        report.AppendLine($"camera.enabled={mainCamera.enabled}");
        report.AppendLine($"camera.cullingMask={mainCamera.cullingMask}");

        var player = Object.FindAnyObjectByType<PlayerControler>();
        report.AppendLine(player == null ? "player=null" : $"player.pos={player.transform.position}");

        var tilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        foreach (var tilemap in tilemaps)
        {
            if (tilemap == null)
                continue;

            var renderer = tilemap.GetComponent<TilemapRenderer>();
            int tileCount = CountTiles(tilemap);
            report.AppendLine(
                $"tilemap.{tilemap.name}.tiles={tileCount};renderer={(renderer != null ? renderer.enabled.ToString() : "missing")};order={(renderer != null ? renderer.sortingOrder.ToString() : "n/a")}");
        }

        var renderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var planes = GeometryUtility.CalculateFrustumPlanes(mainCamera);
        int visible = 0;
        List<string> visibleNames = new List<string>();
        for (int i = 0; i < renderers.Length; i++)
        {
            var spriteRenderer = renderers[i];
            if (spriteRenderer == null || !spriteRenderer.enabled)
                continue;

            if (!GeometryUtility.TestPlanesAABB(planes, spriteRenderer.bounds))
                continue;

            visible++;
            if (visibleNames.Count < 12)
                visibleNames.Add($"{spriteRenderer.name}@{spriteRenderer.transform.position}");
        }

        report.AppendLine($"visibleSpriteRenderers={visible}");
        report.AppendLine($"visibleExamples={string.Join(", ", visibleNames)}");

        var tilemapRenderers = Object.FindObjectsByType<TilemapRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        int visibleTilemaps = 0;
        List<string> visibleTilemapNames = new List<string>();
        for (int i = 0; i < tilemapRenderers.Length; i++)
        {
            var tilemapRenderer = tilemapRenderers[i];
            if (tilemapRenderer == null || !tilemapRenderer.enabled)
                continue;

            if (!GeometryUtility.TestPlanesAABB(planes, tilemapRenderer.bounds))
                continue;

            visibleTilemaps++;
            if (visibleTilemapNames.Count < 12)
                visibleTilemapNames.Add($"{tilemapRenderer.name}:{tilemapRenderer.sortingOrder}");
        }

        report.AppendLine($"visibleTilemaps={visibleTilemaps}");
        report.AppendLine($"visibleTilemapExamples={string.Join(", ", visibleTilemapNames)}");

        var vcam = GameObject.Find("CM vcam FarmScene");
        if (vcam == null)
        {
            report.AppendLine("vcam=null");
        }
        else
        {
            report.AppendLine($"vcam.pos={vcam.transform.position}");
            var binder = vcam.GetComponent<CinemachinePlayerBinder>();
            report.AppendLine($"vcam.binder={(binder != null ? "present" : "missing")}");

            var vcamComponent = vcam.GetComponent("CinemachineVirtualCamera");
            if (vcamComponent != null)
            {
                var type = vcamComponent.GetType();
                var followProperty = type.GetProperty("Follow", BindingFlags.Public | BindingFlags.Instance);
                var followValue = followProperty != null ? followProperty.GetValue(vcamComponent) as Transform : null;
                report.AppendLine($"vcam.follow={(followValue != null ? followValue.name : "null")}");
            }
        }

        Debug.Log(report.ToString());
        return report.ToString();
    }

    private static int CountTiles(Tilemap tilemap)
    {
        int count = 0;
        foreach (var cell in tilemap.cellBounds.allPositionsWithin)
        {
            if (tilemap.GetTile(cell) != null)
                count++;
        }

        return count;
    }
}
