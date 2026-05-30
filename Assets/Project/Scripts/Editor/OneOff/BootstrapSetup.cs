using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Hai Editor utilities trong một file:
///   1. [Tools > DATN > Create Bootstrap Config] — tạo BootstrapConfig.asset trong Resources/
///   2. [Tools > DATN > Stamp Bootstrap Loader to All Scenes] — thêm BootstrapLoader vào 3 scene coreplay
/// </summary>
public static class BootstrapSetup
{
    private static readonly string[] CoreplayScenes = {
        "Assets/Project/Scenes/Coreplay/FarmScene.unity",
        "Assets/Project/Scenes/Coreplay/TownScene.unity",
        "Assets/Project/Scenes/Coreplay/MineScene.unity",
    };

    private const string ConfigResourcePath = "Assets/Project/Resources/BootstrapConfig.asset";
    private const string GMPrefabPath       = "Assets/Project/Prefabs/Systems/_____GameManager____.prefab";
    private const string UIRootPrefabPath   = "Assets/Project/Prefabs/Systems/UIRoot.prefab";

    // ─── 1. Tạo BootstrapConfig ─────────────────────────────────────────────

    [MenuItem("Tools/DATN/Create Bootstrap Config")]
    public static void CreateBootstrapConfig()
    {
        var existing = AssetDatabase.LoadAssetAtPath<BootstrapConfig>(ConfigResourcePath);
        if (existing != null)
        {
            Debug.Log($"[BootstrapSetup] BootstrapConfig already exists at {ConfigResourcePath}. Checking refs...");
            AssignPrefabRefs(existing);
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssets();
            Debug.Log("[BootstrapSetup] BootstrapConfig refs updated.");
            return;
        }

        var config = ScriptableObject.CreateInstance<BootstrapConfig>();
        AssignPrefabRefs(config);

        AssetDatabase.CreateAsset(config, ConfigResourcePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[BootstrapSetup] Created BootstrapConfig.asset at {ConfigResourcePath}. " +
                  $"GM={config.gameManagerPrefab?.name}, UIRoot={config.uiRootPrefab?.name}");
    }

    private static void AssignPrefabRefs(BootstrapConfig config)
    {
        if (config.gameManagerPrefab == null)
        {
            var gm = AssetDatabase.LoadAssetAtPath<GameObject>(GMPrefabPath);
            if (gm != null)
                config.gameManagerPrefab = gm;
            else
                Debug.LogWarning($"[BootstrapSetup] GameManager prefab not found at {GMPrefabPath}");
        }

        if (config.uiRootPrefab == null)
        {
            var ui = AssetDatabase.LoadAssetAtPath<GameObject>(UIRootPrefabPath);
            if (ui != null)
                config.uiRootPrefab = ui;
            else
                Debug.LogWarning($"[BootstrapSetup] UIRoot prefab not found at {UIRootPrefabPath}");
        }
    }

    // ─── 2. Stamp BootstrapLoader vào các scene ─────────────────────────────

    [MenuItem("Tools/DATN/Stamp Bootstrap Loader to All Scenes")]
    public static void StampBootstrapLoaderToAllScenes()
    {
        // Đảm bảo config tồn tại trước
        CreateBootstrapConfig();

        foreach (var path in CoreplayScenes)
            StampScene(path);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BootstrapSetup] Done. BootstrapLoader stamped to all Coreplay scenes.");
    }

    private static void StampScene(string scenePath)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        Debug.Log($"[BootstrapSetup] Processing scene: {scene.name}");

        // Tìm __Bootstrap__ object hiện có
        var existing = GameObject.Find("__Bootstrap__");

        if (existing == null)
        {
            existing = new GameObject("__Bootstrap__");
            Debug.Log($"[BootstrapSetup] {scene.name}: Created '__Bootstrap__' GameObject.");
        }
        else
        {
            Debug.Log($"[BootstrapSetup] {scene.name}: '__Bootstrap__' already exists, ensuring component.");
        }

        // Thêm BootstrapLoader nếu chưa có
        if (existing.GetComponent<BootstrapLoader>() == null)
        {
            existing.AddComponent<BootstrapLoader>();
            Debug.Log($"[BootstrapSetup] {scene.name}: Added BootstrapLoader component.");
        }
        else
        {
            Debug.Log($"[BootstrapSetup] {scene.name}: BootstrapLoader already present.");
        }

        EditorUtility.SetDirty(existing);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
