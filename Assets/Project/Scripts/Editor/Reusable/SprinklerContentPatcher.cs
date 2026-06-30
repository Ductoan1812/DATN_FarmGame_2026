#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class SprinklerContentPatcher
{
    private const string Sprinkler01DefinitionPath = "Assets/Project/Resources/Data/WorldObjects/Sprinkler01.asset";
    private const string Sprinkler02DefinitionPath = "Assets/Project/Resources/Data/WorldObjects/Sprinkler02.asset";
    private const string Sprinkler01PrefabPath = "Assets/Project/Prefabs/WorldEntities/Sprinkler01.prefab";
    private const string Sprinkler02PrefabPath = "Assets/Project/Prefabs/WorldEntities/Sprinkler02.prefab";
    private const string Sprinkler01SpritePath = "Assets/Project/Generated/Icons/sprinkler_t1.png";
    private const string Sprinkler02SpritePath = "Assets/Project/Generated/Icons/sprinkler_t2.png";

    public static void Execute()
    {
        AssetDatabase.StartAssetEditing();
        try
        {
            var prefab01 = CreateSprinklerPrefab("Sprinkler01", Sprinkler01SpritePath, Sprinkler01PrefabPath);
            var prefab02 = CreateSprinklerPrefab("Sprinkler02", Sprinkler02SpritePath, Sprinkler02PrefabPath);

            AssignPrefab(Sprinkler01DefinitionPath, prefab01);
            AssignPrefab(Sprinkler02DefinitionPath, prefab02);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SprinklerContentPatcher] Sprinkler prefabs and WorldObject mappings are ready.");
    }

    private static GameObject CreateSprinklerPrefab(string prefabName, string spritePath, string prefabPath)
    {
        var sprite = LoadSprite(spritePath);
        if (sprite == null)
        {
            Debug.LogError($"[SprinklerContentPatcher] Missing sprite at '{spritePath}'.");
            return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        var root = new GameObject(prefabName);
        var worldObjectLayer = LayerMask.NameToLayer("WorldObject");
        root.layer = worldObjectLayer >= 0 ? worldObjectLayer : 9;
        root.tag = "Plant";

        root.AddComponent<EntityRoot>();

        var collider = root.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.8f, 0.8f);
        collider.offset = Vector2.zero;

        var renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "ObjectLow";
        renderer.sortingOrder = 0;

        var role = root.AddComponent<WorldEntityPrefabRole>();
        role.role = WorldEntityPrefabRoleType.Resource;

        root.SetActive(false);

        EnsureFolder("Assets/Project/Prefabs/WorldEntities");
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        if (prefab == null)
            Debug.LogError($"[SprinklerContentPatcher] Failed to save prefab '{prefabPath}'.");

        return prefab;
    }

    private static Sprite LoadSprite(string path)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null)
            return sprite;

        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (asset is Sprite nestedSprite)
                return nestedSprite;
        }

        return null;
    }

    private static void AssignPrefab(string definitionPath, GameObject prefab)
    {
        var definition = AssetDatabase.LoadAssetAtPath<WorldObjectDefinition>(definitionPath);
        if (definition == null)
        {
            Debug.LogError($"[SprinklerContentPatcher] Missing WorldObjectDefinition '{definitionPath}'.");
            return;
        }

        if (prefab == null)
        {
            Debug.LogError($"[SprinklerContentPatcher] Missing prefab for '{definitionPath}'.");
            return;
        }

        definition.prefab = prefab;
        EditorUtility.SetDirty(definition);
    }

    private static void EnsureFolder(string folderPath)
    {
        var parts = folderPath.Split('/');
        var current = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
