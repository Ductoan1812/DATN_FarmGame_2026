using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Stamps required components onto all Coreplay scenes:
///   1. SceneTilemapRegistry on the Grid object
///   2. SceneContext on the Grid object  
///   3. SceneContentScanner on a "SceneBootstrap" child of Environment
///
/// Run via: Tools > DATN > Stamp Scene Components
/// </summary>
public static class StampSceneComponents
{
    private static readonly string[] CoreplayScenes = {
        "Assets/Project/Scenes/Coreplay/FarmScene.unity",
        "Assets/Project/Scenes/Coreplay/TownScene.unity",
        "Assets/Project/Scenes/Coreplay/MineScene.unity",
    };

    [MenuItem("Tools/DATN/Stamp Scene Components")]
    public static void StampAll()
    {
        foreach (var path in CoreplayScenes)
            StampScene(path);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[StampSceneComponents] Done stamping all Coreplay scenes.");
    }

    private static void StampScene(string scenePath)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        Debug.Log($"[StampSceneComponents] Processing: {scene.name}");

        // 1. Tìm Grid object trong scene
        var grid = GameObject.FindObjectOfType<Grid>();
        if (grid == null)
        {
            Debug.LogWarning($"[StampSceneComponents] {scene.name}: No Grid found! Skipping.");
            return;
        }

        // 2. SceneTilemapRegistry — gắn lên Grid nếu chưa có
        var registry = grid.GetComponent<SceneTilemapRegistry>();
        if (registry == null)
        {
            registry = grid.gameObject.AddComponent<SceneTilemapRegistry>();
            Debug.Log($"[StampSceneComponents] {scene.name}: Added SceneTilemapRegistry to '{grid.name}'.");
        }
        else
        {
            Debug.Log($"[StampSceneComponents] {scene.name}: SceneTilemapRegistry already on '{grid.name}'.");
        }

        // AutoBind tilemap refs
        registry.AutoBind();
        EditorUtility.SetDirty(registry);

        // 3. SceneContext — gắn lên Grid nếu chưa có
        var sceneCtx = grid.GetComponent<SceneContext>();
        if (sceneCtx == null)
        {
            sceneCtx = grid.gameObject.AddComponent<SceneContext>();
            Debug.Log($"[StampSceneComponents] {scene.name}: Added SceneContext to '{grid.name}'.");
        }
        else
        {
            Debug.Log($"[StampSceneComponents] {scene.name}: SceneContext already on '{grid.name}'.");
        }

        // Gọi AutoBind để SceneContext gán RuntimeMarkers ref ngay
        sceneCtx.AutoBind();
        EditorUtility.SetDirty(sceneCtx);

        // 4. SceneContentScanner — gắn lên Grid nếu chưa có
        var scanner = grid.GetComponent<SceneContentScanner>();
        if (scanner == null)
        {
            scanner = grid.gameObject.AddComponent<SceneContentScanner>();
            Debug.Log($"[StampSceneComponents] {scene.name}: Added SceneContentScanner to '{grid.name}'.");
        }
        else
        {
            Debug.Log($"[StampSceneComponents] {scene.name}: SceneContentScanner already on '{grid.name}'.");
        }

        // Wire sceneContext reference via SerializedObject (field is serialized private)
        var so = new SerializedObject(scanner);
        var ctxProp = so.FindProperty("sceneContext");
        if (ctxProp != null && ctxProp.objectReferenceValue == null)
        {
            ctxProp.objectReferenceValue = sceneCtx;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"[StampSceneComponents] {scene.name}: Wired SceneContext into SceneContentScanner.");
        }

        EditorUtility.SetDirty(grid.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
