using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// One-off editor utility: tự động thêm SceneTilemapRegistry vào Grid
/// của FarmScene, TownScene, và MineScene, rồi AutoBind tilemaps.
/// </summary>
public static class StampTilemapRegistry
{
    private static readonly string[] ScenePaths = {
        "Assets/Project/Scenes/Coreplay/FarmScene.unity",
        "Assets/Project/Scenes/Coreplay/TownScene.unity",
        "Assets/Project/Scenes/Coreplay/MineScene.unity",
    };

    [MenuItem("Tools/DATN/Stamp Tilemap Registry to All Scenes")]
    public static void Execute()
    {
        foreach (var path in ScenePaths)
            ProcessScene(path);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[StampTilemapRegistry] Done. SceneTilemapRegistry added to all Coreplay scenes.");
    }

    private static void ProcessScene(string scenePath)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        Debug.Log($"[StampTilemapRegistry] Processing: {scene.name}");

        // Tìm Grid gốc trong scene
        var grid = Object.FindAnyObjectByType<Grid>();
        if (grid == null)
        {
            Debug.LogWarning($"[StampTilemapRegistry] {scene.name}: No Grid found — skipping.");
            EditorSceneManager.SaveScene(scene);
            return;
        }

        // Thêm SceneTilemapRegistry lên Grid nếu chưa có
        var reg = grid.GetComponent<SceneTilemapRegistry>();
        if (reg == null)
            reg = grid.gameObject.AddComponent<SceneTilemapRegistry>();

        // AutoBind để gán tilemaps từ children
        reg.AutoBind();
        EditorUtility.SetDirty(grid.gameObject);

        // Đảm bảo SceneContext cũng có trên scene
        var ctx = Object.FindAnyObjectByType<SceneContext>();
        if (ctx != null)
        {
            ctx.AutoBind();
            EditorUtility.SetDirty(ctx.gameObject);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        LogRegistryState(scene.name, reg);
    }

    private static void LogRegistryState(string sceneName, SceneTilemapRegistry reg)
    {
        Debug.Log($"[StampTilemapRegistry] {sceneName}: " +
                  $"Ground={reg.Ground?.name ?? "MISSING"}, " +
                  $"Watered={reg.Watered?.name ?? "MISSING"}, " +
                  $"Markers={reg.RuntimeMarkers?.name ?? "MISSING"}, " +
                  $"Collision={reg.Collision?.name ?? "MISSING"}");
    }
}
