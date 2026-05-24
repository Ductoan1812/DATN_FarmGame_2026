using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class BootstrapCoreplayM1Scenes
{
    private const string BaseScenePath = "Assets/Project/Scenes/Main/SampleScene.unity";
    private const string SceneFolder = "Assets/Project/Scenes/Coreplay";
    private const string FarmScenePath = SceneFolder + "/FarmScene.unity";
    private const string TownScenePath = SceneFolder + "/TownScene.unity";
    private const string MineScenePath = SceneFolder + "/MineScene.unity";

    private const string MarkerFolder = "Assets/Project/ScriptableObjects/SceneMarkers/MVP";
    private const string UtilityFolder = "Assets/Project/ScriptableObjects/WorldObjects/Utility";
    private const string ResourceFolder = "Assets/Project/ScriptableObjects/WorldObjects/Resources";
    private const string AnimalFolder = "Assets/Project/ScriptableObjects/WorldObjects/Animals";

    [MenuItem("Tools/DATN/Coreplay/Bootstrap M1 Scenes")]
    public static void Execute()
    {
        BootstrapProductionSetup.Execute();
        EnsureFolder(SceneFolder);
        EnsureScenesExist();
        EnsureRoutePortalData();
        EnsureM1Markers();
        StampFarmScene();
        StampTownScene();
        StampMineScene();
        ConfigureBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BootstrapCoreplayM1Scenes] Farm/Town/Mine scenes, portals, spawn markers and build settings generated.");
    }

    public static void ExecuteBatch() => Execute();

    [MenuItem("Tools/DATN/Coreplay/Vertical Slice - Restamp Town Layout")]
    public static void RestampTownLayoutOnly()
    {
        StampTownScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BootstrapCoreplayM1Scenes] Town vertical-slice layout restamped.");
    }

    [MenuItem("Tools/DATN/Coreplay/Sprint 4 - Add Clear Zone")]
    public static void ExecuteSprint4ClearZoneOnly()
    {
        EnsureFolder(MarkerFolder);
        CreateClearZoneMarker();
        
        var scene = EditorSceneManager.OpenScene(FarmScenePath, OpenSceneMode.Single);
        var markerMap = PrepareScene("FarmScene");
        
        SetTiles(markerMap, "Marker_ClearZone_Rock_01",
            new Vector3Int(10, 4, 0),
            new Vector3Int(11, 4, 0),
            new Vector3Int(10, 3, 0),
            new Vector3Int(11, 3, 0),
            new Vector3Int(12, 3, 0));
        
        SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[Sprint4] Clear-zone markers added at farm edge (10,4), (11,4), (10,3), (11,3), (12,3)");
    }

    private static void CreateClearZoneMarker()
    {
        var entity = AssetDatabase.LoadAssetAtPath<EntityData>($"{ResourceFolder}/RockNode_01.asset");
        var tile = LoadOrCreateAsset<SceneSpawnTile>($"{MarkerFolder}/Marker_ClearZone_Rock_01.asset");
        tile.markerKind = SceneMarkerKind.ResourceNode;
        tile.objectType = ObjectType.RockNode01;
        tile.entityData = entity;
        tile.savePolicy = SceneEntitySavePolicy.Persistent;
        tile.spawnGroupId = "farm_clear_edge";
        tile.spawnPointId = string.Empty;
        tile.respawnMinutes = 0;
        tile.initialAmount = 1;
        tile.bypassPlacementValidation = true;
        tile.editorSprite = entity != null ? entity.icon : null;
        tile.editorColor = new Color(1f, 0.4f, 0.1f, 0.8f);
        EditorUtility.SetDirty(tile);
    }

    private static void EnsureScenesExist()
    {
        CopySceneIfMissing(FarmScenePath);
        CopySceneIfMissing(TownScenePath);
        CopySceneIfMissing(MineScenePath);
    }

    private static void CopySceneIfMissing(string scenePath)
    {
        if (File.Exists(scenePath)) return;
        if (!File.Exists(BaseScenePath))
            throw new FileNotFoundException($"Base scene not found: {BaseScenePath}");

        if (!AssetDatabase.CopyAsset(BaseScenePath, scenePath))
            throw new InvalidOperationException($"Could not copy scene {BaseScenePath} -> {scenePath}");
    }

    private static void EnsureRoutePortalData()
    {
        EnsurePortalData("Portal_Farm_To_Town.asset", "portal_farm_to_town", "m1.portal.farm_to_town.name", "TownScene", "town_entry");
        EnsurePortalData("Portal_Town_To_Farm.asset", "portal_town_to_farm", "m1.portal.town_to_farm.name", "FarmScene", SceneSpawnResolver.DefaultPlayerSpawnPointId);
        EnsurePortalData("Portal_Town_To_Mine.asset", "portal_town_to_mine", "m1.portal.town_to_mine.name", "MineScene", "mine_entry");
        EnsurePortalData("Portal_Mine_To_Town.asset", "portal_mine_to_town", "m1.portal.mine_to_town.name", "TownScene", "town_mine_entry");
    }

    private static EntityData EnsurePortalData(string fileName, string id, string keyName, string targetScene, string targetSpawnId)
    {
        var data = LoadOrCreateEntity($"{UtilityFolder}/{fileName}");
        data.id = id;
        data.keyName = keyName;
        data.descKey = "m3.portal.desc";
        data.category = ItemCategory.Placeable;
        data.maxStack = 1;
        data.buyPrice = -1;
        data.sellPrice = 0;
        data.modules = new List<IModuleData>
        {
            new ScenePortalModule
            {
                optionTextKey = "ui.scene.enter",
                priority = 40,
                targetSceneName = targetScene,
                targetSpawnPointId = targetSpawnId,
                saveBeforeTransition = true
            }
        };
        EditorUtility.SetDirty(data);
        return data;
    }

    private static void EnsureM1Markers()
    {
        CreatePlayerSpawnMarker("Marker_Player_Start", SceneSpawnResolver.DefaultPlayerSpawnPointId, new Color(0.2f, 0.8f, 1f, 0.75f));
        CreatePlayerSpawnMarker("Marker_Player_TownEntry", "town_entry", new Color(0.3f, 0.9f, 0.95f, 0.75f));
        CreatePlayerSpawnMarker("Marker_Player_TownMineEntry", "town_mine_entry", new Color(0.3f, 0.7f, 1f, 0.75f));
        CreatePlayerSpawnMarker("Marker_Player_MineEntry", "mine_entry", new Color(0.5f, 0.55f, 1f, 0.75f));

        CreatePortalMarker("Marker_Portal_Farm_To_Town", "Portal_Farm_To_Town.asset", "farm_to_town", new Color(0.45f, 0.85f, 1f, 0.8f));
        CreatePortalMarker("Marker_Portal_Town_To_Farm", "Portal_Town_To_Farm.asset", "town_to_farm", new Color(0.5f, 0.95f, 0.65f, 0.8f));
        CreatePortalMarker("Marker_Portal_Town_To_Mine", "Portal_Town_To_Mine.asset", "town_to_mine", new Color(0.8f, 0.8f, 1f, 0.8f));
        CreatePortalMarker("Marker_Portal_Mine_To_Town", "Portal_Mine_To_Town.asset", "mine_to_town", new Color(0.7f, 0.7f, 1f, 0.8f));

        CreateEntityMarker("Marker_Tree_01", SceneMarkerKind.ResourceNode, ObjectType.TreeNode01, $"{ResourceFolder}/TreeNode_01.asset", SceneEntitySavePolicy.Regenerating, "farm_tree", 720, new Color(0.35f, 0.85f, 0.35f, 0.8f));
        CreateEntityMarker("Marker_Rock_01", SceneMarkerKind.ResourceNode, ObjectType.RockNode01, $"{ResourceFolder}/RockNode_01.asset", SceneEntitySavePolicy.Regenerating, "farm_rock", 720, new Color(0.65f, 0.65f, 0.7f, 0.8f));
        CreateEntityMarker("Marker_Forage_01", SceneMarkerKind.ResourceNode, ObjectType.ForageNode01, $"{ResourceFolder}/ForageNode_01.asset", SceneEntitySavePolicy.Regenerating, "farm_forage", 360, new Color(0.6f, 0.95f, 0.45f, 0.8f));
        CreateEntityMarker("Marker_Bed_01", SceneMarkerKind.Bed, ObjectType.Bed01, $"{UtilityFolder}/Bed_01.asset", SceneEntitySavePolicy.Persistent, "farm_bed", 0, new Color(1f, 0.85f, 0.55f, 0.8f));
        CreateEntityMarker("Marker_Chicken_01", SceneMarkerKind.Object, ObjectType.Animal01, $"{AnimalFolder}/Animal_Chicken_01.asset", SceneEntitySavePolicy.Persistent, "farm_animal", 0, new Color(1f, 0.95f, 0.65f, 0.8f));
    }

    private static void StampFarmScene()
    {
        var scene = EditorSceneManager.OpenScene(FarmScenePath, OpenSceneMode.Single);
        var markerMap = PrepareScene("FarmScene");
        ClearMarkers(markerMap);

        SetTile(markerMap, new Vector3Int(0, 0, 0), "Marker_Player_Start");
        SetTile(markerMap, new Vector3Int(8, 0, 0), "Marker_Portal_Farm_To_Town");
        SetTile(markerMap, new Vector3Int(-3, 2, 0), "Marker_Bed_01");
        SetTiles(markerMap, "Marker_Chicken_01",
            new Vector3Int(-5, -2, 0),
            new Vector3Int(-6, -2, 0),
            new Vector3Int(-5, -3, 0));

        SetTiles(markerMap, "Marker_Tree_01",
            new Vector3Int(-10, 5, 0),
            new Vector3Int(-9, 6, 0),
            new Vector3Int(-8, 4, 0),
            new Vector3Int(-7, 5, 0),
            new Vector3Int(-6, 3, 0),
            new Vector3Int(-11, 3, 0),
            new Vector3Int(-10, 2, 0),
            new Vector3Int(-7, 2, 0));

        SetTiles(markerMap, "Marker_Rock_01",
            new Vector3Int(2, -4, 0),
            new Vector3Int(3, -4, 0),
            new Vector3Int(4, -5, 0),
            new Vector3Int(5, -4, 0),
            new Vector3Int(4, -3, 0),
            new Vector3Int(6, -6, 0));

        SetTiles(markerMap, "Marker_Forage_01",
            new Vector3Int(0, -5, 0),
            new Vector3Int(1, -5, 0),
            new Vector3Int(2, -6, 0),
            new Vector3Int(-1, -6, 0),
            new Vector3Int(1, -7, 0),
            new Vector3Int(3, -7, 0),
            new Vector3Int(-2, -4, 0));

        SaveScene(scene);
    }

    private static void StampTownScene()
    {
        var scene = EditorSceneManager.OpenScene(TownScenePath, OpenSceneMode.Single);
        var markerMap = PrepareScene("TownScene");
        ClearMarkers(markerMap);

        SetTile(markerMap, new Vector3Int(-6, -2, 0), "Marker_Player_TownEntry");
        SetTile(markerMap, new Vector3Int(7, -3, 0), "Marker_Player_TownMineEntry");
        SetTile(markerMap, new Vector3Int(-9, -2, 0), "Marker_Portal_Town_To_Farm");
        SetTile(markerMap, new Vector3Int(10, -3, 0), "Marker_Portal_Town_To_Mine");
        SetTile(markerMap, new Vector3Int(-5, 4, 0), "Marker_NPC_Shop");
        SetTile(markerMap, new Vector3Int(0, 5, 0), "Marker_NPC_Crafting");
        SetTile(markerMap, new Vector3Int(5, 2, 0), "Marker_NPC_Quest");

        SetTiles(markerMap, "Marker_Forage_01",
            new Vector3Int(-6, -6, 0),
            new Vector3Int(5, -5, 0));

        SaveScene(scene);
    }

    private static void StampMineScene()
    {
        var scene = EditorSceneManager.OpenScene(MineScenePath, OpenSceneMode.Single);
        var markerMap = PrepareScene("MineScene");
        ClearMarkers(markerMap);

        SetTile(markerMap, new Vector3Int(0, 0, 0), "Marker_Player_MineEntry");
        SetTile(markerMap, new Vector3Int(-8, 0, 0), "Marker_Portal_Mine_To_Town");

        string[] oreNames = { "Copper", "Iron", "Silver", "Gold", "Mythril" };
        string[] enemyNames = { "Slime", "Bat", "Golem", "Wraith", "Ancient" };
        for (int tier = 1; tier <= 5; tier++)
        {
            int baseX = -10 + (tier - 1) * 5;
            int baseY = -5 - (tier - 1) * 3;
            SetTiles(markerMap, $"Marker_Ore_T{tier}_{oreNames[tier - 1]}",
                new Vector3Int(baseX, baseY, 0),
                new Vector3Int(baseX + 1, baseY, 0),
                new Vector3Int(baseX + 2, baseY, 0),
                new Vector3Int(baseX, baseY - 1, 0),
                new Vector3Int(baseX + 2, baseY - 1, 0),
                new Vector3Int(baseX + 3, baseY, 0));

            SetTiles(markerMap, $"Marker_Enemy_T{tier}_{enemyNames[tier - 1]}",
                new Vector3Int(baseX, baseY - 3, 0),
                new Vector3Int(baseX + 2, baseY - 3, 0),
                new Vector3Int(baseX + 4, baseY - 2, 0));
        }

        SaveScene(scene);
    }

    private static Tilemap PrepareScene(string sceneName)
    {
        var markerMap = EnsureRuntimeMarkerTilemap();
        EnsureSceneMarkerComponents(markerMap);
        EnsureNamedSpawnPoint($"{sceneName}_Anchor", Vector3.zero, sceneName.ToLowerInvariant() + "_anchor");
        return markerMap;
    }

    private static void ClearMarkers(Tilemap markerMap)
    {
        if (markerMap == null) return;
        markerMap.ClearAllTiles();
    }

    private static void ConfigureBuildSettings()
    {
        var scenes = new[]
        {
            new EditorBuildSettingsScene(FarmScenePath, true),
            new EditorBuildSettingsScene(TownScenePath, true),
            new EditorBuildSettingsScene(MineScenePath, true)
        };

        EditorBuildSettings.scenes = scenes;
    }

    private static Tilemap EnsureRuntimeMarkerTilemap()
    {
        foreach (var tilemap in UnityEngine.Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
        {
            if (tilemap != null && tilemap.gameObject.name == SceneContext.RuntimeMarkersTilemapName)
                return tilemap;
        }

        var grid = UnityEngine.Object.FindAnyObjectByType<Grid>();
        if (grid == null)
            grid = new GameObject("Grid").AddComponent<Grid>();

        var go = new GameObject(SceneContext.RuntimeMarkersTilemapName);
        go.transform.SetParent(grid.transform, false);
        var markerMap = go.AddComponent<Tilemap>();
        go.AddComponent<TilemapRenderer>();
        return markerMap;
    }

    private static void EnsureSceneMarkerComponents(Tilemap markerMap)
    {
        var context = UnityEngine.Object.FindAnyObjectByType<SceneContext>();
        if (context == null)
            context = new GameObject("SceneContext").AddComponent<SceneContext>();

        context.AutoBind();
        if (context.GetComponent<SceneContentScanner>() == null)
            context.gameObject.AddComponent<SceneContentScanner>();

        if (markerMap != null)
            EditorUtility.SetDirty(markerMap);
        EditorUtility.SetDirty(context);
    }

    private static void EnsureNamedSpawnPoint(string objectName, Vector3 position, string spawnPointId)
    {
        var points = UnityEngine.Object.FindObjectsByType<SceneSpawnPoint>(FindObjectsSortMode.None);
        foreach (var point in points)
        {
            if (point != null && point.spawnPointId == spawnPointId)
                return;
        }

        var go = new GameObject(objectName);
        go.transform.position = position;
        var spawn = go.AddComponent<SceneSpawnPoint>();
        spawn.spawnPointId = spawnPointId;
    }

    private static void SetTile(Tilemap markerMap, Vector3Int cell, string markerName)
    {
        var tile = AssetDatabase.LoadAssetAtPath<SceneSpawnTile>($"{MarkerFolder}/{markerName}.asset");
        if (tile == null)
        {
            Debug.LogWarning($"[BootstrapCoreplayM1Scenes] Marker tile missing: {markerName}");
            return;
        }

        markerMap.SetTile(cell, tile);
    }

    private static void SetTiles(Tilemap markerMap, string markerName, params Vector3Int[] cells)
    {
        if (cells == null) return;
        foreach (var cell in cells)
            SetTile(markerMap, cell, markerName);
    }

    private static SceneSpawnTile CreatePlayerSpawnMarker(string name, string spawnPointId, Color color)
    {
        var tile = LoadOrCreateAsset<SceneSpawnTile>($"{MarkerFolder}/{name}.asset");
        tile.markerKind = SceneMarkerKind.PlayerSpawn;
        tile.objectType = ObjectType.Player01;
        tile.entityData = null;
        tile.savePolicy = SceneEntitySavePolicy.Temporary;
        tile.spawnGroupId = "player";
        tile.spawnPointId = spawnPointId;
        tile.respawnMinutes = 0;
        tile.initialAmount = 1;
        tile.bypassPlacementValidation = true;
        tile.editorColor = color;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static SceneSpawnTile CreatePortalMarker(string name, string portalAssetName, string groupId, Color color)
    {
        return CreateEntityMarker(name, SceneMarkerKind.Portal, ObjectType.Portal01, $"{UtilityFolder}/{portalAssetName}", SceneEntitySavePolicy.Persistent, groupId, 0, color);
    }

    private static SceneSpawnTile CreateEntityMarker(
        string name,
        SceneMarkerKind markerKind,
        ObjectType objectType,
        string entityPath,
        SceneEntitySavePolicy savePolicy,
        string spawnGroupId,
        int respawnMinutes,
        Color color)
    {
        var entity = AssetDatabase.LoadAssetAtPath<EntityData>(entityPath);
        var tile = LoadOrCreateAsset<SceneSpawnTile>($"{MarkerFolder}/{name}.asset");
        tile.markerKind = markerKind;
        tile.objectType = objectType;
        tile.entityData = entity;
        tile.savePolicy = savePolicy;
        tile.spawnGroupId = spawnGroupId;
        tile.spawnPointId = string.Empty;
        tile.respawnMinutes = Mathf.Max(0, respawnMinutes);
        tile.initialAmount = 1;
        tile.bypassPlacementValidation = true;
        tile.editorSprite = entity != null ? entity.icon : null;
        tile.editorColor = color;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static EntityData LoadOrCreateEntity(string path) => LoadOrCreateAsset<EntityData>(path);

    private static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;

        EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
        asset = ScriptableObject.CreateInstance<T>();
        asset.name = Path.GetFileNameWithoutExtension(path);
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || AssetDatabase.IsValidFolder(folderPath))
            return;

        var parts = folderPath.Split('/');
        var current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static void SaveScene(Scene scene)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
