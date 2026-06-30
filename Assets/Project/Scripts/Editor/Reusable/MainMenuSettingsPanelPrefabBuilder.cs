using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MainMenuSettingsPanelPrefabBuilder
{
    private const string ScenePath = "Assets/Project/Scenes/Main/MainMenuScene.unity";
    private const string PrefabFolder = "Assets/Project/Prefabs/UI";
    private const string PrefabPath = PrefabFolder + "/MainMenuSettingsPanel.prefab";

    [MenuItem("Tools/Project/Rebuild Main Menu Settings Prefab")]
    public static void RebuildFromMenu()
    {
        Execute();
    }

    public static void Execute()
    {
        Directory.CreateDirectory(PrefabFolder);

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError($"[MainMenuSettingsPanelPrefabBuilder] Could not open scene '{ScenePath}'.");
            return;
        }

        var root = FindInScene("Setting Panel");
        if (root == null)
        {
            Debug.LogError("[MainMenuSettingsPanelPrefabBuilder] Could not find 'Setting Panel' in MainMenuScene.");
            return;
        }

        var settings = root.GetComponent<SettingsWindowUI>();
        if (settings == null)
        {
            Debug.LogError("[MainMenuSettingsPanelPrefabBuilder] 'Setting Panel' is missing SettingsWindowUI.");
            return;
        }

        settings.SetCompactMainMenuMode(true);
        settings.RebuildMainMenuAuthoredLayout();

        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(settings);

        var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.AutomatedAction);
        if (prefab == null)
        {
            Debug.LogError($"[MainMenuSettingsPanelPrefabBuilder] Failed to save prefab to '{PrefabPath}'.");
            return;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[MainMenuSettingsPanelPrefabBuilder] Rebuilt prefab at '{PrefabPath}' and connected MainMenuScene.");
    }

    private static GameObject FindInScene(string objectName)
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
        {
            var found = FindDeep(root.transform, objectName);
            if (found != null)
                return found.gameObject;
        }

        return null;
    }

    private static Transform FindDeep(Transform root, string objectName)
    {
        if (root == null)
            return null;
        if (root.name == objectName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindDeep(root.GetChild(i), objectName);
            if (found != null)
                return found;
        }

        return null;
    }
}
