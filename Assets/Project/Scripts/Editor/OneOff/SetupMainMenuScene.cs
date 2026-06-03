using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds a report-ready main menu scene from the project's existing UI sprites.
/// </summary>
public static class SetupMainMenuScene
{
    private const string ScenePath = "Assets/Project/Scenes/Main/MainMenuScene.unity";
    private const string CanvasName = "MainMenuCanvas";
    private const string EventSystemName = "EventSystem";

    [MenuItem("Tools/DATN/One-off Setup/UI/Create Main Menu Scene")]
    public static void Execute()
    {
        BuildMenuScene(closeWhenFinished: false);
    }

    public static void ExecuteSilently()
    {
        BuildMenuScene(closeWhenFinished: true);
    }

    private static void BuildMenuScene(bool closeWhenFinished)
    {
        EnsureSceneFolder();

        var previousActiveScene = SceneManager.GetActiveScene();
        Scene scene = FindLoadedScene(ScenePath);
        bool loadedForBuild = !scene.IsValid();

        if (!scene.IsValid())
        {
            scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        }

        SceneManager.SetActiveScene(scene);
        if (string.IsNullOrEmpty(scene.path))
            EditorSceneManager.SaveScene(scene, ScenePath);

        var canvas = EnsureCanvas(scene);
        var menu = canvas.GetComponent<MainMenuUI>() ?? canvas.gameObject.AddComponent<MainMenuUI>();
        AssignMenuSprites(menu);
        menu.RebuildForEditorPreview();

        EnsureEventSystem(scene);
        EnsureBuildSettings();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        if (closeWhenFinished && loadedForBuild && previousActiveScene.IsValid() && previousActiveScene != scene)
        {
            SceneManager.SetActiveScene(previousActiveScene);
            EditorSceneManager.CloseScene(scene, removeScene: true);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SetupMainMenuScene] Main menu scene ready: {ScenePath}");
    }

    private static void EnsureSceneFolder()
    {
        string folder = Path.GetDirectoryName(ScenePath)?.Replace("\\", "/");
        if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
            return;

        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static Canvas EnsureCanvas(Scene scene)
    {
        var existing = FindInScene<Canvas>(scene, CanvasName);

        if (existing != null)
            return existing;

        var go = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(go, scene);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static void EnsureEventSystem(Scene scene)
    {
        var existing = FindInScene<EventSystem>(scene, EventSystemName);

        if (existing != null)
            return;

        var eventSystem = new GameObject(EventSystemName, typeof(EventSystem), typeof(StandaloneInputModule));
        SceneManager.MoveGameObjectToScene(eventSystem, scene);
    }

    private static void AssignMenuSprites(MainMenuUI menu)
    {
        var so = new SerializedObject(menu);
        so.FindProperty("panelSprite").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/UI/Panels/Panel_Menu.png");
        so.FindProperty("buttonSprite").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/UI/Buttons/Btn_Menu.png")
            ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/UI/Buttons/Btn_List_menu.png");
        so.FindProperty("buttonSelectedSprite").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/UI/Buttons/Btn_List_menu_Select.png");
        so.FindProperty("firstGameplaySceneName").stringValue = "FarmScene";
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(menu);
    }

    private static void EnsureBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };

        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene == null || string.IsNullOrEmpty(scene.path) || scene.path == ScenePath)
                continue;

            scenes.Add(new EditorBuildSettingsScene(scene.path, scene.enabled));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static Scene FindLoadedScene(string path)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene.IsValid() && scene.path == path)
                return scene;
        }

        return default;
    }

    private static T FindInScene<T>(Scene scene, string objectName) where T : Component
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var component in root.GetComponentsInChildren<T>(true))
            {
                if (component != null && component.name == objectName)
                    return component;
            }
        }

        return null;
    }
}
