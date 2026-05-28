using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class BuildCompleteFarmScene
{
    private const string ScenePath = "Assets/Project/Scenes/Coreplay/FarmScene.unity";
    private const string BackupScenePath = "Assets/Project/Scenes/Coreplay/FarmScene_PreCompleteBuild_Backup.unity";
    private const string GeneratedRoot = "Assets/Project/Art/Generated/FarmScene";
    private const string GeneratedSpriteFolder = GeneratedRoot + "/Sprites";
    private const string GeneratedTileFolder = GeneratedRoot + "/Tiles";
    private const string GeneratedPaletteFolder = "Assets/Project/Art/Environment/Palettes";
    private const string ScheduleFolder = "Assets/Project/Resources/Data/Schedules";
    private const string MarkerFolder = "Assets/Project/ScriptableObjects/SceneMarkers/MVP";
    private const string SpriteUnlitMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

    private static Material _spriteUnlitMaterial;

    [MenuItem("Tools/DATN/One-off Setup/Scenes/Build Complete FarmScene")]
    public static void Build()
    {
        EnsureProjectFolders();
        EnsureSourceNote();
        BackupSceneIfNeeded();

        Scene scene = File.Exists(ScenePath)
            ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
            : EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var sprites = EnsureGeneratedSprites();
        var generatedTiles = EnsureGeneratedTiles(sprites);
        var tileData = AssetDatabase.LoadAssetAtPath<TileData>("Assets/Project/Resources/TileData.asset");
        var tiles = ResolveSceneTiles(tileData, generatedTiles);

        BuildTilemaps(tiles, out var tilemaps);
        BuildStaticObjects(sprites, tilemaps);
        BuildAnchors();
        BuildMarkers(sprites, tilemaps.RuntimeMarkers);
        BuildPalettePrefab(tiles);
        EnsureSchedules();
        ConfigurePrefabs();
        ConfigureSceneSystems(tilemaps, tileData);
        ConfigureCamera();
        EnsureSceneInBuildSettings();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BuildCompleteFarmScene] FarmScene complete build finished.");
    }

    private static void EnsureProjectFolders()
    {
        EnsureFolder(GeneratedRoot);
        EnsureFolder(GeneratedSpriteFolder);
        EnsureFolder(GeneratedTileFolder);
        EnsureFolder(GeneratedPaletteFolder);
        EnsureFolder(ScheduleFolder);
        EnsureFolder(MarkerFolder);
    }

    private static void EnsureSourceNote()
    {
        string notePath = GeneratedRoot + "/SOURCE.txt";
        string fullPath = ToAbsolutePath(notePath);
        File.WriteAllText(fullPath,
            "FarmScene generated fallback art.\n" +
            "Created locally by DATN FarmScene builder for missing small sprites/tiles.\n" +
            "Itch.io fallback references approved by project owner:\n" +
            "- https://hellorumin.itch.io/pixel-farm-asset-pack\n" +
            "- https://sakpix.itch.io/nature-outdoor-pixel-art-asset-pack-free-lite-tileset-for-top-down-games-32x32\n" +
            "- https://nogardlab.itch.io/stardew-farm-pixel-art-top-down-assets\n" +
            "- https://suawu.itch.io/free-animal-aseet-32x32\n" +
            "No third-party pack files are redistributed by this generated fallback folder.\n");
        AssetDatabase.ImportAsset(notePath);
    }

    private static void BackupSceneIfNeeded()
    {
        if (!File.Exists(ToAbsolutePath(ScenePath)) || File.Exists(ToAbsolutePath(BackupScenePath)))
            return;

        AssetDatabase.CopyAsset(ScenePath, BackupScenePath);
    }

    private static FarmSprites EnsureGeneratedSprites()
    {
        return new FarmSprites
        {
            Grass = EnsureSprite("tile_grass", 32, 32, DrawGrass, new Vector2(0.5f, 0.5f)),
            Path = EnsureSprite("tile_path", 32, 32, DrawPath, new Vector2(0.5f, 0.5f)),
            Water = EnsureSprite("tile_water", 32, 32, DrawWater, new Vector2(0.5f, 0.5f)),
            Soil = EnsureSprite("tile_soil", 32, 32, DrawSoil, new Vector2(0.5f, 0.5f)),
            Watered = EnsureSprite("tile_watered", 32, 32, DrawWatered, new Vector2(0.5f, 0.5f)),
            Fence = EnsureSprite("tile_fence", 32, 32, DrawFence, new Vector2(0.5f, 0.5f)),
            Collision = EnsureSprite("tile_collision", 32, 32, DrawCollision, new Vector2(0.5f, 0.5f)),
            Crop = EnsureSprite("tile_crop", 32, 32, DrawCrop, new Vector2(0.5f, 0.5f)),
            Flower = EnsureSprite("tile_flower", 32, 32, DrawFlower, new Vector2(0.5f, 0.5f)),
            House = EnsureSprite("object_house", 192, 160, DrawHouse, new Vector2(0.5f, 0f)),
            Barn = EnsureSprite("object_barn", 160, 128, DrawBarn, new Vector2(0.5f, 0f)),
            Tree = EnsureSprite("object_tree", 96, 128, DrawTree, new Vector2(0.5f, 0f)),
            Well = EnsureSprite("object_well", 64, 64, DrawWell, new Vector2(0.5f, 0f)),
            Dock = EnsureSprite("object_dock", 128, 64, DrawDock, new Vector2(0.5f, 0.5f)),
            Rock = EnsureSprite("object_rock", 64, 48, DrawRock, new Vector2(0.5f, 0f)),
            Sign = EnsureSprite("object_sign", 48, 48, DrawSign, new Vector2(0.5f, 0f)),
            Marker = EnsureSprite("marker_runtime", 32, 32, DrawMarker, new Vector2(0.5f, 0.5f))
        };
    }

    private static GeneratedTiles EnsureGeneratedTiles(FarmSprites sprites)
    {
        return new GeneratedTiles
        {
            Grass = EnsureTile("Tile_Farm_Grass", sprites.Grass, Tile.ColliderType.None),
            Path = EnsureTile("Tile_Farm_Path", sprites.Path, Tile.ColliderType.None),
            Water = EnsureTile("Tile_Farm_Water", sprites.Water, Tile.ColliderType.None),
            Soil = EnsureTile("Tile_Farm_Soil", sprites.Soil, Tile.ColliderType.None),
            Watered = EnsureTile("Tile_Farm_Watered", sprites.Watered, Tile.ColliderType.None),
            Fence = EnsureTile("Tile_Farm_Fence", sprites.Fence, Tile.ColliderType.None),
            Collision = EnsureTile("Tile_Farm_Collision", sprites.Collision, Tile.ColliderType.Grid),
            Crop = EnsureTile("Tile_Farm_Crop", sprites.Crop, Tile.ColliderType.None),
            Flower = EnsureTile("Tile_Farm_Flower", sprites.Flower, Tile.ColliderType.None)
        };
    }

    private static SceneTiles ResolveSceneTiles(TileData tileData, GeneratedTiles generated)
    {
        return new SceneTiles
        {
            Grass = tileData != null && tileData.grassTile != null ? tileData.grassTile : generated.Grass,
            Ground = tileData != null && tileData.landTile != null ? tileData.landTile : generated.Grass,
            Soil = tileData != null && tileData.plowedTile != null ? tileData.plowedTile : generated.Soil,
            Watered = tileData != null && tileData.wateredTile != null ? tileData.wateredTile : generated.Watered,
            Path = generated.Path,
            Water = generated.Water,
            Fence = generated.Fence,
            Collision = generated.Collision,
            Crop = generated.Crop,
            Flower = generated.Flower
        };
    }

    private static void BuildTilemaps(SceneTiles tiles, out SceneTilemaps tilemaps)
    {
        var grid = GetOrCreateRoot("Grid").GetComponent<Grid>();
        if (grid == null)
            grid = GetOrCreateRoot("Grid").AddComponent<Grid>();

        grid.cellSize = Vector3.one;
        grid.cellGap = Vector3.zero;
        grid.cellLayout = GridLayout.CellLayout.Rectangle;
        grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;

        tilemaps = new SceneTilemaps
        {
            Ground = GetOrCreateTilemap(grid, "Tm_Ground", 0, false),
            GroundDetail = GetOrCreateTilemap(grid, "Tm_GroundDetail", 1, false),
            Watered = GetOrCreateTilemap(grid, "Tm_Watered", 2, false),
            Collision = GetOrCreateTilemap(grid, "Tm_Collision", 3, true),
            Decoration = GetOrCreateTilemap(grid, "Tm_Decoration", 4, false),
            Overlay = GetOrCreateTilemap(grid, "Tm_Overlay", 8, false),
            RuntimeMarkers = GetOrCreateTilemap(grid, SceneContext.RuntimeMarkersTilemapName, 20, true)
        };

        ClearTilemaps(tilemaps);

        for (int x = -34; x <= 34; x++)
        {
            for (int y = -21; y <= 21; y++)
                SetTile(tilemaps.Ground, x, y, tiles.Grass);
        }

        PaintWater(tilemaps, tiles);
        PaintPaths(tilemaps, tiles);
        PaintFarmField(tilemaps, tiles);
        PaintOrchard(tilemaps, tiles);
        PaintMapEdges(tilemaps, tiles);
        ConfigureCollisionTilemap(tilemaps.Collision);
    }

    private static void PaintWater(SceneTilemaps tilemaps, SceneTiles tiles)
    {
        for (int x = -31; x <= -15; x++)
        {
            for (int y = -15; y <= -5; y++)
            {
                float dx = (x + 23f) / 8.5f;
                float dy = (y + 10f) / 5.8f;
                if (dx * dx + dy * dy <= 1f)
                {
                    SetTile(tilemaps.GroundDetail, x, y, tiles.Water);
                    SetTile(tilemaps.Collision, x, y, tiles.Collision);
                }
            }
        }

        for (int x = -28; x <= -20; x++)
        {
            for (int y = -21; y <= -15; y++)
            {
                SetTile(tilemaps.GroundDetail, x, y, tiles.Water);
                SetTile(tilemaps.Collision, x, y, tiles.Collision);
            }
        }

        PaintRect(tilemaps.Decoration, -23, -7, -20, -6, tiles.Path);
    }

    private static void PaintPaths(SceneTilemaps tilemaps, SceneTiles tiles)
    {
        PaintPath(tilemaps.GroundDetail, tiles.Path, new Vector2Int(-9, 7), new Vector2Int(-1, 1), 1);
        PaintPath(tilemaps.GroundDetail, tiles.Path, new Vector2Int(-1, 1), new Vector2Int(8, 0), 1);
        PaintPath(tilemaps.GroundDetail, tiles.Path, new Vector2Int(8, 0), new Vector2Int(18, -10), 1);
        PaintPath(tilemaps.GroundDetail, tiles.Path, new Vector2Int(-1, 1), new Vector2Int(-20, -6), 1);
        PaintPath(tilemaps.GroundDetail, tiles.Path, new Vector2Int(8, 0), new Vector2Int(17, 12), 1);
        PaintPath(tilemaps.GroundDetail, tiles.Path, new Vector2Int(27, 0), new Vector2Int(33, 0), 1);

        for (int i = 0; i < 90; i++)
        {
            int x = -30 + i % 61;
            int y = -17 + (i * 17 % 35);
            if ((x + y) % 7 == 0)
                SetTile(tilemaps.Decoration, x, y, tiles.Flower);
        }
    }

    private static void PaintFarmField(SceneTilemaps tilemaps, SceneTiles tiles)
    {
        PaintFence(tilemaps, tiles, 7, -8, 28, 8, new Vector2Int(7, 0), new Vector2Int(7, 1));

        for (int x = 10; x <= 25; x++)
        {
            for (int y = -5; y <= 5; y++)
            {
                SetTile(tilemaps.Ground, x, y, tiles.Soil);
                if ((x + y) % 3 == 0)
                    SetTile(tilemaps.Decoration, x, y, tiles.Crop);
            }
        }
    }

    private static void PaintOrchard(SceneTilemaps tilemaps, SceneTiles tiles)
    {
        PaintFence(tilemaps, tiles, 9, 10, 30, 19, new Vector2Int(18, 10), new Vector2Int(19, 10));
    }

    private static void PaintFence(SceneTilemaps tilemaps, SceneTiles tiles, int xMin, int yMin, int xMax, int yMax, params Vector2Int[] gates)
    {
        var gateSet = new HashSet<Vector2Int>(gates);
        for (int x = xMin; x <= xMax; x++)
        {
            SetFenceIfNotGate(tilemaps, tiles, x, yMin, gateSet);
            SetFenceIfNotGate(tilemaps, tiles, x, yMax, gateSet);
        }

        for (int y = yMin; y <= yMax; y++)
        {
            SetFenceIfNotGate(tilemaps, tiles, xMin, y, gateSet);
            SetFenceIfNotGate(tilemaps, tiles, xMax, y, gateSet);
        }
    }

    private static void SetFenceIfNotGate(SceneTilemaps tilemaps, SceneTiles tiles, int x, int y, HashSet<Vector2Int> gates)
    {
        var cell = new Vector2Int(x, y);
        if (gates.Contains(cell))
            return;

        SetTile(tilemaps.Decoration, x, y, tiles.Fence);
        SetTile(tilemaps.Collision, x, y, tiles.Collision);
    }

    private static void PaintMapEdges(SceneTilemaps tilemaps, SceneTiles tiles)
    {
        for (int x = -35; x <= 35; x++)
        {
            SetTile(tilemaps.Collision, x, -22, tiles.Collision);
            SetTile(tilemaps.Collision, x, 22, tiles.Collision);
        }

        for (int y = -22; y <= 22; y++)
        {
            SetTile(tilemaps.Collision, -35, y, tiles.Collision);
            SetTile(tilemaps.Collision, 35, y, tiles.Collision);
        }
    }

    private static void BuildStaticObjects(FarmSprites sprites, SceneTilemaps tilemaps)
    {
        var root = ReplaceGeneratedRoot("SceneObjects_Static", "Generated_FarmScene_Static");

        CreateStaticSprite(root.transform, "FarmHouse", sprites.House, new Vector2(-10, 8), 7, new Vector2(5f, 2.2f), new Vector2(0f, 1.1f));
        FillCollision(tilemaps.Collision, -13, 8, -8, 10);

        CreateStaticSprite(root.transform, "Barn", sprites.Barn, new Vector2(21, -15), 7, new Vector2(4.5f, 2f), new Vector2(0f, 1f));
        FillCollision(tilemaps.Collision, 19, -15, 23, -13);

        CreateStaticSprite(root.transform, "Well", sprites.Well, new Vector2(-26, 8), 5, new Vector2(1.4f, 1.1f), new Vector2(0f, 0.55f));
        CreateStaticSprite(root.transform, "Dock", sprites.Dock, new Vector2(-22, -6.8f), 5, Vector2.zero, Vector2.zero);
        CreateStaticSprite(root.transform, "FarmSign", sprites.Sign, new Vector2(-5, 4), 6, new Vector2(0.5f, 0.35f), new Vector2(0f, 0.2f));

        CreateStaticSprite(root.transform, "ShedRock", sprites.Rock, new Vector2(-23, 2), 5, new Vector2(1.2f, 0.8f), new Vector2(0f, 0.4f));
        FillCollision(tilemaps.Collision, -24, 2, -23, 2);

        Vector2[] trees =
        {
            new(12, 12), new(17, 12), new(22, 12), new(27, 12),
            new(12, 16), new(17, 16), new(22, 16), new(27, 16),
            new(-30, 15), new(-27, 18), new(31, 13), new(31, -8), new(-30, -18)
        };

        for (int i = 0; i < trees.Length; i++)
        {
            CreateStaticSprite(root.transform, $"Tree_{i:00}", sprites.Tree, trees[i], 9, new Vector2(0.9f, 0.7f), new Vector2(0f, 0.35f));
            FillCollision(tilemaps.Collision, Mathf.FloorToInt(trees[i].x), Mathf.FloorToInt(trees[i].y), Mathf.FloorToInt(trees[i].x), Mathf.FloorToInt(trees[i].y));
        }
    }

    private static void BuildAnchors()
    {
        var root = ReplaceGeneratedRoot("Scene_Anchors", "Generated_FarmScene_Anchors");
        CreateAnchor(root.transform, "player_start", -2.5f, -1.5f);
        CreateAnchor(root.transform, "farm_entry_from_town", 31.5f, 0.5f);
        CreateAnchor(root.transform, "farm_house_door", -10.5f, 6.5f);
        CreateAnchor(root.transform, "farm_field_center", 18.5f, 0.5f);
        CreateAnchor(root.transform, "farm_field_gate", 7.5f, 0.5f);
        CreateAnchor(root.transform, "pond_dock", -21.5f, -5.5f);
        CreateAnchor(root.transform, "well", -26.5f, 8.5f);
        CreateAnchor(root.transform, "orchard_center", 20.5f, 14.5f);
        CreateAnchor(root.transform, "barn_door", 21.5f, -12.5f);
        CreateAnchor(root.transform, "path_crossroad", -1.5f, 1.5f);
    }

    private static void BuildMarkers(FarmSprites sprites, Tilemap markerMap)
    {
        markerMap.ClearAllTiles();

        var playerStart = EnsureMarker("Marker_Player_Start", SceneMarkerKind.PlayerSpawn, ObjectType.Player01, null, SceneEntitySavePolicy.Persistent, "player", "player_start", sprites.Marker, Color.cyan);
        var farmEntry = EnsureMarker("Marker_Player_FarmEntryFromTown", SceneMarkerKind.PlayerSpawn, ObjectType.Player01, null, SceneEntitySavePolicy.Persistent, "player", "farm_entry_from_town", sprites.Marker, Color.cyan);
        var portal = EnsureMarker("Marker_Portal_Farm_To_Town", SceneMarkerKind.Portal, ObjectType.Portal01, EnsurePortalEntityData(), SceneEntitySavePolicy.Persistent, "farm_to_town", string.Empty, sprites.Marker, Color.blue);

        var npcShop = AssetDatabase.LoadAssetAtPath<EntityData>("Assets/Project/ScriptableObjects/Characters/NPCs/NPC_Banhang.asset");
        var npcAnimal = AssetDatabase.LoadAssetAtPath<EntityData>("Assets/Project/ScriptableObjects/Characters/NPCs/NPC_ChanNuoi.asset");
        var animal = AssetDatabase.LoadAssetAtPath<EntityData>("Assets/Project/ScriptableObjects/WorldObjects/Animals/Animal_Chicken_01.asset");

        var farmerMarker = EnsureMarker("Marker_NPC_Farm_Helper", SceneMarkerKind.Npc, ObjectType.NPCShop01, npcShop, SceneEntitySavePolicy.Persistent, "farm_helper", string.Empty, sprites.Marker, Color.green);
        var animalNpcMarker = EnsureMarker("Marker_NPC_Farm_AnimalKeeper", SceneMarkerKind.Npc, ObjectType.NPCShop01, npcAnimal, SceneEntitySavePolicy.Persistent, "farm_animal_keeper", string.Empty, sprites.Marker, Color.yellow);
        var animalMarker = EnsureMarker("Marker_Animal_Chicken_Farm", SceneMarkerKind.Object, ObjectType.Animal01, animal, SceneEntitySavePolicy.Persistent, "farm_animals", string.Empty, sprites.Marker, new Color(1f, 0.7f, 0.25f));

        markerMap.SetTile(new Vector3Int(-2, -2, 0), playerStart);
        markerMap.SetTile(new Vector3Int(31, 0, 0), farmEntry);
        markerMap.SetTile(new Vector3Int(33, 0, 0), portal);
        markerMap.SetTile(new Vector3Int(-4, 4, 0), farmerMarker);
        markerMap.SetTile(new Vector3Int(21, -12, 0), animalNpcMarker);
        markerMap.SetTile(new Vector3Int(20, -11, 0), animalMarker);
    }

    private static void BuildPalettePrefab(SceneTiles tiles)
    {
        var paletteRoot = new GameObject("Palette_FarmScene");
        var grid = paletteRoot.AddComponent<Grid>();
        var mapObject = new GameObject("Palette_Tiles");
        mapObject.transform.SetParent(paletteRoot.transform);
        var map = mapObject.AddComponent<Tilemap>();
        mapObject.AddComponent<TilemapRenderer>();

        TileBase[] paletteTiles =
        {
            tiles.Grass, tiles.Path, tiles.Water, tiles.Soil, tiles.Watered, tiles.Fence, tiles.Crop, tiles.Flower, tiles.Collision
        };

        for (int i = 0; i < paletteTiles.Length; i++)
            map.SetTile(new Vector3Int(i, 0, 0), paletteTiles[i]);

        PrefabUtility.SaveAsPrefabAsset(paletteRoot, GeneratedPaletteFolder + "/Palette_FarmScene.prefab");
        UnityEngine.Object.DestroyImmediate(paletteRoot);
    }

    private static void EnsureSchedules()
    {
        EnsureSchedule("npc_banhang", new[]
        {
            Entry(360, "farm_house_door", ScheduleAction.Stand, 0f),
            Entry(480, "farm_field_gate", ScheduleAction.WanderRadius, 2f),
            Entry(720, "well", ScheduleAction.Stand, 0f),
            Entry(900, "farm_field_center", ScheduleAction.WanderRadius, 3f),
            Entry(1080, "farm_house_door", ScheduleAction.Stand, 0f)
        });

        EnsureSchedule("npc_channuoi", new[]
        {
            Entry(360, "barn_door", ScheduleAction.Stand, 0f),
            Entry(480, "orchard_center", ScheduleAction.WanderRadius, 3f),
            Entry(720, "pond_dock", ScheduleAction.Stand, 0f),
            Entry(900, "barn_door", ScheduleAction.WanderRadius, 2f),
            Entry(1080, "barn_door", ScheduleAction.Stand, 0f)
        });

        EnsureSchedule("npc_chetao", new[]
        {
            Entry(360, "farm_house_door", ScheduleAction.Stand, 0f),
            Entry(540, "path_crossroad", ScheduleAction.WanderRadius, 2f),
            Entry(1020, "farm_house_door", ScheduleAction.Stand, 0f)
        });

        EnsureSchedule("npc_sukien", new[]
        {
            Entry(360, "pond_dock", ScheduleAction.Stand, 0f),
            Entry(600, "orchard_center", ScheduleAction.WanderRadius, 2.5f),
            Entry(1080, "farm_house_door", ScheduleAction.Stand, 0f)
        });

        EnsureSchedule("animal_chicken_01", new[]
        {
            Entry(360, "barn_door", ScheduleAction.WanderRadius, 2f),
            Entry(600, "farm_field_gate", ScheduleAction.WanderRadius, 2f),
            Entry(1020, "barn_door", ScheduleAction.WanderRadius, 1.5f)
        });
    }

    private static ScheduleEntry Entry(int minute, string anchor, ScheduleAction action, float radius)
    {
        return new ScheduleEntry
        {
            startMinuteOfDay = minute,
            targetAnchorId = anchor,
            action = action,
            wanderRadius = radius,
            waitSeconds = 2f
        };
    }

    private static void EnsureSchedule(string id, ScheduleEntry[] entries)
    {
        string path = $"{ScheduleFolder}/{id}.asset";
        var schedule = AssetDatabase.LoadAssetAtPath<DailyScheduleData>(path);
        if (schedule == null)
        {
            schedule = ScriptableObject.CreateInstance<DailyScheduleData>();
            AssetDatabase.CreateAsset(schedule, path);
        }

        schedule.scheduleId = id;
        schedule.entries.Clear();
        schedule.entries.AddRange(entries);
        EditorUtility.SetDirty(schedule);
    }

    private static void ConfigurePrefabs()
    {
        ConfigurePortalPrefab("Assets/Project/Prefabs/WorldEntities/Portal_Base.prefab");
        ConfigureMoverPrefab("Assets/Project/Prefabs/Characters/NPC_Base.prefab");
        ConfigureMoverPrefab("Assets/Project/Prefabs/Characters/NPC_Shop.prefab");
        ConfigureMoverPrefab("Assets/Project/Prefabs/Characters/NPC_Crafting.prefab");
        ConfigureMoverPrefab("Assets/Project/Prefabs/Characters/NPC_Quest.prefab");
        ConfigureMoverPrefab("Assets/Project/Prefabs/Characters/Animal_Base.prefab");
    }

    private static void ConfigurePortalPrefab(string path)
    {
        if (!File.Exists(ToAbsolutePath(path)))
            return;

        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var collider = root.GetComponent<BoxCollider2D>();
            if (collider == null)
                collider = root.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = Vector2.one;
            collider.offset = Vector2.zero;

            var body = root.GetComponent<Rigidbody2D>();
            if (body == null)
                body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;

            if (root.GetComponent<AutoPortalTrigger2D>() == null)
                root.AddComponent<AutoPortalTrigger2D>();

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureMoverPrefab(string path)
    {
        if (!File.Exists(ToAbsolutePath(path)))
            return;

        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var body = root.GetComponent<Rigidbody2D>();
            if (body == null)
                body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;

            if (root.GetComponent<PositionSync>() == null)
                root.AddComponent<PositionSync>();
            if (root.GetComponent<NavAgent2D>() == null)
                root.AddComponent<NavAgent2D>();
            if (root.GetComponent<NpcScheduleController>() == null)
                root.AddComponent<NpcScheduleController>();

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureSceneSystems(SceneTilemaps tilemaps, TileData tileData)
    {
        var managers = GetOrCreateRoot("Managers");

        var context = UnityEngine.Object.FindAnyObjectByType<SceneContext>();
        if (context == null)
            context = managers.AddComponent<SceneContext>();
        SetObjectReference(context, "runtimeMarkers", tilemaps.RuntimeMarkers);

        var scanner = UnityEngine.Object.FindAnyObjectByType<SceneContentScanner>();
        if (scanner == null)
            scanner = managers.AddComponent<SceneContentScanner>();
        SetObjectReference(scanner, "sceneContext", context);

        var gameManager = UnityEngine.Object.FindAnyObjectByType<GameManager>();
        if (gameManager != null)
        {
            SetObjectReference(gameManager, "tmGround", tilemaps.Ground);
            SetObjectReference(gameManager, "tmWatered", tilemaps.Watered);
            SetObjectReference(gameManager, "tmGroundDetail", tilemaps.GroundDetail);
            SetObjectReference(gameManager, "tmCollision", tilemaps.Collision);
            SetObjectReference(gameManager, "tmDecoration", tilemaps.Decoration);
            SetObjectReference(gameManager, "tmOverlay", tilemaps.Overlay);
            if (tileData != null)
                SetObjectReference(gameManager, "tileData", tileData);
        }
        else
        {
            Debug.LogWarning("[BuildCompleteFarmScene] GameManager not found in FarmScene; tilemap references could not be assigned.");
        }
    }

    private static void ConfigureCamera()
    {
        var cameras = GetOrCreateRoot("Cameras");
        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(cameras.transform);
            mainCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        mainCamera.orthographic = true;
        mainCamera.orthographicSize = 6f;
        mainCamera.transform.position = new Vector3(0f, 0f, -10f);

        var boundsObject = GameObject.Find("FarmScene_CameraBounds");
        if (boundsObject == null)
            boundsObject = new GameObject("FarmScene_CameraBounds");
        boundsObject.transform.SetParent(cameras.transform);
        var polygon = boundsObject.GetComponent<PolygonCollider2D>();
        if (polygon == null)
            polygon = boundsObject.AddComponent<PolygonCollider2D>();
        polygon.isTrigger = true;
        polygon.points = new[]
        {
            new Vector2(-32f, -20f),
            new Vector2(32f, -20f),
            new Vector2(32f, 20f),
            new Vector2(-32f, 20f)
        };

        AddComponentByName(mainCamera.gameObject, "Cinemachine.CinemachineBrain");

        var vcamObject = GameObject.Find("CM vcam FarmScene");
        if (vcamObject == null)
            vcamObject = new GameObject("CM vcam FarmScene");
        vcamObject.transform.SetParent(cameras.transform);
        var vcam = AddComponentByName(vcamObject, "Cinemachine.CinemachineVirtualCamera");
        var confiner = AddComponentByName(vcamObject, "Cinemachine.CinemachineConfiner2D");
        if (vcam != null)
        {
            SetSerializedProperty(vcam, "m_Priority", 20);
            SetSerializedProperty(vcam, "m_Lens.OrthographicSize", 6f);
            if (vcamObject.GetComponent<CinemachinePlayerBinder>() == null)
                vcamObject.AddComponent<CinemachinePlayerBinder>();
        }

        if (confiner != null)
            SetObjectReference(confiner, "m_BoundingShape2D", polygon);
    }

    private static EntityData EnsurePortalEntityData()
    {
        string path = "Assets/Project/ScriptableObjects/WorldObjects/Utility/Portal_Farm_To_Town.asset";
        EnsureFolder("Assets/Project/ScriptableObjects/WorldObjects/Utility");
        var portal = AssetDatabase.LoadAssetAtPath<EntityData>(path);
        if (portal == null)
        {
            portal = ScriptableObject.CreateInstance<EntityData>();
            AssetDatabase.CreateAsset(portal, path);
        }

        portal.id = "portal_farm_to_town";
        portal.keyName = "m1.portal.farm_to_town.name";
        portal.category = ItemCategory.Placeable;
        EnsurePortalModule(portal);
        EditorUtility.SetDirty(portal);
        return portal;
    }

    private static void EnsurePortalModule(EntityData portal)
    {
        if (portal.modules == null)
            portal.modules = new List<IModuleData>();

        ScenePortalModule module = null;
        for (int i = 0; i < portal.modules.Count; i++)
        {
            if (portal.modules[i] is ScenePortalModule existing)
            {
                module = existing;
                break;
            }
        }

        if (module == null)
        {
            module = new ScenePortalModule();
            portal.modules.Add(module);
        }

        module.targetSceneName = "TownScene";
        module.targetSpawnPointId = "town_entry";
        module.saveBeforeTransition = true;
        module.optionTextKey = "ui.scene.enter";
    }

    private static SceneSpawnTile EnsureMarker(
        string name,
        SceneMarkerKind kind,
        ObjectType objectType,
        EntityData entityData,
        SceneEntitySavePolicy savePolicy,
        string groupId,
        string spawnPointId,
        Sprite editorSprite,
        Color editorColor)
    {
        string path = $"{MarkerFolder}/{name}.asset";
        var marker = AssetDatabase.LoadAssetAtPath<SceneSpawnTile>(path);
        if (marker == null)
        {
            marker = ScriptableObject.CreateInstance<SceneSpawnTile>();
            AssetDatabase.CreateAsset(marker, path);
        }

        marker.name = name;
        marker.markerKind = kind;
        marker.objectType = objectType;
        marker.entityData = entityData;
        marker.savePolicy = savePolicy;
        marker.spawnGroupId = groupId;
        marker.spawnPointId = spawnPointId;
        marker.respawnMinutes = 0;
        marker.initialAmount = 1;
        marker.bypassPlacementValidation = kind == SceneMarkerKind.Portal || kind == SceneMarkerKind.Npc || objectType == ObjectType.Animal01;
        marker.editorSprite = editorSprite;
        marker.editorColor = editorColor;
        EditorUtility.SetDirty(marker);
        return marker;
    }

    private static Tilemap GetOrCreateTilemap(Grid grid, string name, int sortingOrder, bool hideRenderer)
    {
        var existing = FindChild(grid.transform, name);
        var tilemapObject = existing != null ? existing.gameObject : new GameObject(name);
        tilemapObject.transform.SetParent(grid.transform);
        tilemapObject.transform.localPosition = Vector3.zero;
        tilemapObject.transform.localRotation = Quaternion.identity;
        tilemapObject.transform.localScale = Vector3.one;

        var tilemap = tilemapObject.GetComponent<Tilemap>();
        if (tilemap == null)
            tilemap = tilemapObject.AddComponent<Tilemap>();

        var renderer = tilemapObject.GetComponent<TilemapRenderer>();
        if (renderer == null)
            renderer = tilemapObject.AddComponent<TilemapRenderer>();

        if (renderer != null)
        {
            renderer.sortingOrder = sortingOrder;
            renderer.enabled = !hideRenderer;
            renderer.sharedMaterial = LoadSpriteUnlitMaterial();
        }

        return tilemap;
    }

    private static void ConfigureCollisionTilemap(Tilemap tilemap)
    {
        var collider = tilemap.GetComponent<TilemapCollider2D>();
        if (collider == null)
            collider = tilemap.gameObject.AddComponent<TilemapCollider2D>();
        collider.usedByComposite = true;

        var composite = tilemap.GetComponent<CompositeCollider2D>();
        if (composite == null)
            composite = tilemap.gameObject.AddComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;

        var body = tilemap.GetComponent<Rigidbody2D>();
        if (body == null)
            body = tilemap.gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;
    }

    private static void ClearTilemaps(SceneTilemaps tilemaps)
    {
        tilemaps.Ground.ClearAllTiles();
        tilemaps.GroundDetail.ClearAllTiles();
        tilemaps.Watered.ClearAllTiles();
        tilemaps.Collision.ClearAllTiles();
        tilemaps.Decoration.ClearAllTiles();
        tilemaps.Overlay.ClearAllTiles();
        tilemaps.RuntimeMarkers.ClearAllTiles();
    }

    private static GameObject GetOrCreateRoot(string name)
    {
        var existing = GameObject.Find(name);
        return existing != null ? existing : new GameObject(name);
    }

    private static GameObject ReplaceGeneratedRoot(string parentName, string generatedName)
    {
        var parent = GetOrCreateRoot(parentName);
        var existing = FindChild(parent.transform, generatedName);
        if (existing != null)
            UnityEngine.Object.DestroyImmediate(existing.gameObject);

        var root = new GameObject(generatedName);
        root.transform.SetParent(parent.transform);
        root.transform.localPosition = Vector3.zero;
        return root;
    }

    private static void CreateAnchor(Transform root, string id, float x, float y)
    {
        var anchorObject = new GameObject(id);
        anchorObject.transform.SetParent(root);
        anchorObject.transform.position = new Vector3(x, y, 0f);
        var anchor = anchorObject.AddComponent<SceneAnchor>();
        anchor.anchorId = id;
    }

    private static void CreateStaticSprite(Transform root, string name, Sprite sprite, Vector2 position, int sortingOrder, Vector2 colliderSize, Vector2 colliderOffset)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(root);
        obj.transform.position = new Vector3(position.x, position.y, 0f);
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
        renderer.sharedMaterial = LoadSpriteUnlitMaterial();

        if (colliderSize.sqrMagnitude <= 0f)
            return;

        var collider = obj.AddComponent<BoxCollider2D>();
        collider.size = colliderSize;
        collider.offset = colliderOffset;
    }

    private static Material LoadSpriteUnlitMaterial()
    {
        if (_spriteUnlitMaterial != null)
            return _spriteUnlitMaterial;

        _spriteUnlitMaterial = AssetDatabase.LoadAssetAtPath<Material>(SpriteUnlitMaterialPath);
        return _spriteUnlitMaterial;
    }

    private static Sprite EnsureSprite(string name, int width, int height, Action<Texture2D> draw, Vector2 pivot)
    {
        string path = $"{GeneratedSpriteFolder}/{name}.png";
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Clear(texture);
        draw(texture);
        texture.Apply();
        File.WriteAllBytes(ToAbsolutePath(path), texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 32f;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        importer.SetTextureSettings(settings);
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Tile EnsureTile(string name, Sprite sprite, Tile.ColliderType colliderType)
    {
        string path = $"{GeneratedTileFolder}/{name}.asset";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, path);
        }

        tile.sprite = sprite;
        tile.colliderType = colliderType;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static void EnsureSceneInBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path == ScenePath)
                return;
        }

        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static Component AddComponentByName(GameObject obj, string typeName)
    {
        Type type = FindType(typeName);
        if (type == null)
        {
            Debug.LogWarning($"[BuildCompleteFarmScene] Optional component type not found: {typeName}");
            return null;
        }

        var existing = obj.GetComponent(type);
        return existing != null ? existing : obj.AddComponent(type);
    }

    private static Type FindType(string fullName)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < assemblies.Length; i++)
        {
            Type type = assemblies[i].GetType(fullName);
            if (type != null)
                return type;
        }

        return null;
    }

    private static void SetObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(propertyName);
        if (property == null)
            return;

        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetSerializedProperty(UnityEngine.Object target, string propertyName, int value)
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(propertyName);
        if (property == null)
            return;

        property.intValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetSerializedProperty(UnityEngine.Object target, string propertyName, float value)
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(propertyName);
        if (property == null)
            return;

        property.floatValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void EnsureFolder(string folder)
    {
        folder = folder.Replace("\\", "/");
        if (AssetDatabase.IsValidFolder(folder))
            return;

        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static string ToAbsolutePath(string assetPath)
    {
        return Path.Combine(Directory.GetCurrentDirectory(), assetPath).Replace("\\", "/");
    }

    private static Transform FindChild(Transform root, string name)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            if (child.name == name)
                return child;
        }

        return null;
    }

    private static void SetTile(Tilemap map, int x, int y, TileBase tile)
    {
        if (map != null && tile != null)
            map.SetTile(new Vector3Int(x, y, 0), tile);
    }

    private static void PaintRect(Tilemap map, int xMin, int yMin, int xMax, int yMax, TileBase tile)
    {
        for (int x = xMin; x <= xMax; x++)
        {
            for (int y = yMin; y <= yMax; y++)
                SetTile(map, x, y, tile);
        }
    }

    private static void FillCollision(Tilemap collision, int xMin, int yMin, int xMax, int yMax)
    {
        var tile = AssetDatabase.LoadAssetAtPath<TileBase>($"{GeneratedTileFolder}/Tile_Farm_Collision.asset");
        PaintRect(collision, xMin, yMin, xMax, yMax, tile);
    }

    private static void PaintPath(Tilemap map, TileBase tile, Vector2Int from, Vector2Int to, int radius)
    {
        int steps = Mathf.Max(Mathf.Abs(to.x - from.x), Mathf.Abs(to.y - from.y));
        for (int i = 0; i <= steps; i++)
        {
            float t = steps == 0 ? 0f : i / (float)steps;
            int x = Mathf.RoundToInt(Mathf.Lerp(from.x, to.x, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(from.y, to.y, t));
            for (int ox = -radius; ox <= radius; ox++)
            {
                for (int oy = -radius; oy <= radius; oy++)
                    SetTile(map, x + ox, y + oy, tile);
            }
        }
    }

    private static void Clear(Texture2D texture)
    {
        var pixels = new Color32[texture.width * texture.height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(0, 0, 0, 0);
        texture.SetPixels32(pixels);
    }

    private static void DrawGrass(Texture2D t)
    {
        Fill(t, new Color32(73, 151, 49, 255));
        Noise(t, new Color32(88, 174, 58, 255), 6);
        Noise(t, new Color32(52, 123, 42, 255), 11);
    }

    private static void DrawPath(Texture2D t)
    {
        Fill(t, new Color32(183, 137, 73, 255));
        Noise(t, new Color32(216, 171, 97, 255), 5);
        Noise(t, new Color32(137, 93, 52, 255), 13);
    }

    private static void DrawWater(Texture2D t)
    {
        Fill(t, new Color32(31, 116, 163, 255));
        for (int y = 5; y < t.height; y += 10)
            FillRect(t, 2, y, t.width - 3, y + 1, new Color32(71, 171, 201, 255));
        Noise(t, new Color32(20, 91, 139, 255), 9);
    }

    private static void DrawSoil(Texture2D t)
    {
        Fill(t, new Color32(102, 63, 34, 255));
        for (int y = 6; y < t.height; y += 8)
            FillRect(t, 3, y, t.width - 4, y, new Color32(133, 82, 44, 255));
        Noise(t, new Color32(72, 43, 26, 255), 7);
    }

    private static void DrawWatered(Texture2D t)
    {
        Fill(t, new Color32(66, 73, 81, 180));
        Noise(t, new Color32(48, 94, 118, 180), 4);
    }

    private static void DrawFence(Texture2D t)
    {
        FillRect(t, 4, 14, 27, 19, new Color32(118, 73, 33, 255));
        FillRect(t, 7, 5, 11, 27, new Color32(151, 95, 43, 255));
        FillRect(t, 21, 5, 25, 27, new Color32(151, 95, 43, 255));
        FillRect(t, 6, 4, 12, 8, new Color32(92, 56, 25, 255));
        FillRect(t, 20, 4, 26, 8, new Color32(92, 56, 25, 255));
    }

    private static void DrawCollision(Texture2D t)
    {
        Fill(t, new Color32(255, 0, 0, 64));
    }

    private static void DrawCrop(Texture2D t)
    {
        FillRect(t, 12, 12, 19, 25, new Color32(48, 129, 42, 255));
        FillRect(t, 8, 16, 14, 20, new Color32(80, 173, 58, 255));
        FillRect(t, 17, 15, 24, 20, new Color32(80, 173, 58, 255));
    }

    private static void DrawFlower(Texture2D t)
    {
        FillRect(t, 15, 10, 16, 23, new Color32(50, 126, 45, 255));
        FillRect(t, 11, 19, 13, 21, new Color32(243, 218, 80, 255));
        FillRect(t, 18, 20, 20, 22, new Color32(239, 120, 125, 255));
    }

    private static void DrawHouse(Texture2D t)
    {
        FillRect(t, 28, 46, 164, 118, new Color32(137, 85, 42, 255));
        FillRect(t, 18, 116, 174, 150, new Color32(159, 68, 39, 255));
        FillRect(t, 42, 28, 76, 75, new Color32(82, 50, 30, 255));
        FillRect(t, 106, 70, 138, 98, new Color32(61, 133, 155, 255));
        FillRect(t, 16, 42, 176, 48, new Color32(82, 50, 30, 255));
        FillRect(t, 58, 124, 136, 132, new Color32(194, 91, 50, 255));
    }

    private static void DrawBarn(Texture2D t)
    {
        FillRect(t, 22, 30, 138, 98, new Color32(125, 65, 36, 255));
        FillRect(t, 14, 92, 146, 124, new Color32(165, 71, 38, 255));
        FillRect(t, 55, 24, 105, 70, new Color32(69, 45, 32, 255));
        FillRect(t, 32, 72, 55, 94, new Color32(226, 173, 77, 255));
    }

    private static void DrawTree(Texture2D t)
    {
        FillRect(t, 42, 8, 55, 55, new Color32(96, 58, 30, 255));
        FillCircle(t, 48, 78, 39, new Color32(38, 117, 49, 255));
        FillCircle(t, 29, 67, 27, new Color32(53, 147, 56, 255));
        FillCircle(t, 68, 67, 27, new Color32(53, 147, 56, 255));
        FillCircle(t, 48, 97, 24, new Color32(84, 171, 65, 255));
    }

    private static void DrawWell(Texture2D t)
    {
        FillRect(t, 15, 10, 49, 36, new Color32(120, 119, 104, 255));
        FillRect(t, 12, 34, 52, 43, new Color32(77, 65, 51, 255));
        FillRect(t, 20, 42, 44, 57, new Color32(151, 77, 39, 255));
    }

    private static void DrawDock(Texture2D t)
    {
        for (int x = 8; x <= 118; x += 18)
            FillRect(t, x, 12, x + 12, 53, new Color32(144, 91, 43, 255));
        FillRect(t, 4, 18, 124, 26, new Color32(102, 63, 34, 255));
        FillRect(t, 4, 40, 124, 48, new Color32(102, 63, 34, 255));
    }

    private static void DrawRock(Texture2D t)
    {
        FillCircle(t, 30, 22, 22, new Color32(115, 120, 106, 255));
        FillCircle(t, 42, 18, 14, new Color32(91, 96, 88, 255));
        FillRect(t, 14, 6, 50, 17, new Color32(83, 87, 80, 255));
    }

    private static void DrawSign(Texture2D t)
    {
        FillRect(t, 21, 5, 26, 34, new Color32(95, 58, 27, 255));
        FillRect(t, 8, 29, 40, 43, new Color32(153, 93, 41, 255));
        FillRect(t, 12, 33, 36, 35, new Color32(214, 161, 80, 255));
    }

    private static void DrawMarker(Texture2D t)
    {
        FillRect(t, 6, 6, 25, 25, new Color32(80, 220, 255, 180));
        FillRect(t, 11, 11, 20, 20, new Color32(255, 255, 255, 220));
    }

    private static void Fill(Texture2D t, Color32 color)
    {
        FillRect(t, 0, 0, t.width - 1, t.height - 1, color);
    }

    private static void FillRect(Texture2D t, int xMin, int yMin, int xMax, int yMax, Color32 color)
    {
        xMin = Mathf.Clamp(xMin, 0, t.width - 1);
        xMax = Mathf.Clamp(xMax, 0, t.width - 1);
        yMin = Mathf.Clamp(yMin, 0, t.height - 1);
        yMax = Mathf.Clamp(yMax, 0, t.height - 1);

        for (int x = xMin; x <= xMax; x++)
        {
            for (int y = yMin; y <= yMax; y++)
                t.SetPixel(x, y, color);
        }
    }

    private static void FillCircle(Texture2D t, int cx, int cy, int radius, Color32 color)
    {
        int sqr = radius * radius;
        for (int x = cx - radius; x <= cx + radius; x++)
        {
            for (int y = cy - radius; y <= cy + radius; y++)
            {
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= sqr && x >= 0 && y >= 0 && x < t.width && y < t.height)
                    t.SetPixel(x, y, color);
            }
        }
    }

    private static void Noise(Texture2D t, Color32 color, int divisor)
    {
        for (int x = 0; x < t.width; x++)
        {
            for (int y = 0; y < t.height; y++)
            {
                int hash = (x * 73856093) ^ (y * 19349663);
                if (Mathf.Abs(hash) % divisor == 0)
                    t.SetPixel(x, y, color);
            }
        }
    }

    private class FarmSprites
    {
        public Sprite Grass;
        public Sprite Path;
        public Sprite Water;
        public Sprite Soil;
        public Sprite Watered;
        public Sprite Fence;
        public Sprite Collision;
        public Sprite Crop;
        public Sprite Flower;
        public Sprite House;
        public Sprite Barn;
        public Sprite Tree;
        public Sprite Well;
        public Sprite Dock;
        public Sprite Rock;
        public Sprite Sign;
        public Sprite Marker;
    }

    private class GeneratedTiles
    {
        public Tile Grass;
        public Tile Path;
        public Tile Water;
        public Tile Soil;
        public Tile Watered;
        public Tile Fence;
        public Tile Collision;
        public Tile Crop;
        public Tile Flower;
    }

    private class SceneTiles
    {
        public TileBase Grass;
        public TileBase Ground;
        public TileBase Path;
        public TileBase Water;
        public TileBase Soil;
        public TileBase Watered;
        public TileBase Fence;
        public TileBase Collision;
        public TileBase Crop;
        public TileBase Flower;
    }

    private class SceneTilemaps
    {
        public Tilemap Ground;
        public Tilemap GroundDetail;
        public Tilemap Watered;
        public Tilemap Collision;
        public Tilemap Decoration;
        public Tilemap Overlay;
        public Tilemap RuntimeMarkers;
    }
}
