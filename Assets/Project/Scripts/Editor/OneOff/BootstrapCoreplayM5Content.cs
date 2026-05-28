using System;
using System.Collections.Generic;
using System.Linq;
using Assets.HeroEditor4D.Common.Scripts.Enums;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class BootstrapCoreplayM5Content
{
    private const string SampleScenePath = "Assets/Project/Scenes/Main/SampleScene.unity";

    private const string PlayerPath = "Assets/Project/ScriptableObjects/Characters/Player/Player.asset";
    private const string NpcShopPath = "Assets/Project/ScriptableObjects/Characters/NPCs/NPC_Banhang.asset";
    private const string NpcCraftPath = "Assets/Project/ScriptableObjects/Characters/NPCs/NPC_Chetao.asset";
    private const string NpcEventPath = "Assets/Project/ScriptableObjects/Characters/NPCs/NPC_Sukien.asset";

    private const string T1PickaxePath = "Assets/Project/ScriptableObjects/Items/Tools/Pickaxe_01.asset";
    private const string T1ScythePath = "Assets/Project/ScriptableObjects/Items/Tools/Scythe_01.asset";
    private const string T1HoePath = "Assets/Project/ScriptableObjects/Items/Tools/Hoe_01.asset";
    private const string T1AxePath = "Assets/Project/ScriptableObjects/Items/Tools/Axe_01.asset";

    private const string CropFolder = "Assets/Project/ScriptableObjects/Items/Crops";
    private const string PlantFolder = "Assets/Project/ScriptableObjects/WorldObjects/Plants";
    private const string MaterialFolder = "Assets/Project/ScriptableObjects/Items/Materials";
    private const string ResourceFolder = "Assets/Project/ScriptableObjects/WorldObjects/Resources";
    private const string EnemyFolder = "Assets/Project/ScriptableObjects/WorldObjects/Enemies";
    private const string ToolFolder = "Assets/Project/ScriptableObjects/Items/Tools/MVP";
    private const string GearFolder = "Assets/Project/ScriptableObjects/Items/Equipment/MVP";
    private const string RecipeFolder = "Assets/Project/ScriptableObjects/Graph/recipes/mvp";
    private const string QuestFolder = "Assets/Project/ScriptableObjects/Graph/quest/mvp";
    private const string MarkerFolder = "Assets/Project/ScriptableObjects/SceneMarkers/MVP";
    private const string UtilityFolder = "Assets/Project/ScriptableObjects/WorldObjects/Utility";

    private static readonly CropSpec[] Crops =
    {
        new(1, "turnip", "Turnip", 20, 45, 2, 8, 1, false),
        new(2, "tomato", "Tomato", 35, 80, 3, 18, 10, true),
        new(3, "pumpkin", "Pumpkin", 70, 160, 4, 35, 20, true),
        new(4, "melon", "Melon", 120, 300, 5, 60, 30, true),
        new(5, "starfruit", "Starfruit", 220, 600, 6, 100, 40, true)
    };

    private static readonly OreSpec[] Ores =
    {
        new(1, "copper", "Copper", 8, 24, 15, 240, 1),
        new(2, "iron", "Iron", 18, 65, 40, 360, 10),
        new(3, "silver", "Silver", 35, 140, 90, 480, 20),
        new(4, "gold", "Gold", 70, 300, 180, 720, 30),
        new(5, "mythril", "Mythril", 130, 650, 330, 960, 40)
    };

    private static readonly EnemySpec[] Enemies =
    {
        new(1, "slime", "Slime", 20, 3, 0, 30, 20, 120, 1),
        new(2, "bat", "Bat", 55, 8, 2, 80, 55, 180, 10),
        new(3, "golem", "Golem", 120, 16, 5, 180, 120, 240, 20),
        new(4, "wraith", "Wraith", 260, 28, 9, 380, 260, 360, 30),
        new(5, "ancient", "Ancient", 520, 45, 15, 750, 560, 480, 40)
    };

    private static readonly Color[] TierColors =
    {
        new(0.65f, 0.82f, 0.45f, 0.85f),
        new(0.55f, 0.68f, 0.95f, 0.85f),
        new(0.78f, 0.72f, 0.92f, 0.85f),
        new(0.95f, 0.72f, 0.35f, 0.85f),
        new(0.95f, 0.45f, 0.55f, 0.85f)
    };

    [MenuItem("Tools/DATN/One-off Setup/Bootstraps/Bootstrap M5 Content Balance")]
    public static void Execute()
    {
        BootstrapGameplayData.Execute();
        EnsureDirectories();

        var content = BuildContent();
        ConfigurePlayer();
        ConfigureShopNpc(content);
        ConfigureCraftingNpc(content);
        ConfigureQuestNpc(content);
        CreateMarkerTiles(content);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BootstrapCoreplayM5Content] M5 content, balance, shop, crafting, quest and marker assets generated.");
    }

    [MenuItem("Tools/DATN/One-off Setup/Bootstraps/Bootstrap M5 Content + Sample Markers")]
    public static void ExecuteAndStampSampleScene()
    {
        Execute();
        StampSampleSceneMarkers();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BootstrapCoreplayM5Content] M5 content generated and SampleScene marker layer stamped.");
    }

    public static void ExecuteBatch()
    {
        ExecuteAndStampSampleScene();
    }

    private static CoreplayContent BuildContent()
    {
        var content = new CoreplayContent();
        var cropSprites = LoadStageSprites("Assets/Project/ScriptableObjects/WorldObjects/Plants/OnionTree.asset");

        foreach (var crop in Crops)
            content.Crops.Add(EnsureCrop(crop, cropSprites));

        content.Sprinklers[1] = EnsureSprinkler(
            tier: 1,
            radius: 1,
            itemPath: $"{ToolFolder}/Sprinkler_T1.asset",
            placedPath: $"{UtilityFolder}/Sprinkler_T1.asset",
            itemId: "tool_sprinkler_t1",
            placedId: "sprinkler_t1",
            itemKey: "mvp.tool.sprinkler.t1.name",
            itemDesc: "mvp.tool.sprinkler.t1.desc",
            placedKey: "mvp.sprinkler.t1.name",
            placedDesc: "mvp.sprinkler.t1.desc",
            placementType: ObjectType.Sprinkler01,
            buyPrice: 180,
            sellPrice: 60,
            appearanceCopy: CopyAppearance(T1HoePath));
        content.Sprinklers[2] = EnsureSprinkler(
            tier: 2,
            radius: 2,
            itemPath: $"{ToolFolder}/Sprinkler_T2.asset",
            placedPath: $"{UtilityFolder}/Sprinkler_T2.asset",
            itemId: "tool_sprinkler_t2",
            placedId: "sprinkler_t2",
            itemKey: "mvp.tool.sprinkler.t2.name",
            itemDesc: "mvp.tool.sprinkler.t2.desc",
            placedKey: "mvp.sprinkler.t2.name",
            placedDesc: "mvp.sprinkler.t2.desc",
            placementType: ObjectType.Sprinkler02,
            buyPrice: 420,
            sellPrice: 160,
            appearanceCopy: CopyAppearance(T1HoePath));

        foreach (var ore in Ores)
            content.Ores.Add(EnsureOre(ore));

        foreach (var enemy in Enemies)
            content.Enemies.Add(EnsureEnemy(enemy));

        content.BasicHoe = LoadOrCreateEntity(T1HoePath);
        content.BasicAxe = LoadOrCreateEntity(T1AxePath);
        content.Pickaxes[1] = EnsureTool(T1PickaxePath, "Pickaxe01", "mvp.tool.pickaxe.t1.name", "mvp.tool.pickaxe.t1.desc", ToolType.Pickaxe, 1, 2f, 1.3f, 0.35f, 320, 140, "Pickaxe", CopyAppearance(T1PickaxePath));
        content.Scythes[1] = EnsureTool(T1ScythePath, "Scrythe01", "mvp.tool.scythe.t1.name", "mvp.tool.scythe.t1.desc", ToolType.Scythe, 1, 5f, 1.3f, 0.7f, 260, 100, "Harvert", CopyAppearance(T1ScythePath));

        var pickaxeStats = new[]
        {
            (tier: 2, attack: 5f, range: 1.4f, cooldown: 0.32f, sell: 280),
            (tier: 3, attack: 10f, range: 1.5f, cooldown: 0.28f, sell: 600),
            (tier: 4, attack: 18f, range: 1.6f, cooldown: 0.24f, sell: 1200),
            (tier: 5, attack: 30f, range: 1.8f, cooldown: 0.20f, sell: 2500)
        };

        foreach (var stat in pickaxeStats)
        {
            string path = $"{ToolFolder}/Pickaxe_T{stat.tier}.asset";
            content.Pickaxes[stat.tier] = EnsureTool(path, $"tool_pickaxe_t{stat.tier}", $"mvp.tool.pickaxe.t{stat.tier}.name", $"mvp.tool.pickaxe.t{stat.tier}.desc", ToolType.Pickaxe, stat.tier, stat.attack, stat.range, stat.cooldown, -1, stat.sell, "Pickaxe", CopyAppearance(T1PickaxePath));
        }

        var scytheStats = new[]
        {
            (tier: 2, attack: 8f, range: 1.5f, cooldown: 0.62f, sell: 220),
            (tier: 3, attack: 14f, range: 1.7f, cooldown: 0.54f, sell: 520),
            (tier: 4, attack: 24f, range: 1.9f, cooldown: 0.46f, sell: 1050),
            (tier: 5, attack: 38f, range: 2.2f, cooldown: 0.38f, sell: 2300)
        };

        foreach (var stat in scytheStats)
        {
            string path = $"{ToolFolder}/Scythe_T{stat.tier}.asset";
            content.Scythes[stat.tier] = EnsureTool(path, $"tool_scythe_t{stat.tier}", $"mvp.tool.scythe.t{stat.tier}.name", $"mvp.tool.scythe.t{stat.tier}.desc", ToolType.Scythe, stat.tier, stat.attack, stat.range, stat.cooldown, -1, stat.sell, "Harvert", CopyAppearance(T1ScythePath));
        }

        content.Fertilizers[1] = EnsureTool(
            $"{ToolFolder}/Fertilizer_T1.asset",
            "tool_fertilizer_t1",
            "mvp.tool.fertilizer.t1.name",
            "mvp.tool.fertilizer.t1.desc",
            ToolType.Fertilizer,
            1,
            0f,
            1f,
            0.5f,
            120,
            45,
            "Use",
            CopyAppearance(T1HoePath));

        for (int tier = 1; tier <= 5; tier++)
            content.GearByTier[tier] = EnsureGearSet(tier);

        EnsureRecipes(content);
        EnsureQuests(content);
        return content;
    }

    private static CropContent EnsureCrop(CropSpec spec, IReadOnlyList<Sprite> stageSprites)
    {
        string cropPath = $"{CropFolder}/Crop_T{spec.Tier}_{spec.NamePascal}.asset";
        var cropItem = LoadOrCreateEntity(cropPath);
        cropItem.id = $"crop_t{spec.Tier}_{spec.IdSuffix}";
        cropItem.keyName = $"mvp.crop.t{spec.Tier}.{spec.IdSuffix}.name";
        cropItem.descKey = $"mvp.crop.t{spec.Tier}.{spec.IdSuffix}.desc";
        cropItem.category = ItemCategory.Crop;
        cropItem.maxStack = 999;
        cropItem.buyPrice = -1;
        cropItem.sellPrice = spec.SellPrice;
        cropItem.icon = stageSprites.LastOrDefault();
        SetStats(cropItem);
        cropItem.modules = new List<IModuleData>();
        EditorUtility.SetDirty(cropItem);

        string seedPath = $"{PlantFolder}/Seed_T{spec.Tier}_{spec.NamePascal}.asset";
        var seed = LoadOrCreateEntity(seedPath);
        seed.id = $"seed_t{spec.Tier}_{spec.IdSuffix}";
        seed.keyName = $"mvp.seed.t{spec.Tier}.{spec.IdSuffix}.name";
        seed.descKey = $"mvp.seed.t{spec.Tier}.{spec.IdSuffix}.desc";
        seed.category = ItemCategory.Seed;
        seed.maxStack = 999;
        seed.buyPrice = spec.SeedBuyPrice;
        seed.sellPrice = -1;
        seed.icon = stageSprites.FirstOrDefault();
        seed.placementRule = new PlacementRule
        {
            occupyLayer = EntityLayer.Plant,
            requireTags = PlacementTag.Plantable,
            provideTags = PlacementTag.None,
            blockLayers = Array.Empty<EntityLayer>()
        };
        SetStats(seed,
            Stat(StatType.MaxHp, Mathf.Max(5, 3 + spec.Tier * 4)),
            Stat(StatType.Hp, Mathf.Max(5, 3 + spec.Tier * 4)));
        seed.modules = new List<IModuleData>
        {
            new PlacementModule
            {
                objectTypeToSpawn = spec.Regrowable ? ObjectType.Plant02 : ObjectType.Plant01,
                centerTile = true,
                animTrigger = "PutDown"
            },
            new StageModule
            {
                stages = CreateGrowthStages(spec.GrowthDays, stageSprites),
                regrowStageIndex = spec.Regrowable ? 2 : -1
            },
            new HarvestModule { harvestTool = ToolType.Scythe, wrongToolPenalty = 0.3f },
            new HealthModule { canTakeDamage = true },
            new DropModule
            {
                harvestDrops = new[]
                {
                    new DropEntry { item = cropItem, minAmount = 1, maxAmount = 1, dropChance = 1f }
                }
            },
            new ExpRewardModule { rewardExp = spec.ExpReward, sourceType = ExpSourceType.Harvest, requireKiller = true },
            new MortalModule()
        };
        EditorUtility.SetDirty(seed);

        return new CropContent(spec, seed, cropItem);
    }

    private static OreContent EnsureOre(OreSpec spec)
    {
        string materialPath = $"{MaterialFolder}/Ore_T{spec.Tier}_{spec.NamePascal}.asset";
        var material = LoadOrCreateEntity(materialPath);
        material.id = $"ore_t{spec.Tier}_{spec.IdSuffix}";
        material.keyName = $"mvp.ore.t{spec.Tier}.{spec.IdSuffix}.name";
        material.descKey = $"mvp.ore.t{spec.Tier}.{spec.IdSuffix}.desc";
        material.category = ItemCategory.Material;
        material.maxStack = 999;
        material.buyPrice = -1;
        material.sellPrice = spec.SellPrice;
        SetStats(material);
        material.modules = new List<IModuleData>();
        EditorUtility.SetDirty(material);

        string nodePath = $"{ResourceFolder}/OreNode_T{spec.Tier}_{spec.NamePascal}.asset";
        var node = LoadOrCreateEntity(nodePath);
        node.id = $"ore_node_t{spec.Tier}_{spec.IdSuffix}";
        node.keyName = $"mvp.ore_node.t{spec.Tier}.{spec.IdSuffix}.name";
        node.descKey = $"mvp.ore_node.t{spec.Tier}.{spec.IdSuffix}.desc";
        node.category = ItemCategory.Placeable;
        node.maxStack = 1;
        node.buyPrice = -1;
        node.sellPrice = 0;
        node.placementRule = new PlacementRule
        {
            occupyLayer = EntityLayer.Furniture,
            requireTags = PlacementTag.None,
            provideTags = PlacementTag.None,
            blockLayers = Array.Empty<EntityLayer>()
        };
        SetStats(node, Stat(StatType.MaxHp, spec.NodeHp), Stat(StatType.Hp, spec.NodeHp));
        node.modules = new List<IModuleData>
        {
            new HarvestModule { harvestTool = ToolType.Pickaxe, wrongToolPenalty = 0.25f },
            new HealthModule { canTakeDamage = true },
            new DropModule
            {
                harvestDrops = new[]
                {
                    new DropEntry { item = material, minAmount = 1, maxAmount = 3, dropChance = 1f }
                }
            },
            new ExpRewardModule { rewardExp = spec.ExpReward, sourceType = ExpSourceType.Mine, requireKiller = true },
            new MortalModule()
        };
        EditorUtility.SetDirty(node);

        return new OreContent(spec, material, node);
    }

    private static EnemyContent EnsureEnemy(EnemySpec spec)
    {
        string materialPath = $"{MaterialFolder}/Monster_T{spec.Tier}_{spec.NamePascal}.asset";
        var material = LoadOrCreateEntity(materialPath);
        material.id = $"monster_t{spec.Tier}_{spec.IdSuffix}";
        material.keyName = $"mvp.monster.t{spec.Tier}.{spec.IdSuffix}.name";
        material.descKey = $"mvp.monster.t{spec.Tier}.{spec.IdSuffix}.desc";
        material.category = ItemCategory.Material;
        material.maxStack = 999;
        material.buyPrice = -1;
        material.sellPrice = spec.MaterialSellPrice;
        SetStats(material);
        material.modules = new List<IModuleData>();
        EditorUtility.SetDirty(material);

        string enemyPath = $"{EnemyFolder}/Enemy_T{spec.Tier}_{spec.NamePascal}.asset";
        var enemy = LoadOrCreateEntity(enemyPath);
        enemy.id = $"enemy_t{spec.Tier}_{spec.IdSuffix}";
        enemy.keyName = $"mvp.enemy.t{spec.Tier}.{spec.IdSuffix}.name";
        enemy.descKey = $"mvp.enemy.t{spec.Tier}.{spec.IdSuffix}.desc";
        enemy.category = ItemCategory.None;
        enemy.maxStack = 1;
        enemy.buyPrice = -1;
        enemy.sellPrice = 0;
        enemy.placementRule = new PlacementRule
        {
            occupyLayer = EntityLayer.Ground,
            requireTags = PlacementTag.None,
            provideTags = PlacementTag.None,
            blockLayers = Array.Empty<EntityLayer>()
        };
        SetStats(enemy,
            Stat(StatType.MaxHp, spec.Hp),
            Stat(StatType.Hp, spec.Hp),
            Stat(StatType.Attack, spec.Attack),
            Stat(StatType.Defense, spec.Defense),
            Stat(StatType.Speed, 1.6f + spec.Tier * 0.08f));
        enemy.modules = new List<IModuleData>
        {
            new HealthModule { canTakeDamage = true },
            new DropModule
            {
                harvestDrops = new[]
                {
                    new DropEntry { item = material, minAmount = 1, maxAmount = 2, dropChance = 0.65f }
                }
            },
            new ExpRewardModule { rewardExp = spec.ExpReward, sourceType = ExpSourceType.Combat, requireKiller = true },
            new RespawnModule
            {
                defaultRespawnPosition = Vector2.zero,
                respawnDelay = Mathf.Max(20f, spec.RespawnMinutes * 0.25f),
                restoreFullHp = true,
                respawnPrefabId = ObjectType.Enemy01
            }
        };
        EditorUtility.SetDirty(enemy);

        return new EnemyContent(spec, material, enemy);
    }

    private static EntityData EnsureTool(
        string path,
        string id,
        string keyName,
        string descKey,
        ToolType toolType,
        int tier,
        float attack,
        float range,
        float cooldown,
        int buyPrice,
        int sellPrice,
        string animTrigger,
        AppearanceCopy appearanceCopy)
    {
        var tool = LoadOrCreateEntity(path);
        tool.id = id;
        tool.keyName = keyName;
        tool.descKey = descKey;
        tool.category = ItemCategory.Tool;
        tool.maxStack = 1;
        tool.buyPrice = buyPrice;
        tool.sellPrice = sellPrice;
        tool.placementRule = EmptyGroundRule();
        SetStats(tool,
            Stat(StatType.Attack, attack),
            Stat(StatType.Range, range),
            Stat(StatType.CoolDown, cooldown));
        tool.modules = new List<IModuleData>
        {
            new ToolModule { toolType = toolType, animTrigger = animTrigger },
            new AppearanceModule
            {
                spriteId = string.IsNullOrWhiteSpace(appearanceCopy.SpriteId) ? $"MVP.Tool.{toolType}.T{tier}" : appearanceCopy.SpriteId,
                equipmentPart = appearanceCopy.Part == default ? EquipmentPart.MeleeWeapon1H : appearanceCopy.Part
            }
        };
        if (tool.icon == null)
            tool.icon = AssetDatabase.LoadAssetAtPath<EntityData>(path)?.icon;

        EditorUtility.SetDirty(tool);
        return tool;
    }

    private static EntityData EnsureSprinkler(
        int tier,
        int radius,
        string itemPath,
        string placedPath,
        string itemId,
        string placedId,
        string itemKey,
        string itemDesc,
        string placedKey,
        string placedDesc,
        ObjectType placementType,
        int buyPrice,
        int sellPrice,
        AppearanceCopy appearanceCopy)
    {
        var placed = LoadOrCreateEntity(placedPath);
        placed.id = placedId;
        placed.keyName = placedKey;
        placed.descKey = placedDesc;
        placed.category = ItemCategory.Placeable;
        placed.maxStack = 1;
        placed.buyPrice = -1;
        placed.sellPrice = 0;
        placed.placementRule = new PlacementRule
        {
            occupyLayer = EntityLayer.Furniture,
            requireTags = PlacementTag.None,
            provideTags = PlacementTag.None,
            blockLayers = Array.Empty<EntityLayer>()
        };
        placed.modules = new List<IModuleData>
        {
            new SprinklerModule { waterRadius = Mathf.Max(1, radius) }
        };
        SetStats(placed);
        EditorUtility.SetDirty(placed);

        var item = LoadOrCreateEntity(itemPath);
        item.id = itemId;
        item.keyName = itemKey;
        item.descKey = itemDesc;
        item.category = ItemCategory.Placeable;
        item.maxStack = 1;
        item.buyPrice = buyPrice;
        item.sellPrice = sellPrice;
        item.placementRule = EmptyGroundRule();
        SetStats(item);
        item.modules = new List<IModuleData>
        {
            new PlacementModule
            {
                objectTypeToSpawn = placementType,
                placedEntityData = placed,
                centerTile = true,
                animTrigger = "PutDown"
            },
            new AppearanceModule
            {
                spriteId = string.IsNullOrWhiteSpace(appearanceCopy.SpriteId) ? $"MVP.Tool.Sprinkler.T{tier}" : appearanceCopy.SpriteId,
                equipmentPart = appearanceCopy.Part == default ? EquipmentPart.MeleeWeapon1H : appearanceCopy.Part
            }
        };
        EditorUtility.SetDirty(item);
        return item;
    }

    private static GearSetContent EnsureGearSet(int tier)
    {
        var set = new GearSetContent(tier);
        var armorAppearance = CopyAppearance("Assets/Project/ScriptableObjects/Items/Equipment/Armors/Arrmor01.asset");
        var helmetAppearance = CopyAppearance("Assets/Project/ScriptableObjects/Items/Equipment/Helmet/Helmet02.asset");
        var bracerAppearance = CopyAppearance("Assets/Project/ScriptableObjects/Items/Equipment/Bracers/bracer01.asset");
        var leggingAppearance = CopyAppearance("Assets/Project/ScriptableObjects/Items/Equipment/Leggings/legging01.asset");

        set.Armor = EnsureGearPiece(tier, "armor", "Armor", EquipmentPart.Armor, armorAppearance, GearStats(tier, "armor"));
        set.Helmet = EnsureGearPiece(tier, "helmet", "Helmet", EquipmentPart.Helmet, helmetAppearance, GearStats(tier, "helmet"));
        set.Bracer = EnsureGearPiece(tier, "bracer", "Bracer", EquipmentPart.Bracers, bracerAppearance, GearStats(tier, "bracer"));
        set.Legging = EnsureGearPiece(tier, "legging", "Legging", EquipmentPart.Leggings, leggingAppearance, GearStats(tier, "legging"));
        return set;
    }

    private static EntityData EnsureGearPiece(int tier, string idSuffix, string nameSuffix, EquipmentPart part, AppearanceCopy appearanceCopy, StatEntry[] stats)
    {
        string path = $"{GearFolder}/Gear_T{tier}_{nameSuffix}.asset";
        var gear = LoadOrCreateEntity(path);
        gear.id = $"gear_t{tier}_{idSuffix}";
        gear.keyName = $"mvp.gear.t{tier}.{idSuffix}.name";
        gear.descKey = $"mvp.gear.t{tier}.{idSuffix}.desc";
        gear.category = ItemCategory.Armor;
        gear.maxStack = 1;
        gear.buyPrice = tier == 1 ? 80 + tier * 30 : -1;
        gear.sellPrice = 20 + tier * 35;
        gear.placementRule = EmptyGroundRule();
        SetStats(gear, stats);
        gear.modules = new List<IModuleData>
        {
            new AppearanceModule
            {
                spriteId = string.IsNullOrWhiteSpace(appearanceCopy.SpriteId) ? $"MVP.Gear.T{tier}.{nameSuffix}" : appearanceCopy.SpriteId,
                equipmentPart = appearanceCopy.Part == default ? part : appearanceCopy.Part
            }
        };
        EditorUtility.SetDirty(gear);
        return gear;
    }

    private static StatEntry[] GearStats(int tier, string slot)
    {
        return (tier, slot) switch
        {
            (1, "armor") => Stats(Stat(StatType.MaxHp, 8), Stat(StatType.Defense, 1)),
            (1, "helmet") => Stats(Stat(StatType.MaxHp, 5)),
            (1, "bracer") => Stats(Stat(StatType.MaxHp, 3)),
            (1, "legging") => Stats(Stat(StatType.MaxHp, 4)),
            (2, "armor") => Stats(Stat(StatType.MaxHp, 25), Stat(StatType.Defense, 1)),
            (2, "helmet") => Stats(Stat(StatType.MaxHp, 15), Stat(StatType.Defense, 1)),
            (2, "bracer") => Stats(Stat(StatType.MaxHp, 10), Stat(StatType.Attack, 1)),
            (2, "legging") => Stats(Stat(StatType.MaxHp, 10), Stat(StatType.Defense, 1)),
            (3, "armor") => Stats(Stat(StatType.MaxHp, 55), Stat(StatType.Defense, 3)),
            (3, "helmet") => Stats(Stat(StatType.MaxHp, 35), Stat(StatType.Defense, 2)),
            (3, "bracer") => Stats(Stat(StatType.MaxHp, 25), Stat(StatType.Attack, 3)),
            (3, "legging") => Stats(Stat(StatType.MaxHp, 25), Stat(StatType.Defense, 2)),
            (4, "armor") => Stats(Stat(StatType.MaxHp, 110), Stat(StatType.Defense, 6)),
            (4, "helmet") => Stats(Stat(StatType.MaxHp, 70), Stat(StatType.Defense, 4)),
            (4, "bracer") => Stats(Stat(StatType.MaxHp, 50), Stat(StatType.Attack, 6)),
            (4, "legging") => Stats(Stat(StatType.MaxHp, 50), Stat(StatType.Defense, 4)),
            (5, "armor") => Stats(Stat(StatType.MaxHp, 200), Stat(StatType.Defense, 10)),
            (5, "helmet") => Stats(Stat(StatType.MaxHp, 125), Stat(StatType.Defense, 7)),
            (5, "bracer") => Stats(Stat(StatType.MaxHp, 75), Stat(StatType.Attack, 10)),
            (5, "legging") => Stats(Stat(StatType.MaxHp, 100), Stat(StatType.Defense, 8)),
            _ => Stats(Stat(StatType.MaxHp, 1))
        };
    }

    private static void EnsureRecipes(CoreplayContent content)
    {
        var craftExpByTier = new Dictionary<int, int> { { 2, 80 }, { 3, 180 }, { 4, 350 }, { 5, 650 } };

        for (int tier = 2; tier <= 5; tier++)
        {
            var ore = content.Ores[tier - 1];
            var monster = content.Enemies[tier - 1];
            content.Recipes.Add(EnsureRecipe(
                $"{RecipeFolder}/Recipe_Pickaxe_T{tier}.asset",
                $"recipe.pickaxe.t{tier}",
                $"mvp.recipe.pickaxe.t{tier}.name",
                tier == 2 ? 10 : (tier - 1) * 10,
                craftExpByTier[tier],
                new[]
                {
                    Ingredient(content.Pickaxes[tier - 1], 1),
                    Ingredient(ore.Material, 10 + tier * 4),
                    Ingredient(monster.Material, 3 + tier)
                },
                new[] { Ingredient(content.Pickaxes[tier], 1) }));

            content.Recipes.Add(EnsureRecipe(
                $"{RecipeFolder}/Recipe_Scythe_T{tier}.asset",
                $"recipe.scythe.t{tier}",
                $"mvp.recipe.scythe.t{tier}.name",
                tier == 2 ? 10 : (tier - 1) * 10,
                craftExpByTier[tier],
                new[]
                {
                    Ingredient(content.Scythes[tier - 1], 1),
                    Ingredient(ore.Material, 8 + tier * 3),
                    Ingredient(monster.Material, 4 + tier)
                },
                new[] { Ingredient(content.Scythes[tier], 1) }));

            var gearSet = content.GearByTier[tier];
            content.Recipes.Add(EnsureRecipe(
                $"{RecipeFolder}/Recipe_GearSet_T{tier}.asset",
                $"recipe.gear_set.t{tier}",
                $"mvp.recipe.gear_set.t{tier}.name",
                tier == 2 ? 10 : (tier - 1) * 10,
                craftExpByTier[tier],
                new[]
                {
                    Ingredient(content.GearByTier[tier - 1].Armor, 1),
                    Ingredient(content.GearByTier[tier - 1].Helmet, 1),
                    Ingredient(content.GearByTier[tier - 1].Bracer, 1),
                    Ingredient(content.GearByTier[tier - 1].Legging, 1),
                    Ingredient(ore.Material, 14 + tier * 5),
                    Ingredient(monster.Material, 6 + tier * 2)
                },
                new[]
                {
                    Ingredient(gearSet.Armor, 1),
                    Ingredient(gearSet.Helmet, 1),
                    Ingredient(gearSet.Bracer, 1),
                    Ingredient(gearSet.Legging, 1)
                }));
        }

        content.Recipes.Add(EnsureRecipe(
            $"{RecipeFolder}/Recipe_Fertilizer_T1.asset",
            "recipe.fertilizer.t1",
            "mvp.recipe.fertilizer.t1.name",
            5,
            25,
            new[]
            {
                Ingredient(content.Crops[0].CropItem, 2),
                Ingredient(content.Ores[0].Material, 2)
            },
            new[] { Ingredient(content.Fertilizers[1], 3) }));

        content.Recipes.Add(EnsureRecipe(
            $"{RecipeFolder}/Recipe_Sprinkler_T1.asset",
            "recipe.sprinkler.t1",
            "mvp.recipe.sprinkler.t1.name",
            10,
            45,
            new[]
            {
                Ingredient(content.Ores[0].Material, 4),
                Ingredient(content.Ores[1].Material, 2),
                Ingredient(content.Fertilizers[1], 1)
            },
            new[] { Ingredient(content.Sprinklers[1], 1) }));

        content.Recipes.Add(EnsureRecipe(
            $"{RecipeFolder}/Recipe_Sprinkler_T2.asset",
            "recipe.sprinkler.t2",
            "mvp.recipe.sprinkler.t2.name",
            20,
            80,
            new[]
            {
                Ingredient(content.Ores[2].Material, 4),
                Ingredient(content.Ores[3].Material, 2),
                Ingredient(content.Crops[2].CropItem, 2)
            },
            new[] { Ingredient(content.Sprinklers[2], 1) }));
    }

    private static RecipeData EnsureRecipe(
        string path,
        string id,
        string titleKey,
        int requiredLevel,
        int craftExp,
        IEnumerable<RecipeIngredient> ingredients,
        IEnumerable<RecipeIngredient> outputs)
    {
        var recipe = LoadOrCreateAsset<RecipeData>(path);
        recipe.id = id;
        recipe.titleKey = titleKey;
        recipe.requiredLevel = requiredLevel;
        recipe.craftExp = craftExp;
        recipe.ingredients = ingredients.Where(i => i?.item != null && i.amount > 0).ToList();
        recipe.outputs = outputs.Where(i => i?.item != null && i.amount > 0).ToList();
        EditorUtility.SetDirty(recipe);
        return recipe;
    }

    private static void EnsureQuests(CoreplayContent content)
    {
        int[] cropMoney = { 150, 300, 650, 1200, 2200 };
        int[] oreMoney = { 250, 650, 1300, 2600, 5200 };
        int[] mixedMoney = { 700, 1600, 3200, 6500, 10000 };
        int[] cropExp = { 150, 300, 500, 800, 1200 };
        int[] oreExp = { 600, 1500, 3000, 5500, 9000 };
        int[] mixedExp = { 800, 2500, 6000, 12000, 22000 };

        for (int i = 0; i < content.Crops.Count; i++)
        {
            int tier = i + 1;
            var crop = content.Crops[i];
            content.Quests.Add(EnsureQuest(
                $"{QuestFolder}/Quest_M5_Crop_T{tier}.asset",
                $"quest.m5.crop.t{tier}",
                $"mvp.quest.crop.t{tier}.title",
                $"mvp.quest.crop.t{tier}.desc",
                cropMoney[i],
                cropExp[i],
                Objective($"obj.crop.t{tier}", $"mvp.quest.obj.crop.t{tier}", crop.CropItem.id, 5 + tier * 2)));
        }

        for (int i = 0; i < content.Ores.Count; i++)
        {
            int tier = i + 1;
            var ore = content.Ores[i];
            content.Quests.Add(EnsureQuest(
                $"{QuestFolder}/Quest_M5_Ore_T{tier}.asset",
                $"quest.m5.ore.t{tier}",
                $"mvp.quest.ore.t{tier}.title",
                $"mvp.quest.ore.t{tier}.desc",
                oreMoney[i],
                oreExp[i],
                Objective($"obj.ore.t{tier}", $"mvp.quest.obj.ore.t{tier}", ore.Material.id, 8 + tier * 3)));
        }

        for (int i = 0; i < content.Enemies.Count; i++)
        {
            int tier = i + 1;
            var enemy = content.Enemies[i];
            var ore = content.Ores[i];
            content.Quests.Add(EnsureQuest(
                $"{QuestFolder}/Quest_M5_Mixed_T{tier}.asset",
                $"quest.m5.mixed.t{tier}",
                $"mvp.quest.mixed.t{tier}.title",
                $"mvp.quest.mixed.t{tier}.desc",
                mixedMoney[i],
                mixedExp[i],
                Objective($"obj.monster.t{tier}", $"mvp.quest.obj.monster.t{tier}", enemy.Material.id, 4 + tier * 2),
                Objective($"obj.mixed_ore.t{tier}", $"mvp.quest.obj.mixed_ore.t{tier}", ore.Material.id, 6 + tier * 4)));
        }
    }

    private static QuestGraphData EnsureQuest(
        string path,
        string id,
        string titleKey,
        string descriptionKey,
        int rewardMoney,
        int rewardExp,
        params QuestObjectiveData[] objectives)
    {
        var quest = LoadOrCreateAsset<QuestGraphData>(path);
        quest.id = id;
        quest.titleKey = titleKey;
        quest.descriptionKey = descriptionKey;
        quest.offerOptionKey = "ui.quest.accept";
        quest.inProgressOptionKey = "ui.quest.view";
        quest.completeOptionKey = "ui.quest.complete";
        quest.completedOptionKey = "ui.quest.completed";
        quest.rewardMoney = rewardMoney;
        quest.rewardExp = rewardExp;
        quest.objectives = objectives.Where(o => o != null).ToList();
        quest.rewardItems = new List<QuestRewardItemData>();
        EditorUtility.SetDirty(quest);
        return quest;
    }

    private static void ConfigurePlayer()
    {
        var player = AssetDatabase.LoadAssetAtPath<EntityData>(PlayerPath);
        if (player == null) return;

        player.baseStats ??= new StatsData();
        player.baseStats.baseStats ??= new List<StatEntry>();
        SetOrAddStat(player.baseStats.baseStats, StatType.Level, 1f);
        SetOrAddStat(player.baseStats.baseStats, StatType.Exp, 0f);
        SetOrAddStat(player.baseStats.baseStats, StatType.MaxExp, ProgressionService.RequiredExp(1));
        SetOrAddStat(player.baseStats.baseStats, StatType.Money, Mathf.Max(500f, FindStat(player, StatType.Money, 500f)));
        EnsureModule<QuestLogModule>(player);
        EditorUtility.SetDirty(player);
    }

    private static void ConfigureShopNpc(CoreplayContent content)
    {
        var npc = AssetDatabase.LoadAssetAtPath<EntityData>(NpcShopPath);
        if (npc == null) return;

        npc.baseStats ??= new StatsData();
        npc.baseStats.baseStats ??= new List<StatEntry>();
        SetOrAddStat(npc.baseStats.baseStats, StatType.Money, 999999f);

        EnsureModule<InventoryModule>(npc).size = 60;
        var shop = EnsureModule<ShopModule>(npc);
        shop.optionTextKey = "ui.shop.open";
        shop.priority = 30;
        shop.sellsToPlayer = true;
        shop.buysFromPlayer = true;
        shop.buysAllItems = true;
        shop.infiniteStock = true;
        shop.stockInventoryType = InventoryType.Backpack;
        shop.initialStock = new List<ShopStockEntry>();

        foreach (var crop in content.Crops)
            shop.initialStock.Add(new ShopStockEntry { itemData = crop.Seed, amount = 10, requiredLevel = crop.Spec.RequiredLevel });
        shop.initialStock.Add(new ShopStockEntry { itemData = content.Fertilizers[1], amount = 10, requiredLevel = 5 });
        shop.initialStock.Add(new ShopStockEntry { itemData = content.Sprinklers[1], amount = 5, requiredLevel = 10 });
        shop.initialStock.Add(new ShopStockEntry { itemData = content.Sprinklers[2], amount = 3, requiredLevel = 20 });

        AddShopItem(shop, content.BasicHoe, 1, 1);
        AddShopItem(shop, content.BasicAxe, 1, 1);
        AddShopItem(shop, content.Pickaxes[1], 1, 1);
        AddShopItem(shop, content.Scythes[1], 1, 1);
        foreach (var item in content.GearByTier[1].AllItems())
            AddShopItem(shop, item, 1, 1);

        shop.buyWhitelist = new List<EntityData>();
        shop.buyWhitelist.AddRange(content.Crops.Select(c => c.CropItem));
        shop.buyWhitelist.AddRange(content.Ores.Select(o => o.Material));
        shop.buyWhitelist.AddRange(content.Enemies.Select(e => e.Material));
        shop.buyWhitelist.Add(content.Fertilizers[1]);
        shop.buyWhitelist.Add(content.Sprinklers[1]);
        shop.buyWhitelist.Add(content.Sprinklers[2]);
        RemoveModule<QuestModule>(npc);

        EditorUtility.SetDirty(npc);
    }

    private static void ConfigureCraftingNpc(CoreplayContent content)
    {
        var npc = AssetDatabase.LoadAssetAtPath<EntityData>(NpcCraftPath);
        if (npc == null) return;

        npc.baseStats ??= new StatsData();
        npc.baseStats.baseStats ??= new List<StatEntry>();
        SetOrAddStat(npc.baseStats.baseStats, StatType.Money, 999999f);

        EnsureModule<InventoryModule>(npc).size = 60;
        var crafting = EnsureModule<CraftingModule>(npc);
        crafting.optionTextKey = "ui.crafting.open";
        crafting.priority = 35;
        crafting.recipes = content.Recipes;
        RemoveModule<ShopModule>(npc);
        RemoveModule<QuestModule>(npc);
        EditorUtility.SetDirty(npc);
    }

    private static void ConfigureQuestNpc(CoreplayContent content)
    {
        var npc = AssetDatabase.LoadAssetAtPath<EntityData>(NpcEventPath);
        if (npc == null) return;

        npc.baseStats ??= new StatsData();
        npc.baseStats.baseStats ??= new List<StatEntry>();
        SetOrAddStat(npc.baseStats.baseStats, StatType.Money, 999999f);

        EnsureModule<InventoryModule>(npc).size = 60;
        var quest = EnsureModule<QuestModule>(npc);
        quest.priority = 20;
        quest.quests = content.Quests;
        RemoveModule<ShopModule>(npc);
        EditorUtility.SetDirty(npc);
    }

    private static void CreateMarkerTiles(CoreplayContent content)
    {
        CreateMarker("Marker_NPC_Shop", SceneMarkerKind.Npc, ObjectType.NPCShop01, AssetDatabase.LoadAssetAtPath<EntityData>(NpcShopPath), SceneEntitySavePolicy.Persistent, "town_shop", 0, 1, Color.green);
        CreateMarker("Marker_NPC_Crafting", SceneMarkerKind.CraftingStation, ObjectType.NPCCrafting01, AssetDatabase.LoadAssetAtPath<EntityData>(NpcCraftPath), SceneEntitySavePolicy.Persistent, "town_crafting", 0, 1, Color.cyan);
        CreateMarker("Marker_NPC_Quest", SceneMarkerKind.Npc, ObjectType.NPCEvent01, AssetDatabase.LoadAssetAtPath<EntityData>(NpcEventPath), SceneEntitySavePolicy.Persistent, "town_quest", 0, 1, Color.yellow);

        foreach (var ore in content.Ores)
        {
            var color = TierColors[Mathf.Clamp(ore.Spec.Tier - 1, 0, TierColors.Length - 1)];
            CreateMarker($"Marker_Ore_T{ore.Spec.Tier}_{ore.Spec.NamePascal}", SceneMarkerKind.Ore, ObjectType.OreNode01, ore.Node, SceneEntitySavePolicy.Regenerating, $"mine_t{ore.Spec.Tier}", ore.Spec.RespawnMinutes, 1, color);
        }

        foreach (var enemy in content.Enemies)
        {
            var color = TierColors[Mathf.Clamp(enemy.Spec.Tier - 1, 0, TierColors.Length - 1)];
            CreateMarker($"Marker_Enemy_T{enemy.Spec.Tier}_{enemy.Spec.NamePascal}", SceneMarkerKind.Enemy, ObjectType.Enemy01, enemy.Entity, SceneEntitySavePolicy.Regenerating, $"mine_t{enemy.Spec.Tier}", enemy.Spec.RespawnMinutes, 1, color);
        }
    }

    private static SceneSpawnTile CreateMarker(
        string name,
        SceneMarkerKind markerKind,
        ObjectType objectType,
        EntityData entityData,
        SceneEntitySavePolicy savePolicy,
        string spawnGroupId,
        int respawnMinutes,
        int initialAmount,
        Color color)
    {
        var tile = LoadOrCreateAsset<SceneSpawnTile>($"{MarkerFolder}/{name}.asset");
        tile.name = name;
        tile.markerKind = markerKind;
        tile.objectType = objectType;
        tile.entityData = entityData;
        tile.savePolicy = savePolicy;
        tile.spawnGroupId = spawnGroupId;
        tile.respawnMinutes = Mathf.Max(0, respawnMinutes);
        tile.initialAmount = Mathf.Max(1, initialAmount);
        tile.bypassPlacementValidation = true;
        tile.editorSprite = entityData != null ? entityData.icon : null;
        tile.editorColor = color;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static void StampSampleSceneMarkers()
    {
        if (!System.IO.File.Exists(SampleScenePath))
        {
            Debug.LogWarning($"[BootstrapCoreplayM5Content] Sample scene not found: {SampleScenePath}");
            return;
        }

        var scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
        var markerMap = EnsureRuntimeMarkerTilemap();
        if (markerMap == null)
        {
            Debug.LogWarning("[BootstrapCoreplayM5Content] Cannot stamp markers because Tm_RuntimeMarkers is missing.");
            return;
        }

        SetTile(markerMap, new Vector3Int(-8, 2, 0), "Marker_NPC_Shop");
        SetTile(markerMap, new Vector3Int(-6, 2, 0), "Marker_NPC_Crafting");
        SetTile(markerMap, new Vector3Int(-4, 2, 0), "Marker_NPC_Quest");

        for (int tier = 1; tier <= 5; tier++)
        {
            int baseX = -10 + (tier - 1) * 5;
            SetTile(markerMap, new Vector3Int(baseX, -6, 0), $"Marker_Ore_T{tier}_{Ores[tier - 1].NamePascal}");
            SetTile(markerMap, new Vector3Int(baseX + 1, -6, 0), $"Marker_Ore_T{tier}_{Ores[tier - 1].NamePascal}");
            SetTile(markerMap, new Vector3Int(baseX + 2, -6, 0), $"Marker_Ore_T{tier}_{Ores[tier - 1].NamePascal}");
            SetTile(markerMap, new Vector3Int(baseX, -8, 0), $"Marker_Enemy_T{tier}_{Enemies[tier - 1].NamePascal}");
            SetTile(markerMap, new Vector3Int(baseX + 2, -8, 0), $"Marker_Enemy_T{tier}_{Enemies[tier - 1].NamePascal}");
        }

        EnsureSceneMarkerComponents(markerMap);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static Tilemap EnsureRuntimeMarkerTilemap()
    {
        var marker = FindTilemap(SceneContext.RuntimeMarkersTilemapName);
        if (marker != null) return marker;

        var grid = UnityEngine.Object.FindAnyObjectByType<Grid>();
        if (grid == null)
        {
            var gridGo = new GameObject("Grid");
            grid = gridGo.AddComponent<Grid>();
        }

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
        {
            var go = new GameObject("SceneContext");
            context = go.AddComponent<SceneContext>();
        }

        context.AutoBind();
        if (context.GetComponent<SceneContentScanner>() == null)
            context.gameObject.AddComponent<SceneContentScanner>();

        EditorUtility.SetDirty(context);
        EditorUtility.SetDirty(markerMap);
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

    private static void SetTile(Tilemap markerMap, Vector3Int cell, string markerName)
    {
        var tile = AssetDatabase.LoadAssetAtPath<SceneSpawnTile>($"{MarkerFolder}/{markerName}.asset");
        if (tile == null)
        {
            Debug.LogWarning($"[BootstrapCoreplayM5Content] Marker tile missing: {markerName}");
            return;
        }

        markerMap.SetTile(cell, tile);
    }

    private static void EnsureDirectories()
    {
        EnsureFolder(CropFolder);
        EnsureFolder(PlantFolder);
        EnsureFolder(MaterialFolder);
        EnsureFolder(ResourceFolder);
        EnsureFolder(EnemyFolder);
        EnsureFolder(ToolFolder);
        EnsureFolder(GearFolder);
        EnsureFolder(RecipeFolder);
        EnsureFolder(QuestFolder);
        EnsureFolder(MarkerFolder);
        EnsureFolder(UtilityFolder);
    }

    private static EntityData LoadOrCreateEntity(string path) => LoadOrCreateAsset<EntityData>(path);

    private static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;

        EnsureFolder(System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/'));
        asset = ScriptableObject.CreateInstance<T>();
        asset.name = System.IO.Path.GetFileNameWithoutExtension(path);
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

    private static GrowthStage[] CreateGrowthStages(int growthDays, IReadOnlyList<Sprite> sprites)
    {
        int safeDays = Mathf.Max(1, growthDays);
        return new[]
        {
            new GrowthStage { sprite = SpriteAt(sprites, 0), daysToGrow = 1, canHarvest = false },
            new GrowthStage { sprite = SpriteAt(sprites, Mathf.Max(0, sprites.Count / 2)), daysToGrow = Mathf.Max(1, safeDays - 1), canHarvest = false },
            new GrowthStage { sprite = SpriteAt(sprites, sprites.Count - 1), daysToGrow = 999, canHarvest = true }
        };
    }

    private static List<Sprite> LoadStageSprites(string path)
    {
        var plant = AssetDatabase.LoadAssetAtPath<EntityData>(path);
        var stageModule = plant?.modules?.OfType<StageModule>().FirstOrDefault();
        if (stageModule?.stages == null || stageModule.stages.Length == 0)
            return new List<Sprite>();

        return stageModule.stages
            .Where(stage => stage != null && stage.sprite != null)
            .Select(stage => stage.sprite)
            .ToList();
    }

    private static Sprite SpriteAt(IReadOnlyList<Sprite> sprites, int index)
    {
        if (sprites == null || sprites.Count == 0) return null;
        return sprites[Mathf.Clamp(index, 0, sprites.Count - 1)];
    }

    private static AppearanceCopy CopyAppearance(string path)
    {
        var data = AssetDatabase.LoadAssetAtPath<EntityData>(path);
        var appearance = data?.modules?.OfType<AppearanceModule>().FirstOrDefault();
        if (appearance == null) return default;
        return new AppearanceCopy(appearance.spriteId, appearance.equipmentPart);
    }

    private static PlacementRule EmptyGroundRule()
    {
        return new PlacementRule
        {
            occupyLayer = EntityLayer.Ground,
            requireTags = PlacementTag.None,
            provideTags = PlacementTag.None,
            blockLayers = Array.Empty<EntityLayer>()
        };
    }

    private static QuestObjectiveData Objective(string id, string descriptionKey, string requiredEntityDataId, int amount)
    {
        return new QuestObjectiveData
        {
            id = id,
            descriptionKey = descriptionKey,
            requiredEntityDataId = requiredEntityDataId,
            requiredAmount = Mathf.Max(1, amount)
        };
    }

    private static RecipeIngredient Ingredient(EntityData item, int amount)
    {
        return new RecipeIngredient { item = item, amount = Mathf.Max(1, amount) };
    }

    private static StatEntry Stat(StatType type, float value) => new() { statType = type, value = value };
    private static StatEntry[] Stats(params StatEntry[] stats) => stats;

    private static void SetStats(EntityData data, params StatEntry[] stats)
    {
        data.baseStats ??= new StatsData();
        data.baseStats.baseStats = stats?.Where(s => s != null).ToList() ?? new List<StatEntry>();
    }

    private static void SetOrAddStat(List<StatEntry> stats, StatType statType, float value)
    {
        for (int i = 0; i < stats.Count; i++)
        {
            if (stats[i] == null) continue;
            if (stats[i].statType != statType) continue;
            stats[i].value = value;
            return;
        }

        stats.Add(new StatEntry { statType = statType, value = value });
    }

    private static float FindStat(EntityData data, StatType statType, float fallback)
    {
        if (data?.baseStats?.baseStats == null) return fallback;
        foreach (var stat in data.baseStats.baseStats)
        {
            if (stat != null && stat.statType == statType)
                return stat.value;
        }

        return fallback;
    }

    private static T EnsureModule<T>(EntityData data) where T : IModuleData
    {
        data.modules ??= new List<IModuleData>();

        var module = data.modules.OfType<T>().FirstOrDefault();
        if (module != null)
            return module;

        module = Activator.CreateInstance(typeof(T)) as T;
        if (module == null)
            throw new InvalidOperationException($"Cannot create module instance for '{typeof(T).Name}'.");

        data.modules.Add(module);
        return module;
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

    private static void AddShopItem(ShopModule shop, EntityData item, int amount, int requiredLevel)
    {
        if (shop == null || item == null) return;
        shop.initialStock.Add(new ShopStockEntry
        {
            itemData = item,
            amount = Mathf.Max(1, amount),
            requiredLevel = Mathf.Max(1, requiredLevel)
        });
    }

    private readonly struct AppearanceCopy
    {
        public readonly string SpriteId;
        public readonly EquipmentPart Part;

        public AppearanceCopy(string spriteId, EquipmentPart part)
        {
            SpriteId = spriteId;
            Part = part;
        }
    }

    private sealed class CoreplayContent
    {
        public readonly List<CropContent> Crops = new();
        public readonly List<OreContent> Ores = new();
        public readonly List<EnemyContent> Enemies = new();
        public readonly Dictionary<int, EntityData> Pickaxes = new();
        public readonly Dictionary<int, EntityData> Scythes = new();
        public readonly Dictionary<int, EntityData> Fertilizers = new();
        public readonly Dictionary<int, EntityData> Sprinklers = new();
        public readonly Dictionary<int, GearSetContent> GearByTier = new();
        public readonly List<RecipeData> Recipes = new();
        public readonly List<QuestGraphData> Quests = new();
        public EntityData BasicHoe;
        public EntityData BasicAxe;
    }

    private sealed class CropContent
    {
        public readonly CropSpec Spec;
        public readonly EntityData Seed;
        public readonly EntityData CropItem;

        public CropContent(CropSpec spec, EntityData seed, EntityData cropItem)
        {
            Spec = spec;
            Seed = seed;
            CropItem = cropItem;
        }
    }

    private sealed class OreContent
    {
        public readonly OreSpec Spec;
        public readonly EntityData Material;
        public readonly EntityData Node;

        public OreContent(OreSpec spec, EntityData material, EntityData node)
        {
            Spec = spec;
            Material = material;
            Node = node;
        }
    }

    private sealed class EnemyContent
    {
        public readonly EnemySpec Spec;
        public readonly EntityData Material;
        public readonly EntityData Entity;

        public EnemyContent(EnemySpec spec, EntityData material, EntityData entity)
        {
            Spec = spec;
            Material = material;
            Entity = entity;
        }
    }

    private sealed class GearSetContent
    {
        public readonly int Tier;
        public EntityData Armor;
        public EntityData Helmet;
        public EntityData Bracer;
        public EntityData Legging;

        public GearSetContent(int tier)
        {
            Tier = tier;
        }

        public IEnumerable<EntityData> AllItems()
        {
            yield return Armor;
            yield return Helmet;
            yield return Bracer;
            yield return Legging;
        }
    }

    private sealed class CropSpec
    {
        public readonly int Tier;
        public readonly string IdSuffix;
        public readonly string NamePascal;
        public readonly int SeedBuyPrice;
        public readonly int SellPrice;
        public readonly int GrowthDays;
        public readonly int ExpReward;
        public readonly int RequiredLevel;
        public readonly bool Regrowable;

        public CropSpec(int tier, string idSuffix, string namePascal, int seedBuyPrice, int sellPrice, int growthDays, int expReward, int requiredLevel, bool regrowable)
        {
            Tier = tier;
            IdSuffix = idSuffix;
            NamePascal = namePascal;
            SeedBuyPrice = seedBuyPrice;
            SellPrice = sellPrice;
            GrowthDays = growthDays;
            ExpReward = expReward;
            RequiredLevel = requiredLevel;
            Regrowable = regrowable;
        }
    }

    private sealed class OreSpec
    {
        public readonly int Tier;
        public readonly string IdSuffix;
        public readonly string NamePascal;
        public readonly int NodeHp;
        public readonly int SellPrice;
        public readonly int ExpReward;
        public readonly int RespawnMinutes;
        public readonly int RequiredLevel;

        public OreSpec(int tier, string idSuffix, string namePascal, int nodeHp, int sellPrice, int expReward, int respawnMinutes, int requiredLevel)
        {
            Tier = tier;
            IdSuffix = idSuffix;
            NamePascal = namePascal;
            NodeHp = nodeHp;
            SellPrice = sellPrice;
            ExpReward = expReward;
            RespawnMinutes = respawnMinutes;
            RequiredLevel = requiredLevel;
        }
    }

    private sealed class EnemySpec
    {
        public readonly int Tier;
        public readonly string IdSuffix;
        public readonly string NamePascal;
        public readonly int Hp;
        public readonly int Attack;
        public readonly int Defense;
        public readonly int ExpReward;
        public readonly int MaterialSellPrice;
        public readonly int RespawnMinutes;
        public readonly int RequiredLevel;

        public EnemySpec(int tier, string idSuffix, string namePascal, int hp, int attack, int defense, int expReward, int materialSellPrice, int respawnMinutes, int requiredLevel)
        {
            Tier = tier;
            IdSuffix = idSuffix;
            NamePascal = namePascal;
            Hp = hp;
            Attack = attack;
            Defense = defense;
            ExpReward = expReward;
            MaterialSellPrice = materialSellPrice;
            RespawnMinutes = respawnMinutes;
            RequiredLevel = requiredLevel;
        }
    }
}
