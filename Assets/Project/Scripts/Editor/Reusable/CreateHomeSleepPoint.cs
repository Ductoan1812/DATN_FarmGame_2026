using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CreateHomeSleepPoint
{
    private const string ObjectName = "HomeBedSleepPoint";
    private static readonly Vector3 SleepPointPosition = new(-40.02f, 117.55f, 0f);
    private const string BedEntityDataAssetPath = "Assets/Project/Resources/Data/Entities/World/Utility/world_utility_bed_basic.asset";

    public static void Execute()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("[CreateHomeSleepPoint] No active loaded scene.");
            return;
        }

        var existing = GameObject.Find(ObjectName);
        var root = existing != null ? existing : new GameObject(ObjectName);

        Undo.RegisterCreatedObjectUndo(root, "Create Home Sleep Point");

        root.transform.position = SleepPointPosition;
        root.layer = LayerMask.NameToLayer("Interactable");

        EnsureComponent<EntityRoot>(root);
        var collider = EnsureComponent<CircleCollider2D>(root);
        collider.isTrigger = true;
        collider.offset = Vector2.zero;
        collider.radius = 0.9f;

        var prompt = EnsureComponent<InteractablePrompt>(root);
        var sleepPoint = EnsureComponent<SceneSleepPoint2D>(root);

        var sleepData = AssetDatabase.LoadAssetAtPath<EntityData>(BedEntityDataAssetPath);
        if (sleepData != null)
        {
            var so = new SerializedObject(sleepPoint);
            so.FindProperty("sleepEntityData").objectReferenceValue = sleepData;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        var promptSo = new SerializedObject(prompt);
        promptSo.FindProperty("promptText").stringValue = "[E] Ngu";
        promptSo.FindProperty("offset").vector2Value = new Vector2(0f, 0.9f);
        promptSo.FindProperty("fontSize").floatValue = 3f;
        promptSo.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = root;

        Debug.Log($"[CreateHomeSleepPoint] Ready at {SleepPointPosition} in scene '{scene.path}'.");
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        var component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }
}
