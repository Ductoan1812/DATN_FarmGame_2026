#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// One-off editor tool: paint SceneSpawnTile markers vào FarmScene Tm_RuntimeMarkers.
/// Menu: Tools > DATN > Seed FarmScene Markers
///
/// Chạy 1 lần để setup content ban đầu. Sau đó artist có thể chỉnh lại vị trí tile trong Tile Palette.
/// </summary>
public static class SeedFarmSceneMarkers
{
    private const string FarmScenePath = "Assets/Project/Scenes/Coreplay/FarmScene.unity";

    // ── Marker asset GUIDs ─────────────────────────────────────────────────
    private const string GUID_PlayerSpawn      = "817a9b874d3664b40ad331451271cc91";
    private const string GUID_PortalFarmToTown = "fb7b78da83af6da42aa54fc7ecb029fc";
    private const string GUID_AnchorFarmToTown = "46eb307d5e610614f90e84b547926ae3";
    private const string GUID_Tree01           = "882e7edc693564b46bbea820d00698f3";
    private const string GUID_Rock01           = "bfdb3dd91f464a2418545d7e5e937434";

    [MenuItem("Tools/DATN/Seed FarmScene Markers")]
    public static void SeedFarmScene()
    {
        // Load các marker tile assets
        var tilePlayerSpawn      = LoadTile(GUID_PlayerSpawn,      "Marker_Anchor_Player_Start");
        var tilePortalFarmToTown = LoadTile(GUID_PortalFarmToTown, "Marker_Portal_Farm_To_Town");
        var tileAnchorFarmToTown = LoadTile(GUID_AnchorFarmToTown, "Marker_Anchor_Farm_To_Town");
        var tileTree             = LoadTile(GUID_Tree01,           "Marker_Tree_01");
        var tileRock             = LoadTile(GUID_Rock01,           "Marker_Rock_01");

        if (tilePlayerSpawn == null || tilePortalFarmToTown == null)
        {
            Debug.LogError("[SeedFarmSceneMarkers] Missing required marker tile assets.");
            return;
        }

        // Open FarmScene (nếu chưa mở)
        bool needReopen = false;
        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != FarmScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(FarmScenePath);
            needReopen = true;
        }

        var scene = SceneManager.GetActiveScene();

        // Tìm Tm_RuntimeMarkers
        Tilemap markerMap = FindTilemap(scene, "Tm_RuntimeMarkers");
        if (markerMap == null)
        {
            Debug.LogError("[SeedFarmSceneMarkers] Tm_RuntimeMarkers not found in FarmScene.");
            return;
        }

        // FarmScene Ground bounds: min(-69,-46) max(42,32)
        // Đặt PlayerSpawn gần trung tâm phía phải (gần nhà)
        // Đặt Portal ở mép phải map (cạnh Town direction)
        // Cây và đá scattered khắp farm area

        int painted = 0;

        // 1. Player Spawn — gần center: (0, 0)
        PaintTile(markerMap, new Vector3Int(0, 0, 0), tilePlayerSpawn, ref painted);

        // 2. Portal Farm → Town — mép phải map (x=38, gần max=42)
        //    Đặt 2 tile cạnh nhau cho portal
        PaintTile(markerMap, new Vector3Int(38, 0, 0),  tilePortalFarmToTown, ref painted);
        PaintTile(markerMap, new Vector3Int(38, 1, 0),  tilePortalFarmToTown, ref painted);

        // 3. Anchor Farm→Town (spawn point người chơi khi đến từ Town)
        //    Đặt ngay cạnh portal ở phía bên trong
        PaintTile(markerMap, new Vector3Int(36, 0, 0),  tileAnchorFarmToTown, ref painted);

        // 4. Trees — farm area (scatter)
        if (tileTree != null)
        {
            var treePositions = new[]
            {
                new Vector3Int(-30, 15, 0), new Vector3Int(-25, 18, 0),
                new Vector3Int(-35, 10, 0), new Vector3Int(-20, 20, 0),
                new Vector3Int(-40, 5,  0), new Vector3Int(-15, 22, 0),
                new Vector3Int(-45, 0,  0), new Vector3Int(-50, 8,  0),
                new Vector3Int(-55, 15, 0), new Vector3Int(-60, 20, 0),
                new Vector3Int(-30, -20, 0), new Vector3Int(-40, -25, 0),
                new Vector3Int(-50, -30, 0), new Vector3Int(-20, -15, 0),
                new Vector3Int(-60, -10, 0), new Vector3Int(10,  20, 0),
                new Vector3Int(15,  25, 0),  new Vector3Int(20,  18, 0),
                new Vector3Int(25,  22, 0),  new Vector3Int(5,   28, 0),
            };
            foreach (var pos in treePositions)
                PaintTile(markerMap, pos, tileTree, ref painted);
        }

        // 5. Rocks — scattered ở các góc
        if (tileRock != null)
        {
            var rockPositions = new[]
            {
                new Vector3Int(-60, -35, 0), new Vector3Int(-55, -40, 0),
                new Vector3Int(-50, -38, 0), new Vector3Int(-65, -30, 0),
                new Vector3Int(30,  -20, 0), new Vector3Int(32,  -25, 0),
                new Vector3Int(28,  -30, 0), new Vector3Int(35,  -18, 0),
            };
            foreach (var pos in rockPositions)
                PaintTile(markerMap, pos, tileRock, ref painted);
        }

        // Save scene
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"[SeedFarmSceneMarkers] Painted {painted} marker tiles into FarmScene Tm_RuntimeMarkers. Scene saved.");
        EditorUtility.DisplayDialog("Done", $"Seeded {painted} markers into FarmScene Tm_RuntimeMarkers.\n\nSave file xong. Kiểm tra lại trong Scene view.", "OK");
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static SceneSpawnTile LoadTile(string guid, string name)
    {
        var path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning($"[SeedFarmSceneMarkers] Asset GUID not found: {guid} ({name})");
            return null;
        }
        var tile = AssetDatabase.LoadAssetAtPath<SceneSpawnTile>(path);
        if (tile == null)
            Debug.LogWarning($"[SeedFarmSceneMarkers] Cannot load SceneSpawnTile: {path}");
        return tile;
    }

    private static Tilemap FindTilemap(Scene scene, string tilemapName)
    {
        foreach (var go in scene.GetRootGameObjects())
        {
            var found = FindInChildren(go.transform, tilemapName);
            if (found != null)
                return found.GetComponent<Tilemap>();
        }
        return null;
    }

    private static Transform FindInChildren(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            var result = FindInChildren(child, name);
            if (result != null) return result;
        }
        return null;
    }

    private static void PaintTile(Tilemap map, Vector3Int cell, SceneSpawnTile tile, ref int count)
    {
        // Kiểm tra nếu ô đã có tile thì skip (không overwrite)
        if (map.GetTile(cell) != null)
        {
            Debug.Log($"[SeedFarmSceneMarkers] Cell {cell} already has tile, skipping.");
            return;
        }
        map.SetTile(cell, tile);
        count++;
    }
}
#endif
