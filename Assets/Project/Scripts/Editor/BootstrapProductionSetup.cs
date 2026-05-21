using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.HeroEditor4D.Common.Scripts.Enums;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class BootstrapProductionSetup
{
    private const string SampleScenePath = "Assets/Project/Scenes/Main/SampleScene.unity";

    private const string PlayerPrefabPath = "Assets/Project/Prefabs/Characters/Player.prefab";
    private const string LegacyNpcPrefabPath = "Assets/Project/Prefabs/Characters/NPC.prefab";
    private const string PlantPrefabPath = "Assets/Project/Prefabs/WorldEntities/PlantPrefab.prefab";
    private const string DropPrefabPath = "Assets/Project/Prefabs/Items/EntityDrop.prefab";

    private const string NpcBasePrefabPath = "Assets/Project/Prefabs/Characters/NPC_Base.prefab";
    private const string NpcShopPrefabPath = "Assets/Project/Prefabs/Characters/NPC_Shop.prefab";
    private const string NpcCraftingPrefabPath = "Assets/Project/Prefabs/Characters/NPC_Crafting.prefab";
    private const string NpcQuestPrefabPath = "Assets/Project/Prefabs/Characters/NPC_Quest.prefab";
    private const string EnemyBasePrefabPath = "Assets/Project/Prefabs/Characters/Enemy_Base.prefab";
    private const string CropPlantPrefabPath = "Assets/Project/Prefabs/WorldEntities/CropPlant_Base.prefab";
    private const string OreNodePrefabPath = "Assets/Project/Prefabs/WorldEntities/OreNode_Base.prefab";
    private const string TreeNodePrefabPath = "Assets/Project/Prefabs/WorldEntities/TreeNode_Base.prefab";
    private const string RockNodePrefabPath = "Assets/Project/Prefabs/WorldEntities/RockNode_Base.prefab";
    private const string ForageNodePrefabPath = "Assets/Project/Prefabs/WorldEntities/ForageNode_Base.prefab";
    private const string PortalPrefabPath = "Assets/Project/Prefabs/WorldEntities/Portal_Base.prefab";
    private const string BedPrefabPath = "Assets/Project/Prefabs/WorldEntities/Bed_Base.prefab";
    private const string AnimalPrefabPath = "Assets/Project/Prefabs/Characters/Animal_Base.prefab";

    private const string StarterLoadoutPath = "Assets/Project/Resources/Data/StarterLoadouts/DefaultStarterLoadout.asset";
    private const string MasteryUnlockPath = "Assets/Project/Resources/Data/MasteryUnlockData.asset";
    private const string MarkerFolder = "Assets/Project/ScriptableObjects/SceneMarkers/MVP";
    private const string PlayerStartMarkerPath = MarkerFolder + "/Marker_Player_Start.asset";
    private const string GeneratedIconFolder = "Assets/Project/Generated/Icons";
    private const string CropPlantFolder = "Assets/Project/ScriptableObjects/WorldObjects/Plants/Placed";

    [MenuItem("Tools/DATN/Production/Bootstrap Production Setup")]
    public static void Execute()
    {
        BootstrapCoreplayM5Content.ExecuteAndStampSampleScene();
        EnsureFolders();
        EnsureProductionPrefabs();
        EnsureM3PrefabAndWorldObjectDefinitions();
        RepointWorldObjects();
        ConfigureDataContracts();
        EnsureM4WeaponData();
        EnsureM6AnimalData();
        EnsureStarterLoadout();
        EnsureGeneratedIcons();
        EnsurePlayerStartMarker();
        EnsureMasteryUnlockData();
        EnsureCropQualityModules();
        EnsureSprint5NarrativeData();
        EnsureSprint5ResearchData();
        StampPlayerStartMarker();
        ValidateSetup(logSuccess: true);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BootstrapProductionSetup] Production prefab/data/spawn/icon setup completed.");
    }

    public static void ExecuteBatch() => Execute();

    [MenuItem("Tools/DATN/Production/Sprint 4 Data Only")]
    public static void ExecuteSprint4DataOnly()
    {
        EnsureSprint4Data();
        EnsureCropQualityModules();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BootstrapProductionSetup] Sprint 4 data created/updated.");
    }

    [MenuItem("Tools/DATN/Production/Sprint 2 Quality Data Only")]
    public static void ExecuteSprint2QualityDataOnly()
    {
        EnsureCropQualityModules();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BootstrapProductionSetup] Sprint 2 quality modules created/updated.");
    }

    [MenuItem("Tools/DATN/Production/Sprint 5 Narrative Data Only")]
    public static void ExecuteSprint5NarrativeDataOnly()
    {
        EnsureSprint5NarrativeData();
        EnsureSprint5ResearchData();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BootstrapProductionSetup] Sprint 5 narrative/research data created/updated.");
    }

    [MenuItem("Tools/DATN/Production/Sprint 5 Research Data Only")]
    public static void ExecuteSprint5ResearchDataOnly()
    {
        EnsureSprint5ResearchData();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BootstrapProductionSetup] Sprint 5 research data created/updated.");
    }

    [MenuItem("Tools/DATN/Production/Vertical Slice VS-1 Crop Data Only")]
    public static void ExecuteVerticalSliceCropDataOnly()
    {
        EnsureVerticalSliceCropData();
        EnsureCropQualityModules();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BootstrapProductionSetup] VS-1 crop/seed data created/updated.");
    }

    [MenuItem("Tools/DATN/Production/Validate Production Setup")]
    public static void ValidateMenu() => ValidateSetup(logSuccess: true);

    private static void EnsureProductionPrefabs()
    {
        EnsureTag("Player");
        EnsureTag("NPC");
        EnsureTag("Plant");
        EnsureTag("Enemy");

        ConfigurePrefab(PlayerPrefabPath, WorldEntityPrefabRoleType.Player, "Player", "Player", Color.white,
            removeNameplate: true,
            addEnemyObject: false,
            removeInteraction: false,
            removeStageObject: false);
        ConfigurePrefab(LegacyNpcPrefabPath, WorldEntityPrefabRoleType.Npc, "NPC", "NPC", new Color(0.55f, 0.75f, 1f, 1f),
            removeNameplate: false, addEnemyObject: false, removeInteraction: false, removeStageObject: false);

        CopyAssetIfMissing(LegacyNpcPrefabPath, NpcBasePrefabPath);
        CopyAssetIfMissing(LegacyNpcPrefabPath, NpcShopPrefabPath);
        CopyAssetIfMissing(LegacyNpcPrefabPath, NpcCraftingPrefabPath);
        CopyAssetIfMissing(LegacyNpcPrefabPath, NpcQuestPrefabPath);
        CopyAssetIfMissing(LegacyNpcPrefabPath, EnemyBasePrefabPath);
        CopyAssetIfMissing(PlantPrefabPath, CropPlantPrefabPath);
        CopyAssetIfMissing(PlantPrefabPath, OreNodePrefabPath);
        CopyAssetIfMissing(OreNodePrefabPath, TreeNodePrefabPath);
        CopyAssetIfMissing(OreNodePrefabPath, RockNodePrefabPath);
        CopyAssetIfMissing(CropPlantPrefabPath, ForageNodePrefabPath);
        CopyAssetIfMissing(LegacyNpcPrefabPath, PortalPrefabPath);
        CopyAssetIfMissing(LegacyNpcPrefabPath, BedPrefabPath);
        CopyAssetIfMissing(LegacyNpcPrefabPath, AnimalPrefabPath);

        ConfigurePrefab(NpcBasePrefabPath, WorldEntityPrefabRoleType.Npc, "NPC", "NPC", new Color(0.55f, 0.75f, 1f, 1f),
            removeNameplate: false, addEnemyObject: false, removeInteraction: false, removeStageObject: false);
        ConfigurePrefab(NpcShopPrefabPath, WorldEntityPrefabRoleType.Npc, "NPC", "NPC", new Color(0.5f, 1f, 0.55f, 1f),
            removeNameplate: false, addEnemyObject: false, removeInteraction: false, removeStageObject: false);
        ConfigurePrefab(NpcCraftingPrefabPath, WorldEntityPrefabRoleType.Npc, "NPC", "NPC", new Color(0.45f, 0.95f, 1f, 1f),
            removeNameplate: false, addEnemyObject: false, removeInteraction: false, removeStageObject: false);
        ConfigurePrefab(NpcQuestPrefabPath, WorldEntityPrefabRoleType.Npc, "NPC", "NPC", new Color(1f, 0.9f, 0.35f, 1f),
            removeNameplate: false, addEnemyObject: false, removeInteraction: false, removeStageObject: false);
        ConfigurePrefab(EnemyBasePrefabPath, WorldEntityPrefabRoleType.Enemy, "Enemy", "Enemy", new Color(1f, 0.35f, 0.35f, 1f),
            removeNameplate: false, addEnemyObject: true, removeInteraction: true, removeStageObject: false);
        ConfigurePrefab(CropPlantPrefabPath, WorldEntityPrefabRoleType.Crop, "Plant", "Interactable", Color.white,
            removeNameplate: true, addEnemyObject: false, removeInteraction: false, removeStageObject: false);
        ConfigurePrefab(OreNodePrefabPath, WorldEntityPrefabRoleType.Resource, "Plant", "Interactable", new Color(0.7f, 0.75f, 0.85f, 1f),
            removeNameplate: true, addEnemyObject: false, removeInteraction: false, removeStageObject: true);
        ConfigurePrefab(TreeNodePrefabPath, WorldEntityPrefabRoleType.Resource, "Plant", "Interactable", new Color(0.4f, 0.72f, 0.42f, 1f),
            removeNameplate: true, addEnemyObject: false, removeInteraction: false, removeStageObject: true);
        ConfigurePrefab(RockNodePrefabPath, WorldEntityPrefabRoleType.Resource, "Plant", "Interactable", new Color(0.55f, 0.57f, 0.62f, 1f),
            removeNameplate: true, addEnemyObject: false, removeInteraction: false, removeStageObject: true);
        ConfigurePrefab(ForageNodePrefabPath, WorldEntityPrefabRoleType.Resource, "Plant", "Interactable", new Color(0.58f, 0.86f, 0.52f, 1f),
            removeNameplate: true, addEnemyObject: false, removeInteraction: false, removeStageObject: true);
        ConfigurePrefab(PortalPrefabPath, WorldEntityPrefabRoleType.Resource, "NPC", "Interactable", new Color(0.45f, 0.8f, 1f, 1f),
            removeNameplate: false, addEnemyObject: false, removeInteraction: false, removeStageObject: true);
        ConfigurePrefab(BedPrefabPath, WorldEntityPrefabRoleType.Resource, "NPC", "Interactable", new Color(0.95f, 0.85f, 0.6f, 1f),
            removeNameplate: false, addEnemyObject: false, removeInteraction: false, removeStageObject: true);
        ConfigurePrefab(AnimalPrefabPath, WorldEntityPrefabRoleType.Npc, "NPC", "NPC", new Color(1f, 0.92f, 0.75f, 1f),
            removeNameplate: false, addEnemyObject: false, removeInteraction: false, removeStageObject: true);
        EnsureAnimalPrefabBridge();
        ConfigurePrefab(DropPrefabPath, WorldEntityPrefabRoleType.Drop, "Untagged", "Default", Color.white,
            removeNameplate: true, addEnemyObject: false, removeInteraction: false, removeStageObject: false);
    }

    private static void EnsureM3PrefabAndWorldObjectDefinitions()
    {
        EnsureWorldObjectDefinition("Assets/Project/Resources/Data/WorldObjects/TreeNode01.asset", ObjectType.TreeNode01, TreeNodePrefabPath);
        EnsureWorldObjectDefinition("Assets/Project/Resources/Data/WorldObjects/RockNode01.asset", ObjectType.RockNode01, RockNodePrefabPath);
        EnsureWorldObjectDefinition("Assets/Project/Resources/Data/WorldObjects/ForageNode01.asset", ObjectType.ForageNode01, ForageNodePrefabPath);
        EnsureWorldObjectDefinition("Assets/Project/Resources/Data/WorldObjects/Portal01.asset", ObjectType.Portal01, PortalPrefabPath);
        EnsureWorldObjectDefinition("Assets/Project/Resources/Data/WorldObjects/Bed01.asset", ObjectType.Bed01, BedPrefabPath);
        EnsureWorldObjectDefinition("Assets/Project/Resources/Data/WorldObjects/Animal01.asset", ObjectType.Animal01, AnimalPrefabPath);
        EnsureWorldObjectDefinition("Assets/Project/Resources/Data/WorldObjects/Sprinkler01.asset", ObjectType.Sprinkler01, CropPlantPrefabPath);
        EnsureWorldObjectDefinition("Assets/Project/Resources/Data/WorldObjects/Sprinkler02.asset", ObjectType.Sprinkler02, CropPlantPrefabPath);
    }

    private static void ConfigurePrefab(
        string prefabPath,
        WorldEntityPrefabRoleType role,
        string tag,
        string layerName,
        Color spriteColor,
        bool removeNameplate,
        bool addEnemyObject,
        bool removeInteraction,
        bool removeStageObject)
    {
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null)
        {
            Debug.LogWarning($"[BootstrapProductionSetup] Missing prefab: {prefabPath}");
            return;
        }

        try
        {
            root.name = Path.GetFileNameWithoutExtension(prefabPath);
            root.SetActive(false);
            SetTagAndLayer(root, tag, layerName);
            RemovePlayerOnlyComponents(root);

            if (role == WorldEntityPrefabRoleType.Player)
            {
                EnsureComponent<PlayerControler>(root);
                EnsureComponent<PlayerInventory>(root);
                EnsureComponent<PlayerEquipment>(root);
                EnsureComponent<PlayerBridge>(root);
                EnsureComponent<ToolActionBridge>(root);
            }

            if (role != WorldEntityPrefabRoleType.Player)
            {
                RemoveComponent<PlayerControler>(root);
                RemoveComponent<PlayerInventory>(root);
                RemoveComponent<PlayerEquipment>(root);
                RemoveComponent<PlayerBridge>(root);
                RemoveComponent<ToolActionBridge>(root);
                RemoveComponent<TileCursorHighlight>(root);
            }

            if (removeInteraction)
            {
                RemoveComponent<InteractablePrompt>(root);
                foreach (var circle in root.GetComponents<CircleCollider2D>())
                    UnityEngine.Object.DestroyImmediate(circle);
            }

            if (removeStageObject)
                RemoveComponent<StageObject>(root);

            if (removeNameplate)
                RemoveComponent<WorldEntityNameplate>(root);
            else
                EnsureComponent<WorldEntityNameplate>(root);

            if (addEnemyObject)
                EnsureComponent<EnemyObject>(root);
            else
                RemoveComponent<EnemyObject>(root);

            EnsureComponent<EntityRoot>(root);
            EnsureRole(root, role);
            ConfigureRigidbody(root, role);
            TintSpriteRenderer(root, spriteColor);

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void RemovePlayerOnlyComponents(GameObject root)
    {
        if (root == null) return;
        if (root.GetComponent<WorldEntityPrefabRole>()?.role == WorldEntityPrefabRoleType.Player) return;

        RemoveComponent<PlayerControler>(root);
        RemoveComponent<PlayerInventory>(root);
        RemoveComponent<PlayerEquipment>(root);
        RemoveComponent<PlayerBridge>(root);
        RemoveComponent<ToolActionBridge>(root);
        RemoveComponent<TileCursorHighlight>(root);
    }

    private static void RepointWorldObjects()
    {
        SetWorldObjectPrefab("Assets/Project/Resources/Data/WorldObjects/Player.asset", PlayerPrefabPath);
        SetWorldObjectPrefab("Assets/Project/Resources/Data/WorldObjects/NPC01.asset", NpcBasePrefabPath);
        SetWorldObjectPrefab("Assets/Project/Resources/Data/WorldObjects/NPCShop01.asset", NpcShopPrefabPath);
        SetWorldObjectPrefab("Assets/Project/Resources/Data/WorldObjects/NPCCrafting01.asset", NpcCraftingPrefabPath);
        SetWorldObjectPrefab("Assets/Project/Resources/Data/WorldObjects/NPCEvent01.asset", NpcQuestPrefabPath);
        SetWorldObjectPrefab("Assets/Project/Resources/Data/WorldObjects/Enemy01.asset", EnemyBasePrefabPath);
        SetWorldObjectPrefab("Assets/Project/Resources/Data/WorldObjects/OreNode01.asset", OreNodePrefabPath);
        SetWorldObjectPrefab("Assets/Project/Resources/Data/WorldObjects/Plant01.asset", CropPlantPrefabPath);
        SetWorldObjectPrefab("Assets/Project/Resources/Data/WorldObjects/Plant02.asset", CropPlantPrefabPath);
        SetWorldObjectPrefab("Assets/Project/Resources/Data/WorldObjects/EntityDrop.asset", DropPrefabPath);
        SetWorldObjectPrefab("Assets/Project/Resources/Data/WorldObjects/TreeNode01.asset", TreeNodePrefabPath);
        SetWorldObjectPrefab("Assets/Project/Resources/Data/WorldObjects/RockNode01.asset", RockNodePrefabPath);
        SetWorldObjectPrefab("Assets/Project/Resources/Data/WorldObjects/ForageNode01.asset", ForageNodePrefabPath);
        SetWorldObjectPrefab("Assets/Project/Resources/Data/WorldObjects/Portal01.asset", PortalPrefabPath);
        SetWorldObjectPrefab("Assets/Project/Resources/Data/WorldObjects/Bed01.asset", BedPrefabPath);
        SetWorldObjectPrefab("Assets/Project/Resources/Data/WorldObjects/Animal01.asset", AnimalPrefabPath);
        SetWorldObjectPrefab("Assets/Project/Resources/Data/WorldObjects/Sprinkler01.asset", CropPlantPrefabPath);
        SetWorldObjectPrefab("Assets/Project/Resources/Data/WorldObjects/Sprinkler02.asset", CropPlantPrefabPath);
    }

    private static void ConfigureDataContracts()
    {
        ConfigurePlayerData();
        ConfigureNpcData("Assets/Project/ScriptableObjects/Characters/NPCs/NPC_Banhang.asset", requireShop: true, requireCrafting: false, requireQuest: false);
        ConfigureNpcData("Assets/Project/ScriptableObjects/Characters/NPCs/NPC_Chetao.asset", requireShop: false, requireCrafting: true, requireQuest: false);
        ConfigureNpcData("Assets/Project/ScriptableObjects/Characters/NPCs/NPC_Sukien.asset", requireShop: false, requireCrafting: false, requireQuest: true);
        ConfigureCropPlacementData();
        ConfigureEnemyData();
        ConfigureOreData();
        ConfigureM3ResourceData();
    }

    private static void ConfigureM3ResourceData()
    {
        var wood = EnsureMaterialItem("Assets/Project/ScriptableObjects/Items/Materials/Wood_01.asset", "mat_wood_01", "m3.mat.wood.name", "m3.mat.wood.desc", 8);
        var sap = EnsureMaterialItem("Assets/Project/ScriptableObjects/Items/Materials/Sap_01.asset", "mat_sap_01", "m3.mat.sap.name", "m3.mat.sap.desc", 12);
        var stone = EnsureMaterialItem("Assets/Project/ScriptableObjects/Items/Materials/Stone_01.asset", "mat_stone_01", "m3.mat.stone.name", "m3.mat.stone.desc", 10);
        var coal = EnsureMaterialItem("Assets/Project/ScriptableObjects/Items/Materials/Coal_01.asset", "mat_coal_01", "m3.mat.coal.name", "m3.mat.coal.desc", 18);
        var fiber = EnsureMaterialItem("Assets/Project/ScriptableObjects/Items/Materials/Fiber_01.asset", "mat_fiber_01", "m3.mat.fiber.name", "m3.mat.fiber.desc", 7);
        var forage = EnsureMaterialItem("Assets/Project/ScriptableObjects/Items/Materials/Forage_01.asset", "mat_forage_01", "m3.mat.forage.name", "m3.mat.forage.desc", 14);

        ConfigureResourceEntity(
            assetPath: "Assets/Project/ScriptableObjects/WorldObjects/Resources/TreeNode_01.asset",
            id: "tree_node_01",
            keyName: "m3.tree.name",
            descKey: "m3.tree.desc",
            hp: 8f,
            requiredTool: ToolType.Axe,
            drops: new[] { Drop(wood, 1, 2, 1f), Drop(sap, 1, 1, 0.35f) },
            rewardExp: 12);

        ConfigureResourceEntity(
            assetPath: "Assets/Project/ScriptableObjects/WorldObjects/Resources/RockNode_01.asset",
            id: "rock_node_01",
            keyName: "m3.rock.name",
            descKey: "m3.rock.desc",
            hp: 10f,
            requiredTool: ToolType.Pickaxe,
            drops: new[] { Drop(stone, 1, 2, 1f), Drop(coal, 1, 1, 0.25f) },
            rewardExp: 14);

        ConfigureResourceEntity(
            assetPath: "Assets/Project/ScriptableObjects/WorldObjects/Resources/ForageNode_01.asset",
            id: "forage_node_01",
            keyName: "m3.forage.name",
            descKey: "m3.forage.desc",
            hp: 5f,
            requiredTool: ToolType.Scythe,
            drops: new[] { Drop(fiber, 1, 2, 1f), Drop(forage, 1, 1, 0.4f) },
            rewardExp: 10);

        ConfigurePortalEntity();
        ConfigureBedEntity();
        ConfigureAnimalPlaceholderEntity();
    }

    private static EntityData EnsureMaterialItem(string assetPath, string id, string keyName, string descKey, int sellPrice)
    {
        var item = LoadOrCreateEntity(assetPath);
        item.id = id;
        item.keyName = keyName;
        item.descKey = descKey;
        item.category = ItemCategory.Material;
        item.maxStack = Mathf.Max(1, item.maxStack > 0 ? item.maxStack : 99);
        item.buyPrice = -1;
        item.sellPrice = sellPrice;
        item.modules ??= new List<IModuleData>();
        item.modules.Clear();
        item.baseStats = new StatsData { baseStats = new List<StatEntry>() };
        EditorUtility.SetDirty(item);
        return item;
    }

    private static void ConfigureResourceEntity(
        string assetPath,
        string id,
        string keyName,
        string descKey,
        float hp,
        ToolType requiredTool,
        DropEntry[] drops,
        int rewardExp)
    {
        var resource = LoadOrCreateEntity(assetPath);
        resource.id = id;
        resource.keyName = keyName;
        resource.descKey = descKey;
        resource.category = ItemCategory.Placeable;
        resource.maxStack = 1;
        resource.buyPrice = -1;
        resource.sellPrice = 0;
        resource.placementRule = new PlacementRule
        {
            occupyLayer = EntityLayer.Furniture,
            requireTags = PlacementTag.None,
            provideTags = PlacementTag.None,
            blockLayers = Array.Empty<EntityLayer>()
        };

        resource.modules = resource.modules ?? new List<IModuleData>();
        resource.modules.Clear();
        resource.modules.Add(new HealthModule { canTakeDamage = true });
        resource.modules.Add(new HarvestModule { harvestTool = requiredTool, wrongToolPenalty = 0f });
        resource.modules.Add(new DropModule { harvestDrops = drops });
        resource.modules.Add(new ExpRewardModule { rewardExp = rewardExp, sourceType = ExpSourceType.Harvest, requireKiller = false });
        resource.modules.Add(new MortalModule());

        SetOrAddStat(resource, StatType.MaxHp, hp);
        SetOrAddStat(resource, StatType.Hp, hp);
        SetOrAddStat(resource, StatType.Defense, 0f);
        EditorUtility.SetDirty(resource);
    }

    private static void ConfigurePortalEntity()
    {
        var portal = LoadOrCreateEntity("Assets/Project/ScriptableObjects/WorldObjects/Utility/Portal_01.asset");
        portal.id = "portal_01";
        portal.keyName = "m3.portal.name";
        portal.descKey = "m3.portal.desc";
        portal.category = ItemCategory.Placeable;
        portal.maxStack = 1;
        portal.buyPrice = -1;
        portal.sellPrice = 0;
        portal.modules = portal.modules ?? new List<IModuleData>();
        portal.modules.Clear();
        portal.modules.Add(new ScenePortalModule
        {
            optionTextKey = "ui.scene.enter",
            priority = 40,
            targetSceneName = "TownScene",
            targetSpawnPointId = "town_entry",
            saveBeforeTransition = true
        });
        portal.modules.Add(new DialogueModule
        {
            graph = null,
            optionTextKey = "ui.scene.enter",
            priority = 80
        });
        EditorUtility.SetDirty(portal);
    }

    private static void ConfigureBedEntity()
    {
        var bed = LoadOrCreateEntity("Assets/Project/ScriptableObjects/WorldObjects/Utility/Bed_01.asset");
        bed.id = "bed_01";
        bed.keyName = "m3.bed.name";
        bed.descKey = "m3.bed.desc";
        bed.category = ItemCategory.Placeable;
        bed.maxStack = 1;
        bed.buyPrice = -1;
        bed.sellPrice = 0;
        bed.modules = bed.modules ?? new List<IModuleData>();
        bed.modules.Clear();
        bed.modules.Add(new DialogueModule
        {
            graph = null,
            optionTextKey = "ui.common.use",
            priority = 20
        });
        EditorUtility.SetDirty(bed);
    }

    private static void ConfigureAnimalPlaceholderEntity()
    {
        var animal = LoadOrCreateEntity("Assets/Project/ScriptableObjects/WorldObjects/Animals/Animal_Chicken_01.asset");
        var feed = AssetDatabase.LoadAssetAtPath<EntityData>("Assets/Project/ScriptableObjects/Items/Animal/AnimalFeed.asset");
        var egg = AssetDatabase.LoadAssetAtPath<EntityData>("Assets/Project/ScriptableObjects/Items/Animal/Egg.asset");
        animal.id = "animal_chicken_01";
        animal.keyName = "m3.animal.chicken.name";
        animal.descKey = "m3.animal.chicken.desc";
        animal.category = ItemCategory.Placeable;
        animal.maxStack = 1;
        animal.buyPrice = -1;
        animal.sellPrice = 0;
        animal.modules = animal.modules ?? new List<IModuleData>();
        animal.modules.Clear();
        animal.modules.Add(new AnimalModule
        {
            speciesKey = "m3.animal.chicken.name",
            feedItem = feed,
            productItem = egg,
            productAmount = 1,
            priority = 25
        });
        EditorUtility.SetDirty(animal);
    }

    private static void EnsureM6AnimalData()
    {
        EnsureSimpleItem(
            "Assets/Project/ScriptableObjects/Items/Animal/AnimalFeed.asset",
            "animal_feed",
            "m6.item.animal_feed.name",
            "m6.item.animal_feed.desc",
            ItemCategory.Consumable,
            maxStack: 99,
            buyPrice: 20,
            sellPrice: 8);

        EnsureSimpleItem(
            "Assets/Project/ScriptableObjects/Items/Animal/Egg.asset",
            "egg",
            "m6.item.egg.name",
            "m6.item.egg.desc",
            ItemCategory.AnimalProduct,
            maxStack: 99,
            buyPrice: -1,
            sellPrice: 45);

        ConfigureAnimalPlaceholderEntity();
        EnsureAnimalFeedRecipe();
    }

    private static EntityData EnsureSimpleItem(
        string path,
        string id,
        string keyName,
        string descKey,
        ItemCategory category,
        int maxStack,
        int buyPrice,
        int sellPrice)
    {
        var item = LoadOrCreateEntity(path);
        item.id = id;
        item.keyName = keyName;
        item.descKey = descKey;
        item.category = category;
        item.maxStack = Mathf.Max(1, maxStack);
        item.buyPrice = buyPrice;
        item.sellPrice = sellPrice;
        item.modules = new List<IModuleData>();
        item.baseStats = new StatsData { baseStats = new List<StatEntry>() };
        EditorUtility.SetDirty(item);
        return item;
    }

    private static void EnsureAnimalFeedRecipe()
    {
        var feed = AssetDatabase.LoadAssetAtPath<EntityData>("Assets/Project/ScriptableObjects/Items/Animal/AnimalFeed.asset");
        var fiber = AssetDatabase.LoadAssetAtPath<EntityData>("Assets/Project/ScriptableObjects/Items/Materials/Fiber_01.asset");
        var turnip = AssetDatabase.LoadAssetAtPath<EntityData>("Assets/Project/ScriptableObjects/Items/Crops/Crop_T1_Turnip.asset");
        if (feed == null || fiber == null || turnip == null) return;

        string recipePath = "Assets/Project/ScriptableObjects/Graph/recipes/mvp/Recipe_AnimalFeed.asset";
        var recipe = AssetDatabase.LoadAssetAtPath<RecipeData>(recipePath);
        if (recipe == null)
        {
            EnsureFolder(Path.GetDirectoryName(recipePath)?.Replace('\\', '/'));
            recipe = ScriptableObject.CreateInstance<RecipeData>();
            recipe.name = Path.GetFileNameWithoutExtension(recipePath);
            AssetDatabase.CreateAsset(recipe, recipePath);
        }

        recipe.id = "recipe_animal_feed";
        recipe.titleKey = "m6.recipe.animal_feed.name";
        recipe.requiredLevel = 1;
        recipe.craftExp = 10;
        recipe.ingredients = new List<RecipeIngredient>
        {
            new RecipeIngredient { item = fiber, amount = 2 },
            new RecipeIngredient { item = turnip, amount = 1 }
        };
        recipe.outputs = new List<RecipeIngredient>
        {
            new RecipeIngredient { item = feed, amount = 2 }
        };
        EditorUtility.SetDirty(recipe);

        var npc = AssetDatabase.LoadAssetAtPath<EntityData>("Assets/Project/ScriptableObjects/Characters/NPCs/NPC_Chetao.asset");
        var crafting = npc?.modules?.OfType<CraftingModule>().FirstOrDefault();
        if (crafting != null && !crafting.recipes.Contains(recipe))
        {
            crafting.recipes.Add(recipe);
            EditorUtility.SetDirty(npc);
        }
    }

    private static void EnsureSprint5NarrativeData()
    {
        const string narrativeFolder = "Assets/Project/Resources/Data/Narrative";
        EnsureFolder(narrativeFolder);

        LoadOrCreateStoryEvent($"{narrativeFolder}/Story_Day01_Welcome.asset", "story_day01_welcome", "s5.story.day01.title", "s5.story.day01.body", 1, StoryEventChannel.Message, true);
        LoadOrCreateStoryEvent($"{narrativeFolder}/Story_Day03_FarmRoutine.asset", "story_day03_farm_routine", "s5.story.day03.title", "s5.story.day03.body", 3, StoryEventChannel.Diary, true);
        LoadOrCreateStoryEvent($"{narrativeFolder}/Story_Day07_News_MutantRumor.asset", "story_day07_news_mutant_rumor", "s5.story.day07.title", "s5.story.day07.body", 7, StoryEventChannel.News, true);
        LoadOrCreateStoryEvent($"{narrativeFolder}/Story_Day10_MutantThreat.asset", "story_day10_mutant_threat", "s5.story.day10.title", "s5.story.day10.body", 10, StoryEventChannel.Message, true);
        LoadOrCreateStoryEvent($"{narrativeFolder}/Story_Day14_ClearFarmEdge.asset", "story_day14_clear_farm_edge", "s5.story.day14.title", "s5.story.day14.body", 14, StoryEventChannel.Diary, true);
        LoadOrCreateStoryEvent($"{narrativeFolder}/Story_Day21_BuildingPlan.asset", "story_day21_building_plan", "s5.story.day21.title", "s5.story.day21.body", 21, StoryEventChannel.Message, true);
    }

    private static StoryEventData LoadOrCreateStoryEvent(string path, string id, string titleKey, string bodyKey, int triggerDay, StoryEventChannel channel, bool showNotification)
    {
        var story = AssetDatabase.LoadAssetAtPath<StoryEventData>(path);
        if (story == null)
        {
            story = ScriptableObject.CreateInstance<StoryEventData>();
            story.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(story, path);
        }

        story.id = id;
        story.titleKey = titleKey;
        story.bodyKey = bodyKey;
        story.triggerDay = triggerDay;
        story.channel = channel;
        story.showNotification = showNotification;
        EditorUtility.SetDirty(story);
        return story;
    }

    private static void EnsureSprint5ResearchData()
    {
        const string researchFolder = "Assets/Project/Resources/Data/Research";
        EnsureFolder(researchFolder);

        LoadOrCreateResearch($"{researchFolder}/Research_Day03_WateringRoutine.asset", "research_day03_watering_routine", "s5.research.day03.title", "s5.research.day03.body", 3, "", "watering_routine");
        LoadOrCreateResearch($"{researchFolder}/Research_Day07_MiningSurvey.asset", "research_day07_mining_survey", "s5.research.day07.title", "s5.research.day07.body", 7, "", "mining_survey");
        LoadOrCreateResearch($"{researchFolder}/Research_Day10_MutantNotes.asset", "research_day10_mutant_notes", "s5.research.day10.title", "s5.research.day10.body", 10, "", "mutant_notes");
        LoadOrCreateResearch($"{researchFolder}/Research_Day14_FieldExpansion.asset", "research_day14_field_expansion", "s5.research.day14.title", "s5.research.day14.body", 14, "", "field_expansion");
        LoadOrCreateResearch($"{researchFolder}/Research_Day21_BuildingMethods.asset", "research_day21_building_methods", "s5.research.day21.title", "s5.research.day21.body", 21, "", "building_methods");
    }

    private static ResearchData LoadOrCreateResearch(string path, string id, string titleKey, string descriptionKey, int unlockDay, string unlockedRecipeId, string rewardId)
    {
        var research = AssetDatabase.LoadAssetAtPath<ResearchData>(path);
        if (research == null)
        {
            research = ScriptableObject.CreateInstance<ResearchData>();
            research.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(research, path);
        }

        research.id = id;
        research.titleKey = titleKey;
        research.descriptionKey = descriptionKey;
        research.unlockDay = unlockDay;
        research.unlockedRecipeId = unlockedRecipeId;
        research.rewardId = rewardId;
        EditorUtility.SetDirty(research);
        return research;
    }

    private static void EnsureCropQualityModules()
    {
        var guids = AssetDatabase.FindAssets("t:EntityData", new[] { "Assets/Project/ScriptableObjects/WorldObjects/Plants" });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var data = AssetDatabase.LoadAssetAtPath<EntityData>(path);
            if (data?.modules == null) continue;

            bool isCrop = data.modules.OfType<StageModule>().Any()
                       && data.modules.OfType<HarvestModule>().Any(module => module.harvestTool == ToolType.None);
            if (!isCrop) continue;

            var quality = EnsureModule<QualityModule>(data);
            quality.minQuality = Mathf.Max(1, quality.minQuality);
            quality.maxQuality = Mathf.Max(quality.minQuality, quality.maxQuality);
            quality.soilQualityPerStar = Mathf.Max(1, quality.soilQualityPerStar);
            EditorUtility.SetDirty(data);
        }
    }

    private static void EnsureVerticalSliceCropData()
    {
        var crops = new[] {
            new { tier = 1, suffix = "tomato", keyName = "vs.crop.tomato.name", descKey = "vs.crop.tomato.desc", growthDays = 4, sellPrice = 35, regrow = 0 },
            new { tier = 1, suffix = "potato", keyName = "vs.crop.potato.name", descKey = "vs.crop.potato.desc", growthDays = 5, sellPrice = 40, regrow = 0 },
            new { tier = 1, suffix = "cucumber", keyName = "vs.crop.cucumber.name", descKey = "vs.crop.cucumber.desc", growthDays = 6, sellPrice = 50, regrow = 2 },
            new { tier = 2, suffix = "corn", keyName = "vs.crop.corn.name", descKey = "vs.crop.corn.desc", growthDays = 8, sellPrice = 80, regrow = 0 },
            new { tier = 2, suffix = "watermelon", keyName = "vs.crop.watermelon.name", descKey = "vs.crop.watermelon.desc", growthDays = 10, sellPrice = 150, regrow = 0 },
            new { tier = 2, suffix = "pepper", keyName = "vs.crop.pepper.name", descKey = "vs.crop.pepper.desc", growthDays = 7, sellPrice = 70, regrow = 3 },
            new { tier = 3, suffix = "pumpkin", keyName = "vs.crop.pumpkin.name", descKey = "vs.crop.pumpkin.desc", growthDays = 12, sellPrice = 200, regrow = 0 },
            new { tier = 3, suffix = "grape", keyName = "vs.crop.grape.name", descKey = "vs.crop.grape.desc", growthDays = 10, sellPrice = 180, regrow = 3 },
            new { tier = 3, suffix = "coffee", keyName = "vs.crop.coffee.name", descKey = "vs.crop.coffee.desc", growthDays = 14, sellPrice = 250, regrow = 4 },
            new { tier = 4, suffix = "ginseng", keyName = "vs.crop.ginseng.name", descKey = "vs.crop.ginseng.desc", growthDays = 18, sellPrice = 500, regrow = 0 },
            new { tier = 4, suffix = "orchid", keyName = "vs.crop.orchid.name", descKey = "vs.crop.orchid.desc", growthDays = 16, sellPrice = 400, regrow = 0 },
            new { tier = 5, suffix = "forbidden_fruit", keyName = "vs.crop.forbidden_fruit.name", descKey = "vs.crop.forbidden_fruit.desc", growthDays = 22, sellPrice = 1000, regrow = 0 },
            new { tier = 5, suffix = "golden_mushroom", keyName = "vs.crop.golden_mushroom.name", descKey = "vs.crop.golden_mushroom.desc", growthDays = 20, sellPrice = 800, regrow = 5 }
        };

        foreach (var spec in crops)
        {
            string pascalName = ToPascalCase(spec.suffix);
            string cropPath = $"Assets/Project/ScriptableObjects/Items/Crops/VerticalSlice/Crop_T{spec.tier}_{pascalName}.asset";
            string seedPath = $"Assets/Project/ScriptableObjects/WorldObjects/Plants/VerticalSlice/Seed_T{spec.tier}_{pascalName}.asset";
            string plantPath = $"Assets/Project/ScriptableObjects/WorldObjects/Plants/Placed/VerticalSlice/CropPlant_T{spec.tier}_{pascalName}.asset";

            EnsureFolder(Path.GetDirectoryName(cropPath)?.Replace('\\', '/'));
            EnsureFolder(Path.GetDirectoryName(seedPath)?.Replace('\\', '/'));
            EnsureFolder(Path.GetDirectoryName(plantPath)?.Replace('\\', '/'));

            var cropItem = LoadOrCreateEntity(cropPath);
            cropItem.id = $"vs_crop_t{spec.tier}_{spec.suffix}";
            cropItem.keyName = spec.keyName;
            cropItem.descKey = spec.descKey;
            cropItem.category = ItemCategory.Crop;
            cropItem.maxStack = 999;
            cropItem.buyPrice = -1;
            cropItem.sellPrice = spec.sellPrice;
            cropItem.modules = new List<IModuleData>();
            cropItem.baseStats = new StatsData { baseStats = new List<StatEntry>() };
            EditorUtility.SetDirty(cropItem);

            var plantEntity = LoadOrCreateEntity(plantPath);
            plantEntity.id = $"vs_crop_plant_t{spec.tier}_{spec.suffix}";
            plantEntity.keyName = spec.keyName;
            plantEntity.descKey = spec.descKey;
            plantEntity.category = ItemCategory.Placeable;
            plantEntity.maxStack = 1;
            plantEntity.buyPrice = -1;
            plantEntity.sellPrice = 0;
            plantEntity.placementRule = new PlacementRule { occupyLayer = EntityLayer.Plant, requireTags = PlacementTag.Plantable, provideTags = PlacementTag.None, blockLayers = new[] { EntityLayer.Plant } };
            var stageModule = new StageModule { stages = CreateGrowthStages(spec.growthDays, spec.regrow), wiltSprite = null, regrowStageIndex = spec.regrow > 0 ? 2 : -1 };
            plantEntity.modules = new List<IModuleData>
            {
                stageModule,
                new HarvestModule { harvestTool = ToolType.Scythe, wrongToolPenalty = 0f },
                new HealthModule { canTakeDamage = true },
                new DropModule { harvestDrops = new[] { new DropEntry { item = cropItem, minAmount = 1, maxAmount = 1, dropChance = 1f } } },
                new ExpRewardModule { rewardExp = 5 + spec.tier * 3, sourceType = ExpSourceType.Harvest, requireKiller = true },
                new MortalModule(),
                new QualityModule { minQuality = 1, maxQuality = 5, soilQualityPerStar = 10 }
            };
            SetOrAddStat(plantEntity, StatType.MaxHp, 10f);
            SetOrAddStat(plantEntity, StatType.Hp, 10f);
            EditorUtility.SetDirty(plantEntity);

            var seedItem = LoadOrCreateEntity(seedPath);
            seedItem.id = $"vs_seed_t{spec.tier}_{spec.suffix}";
            seedItem.keyName = spec.keyName;
            seedItem.descKey = spec.descKey;
            seedItem.category = ItemCategory.Seed;
            seedItem.maxStack = 999;
            seedItem.buyPrice = Mathf.RoundToInt(spec.sellPrice * 0.5f);
            seedItem.sellPrice = -1;
            seedItem.placementRule = plantEntity.placementRule;
            seedItem.modules = new List<IModuleData>
            {
                new PlacementModule { objectTypeToSpawn = spec.tier <= 2 ? ObjectType.Plant01 : ObjectType.Plant02, placedEntityData = plantEntity, centerTile = true, animTrigger = "PutDown" }
            };
            EditorUtility.SetDirty(seedItem);
        }
    }

    private static GrowthStage[] CreateGrowthStages(int growthDays, int regrowDays)
    {
        int safeGrowthDays = Mathf.Max(1, growthDays);
        int safeRegrowDays = Mathf.Max(0, regrowDays);

        if (safeRegrowDays > 0)
        {
            return new[]
            {
                new GrowthStage { sprite = null, daysToGrow = 1, canHarvest = false },
                new GrowthStage { sprite = null, daysToGrow = Mathf.Max(1, safeGrowthDays - safeRegrowDays - 1), canHarvest = false },
                new GrowthStage { sprite = null, daysToGrow = safeRegrowDays, canHarvest = false },
                new GrowthStage { sprite = null, daysToGrow = 999, canHarvest = true }
            };
        }

        return new[]
        {
            new GrowthStage { sprite = null, daysToGrow = 1, canHarvest = false },
            new GrowthStage { sprite = null, daysToGrow = Mathf.Max(1, safeGrowthDays - 1), canHarvest = false },
            new GrowthStage { sprite = null, daysToGrow = 999, canHarvest = true }
        };
    }

    private static string ToPascalCase(string input)
    {
        var parts = input.Split('_');
        return string.Concat(parts.Select(p => char.ToUpperInvariant(p[0]) + p.Substring(1).ToLowerInvariant()));
    }

    private static void EnsureSprint4Data()
    {
        var feed = AssetDatabase.LoadAssetAtPath<EntityData>("Assets/Project/ScriptableObjects/Items/Animal/AnimalFeed.asset");

        var milk = EnsureSimpleItem("Assets/Project/ScriptableObjects/Items/Animal/Milk.asset", "milk", "s4.item.milk.name", "s4.item.milk.desc", ItemCategory.AnimalProduct, 99, -1, 80);
        var wool = EnsureSimpleItem("Assets/Project/ScriptableObjects/Items/Animal/Wool.asset", "wool", "s4.item.wool.name", "s4.item.wool.desc", ItemCategory.AnimalProduct, 99, -1, 110);

        var cowEntity = LoadOrCreateEntity("Assets/Project/ScriptableObjects/WorldObjects/Animals/Animal_Cow_01.asset");
        cowEntity.id = "animal_cow_01";
        cowEntity.keyName = "s4.animal.cow.name";
        cowEntity.descKey = "s4.animal.cow.desc";
        cowEntity.category = ItemCategory.Placeable;
        cowEntity.maxStack = 1;
        cowEntity.buyPrice = -1;
        cowEntity.sellPrice = 0;
        cowEntity.modules = new List<IModuleData> { new AnimalModule { speciesKey = "s4.animal.cow.name", feedItem = feed, productItem = milk, productAmount = 1, daysWithoutFoodToDie = 3, priority = 25 } };
        EditorUtility.SetDirty(cowEntity);

        var sheepEntity = LoadOrCreateEntity("Assets/Project/ScriptableObjects/WorldObjects/Animals/Animal_Sheep_01.asset");
        sheepEntity.id = "animal_sheep_01";
        sheepEntity.keyName = "s4.animal.sheep.name";
        sheepEntity.descKey = "s4.animal.sheep.desc";
        sheepEntity.category = ItemCategory.Placeable;
        sheepEntity.maxStack = 1;
        sheepEntity.buyPrice = -1;
        sheepEntity.sellPrice = 0;
        sheepEntity.modules = new List<IModuleData> { new AnimalModule { speciesKey = "s4.animal.sheep.name", feedItem = feed, productItem = wool, productAmount = 1, daysWithoutFoodToDie = 3, priority = 25 } };
        EditorUtility.SetDirty(sheepEntity);

        var coopEntity = LoadOrCreateEntity("Assets/Project/ScriptableObjects/WorldObjects/Buildings/Building_ChickenCoop.asset");
        coopEntity.id = "building_chicken_coop";
        coopEntity.keyName = "s4.building.coop.name";
        coopEntity.descKey = "s4.building.coop.desc";
        coopEntity.category = ItemCategory.Placeable;
        coopEntity.maxStack = 1;
        coopEntity.buyPrice = -1;
        coopEntity.sellPrice = 0;
        coopEntity.placementRule = new PlacementRule { occupyLayer = EntityLayer.Furniture, blockLayers = new[] { EntityLayer.Furniture, EntityLayer.Plant, EntityLayer.Decoration }, requireTags = PlacementTag.None, provideTags = PlacementTag.None };
        coopEntity.baseStats = new StatsData { baseStats = new List<StatEntry> { new StatEntry { statType = StatType.AreaX, value = 3 }, new StatEntry { statType = StatType.AreaY, value = 2 } } };
        coopEntity.modules = new List<IModuleData>();
        EditorUtility.SetDirty(coopEntity);

        var barnEntity = LoadOrCreateEntity("Assets/Project/ScriptableObjects/WorldObjects/Buildings/Building_CowBarn.asset");
        barnEntity.id = "building_cow_barn";
        barnEntity.keyName = "s4.building.barn.name";
        barnEntity.descKey = "s4.building.barn.desc";
        barnEntity.category = ItemCategory.Placeable;
        barnEntity.maxStack = 1;
        barnEntity.buyPrice = -1;
        barnEntity.sellPrice = 0;
        barnEntity.placementRule = new PlacementRule { occupyLayer = EntityLayer.Furniture, blockLayers = new[] { EntityLayer.Furniture, EntityLayer.Plant, EntityLayer.Decoration }, requireTags = PlacementTag.None, provideTags = PlacementTag.None };
        barnEntity.baseStats = new StatsData { baseStats = new List<StatEntry> { new StatEntry { statType = StatType.AreaX, value = 4 }, new StatEntry { statType = StatType.AreaY, value = 3 } } };
        barnEntity.modules = new List<IModuleData>();
        EditorUtility.SetDirty(barnEntity);

        var coopItem = LoadOrCreateEntity("Assets/Project/ScriptableObjects/Items/Buildings/Item_ChickenCoop.asset");
        coopItem.id = "item_chicken_coop";
        coopItem.keyName = "s4.building.coop.name";
        coopItem.descKey = "s4.building.coop.desc";
        coopItem.category = ItemCategory.Placeable;
        coopItem.maxStack = 1;
        coopItem.buyPrice = -1;
        coopItem.sellPrice = 0;
        coopItem.modules = new List<IModuleData> { new BuildingModule { buildingEntity = coopEntity, buildingPrefabId = ObjectType.Bed01, consumeItemOnSuccess = true } };
        EditorUtility.SetDirty(coopItem);

        var barnItem = LoadOrCreateEntity("Assets/Project/ScriptableObjects/Items/Buildings/Item_CowBarn.asset");
        barnItem.id = "item_cow_barn";
        barnItem.keyName = "s4.building.barn.name";
        barnItem.descKey = "s4.building.barn.desc";
        barnItem.category = ItemCategory.Placeable;
        barnItem.maxStack = 1;
        barnItem.buyPrice = -1;
        barnItem.sellPrice = 0;
        barnItem.modules = new List<IModuleData> { new BuildingModule { buildingEntity = barnEntity, buildingPrefabId = ObjectType.Bed01, consumeItemOnSuccess = true } };
        EditorUtility.SetDirty(barnItem);

        var mutantMat = EnsureSimpleItem("Assets/Project/ScriptableObjects/Items/Materials/MutantMaterial_01.asset", "mutant_material_01", "s4.mat.mutant.name", "s4.mat.mutant.desc", ItemCategory.Material, 99, -1, 25);

        string[] enemyPaths = {
            "Assets/Project/ScriptableObjects/WorldObjects/Enemies/Enemy_T1_Slime.asset",
            "Assets/Project/ScriptableObjects/WorldObjects/Enemies/Enemy_T2_Bat.asset",
            "Assets/Project/ScriptableObjects/WorldObjects/Enemies/Enemy_T3_Golem.asset",
            "Assets/Project/ScriptableObjects/WorldObjects/Enemies/Enemy_T4_Wraith.asset",
            "Assets/Project/ScriptableObjects/WorldObjects/Enemies/Enemy_T5_Ancient.asset"
        };

        foreach (var path in enemyPaths)
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EntityData>(path);
            if (enemy == null) continue;
            var drop = enemy.modules?.OfType<DropModule>().FirstOrDefault();
            if (drop == null) continue;
            drop.harvestDrops ??= Array.Empty<DropEntry>();
            bool hasMutant = drop.harvestDrops.Any(d => d.item == mutantMat);
            if (!hasMutant)
            {
                var list = drop.harvestDrops.ToList();
                list.Add(new DropEntry { item = mutantMat, minAmount = 1, maxAmount = 2, dropChance = 0.4f });
                drop.harvestDrops = list.ToArray();
                EditorUtility.SetDirty(enemy);
            }
        }
    }

    private static void EnsureAnimalPrefabBridge()
    {
        var root = PrefabUtility.LoadPrefabContents(AnimalPrefabPath);
        if (root == null) return;

        try
        {
            EnsureComponent<NextDayEntityBridge>(root);
            PrefabUtility.SaveAsPrefabAsset(root, AnimalPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static DropEntry Drop(EntityData item, int minAmount, int maxAmount, float chance)
    {
        return new DropEntry
        {
            item = item,
            minAmount = minAmount,
            maxAmount = maxAmount,
            dropChance = chance
        };
    }

    private static void EnsureWorldObjectDefinition(string path, ObjectType id, string prefabPath)
    {
        var def = AssetDatabase.LoadAssetAtPath<WorldObjectDefinition>(path);
        if (def == null)
        {
            EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            def = ScriptableObject.CreateInstance<WorldObjectDefinition>();
            def.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(def, path);
        }

        def.idObject = id;
        def.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        EditorUtility.SetDirty(def);
    }

    private static void ConfigurePlayerData()
    {
        var player = AssetDatabase.LoadAssetAtPath<EntityData>("Assets/Project/ScriptableObjects/Characters/Player/Player.asset");
        if (player == null) return;

        EnsureInventory(player, InventoryType.Backpack, 60);
        EnsureInventory(player, InventoryType.Hotbar, 10);
        EnsureModule<EquipmentModule>(player);
        EnsureModule<ActionModule>(player);
        EnsureModule<HealthModule>(player);
        EnsureModule<QuestLogModule>(player);
        RemoveModule<DialogueModule>(player);
        RemoveModule<ShopModule>(player);
        RemoveModule<CraftingModule>(player);
        RemoveModule<QuestModule>(player);
        RemoveModule<ExpRewardModule>(player);

        player.category = ItemCategory.None;
        SetOrAddStat(player, StatType.Level, 1);
        SetOrAddStat(player, StatType.Exp, 0);
        SetOrAddStat(player, StatType.MaxExp, ProgressionService.RequiredExp(1));
        SetOrAddStat(player, StatType.MaxHp, Mathf.Max(100, FindStat(player, StatType.MaxHp, 100)));
        SetOrAddStat(player, StatType.Hp, FindStat(player, StatType.MaxHp, 100));
        SetOrAddStat(player, StatType.MaxStamina, Mathf.Max(100, FindStat(player, StatType.MaxStamina, 100)));
        SetOrAddStat(player, StatType.Stamina, FindStat(player, StatType.MaxStamina, 100));
        SetOrAddStat(player, StatType.Attack, Mathf.Max(2, FindStat(player, StatType.Attack, 2)));
        SetOrAddStat(player, StatType.Defense, Mathf.Max(0, FindStat(player, StatType.Defense, 0)));
        SetOrAddStat(player, StatType.Money, 500);
        EditorUtility.SetDirty(player);
    }

    private static void EnsureM4WeaponData()
    {
        EnsureWeapon(
            "Assets/Project/ScriptableObjects/Items/Weapons/Sword_T1.asset",
            "sword_t1",
            "m4.weapon.sword_t1.name",
            "m4.weapon.sword_t1.desc",
            WeaponArchetype.Sword,
            attack: 7f,
            range: 1.35f,
            cooldown: 0.45f,
            staminaCost: 5f,
            knockback: 1.2f,
            buyPrice: 150,
            sellPrice: 50,
            animTrigger: "Slash1H",
            spriteId: "Art.Equipment.MeleeWeapon1H.Sword01",
            part: EquipmentPart.MeleeWeapon1H);

        EnsureWeapon(
            "Assets/Project/ScriptableObjects/Items/Weapons/Spear_T1.asset",
            "spear_t1",
            "m4.weapon.spear_t1.name",
            "m4.weapon.spear_t1.desc",
            WeaponArchetype.Spear,
            attack: 6f,
            range: 1.8f,
            cooldown: 0.65f,
            staminaCost: 7f,
            knockback: 1.8f,
            buyPrice: 180,
            sellPrice: 60,
            animTrigger: "Jab",
            spriteId: "Art.Equipment.MeleeWeapon1H.Spear01",
            part: EquipmentPart.MeleeWeapon2H);
    }

    private static void EnsureWeapon(
        string path,
        string id,
        string keyName,
        string descKey,
        WeaponArchetype archetype,
        float attack,
        float range,
        float cooldown,
        float staminaCost,
        float knockback,
        int buyPrice,
        int sellPrice,
        string animTrigger,
        string spriteId,
        EquipmentPart part)
    {
        var weapon = LoadOrCreateEntity(path);
        weapon.id = id;
        weapon.keyName = keyName;
        weapon.descKey = descKey;
        weapon.category = ItemCategory.Weapon;
        weapon.maxStack = 1;
        weapon.buyPrice = buyPrice;
        weapon.sellPrice = sellPrice;
        weapon.modules = new List<IModuleData>
        {
            new WeaponModule
            {
                archetype = archetype,
                animTrigger = animTrigger,
                baseRange = range,
                baseDamage = attack,
                cooldown = cooldown,
                staminaCost = staminaCost,
                knockback = knockback
            },
            new AppearanceModule
            {
                spriteId = spriteId,
                equipmentPart = part
            }
        };

        SetOrAddStat(weapon, StatType.Attack, attack);
        SetOrAddStat(weapon, StatType.Range, range);
        SetOrAddStat(weapon, StatType.CoolDown, cooldown);
        SetOrAddStat(weapon, StatType.CritChance, archetype == WeaponArchetype.Sword ? 0.05f : 0.03f);
        SetOrAddStat(weapon, StatType.CritDamage, 0.5f);
        EditorUtility.SetDirty(weapon);
    }

    private static void ConfigureNpcData(string path, bool requireShop, bool requireCrafting, bool requireQuest)
    {
        var npc = AssetDatabase.LoadAssetAtPath<EntityData>(path);
        if (npc == null) return;

        EnsureModule<DialogueModule>(npc);
        if (requireShop) EnsureModule<ShopModule>(npc); else RemoveModule<ShopModule>(npc);
        if (requireCrafting) EnsureModule<CraftingModule>(npc); else RemoveModule<CraftingModule>(npc);
        if (requireQuest) EnsureModule<QuestModule>(npc); else RemoveModule<QuestModule>(npc);

        RemoveModule<AttackModule>(npc);
        RemoveModule<HealthModule>(npc);
        RemoveModule<DropModule>(npc);
        RemoveModule<ExpRewardModule>(npc);
        RemoveModule<MortalModule>(npc);
        RemoveModule<RespawnModule>(npc);
        RemoveModule<PlacementModule>(npc);
        RemoveModule<StageModule>(npc);
        RemoveModule<HarvestModule>(npc);
        EditorUtility.SetDirty(npc);
    }

    private static void ConfigureCropPlacementData()
    {
        foreach (var seed in LoadAllEntities("Assets/Project/ScriptableObjects/WorldObjects/Plants", "Seed_T"))
        {
            if (seed == null) continue;
            string suffix = seed.name.StartsWith("Seed_", StringComparison.Ordinal) ? seed.name["Seed_".Length..] : seed.name;
            string plantPath = $"{CropPlantFolder}/CropPlant_{suffix}.asset";
            var plant = LoadOrCreateEntity(plantPath);

            plant.id = $"crop_plant_{suffix.ToLowerInvariant()}";
            plant.keyName = seed.keyName;
            plant.descKey = seed.descKey;
            plant.category = ItemCategory.Placeable;
            plant.maxStack = 1;
            plant.buyPrice = -1;
            plant.sellPrice = 0;
            plant.icon = seed.icon;
            plant.placementRule = seed.placementRule;
            CopyStats(seed, plant);
            plant.modules = CloneModules(seed.modules)
                .Where(module => module is not PlacementModule)
                .ToList();
            EditorUtility.SetDirty(plant);

            seed.modules = new List<IModuleData>
            {
                new PlacementModule
                {
                    objectTypeToSpawn = ObjectType.Plant01,
                    placedEntityData = plant,
                    centerTile = true,
                    animTrigger = "PutDown"
                }
            };
            EditorUtility.SetDirty(seed);
        }
    }

    private static void ConfigureEnemyData()
    {
        foreach (var enemy in LoadAllEntities("Assets/Project/ScriptableObjects/WorldObjects/Enemies", "Enemy_"))
        {
            if (enemy == null) continue;

            EnsureModule<HealthModule>(enemy).canTakeDamage = true;
            EnsureModule<AttackModule>(enemy);
            if (!HasModule<DropModule>(enemy)) EnsureModule<DropModule>(enemy);
            if (!HasModule<ExpRewardModule>(enemy)) EnsureModule<ExpRewardModule>(enemy);
            if (!HasModule<RespawnModule>(enemy)) EnsureModule<RespawnModule>(enemy).respawnPrefabId = ObjectType.Enemy01;

            RemoveModule<DialogueModule>(enemy);
            RemoveModule<ShopModule>(enemy);
            RemoveModule<CraftingModule>(enemy);
            RemoveModule<QuestModule>(enemy);
            RemoveModule<PlacementModule>(enemy);
            RemoveModule<StageModule>(enemy);
            RemoveModule<HarvestModule>(enemy);
            EditorUtility.SetDirty(enemy);
        }
    }

    private static void ConfigureOreData()
    {
        foreach (var ore in LoadAllEntities("Assets/Project/ScriptableObjects/WorldObjects/Resources", "OreNode_"))
        {
            if (ore == null) continue;
            EnsureModule<HealthModule>(ore).canTakeDamage = true;
            if (!HasModule<HarvestModule>(ore)) EnsureModule<HarvestModule>(ore).harvestTool = ToolType.Pickaxe;
            if (!HasModule<DropModule>(ore)) EnsureModule<DropModule>(ore);
            if (!HasModule<ExpRewardModule>(ore)) EnsureModule<ExpRewardModule>(ore);
            RemoveModule<DialogueModule>(ore);
            RemoveModule<ShopModule>(ore);
            RemoveModule<CraftingModule>(ore);
            RemoveModule<QuestModule>(ore);
            RemoveModule<AttackModule>(ore);
            RemoveModule<PlacementModule>(ore);
            RemoveModule<StageModule>(ore);
            EditorUtility.SetDirty(ore);
        }
    }

    private static void EnsureStarterLoadout()
    {
        var loadout = AssetDatabase.LoadAssetAtPath<StarterLoadoutData>(StarterLoadoutPath);
        if (loadout == null)
        {
            EnsureFolder(Path.GetDirectoryName(StarterLoadoutPath)?.Replace('\\', '/'));
            loadout = ScriptableObject.CreateInstance<StarterLoadoutData>();
            loadout.name = Path.GetFileNameWithoutExtension(StarterLoadoutPath);
            AssetDatabase.CreateAsset(loadout, StarterLoadoutPath);
        }

        loadout.startSpawnPointId = SceneSpawnResolver.DefaultPlayerSpawnPointId;
        loadout.initialMoney = 500;
        loadout.selectedHotbarIndex = 0;
        loadout.entries = new[]
        {
            Entry(InventoryType.Hotbar, 0, "Assets/Project/ScriptableObjects/Items/Tools/Hoe_01.asset", 1),
            Entry(InventoryType.Hotbar, 1, "Assets/Project/ScriptableObjects/Items/Tools/Pickaxe_01.asset", 1),
            Entry(InventoryType.Hotbar, 2, "Assets/Project/ScriptableObjects/Items/Tools/Scythe_01.asset", 1),
            Entry(InventoryType.Hotbar, 3, "Assets/Project/ScriptableObjects/Items/Tools/Axe_01.asset", 1),
            Entry(InventoryType.Hotbar, 4, "Assets/Project/ScriptableObjects/WorldObjects/Plants/Seed_T1_Turnip.asset", 10),
            Entry(InventoryType.Hotbar, 5, "Assets/Project/ScriptableObjects/Items/Weapons/Sword_T1.asset", 1),
            Entry(InventoryType.Backpack, 0, "Assets/Project/ScriptableObjects/Items/Weapons/Spear_T1.asset", 1)
        }.Where(entry => entry.itemData != null).ToArray();

        EditorUtility.SetDirty(loadout);
    }

    private static void EnsureGeneratedIcons()
    {
        foreach (var data in LoadAllEntities("Assets/Project", null))
        {
            if (data == null || data.icon != null) continue;
            data.icon = EnsurePlaceholderIcon(data);
            EditorUtility.SetDirty(data);
        }
    }

    private static Sprite EnsurePlaceholderIcon(EntityData data)
    {
        EnsureFolder(GeneratedIconFolder);
        string safeName = SanitizeFileName(string.IsNullOrWhiteSpace(data.id) ? data.name : data.id);
        string path = $"{GeneratedIconFolder}/{safeName}.png";

        if (!File.Exists(path))
        {
            var texture = BuildIconTexture(data);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Texture2D BuildIconTexture(EntityData data)
    {
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var background = CategoryColor(data);
        var accent = Color.Lerp(background, Color.white, 0.45f);

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            texture.SetPixel(x, y, new Color(0, 0, 0, 0));

        FillRect(texture, 3, 3, 26, 26, new Color(background.r, background.g, background.b, 0.95f));

        string id = (data.id + " " + data.name).ToLowerInvariant();
        if (data.category == ItemCategory.Tool || id.Contains("pickaxe") || id.Contains("hoe") || id.Contains("axe") || id.Contains("scythe"))
        {
            DrawLine(texture, 8, 23, 23, 8, Color.white);
            DrawLine(texture, 15, 7, 25, 10, accent);
            DrawLine(texture, 7, 15, 11, 25, accent);
        }
        else if (id.Contains("enemy") || id.Contains("monster"))
        {
            FillCircle(texture, 16, 16, 9, accent);
            FillCircle(texture, 12, 18, 2, Color.black);
            FillCircle(texture, 20, 18, 2, Color.black);
        }
        else if (data.category == ItemCategory.Seed)
        {
            FillCircle(texture, 12, 14, 4, accent);
            FillCircle(texture, 19, 18, 5, Color.white);
        }
        else if (data.category == ItemCategory.Crop)
        {
            FillCircle(texture, 16, 16, 9, accent);
            FillRect(texture, 15, 7, 3, 8, new Color(0.25f, 0.7f, 0.25f, 1f));
        }
        else if (data.category == ItemCategory.Material || id.Contains("ore"))
        {
            DrawDiamond(texture, 16, 16, 11, accent);
        }
        else if (data.category == ItemCategory.Armor)
        {
            DrawShield(texture, accent);
        }
        else
        {
            FillCircle(texture, 16, 16, 10, accent);
        }

        texture.Apply();
        return texture;
    }

    private static void EnsurePlayerStartMarker()
    {
        var marker = AssetDatabase.LoadAssetAtPath<SceneSpawnTile>(PlayerStartMarkerPath);
        if (marker == null)
        {
            marker = ScriptableObject.CreateInstance<SceneSpawnTile>();
            marker.name = "Marker_Player_Start";
            AssetDatabase.CreateAsset(marker, PlayerStartMarkerPath);
        }

        marker.markerKind = SceneMarkerKind.PlayerSpawn;
        marker.objectType = ObjectType.Player01;
        marker.entityData = null;
        marker.savePolicy = SceneEntitySavePolicy.Temporary;
        marker.spawnGroupId = "player";
        marker.spawnPointId = SceneSpawnResolver.DefaultPlayerSpawnPointId;
        marker.respawnMinutes = 0;
        marker.initialAmount = 1;
        marker.bypassPlacementValidation = true;
        marker.editorSprite = null;
        marker.editorColor = new Color(0.2f, 0.8f, 1f, 0.75f);
        EditorUtility.SetDirty(marker);
    }

    private static void EnsureMasteryUnlockData()
    {
        var data = AssetDatabase.LoadAssetAtPath<MasteryUnlockData>(MasteryUnlockPath);
        if (data == null)
        {
            EnsureFolder(Path.GetDirectoryName(MasteryUnlockPath)?.Replace('\\', '/'));
            data = ScriptableObject.CreateInstance<MasteryUnlockData>();
            data.name = Path.GetFileNameWithoutExtension(MasteryUnlockPath);
            AssetDatabase.CreateAsset(data, MasteryUnlockPath);
        }

        data.unlocks = new[]
        {
            new MasteryUnlockData.UnlockEntry { masteryLevel = 3, unlockId = "recipe.fertilizer.t1", description = "Unlock fertilizer crafting" },
            new MasteryUnlockData.UnlockEntry { masteryLevel = 5, unlockId = "tool.fertilizer.t1", description = "Unlock fertilizer use" },
            new MasteryUnlockData.UnlockEntry { masteryLevel = 10, unlockId = "recipe.sprinkler.t1", description = "Unlock sprinkler T1 crafting" },
            new MasteryUnlockData.UnlockEntry { masteryLevel = 10, unlockId = "tool.sprinkler.t1", description = "Unlock sprinkler T1 use" },
            new MasteryUnlockData.UnlockEntry { masteryLevel = 20, unlockId = "recipe.sprinkler.t2", description = "Unlock sprinkler T2 crafting" },
            new MasteryUnlockData.UnlockEntry { masteryLevel = 20, unlockId = "tool.sprinkler.t2", description = "Unlock sprinkler T2 use" }
        };
        EditorUtility.SetDirty(data);
    }

    private static void StampPlayerStartMarker()
    {
        if (!File.Exists(SampleScenePath))
            return;

        var scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
        var markerMap = EnsureRuntimeMarkerTilemap();
        var marker = AssetDatabase.LoadAssetAtPath<SceneSpawnTile>(PlayerStartMarkerPath);
        if (markerMap != null && marker != null)
            markerMap.SetTile(new Vector3Int(0, 0, 0), marker);

        EnsureSceneMarkerComponents(markerMap);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ValidateSetup(bool logSuccess)
    {
        int errors = 0;
        errors += ValidateWorldObject("Player", "Assets/Project/Resources/Data/WorldObjects/Player.asset", WorldEntityPrefabRoleType.Player);
        errors += ValidateWorldObject("NPCShop01", "Assets/Project/Resources/Data/WorldObjects/NPCShop01.asset", WorldEntityPrefabRoleType.Npc);
        errors += ValidateWorldObject("NPCCrafting01", "Assets/Project/Resources/Data/WorldObjects/NPCCrafting01.asset", WorldEntityPrefabRoleType.Npc);
        errors += ValidateWorldObject("NPCEvent01", "Assets/Project/Resources/Data/WorldObjects/NPCEvent01.asset", WorldEntityPrefabRoleType.Npc);
        errors += ValidateWorldObject("Enemy01", "Assets/Project/Resources/Data/WorldObjects/Enemy01.asset", WorldEntityPrefabRoleType.Enemy);
        errors += ValidateWorldObject("OreNode01", "Assets/Project/Resources/Data/WorldObjects/OreNode01.asset", WorldEntityPrefabRoleType.Resource);
        errors += ValidateWorldObject("Plant01", "Assets/Project/Resources/Data/WorldObjects/Plant01.asset", WorldEntityPrefabRoleType.Crop);
        errors += ValidateWorldObject("Sprinkler01", "Assets/Project/Resources/Data/WorldObjects/Sprinkler01.asset", WorldEntityPrefabRoleType.Crop);
        errors += ValidateWorldObject("Sprinkler02", "Assets/Project/Resources/Data/WorldObjects/Sprinkler02.asset", WorldEntityPrefabRoleType.Crop);

        var starter = AssetDatabase.LoadAssetAtPath<StarterLoadoutData>(StarterLoadoutPath);
        if (starter == null || starter.entries == null || starter.entries.Length == 0)
        {
            Debug.LogError("[BootstrapProductionSetup] Starter loadout is missing or empty.");
            errors++;
        }

        foreach (var data in LoadAllEntities("Assets/Project", null))
        {
            if (data != null && data.icon == null)
            {
                Debug.LogError($"[BootstrapProductionSetup] EntityData missing icon: {AssetDatabase.GetAssetPath(data)}");
                errors++;
            }
        }

        if (errors == 0 && logSuccess)
            Debug.Log("[BootstrapProductionSetup] Validation passed.");
    }

    private static int ValidateWorldObject(string label, string path, WorldEntityPrefabRoleType expectedRole)
    {
        var def = AssetDatabase.LoadAssetAtPath<WorldObjectDefinition>(path);
        var role = def?.prefab != null ? def.prefab.GetComponent<WorldEntityPrefabRole>() : null;
        if (def?.prefab != null && role != null && role.role == expectedRole)
            return 0;

        Debug.LogError($"[BootstrapProductionSetup] {label} prefab role invalid at {path}.");
        return 1;
    }

    private static void SetWorldObjectPrefab(string definitionPath, string prefabPath)
    {
        var def = AssetDatabase.LoadAssetAtPath<WorldObjectDefinition>(definitionPath);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (def == null || prefab == null)
        {
            Debug.LogWarning($"[BootstrapProductionSetup] Cannot repoint world object '{definitionPath}' -> '{prefabPath}'.");
            return;
        }

        def.prefab = prefab;
        EditorUtility.SetDirty(def);
    }

    private static void EnsureRole(GameObject root, WorldEntityPrefabRoleType roleType)
    {
        var role = EnsureComponent<WorldEntityPrefabRole>(root);
        role.role = roleType;
    }

    private static void SetTagAndLayer(GameObject root, string tag, string layerName)
    {
        if (!string.IsNullOrWhiteSpace(tag))
        {
            EnsureTag(tag);
            root.tag = tag;
        }

        int layer = LayerMask.NameToLayer(layerName);
        if (layer >= 0)
            SetLayerRecursive(root, layer);
    }

    private static void SetLayerRecursive(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform child in root.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    private static void EnsureTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag) || tag == "Untagged")
            return;

        if (InternalEditorUtility.tags.Contains(tag))
            return;

        var tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (tagManagerAssets == null || tagManagerAssets.Length == 0)
            return;

        var serializedObject = new SerializedObject(tagManagerAssets[0]);
        var tagsProperty = serializedObject.FindProperty("tags");
        if (tagsProperty == null) return;

        int index = tagsProperty.arraySize;
        tagsProperty.InsertArrayElementAtIndex(index);
        tagsProperty.GetArrayElementAtIndex(index).stringValue = tag;
        serializedObject.ApplyModifiedProperties();
    }

    private static void ConfigureRigidbody(GameObject root, WorldEntityPrefabRoleType role)
    {
        var rb = root.GetComponent<Rigidbody2D>();
        if (rb == null) return;

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        if (role == WorldEntityPrefabRoleType.Npc || role == WorldEntityPrefabRoleType.Crop || role == WorldEntityPrefabRoleType.Resource)
            rb.bodyType = RigidbodyType2D.Kinematic;
    }

    private static void TintSpriteRenderer(GameObject root, Color color)
    {
        var renderer = root.GetComponentInChildren<SpriteRenderer>(true);
        if (renderer != null)
            renderer.color = color;
    }

    private static T EnsureComponent<T>(GameObject root) where T : Component
    {
        return root.GetComponent<T>() ?? root.AddComponent<T>();
    }

    private static void RemoveComponent<T>(GameObject root) where T : Component
    {
        var component = root.GetComponent<T>();
        if (component != null)
            UnityEngine.Object.DestroyImmediate(component);
    }

    private static void EnsureInventory(EntityData data, InventoryType type, int size)
    {
        data.modules ??= new List<IModuleData>();
        var inventory = data.modules.OfType<InventoryModule>().FirstOrDefault(module => module.inventoryType == type);
        if (inventory == null)
        {
            inventory = new InventoryModule { inventoryType = type };
            data.modules.Add(inventory);
        }

        inventory.size = Mathf.Max(size, inventory.size);
    }

    private static T EnsureModule<T>(EntityData data) where T : IModuleData
    {
        data.modules ??= new List<IModuleData>();
        var module = data.modules.OfType<T>().FirstOrDefault();
        if (module != null)
            return module;

        module = Activator.CreateInstance<T>();
        data.modules.Add(module);
        return module;
    }

    private static bool HasModule<T>(EntityData data) where T : IModuleData
    {
        return data?.modules != null && data.modules.OfType<T>().Any();
    }

    private static void RemoveModule<T>(EntityData data) where T : IModuleData
    {
        if (data?.modules == null) return;
        for (int i = data.modules.Count - 1; i >= 0; i--)
        {
            if (data.modules[i] is T)
                data.modules.RemoveAt(i);
        }
    }

    private static List<IModuleData> CloneModules(IEnumerable<IModuleData> modules)
    {
        var result = new List<IModuleData>();
        if (modules == null) return result;

        foreach (var module in modules)
        {
            if (module == null) continue;
            var clone = Activator.CreateInstance(module.GetType()) as IModuleData;
            if (clone == null) continue;
            EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(module), clone);
            result.Add(clone);
        }

        return result;
    }

    private static void SetOrAddStat(EntityData data, StatType type, float value)
    {
        data.baseStats ??= new StatsData();
        data.baseStats.baseStats ??= new List<StatEntry>();
        foreach (var stat in data.baseStats.baseStats)
        {
            if (stat == null || stat.statType != type) continue;
            stat.value = value;
            return;
        }

        data.baseStats.baseStats.Add(new StatEntry { statType = type, value = value });
    }

    private static float FindStat(EntityData data, StatType type, float fallback)
    {
        if (data?.baseStats?.baseStats == null) return fallback;
        foreach (var stat in data.baseStats.baseStats)
        {
            if (stat != null && stat.statType == type)
                return stat.value;
        }

        return fallback;
    }

    private static void CopyStats(EntityData source, EntityData target)
    {
        target.baseStats ??= new StatsData();
        target.baseStats.baseStats = source?.baseStats?.baseStats?
            .Where(stat => stat != null)
            .Select(stat => new StatEntry { statType = stat.statType, value = stat.value })
            .ToList() ?? new List<StatEntry>();
    }

    private static StarterLoadoutEntry Entry(InventoryType type, int slot, string itemPath, int amount)
    {
        return new StarterLoadoutEntry
        {
            inventoryType = type,
            slotIndex = slot,
            itemData = AssetDatabase.LoadAssetAtPath<EntityData>(itemPath),
            amount = amount
        };
    }

    private static IEnumerable<EntityData> LoadAllEntities(string folder, string namePrefix)
    {
        string filter = string.IsNullOrWhiteSpace(namePrefix) ? "t:EntityData" : $"{namePrefix} t:EntityData";
        return AssetDatabase.FindAssets(filter, new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<EntityData>)
            .Where(data => data != null);
    }

    private static EntityData LoadOrCreateEntity(string path)
    {
        var data = AssetDatabase.LoadAssetAtPath<EntityData>(path);
        if (data != null) return data;

        EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
        data = ScriptableObject.CreateInstance<EntityData>();
        data.name = Path.GetFileNameWithoutExtension(path);
        AssetDatabase.CreateAsset(data, path);
        return data;
    }

    private static void CopyAssetIfMissing(string sourcePath, string targetPath)
    {
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(targetPath) != null)
            return;

        EnsureFolder(Path.GetDirectoryName(targetPath)?.Replace('\\', '/'));
        if (!AssetDatabase.CopyAsset(sourcePath, targetPath))
            Debug.LogWarning($"[BootstrapProductionSetup] CopyAsset failed: {sourcePath} -> {targetPath}");
    }

    private static Tilemap EnsureRuntimeMarkerTilemap()
    {
        var marker = FindTilemap(SceneContext.RuntimeMarkersTilemapName);
        if (marker != null) return marker;

        var grid = UnityEngine.Object.FindAnyObjectByType<Grid>();
        if (grid == null)
            grid = new GameObject("Grid").AddComponent<Grid>();

        var go = new GameObject(SceneContext.RuntimeMarkersTilemapName);
        go.transform.SetParent(grid.transform);
        marker = go.AddComponent<Tilemap>();
        go.AddComponent<TilemapRenderer>();
        return marker;
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

    private static Tilemap FindTilemap(string tilemapName)
    {
        foreach (var tilemap in UnityEngine.Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
        {
            if (tilemap != null && string.Equals(tilemap.gameObject.name, tilemapName, StringComparison.Ordinal))
                return tilemap;
        }

        return null;
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/Project/Prefabs/Characters");
        EnsureFolder("Assets/Project/Prefabs/WorldEntities");
        EnsureFolder("Assets/Project/ScriptableObjects/WorldObjects/Resources");
        EnsureFolder("Assets/Project/ScriptableObjects/WorldObjects/Utility");
        EnsureFolder("Assets/Project/ScriptableObjects/WorldObjects/Animals");
        EnsureFolder("Assets/Project/Resources/Data");
        EnsureFolder("Assets/Project/Resources/Data/StarterLoadouts");
        EnsureFolder(MarkerFolder);
        EnsureFolder(GeneratedIconFolder);
        EnsureFolder(CropPlantFolder);
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

    private static Color CategoryColor(EntityData data)
    {
        string id = (data.id + data.name).ToLowerInvariant();
        if (id.Contains("enemy") || id.Contains("monster")) return new Color(0.8f, 0.18f, 0.2f, 1f);
        if (id.Contains("npc")) return new Color(0.2f, 0.55f, 0.95f, 1f);
        if (id.Contains("ore")) return new Color(0.45f, 0.5f, 0.65f, 1f);

        return data.category switch
        {
            ItemCategory.Tool => new Color(0.45f, 0.42f, 0.36f, 1f),
            ItemCategory.Seed => new Color(0.3f, 0.72f, 0.35f, 1f),
            ItemCategory.Crop => new Color(0.95f, 0.45f, 0.25f, 1f),
            ItemCategory.Material => new Color(0.55f, 0.55f, 0.7f, 1f),
            ItemCategory.Armor => new Color(0.28f, 0.38f, 0.85f, 1f),
            _ => new Color(0.55f, 0.65f, 0.75f, 1f)
        };
    }

    private static string SanitizeFileName(string value)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            value = value.Replace(c, '_');
        return string.IsNullOrWhiteSpace(value) ? "icon" : value;
    }

    private static void FillRect(Texture2D tex, int x, int y, int width, int height, Color color)
    {
        for (int yy = y; yy < y + height; yy++)
        for (int xx = x; xx < x + width; xx++)
            SetPixelSafe(tex, xx, yy, color);
    }

    private static void FillCircle(Texture2D tex, int cx, int cy, int radius, Color color)
    {
        int r2 = radius * radius;
        for (int y = cy - radius; y <= cy + radius; y++)
        for (int x = cx - radius; x <= cx + radius; x++)
        {
            int dx = x - cx;
            int dy = y - cy;
            if (dx * dx + dy * dy <= r2)
                SetPixelSafe(tex, x, y, color);
        }
    }

    private static void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, Color color)
    {
        int dx = Mathf.Abs(x1 - x0);
        int sx = x0 < x1 ? 1 : -1;
        int dy = -Mathf.Abs(y1 - y0);
        int sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;

        while (true)
        {
            FillCircle(tex, x0, y0, 1, color);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    private static void DrawDiamond(Texture2D tex, int cx, int cy, int radius, Color color)
    {
        for (int y = -radius; y <= radius; y++)
        {
            int half = radius - Mathf.Abs(y);
            for (int x = -half; x <= half; x++)
                SetPixelSafe(tex, cx + x, cy + y, color);
        }
    }

    private static void DrawShield(Texture2D tex, Color color)
    {
        FillRect(tex, 10, 9, 12, 10, color);
        for (int i = 0; i < 6; i++)
            FillRect(tex, 11 + i, 19 + i, 10 - i * 2, 1, color);
    }

    private static void SetPixelSafe(Texture2D tex, int x, int y, Color color)
    {
        if (x < 0 || y < 0 || x >= tex.width || y >= tex.height)
            return;
        tex.SetPixel(x, y, color);
    }
}
