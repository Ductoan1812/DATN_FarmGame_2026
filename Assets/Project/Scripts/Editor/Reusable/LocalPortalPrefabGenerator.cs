using System.IO;
using UnityEditor;
using UnityEngine;

public static class LocalPortalPrefabGenerator
{
    private const string PrefabFolder = "Assets/Project/Prefabs/WorldEntities";
    private const string PrefabPath = PrefabFolder + "/Door_InOut_LocalPortal.prefab";
    private const string OutsideEntryId = "house_outside";
    private const string InsideEntryId = "house_inside";

    [InitializeOnLoadMethod]
    private static void EnsurePrefabOnEditorLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(PrefabPath))
                CreateOrReplacePrefab(logResult: false);
        };
    }

    [MenuItem("Tools/Project/Regenerate Door InOut Local Portal Prefab")]
    public static void CreateOrReplacePrefabMenu()
    {
        CreateOrReplacePrefab(logResult: true);
    }

    public static void Execute()
    {
        CreateOrReplacePrefab(logResult: true);
    }

    private static void CreateOrReplacePrefab(bool logResult)
    {
        Directory.CreateDirectory(PrefabFolder);

        var root = new GameObject("Door_InOut_LocalPortal");
        try
        {
            var outsideTrigger = CreateTrigger(root.transform, "OutsideTrigger", InsideEntryId, new Vector2(0f, 0f));
            var outsideSpawn = CreateSpawnPoint(root.transform, "OutsideSpawnPoint", OutsideEntryId, new Vector2(0f, -1f));
            var insideTrigger = CreateTrigger(root.transform, "InsideTrigger", OutsideEntryId, new Vector2(0f, 4f));
            var insideSpawn = CreateSpawnPoint(root.transform, "InsideSpawnPoint", InsideEntryId, new Vector2(0f, 5f));

            // Keep edit-time layout readable so users can drag children into place.
            outsideTrigger.transform.localScale = Vector3.one;
            outsideSpawn.transform.localScale = Vector3.one;
            insideTrigger.transform.localScale = Vector3.one;
            insideSpawn.transform.localScale = Vector3.one;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            if (logResult && prefab != null)
                Debug.Log($"[LocalPortalPrefabGenerator] Created prefab at '{PrefabPath}'.");
        }
        finally
        {
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    private static GameObject CreateTrigger(Transform parent, string name, string targetEntryId, Vector2 localPosition)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;

        var collider = go.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(1.2f, 0.8f);

        var trigger = go.AddComponent<LocalPortalTrigger2D>();
        SetSerializedString(trigger, "targetSpawnPointId", targetEntryId);
        SetSerializedBool(trigger, "hideRenderersOnPlay", true);
        SetSerializedFloat(trigger, "cooldownSeconds", 0.5f);

        return go;
    }

    private static GameObject CreateSpawnPoint(Transform parent, string name, string entryId, Vector2 localPosition)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;

        var spawnPoint = go.AddComponent<ScenePortalTrigger2D>();
        SetSerializedEnum(spawnPoint, "mode", (int)ScenePortalPointMode.Entry);
        SetSerializedString(spawnPoint, "_spawnPointId", entryId);

        return go;
    }

    private static void SetSerializedString(Object target, string propertyName, string value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(propertyName).stringValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetSerializedBool(Object target, string propertyName, bool value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(propertyName).boolValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetSerializedFloat(Object target, string propertyName, float value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(propertyName).floatValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetSerializedEnum(Object target, string propertyName, int enumValueIndex)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(propertyName).enumValueIndex = enumValueIndex;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
