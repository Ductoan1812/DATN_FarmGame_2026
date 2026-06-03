using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-click UI polish pass for report screenshots: menu scene plus visible panel skins.
/// </summary>
public static class CompletePresentationUiSetup
{
    private static string MarkerPath =>
        Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "ProjectSettings", "DATN_CompletePresentationUiSetupOnce.flag");

    [MenuItem("Tools/DATN/One-off Setup/UI/Complete Report UI Setup")]
    public static void Execute()
    {
        ExecuteSilently();
    }

    [MenuItem("Tools/DATN/One-off Setup/UI/Queue Complete Report UI Setup Once")]
    public static void QueueOnce()
    {
        File.WriteAllText(MarkerPath, DateTime.Now.ToString("O"));
        Debug.Log("[CompletePresentationUiSetup] Queued one-time UI setup. It will run after scripts reload.");
    }

    public static void ExecuteSilently()
    {
        PolishUIRootSprites.Execute();
        EnsurePresentationComponents();
        SetupMainMenuScene.ExecuteSilently();
        Debug.Log("[CompletePresentationUiSetup] Report UI setup completed.");
    }

    private static void EnsurePresentationComponents()
    {
        const string prefabPath = "Assets/Project/Prefabs/Systems/UIRoot.prefab";
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            EnsureComponent<QuestLogWindowUI>(root.transform, "QuestWindow");
            EnsureComponent<SettingsWindowUI>(root.transform, "SettingsWindow");
            EnsureComponent<HudStatusMapUI>(root.transform, "HudStatusMapPanel");
            EnsureMapWindowFallback(root.transform);

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log("[CompletePresentationUiSetup] Ensured core UI components on UIRoot prefab.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void EnsureComponent<T>(Transform root, string objectName) where T : Component
    {
        var target = FindDeepChild(root, objectName);
        if (target == null)
        {
            Debug.LogWarning($"[CompletePresentationUiSetup] Missing '{objectName}'.");
            return;
        }

        if (target.GetComponent<T>() == null)
            target.gameObject.AddComponent<T>();
    }

    private static void EnsureMapWindowFallback(Transform root)
    {
        var mapWindow = FindDeepChild(root, "MapWindow");
        if (mapWindow == null)
            return;

        if (FindDeepChild(mapWindow, "MapFallbackContent") != null)
            return;

        var content = new GameObject("MapFallbackContent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        content.transform.SetParent(mapWindow, false);
        var rect = content.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(32f, 84f);
        rect.offsetMax = new Vector2(-32f, -32f);
        content.GetComponent<Image>().color = new Color(0.19f, 0.37f, 0.19f, 0.82f);

        CreateMapBlock(content.transform, "Water", new Color(0.12f, 0.44f, 0.62f, 1f), new Vector2(0.05f, 0.08f), new Vector2(0.36f, 0.74f));
        CreateMapBlock(content.transform, "Farm", new Color(0.27f, 0.57f, 0.20f, 1f), new Vector2(0.35f, 0.08f), new Vector2(0.95f, 0.92f));
        CreateMapBlock(content.transform, "Path", new Color(0.72f, 0.55f, 0.26f, 1f), new Vector2(0.35f, 0.48f), new Vector2(0.95f, 0.64f));
        CreateMapBlock(content.transform, "House", new Color(0.38f, 0.18f, 0.08f, 1f), new Vector2(0.58f, 0.18f), new Vector2(0.82f, 0.40f));
        CreateMapBlock(content.transform, "PlayerMarker", new Color(0.80f, 0.94f, 1f, 1f), new Vector2(0.45f, 0.56f), new Vector2(0.52f, 0.66f));
    }

    private static void CreateMapBlock(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        var block = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        block.transform.SetParent(parent, false);
        var rect = block.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        block.GetComponent<Image>().color = color;
    }

    private static Transform FindDeepChild(Transform root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name))
            return null;

        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindDeepChild(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }

    internal static bool TryConsumeQueuedRun()
    {
        if (!File.Exists(MarkerPath))
            return false;

        File.Delete(MarkerPath);
        ExecuteSilently();
        return true;
    }
}

[InitializeOnLoad]
public static class CompletePresentationUiSetupAutoRun
{
    static CompletePresentationUiSetupAutoRun()
    {
        EditorApplication.delayCall += TryRun;
    }

    private static void TryRun()
    {
        if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        try
        {
            CompletePresentationUiSetup.TryConsumeQueuedRun();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CompletePresentationUiSetupAutoRun] Failed: {ex}");
        }
    }
}
