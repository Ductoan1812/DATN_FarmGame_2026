using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.IO;
using System.Linq;

public static class ConfigureRealDesignPortals
{
    private const string MarkerFolder = "Assets/Project/ScriptableObjects/SceneMarkers/MVP";
    private const string TexturePath = "Assets/Project/Art/Environment/makets/market_portol.png";
    private const string UtilityFolder = "Assets/Project/ScriptableObjects/WorldObjects/Utility";

    [MenuItem("Tools/DATN/Configure Real Design Portals")]
    public static void Execute()
    {
        // 1. Configure Portal EntityData ScriptableObjects with symmetrical spawn point IDs
        ConfigurePortalEntityAssets();

        // 2. Dọn dẹp các Asset cũ thừa thãi
        CleanOldAssets();

        // 3. Tạo/đảm bảo tất cả 5 Anchor và 4 Portal được cấu hình chuẩn tên gọi và sprite
        EnsureAllMarkers();

        // 4. Configure FarmScene
        ConfigureFarmScene();

        // 5. Configure TownScene
        ConfigureTownScene();

        // 6. Configure MineScene
        ConfigureMineScene();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ConfigureRealDesignPortals] Completed successfully!");
    }

    private static void ConfigurePortalEntityAssets()
    {
        ConfigurePortalEntityAsset($"{UtilityFolder}/Portal_Farm_To_Town.asset", "portal_farm_to_town", "TownScene", "farm_to_town");
        ConfigurePortalEntityAsset($"{UtilityFolder}/Portal_Town_To_Farm.asset", "portal_town_to_farm", "FarmScene", "town_to_farm");
        ConfigurePortalEntityAsset($"{UtilityFolder}/Portal_Town_To_Mine.asset", "portal_town_to_mine", "MineScene", "town_to_mine");
        ConfigurePortalEntityAsset($"{UtilityFolder}/Portal_Mine_To_Town.asset", "portal_mine_to_town", "TownScene", "mine_to_town");
    }

    private static void ConfigurePortalEntityAsset(string path, string id, string targetScene, string targetSpawnId)
    {
        var data = AssetDatabase.LoadAssetAtPath<EntityData>(path);
        if (data == null)
        {
            Debug.LogError($"[ConfigureRealDesignPortals] Portal EntityData not found at path: {path}");
            return;
        }

        data.id = id;
        data.category = ItemCategory.Placeable;
        data.maxStack = 1;

        // Find or add ScenePortalModule
        ScenePortalModule portalModule = null;
        if (data.modules == null) data.modules = new System.Collections.Generic.List<IModuleData>();
        
        foreach (var m in data.modules)
        {
            if (m is ScenePortalModule pm)
            {
                portalModule = pm;
                break;
            }
        }

        if (portalModule == null)
        {
            portalModule = new ScenePortalModule();
            data.modules.Add(portalModule);
        }

        portalModule.optionTextKey = "ui.scene.enter";
        portalModule.priority = 40;
        portalModule.targetSceneName = targetScene;
        portalModule.targetSpawnPointId = targetSpawnId;
        portalModule.saveBeforeTransition = true;

        EditorUtility.SetDirty(data);
        Debug.Log($"[ConfigureRealDesignPortals] Configured Portal EntityData: {path} -> Target: {targetScene} at SpawnPoint: {targetSpawnId}");
    }

    private static void CleanOldAssets()
    {
        string[] oldAssets = {
            "Marker_Player_FarmEntryFromTown.asset",
            "Marker_Player_TownEntry.asset",
            "Marker_Player_MineEntry.asset",
            "Marker_Player_TownMineEntry.asset",
            "Marker_Player_Start.asset"
        };

        foreach (var name in oldAssets)
        {
            string path = $"{MarkerFolder}/{name}";
            if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(path) != null)
            {
                Debug.Log($"[Cleanup] Deleting old asset: {path}");
                AssetDatabase.DeleteAsset(path);
            }
        }
    }

    private static void EnsureAllMarkers()
    {
        Debug.Log("=== Configuring All Portals and Anchors according to the new naming standard ===");

        // --- 5 ANCHORS (Sử dụng market_portol_1) ---
        EnsureAnchorMarker("Marker_Anchor_Player_Start", "player_start", Color.white);
        EnsureAnchorMarker("Marker_Anchor_Farm_To_Town", "farm_to_town", Color.green);
        EnsureAnchorMarker("Marker_Anchor_Town_To_Farm", "town_to_farm", Color.cyan);
        EnsureAnchorMarker("Marker_Anchor_Town_To_Mine", "town_to_mine", Color.yellow);
        EnsureAnchorMarker("Marker_Anchor_Mine_To_Town", "mine_to_town", Color.magenta);

        // --- 4 PORTALS (Sử dụng market_portol_0, link to correct EntityData) ---
        EnsurePortalMarker("Marker_Portal_Farm_To_Town", "farm_to_town", $"{UtilityFolder}/Portal_Farm_To_Town.asset", Color.green);
        EnsurePortalMarker("Marker_Portal_Town_To_Farm", "town_to_farm", $"{UtilityFolder}/Portal_Town_To_Farm.asset", Color.cyan);
        EnsurePortalMarker("Marker_Portal_Town_To_Mine", "town_to_mine", $"{UtilityFolder}/Portal_Town_To_Mine.asset", Color.yellow);
        EnsurePortalMarker("Marker_Portal_Mine_To_Town", "mine_to_town", $"{UtilityFolder}/Portal_Mine_To_Town.asset", Color.magenta);
    }

    private static void ConfigureFarmScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Project/Scenes/Coreplay/FarmScene.unity", OpenSceneMode.Single);
        Debug.Log("=== Configuring FarmScene ===");

        // 1. Loại bỏ tất cả anchor object (SceneSpawnPoint) và physical portals (ScenePortalTrigger2D)
        RemovePhysicalPortalsAndSpawnPoints();

        // 2. Cấu hình Marker Anchor và Portal trên tilemap Tm_RuntimeMarkers
        var markerMap = FindTilemapByName("Tm_RuntimeMarkers");
        if (markerMap != null)
        {
            // Clear old cells to prevent leftovers
            Vector3Int[] oldCells = {
                new Vector3Int(0, 0, 0),
                new Vector3Int(7, 0, 0),
                new Vector3Int(8, 0, 0),
                new Vector3Int(7, -16, 0),
                new Vector3Int(8, -16, 0)
            };
            foreach (var c in oldCells) markerMap.SetTile(c, null);

            // Stamp Player Start Anchor
            var startTile = EnsureAnchorMarker("Marker_Anchor_Player_Start", "player_start", Color.white);
            markerMap.SetTile(new Vector3Int(0, 0, 0), startTile);

            // Stamp Town to Farm Anchor (spawn in Farm when entering from Town)
            var anchorTile = EnsureAnchorMarker("Marker_Anchor_Town_To_Farm", "town_to_farm", Color.cyan);
            markerMap.SetTile(new Vector3Int(7, -16, 0), anchorTile);

            // Stamp Farm to Town Portal (leads to TownScene)
            var portalTile = EnsurePortalMarker("Marker_Portal_Farm_To_Town", "farm_to_town", $"{UtilityFolder}/Portal_Farm_To_Town.asset", Color.green);
            markerMap.SetTile(new Vector3Int(8, -16, 0), portalTile);
            
            EditorUtility.SetDirty(markerMap.gameObject);
            Debug.Log("[ConfigureRealDesignPortals] Stamped anchors and portal markers in FarmScene.");
        }
        else
        {
            Debug.LogError("Tm_RuntimeMarkers not found in FarmScene!");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigureTownScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Project/Scenes/Coreplay/TownScene.unity", OpenSceneMode.Single);
        Debug.Log("=== Configuring TownScene ===");

        // 1. Loại bỏ tất cả anchor object (SceneSpawnPoint) và physical portals (ScenePortalTrigger2D)
        RemovePhysicalPortalsAndSpawnPoints();

        // 2. Cấu hình Marker Anchor và Portal trên tilemap Tm_RuntimeMarkers
        var markerMap = FindTilemapByName("Tm_RuntimeMarkers");
        if (markerMap != null)
        {
            // Clear old cells to prevent leftovers
            Vector3Int[] oldCells = {
                new Vector3Int(-6, -2, 0),
                new Vector3Int(7, -3, 0),
                new Vector3Int(-9, -2, 0),
                new Vector3Int(10, -3, 0),
                new Vector3Int(-3, 0, 0),
                new Vector3Int(-4, 0, 0),
                new Vector3Int(7, -3, 0),
                new Vector3Int(8, -3, 0)
            };
            foreach (var c in oldCells) markerMap.SetTile(c, null);

            // Stamp Farm to Town Anchor (spawn in Town when entering from Farm)
            var anchorFarmTile = EnsureAnchorMarker("Marker_Anchor_Farm_To_Town", "farm_to_town", Color.green);
            markerMap.SetTile(new Vector3Int(-3, 0, 0), anchorFarmTile);

            // Stamp Town to Farm Portal (leads to FarmScene)
            var portalFarmTile = EnsurePortalMarker("Marker_Portal_Town_To_Farm", "town_to_farm", $"{UtilityFolder}/Portal_Town_To_Farm.asset", Color.cyan);
            markerMap.SetTile(new Vector3Int(-4, 0, 0), portalFarmTile);

            // Stamp Mine to Town Anchor (spawn in Town when entering from Mine)
            var anchorMineTile = EnsureAnchorMarker("Marker_Anchor_Mine_To_Town", "mine_to_town", Color.magenta);
            markerMap.SetTile(new Vector3Int(7, -3, 0), anchorMineTile);

            // Stamp Town to Mine Portal (leads to MineScene)
            var portalMineTile = EnsurePortalMarker("Marker_Portal_Town_To_Mine", "town_to_mine", $"{UtilityFolder}/Portal_Town_To_Mine.asset", Color.yellow);
            markerMap.SetTile(new Vector3Int(8, -3, 0), portalMineTile);
            
            EditorUtility.SetDirty(markerMap.gameObject);
            Debug.Log("[ConfigureRealDesignPortals] Stamped anchors and portal markers in TownScene.");
        }
        else
        {
            Debug.LogError("Tm_RuntimeMarkers not found in TownScene!");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigureMineScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Project/Scenes/Coreplay/MineScene.unity", OpenSceneMode.Single);
        Debug.Log("=== Configuring MineScene ===");

        // 1. Loại bỏ tất cả anchor object (SceneSpawnPoint) và physical portals (ScenePortalTrigger2D)
        RemovePhysicalPortalsAndSpawnPoints();

        // 2. Cấu hình Marker Anchor và Portal trên tilemap Tm_RuntimeMarkers
        var markerMap = FindTilemapByName("Tm_RuntimeMarkers");
        if (markerMap != null)
        {
            // Clear old cells to prevent leftovers
            Vector3Int[] oldCells = {
                new Vector3Int(0, 0, 0),
                new Vector3Int(-8, 0, 0),
                new Vector3Int(-1, 0, 0)
            };
            foreach (var c in oldCells) markerMap.SetTile(c, null);

            // Stamp Town to Mine Anchor (spawn in Mine when entering from Town)
            var anchorTile = EnsureAnchorMarker("Marker_Anchor_Town_To_Mine", "town_to_mine", Color.yellow);
            markerMap.SetTile(new Vector3Int(0, 0, 0), anchorTile);

            // Stamp Mine to Town Portal (leads to TownScene)
            var portalTile = EnsurePortalMarker("Marker_Portal_Mine_To_Town", "mine_to_town", $"{UtilityFolder}/Portal_Mine_To_Town.asset", Color.magenta);
            markerMap.SetTile(new Vector3Int(-1, 0, 0), portalTile);
            
            EditorUtility.SetDirty(markerMap.gameObject);
            Debug.Log("[ConfigureRealDesignPortals] Stamped anchors and portal markers in MineScene.");
        }
        else
        {
            Debug.LogError("Tm_RuntimeMarkers not found in MineScene!");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void RemovePhysicalPortalsAndSpawnPoints()
    {
        var portals = Object.FindObjectsByType<ScenePortalTrigger2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var p in portals)
        {
            Debug.Log($"[Cleanup] Removing physical portal/spawnpoint object: {p.gameObject.name} in active scene.");
            Object.DestroyImmediate(p.gameObject);
        }
    }

    private static Tilemap FindTilemapByName(string name)
    {
        var tilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var tm in tilemaps)
        {
            if (tm.name == name) return tm;
        }
        return null;
    }

    private static Sprite LoadSubSprite(string texturePath, string spriteName)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(texturePath);
        foreach (var asset in assets)
        {
            if (asset is Sprite sprite && sprite.name == spriteName)
            {
                return sprite;
            }
        }
        return null;
    }

    public static SceneSpawnTile EnsurePortalMarker(string name, string spawnPointId, string entityDataPath, Color color)
    {
        string path = $"{MarkerFolder}/{name}.asset";
        var tile = AssetDatabase.LoadAssetAtPath<SceneSpawnTile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<SceneSpawnTile>();
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            AssetDatabase.CreateAsset(tile, path);
        }

        tile.markerKind = SceneMarkerKind.Portal;
        tile.objectType = ObjectType.Portal01;
        tile.entityData = AssetDatabase.LoadAssetAtPath<EntityData>(entityDataPath);
        tile.savePolicy = SceneEntitySavePolicy.Persistent;
        tile.spawnGroupId = "portal";
        tile.spawnPointId = spawnPointId;
        tile.respawnMinutes = 0;
        tile.initialAmount = 1;
        tile.bypassPlacementValidation = true;
        tile.editorSprite = LoadSubSprite(TexturePath, "market_portol_0");
        tile.editorColor = color;
        
        EditorUtility.SetDirty(tile);
        return tile;
    }

    public static SceneSpawnTile EnsureAnchorMarker(string name, string spawnPointId, Color color)
    {
        string path = $"{MarkerFolder}/{name}.asset";
        var tile = AssetDatabase.LoadAssetAtPath<SceneSpawnTile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<SceneSpawnTile>();
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            AssetDatabase.CreateAsset(tile, path);
        }

        tile.markerKind = SceneMarkerKind.PlayerSpawn;
        tile.objectType = ObjectType.Player01;
        tile.entityData = null;
        tile.savePolicy = SceneEntitySavePolicy.Temporary;
        tile.spawnGroupId = "player";
        tile.spawnPointId = spawnPointId;
        tile.respawnMinutes = 0;
        tile.initialAmount = 1;
        tile.bypassPlacementValidation = true;
        tile.editorSprite = LoadSubSprite(TexturePath, "market_portol_1");
        tile.editorColor = color;
        
        EditorUtility.SetDirty(tile);
        return tile;
    }
}
