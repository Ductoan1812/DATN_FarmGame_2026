using System;
using System.Collections.Generic;
using System.Linq;
using Assets.HeroEditor4D.Common.Scripts.Enums;
using DialogueGraphTool;
using UnityEditor;
using UnityEngine;

public static class BootstrapGameplayData
{
    private const string SampleQuestPath = "Assets/Project/ScriptableObjects/Graph/quest/Quest_SampleFarmerWork.asset";
    private const string SampleDialoguePath = "Assets/Project/ScriptableObjects/Graph/dialogue/SampleFarmerDialogueGraph.asset";
    private const string PlayerPath = "Assets/Project/ScriptableObjects/Characters/Player/Player.asset";
    private const string NpcShopPath = "Assets/Project/ScriptableObjects/Characters/NPCs/NPC_Banhang.asset";
    private const string NpcCraftPath = "Assets/Project/ScriptableObjects/Characters/NPCs/NPC_Chetao.asset";
    private const string NpcEventPath = "Assets/Project/ScriptableObjects/Characters/NPCs/NPC_Sukien.asset";
    private const string PickaxePath = "Assets/Project/ScriptableObjects/Items/Tools/Pickaxe_01.asset";
    private const string OreChunkPath = "Assets/Project/ScriptableObjects/Items/Materials/OreChunk_01.asset";
    private const string OreNodePath = "Assets/Project/ScriptableObjects/WorldObjects/Resources/OreNode_01.asset";
    private const string EnemyPath = "Assets/Project/ScriptableObjects/WorldObjects/Enemies/Enemy_01.asset";

    [MenuItem("Tools/DATN/One-off Setup/Bootstraps/Bootstrap Core Content")]
    public static void Execute()
    {
        EnsureDirectories();

        var sampleQuest = AssetDatabase.LoadAssetAtPath<QuestGraphData>(SampleQuestPath);
        var sampleDialogue = AssetDatabase.LoadAssetAtPath<DialogueGraphData>(SampleDialoguePath);

        EnsurePlayerHasQuestLog();
        EnsureNpcShopQuest(sampleQuest);
        EnsureNpcTemplates(sampleDialogue, sampleQuest);

        var pickaxe = EnsurePickaxeItem();
        var oreChunk = EnsureOreChunkItem();
        var oreNode = EnsureOreNodeEntity(oreChunk);
        var enemy = EnsureEnemyEntity(oreChunk);

        EnsureWorldObjectDefinitions(oreNode, enemy);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BootstrapGameplayData] Done. Pickaxe={pickaxe != null}, OreChunk={oreChunk != null}, OreNode={oreNode != null}, Enemy={enemy != null}");
    }

    private static void EnsureDirectories()
    {
        EnsureFolder("Assets/Project/ScriptableObjects/Items/Materials");
        EnsureFolder("Assets/Project/ScriptableObjects/WorldObjects/Resources");
        EnsureFolder("Assets/Project/ScriptableObjects/WorldObjects/Enemies");
        EnsureFolder("Assets/Project/ScriptableObjects/Characters/NPCs");
        EnsureFolder("Assets/Project/Resources/Data/WorldObjects");
    }

    private static void EnsurePlayerHasQuestLog()
    {
        var player = AssetDatabase.LoadAssetAtPath<EntityData>(PlayerPath);
        if (player == null) return;

        if (player.modules.OfType<QuestLogModule>().Any())
            return;

        player.modules.Add(new QuestLogModule());
        EditorUtility.SetDirty(player);
    }

    private static void EnsureNpcShopQuest(QuestGraphData sampleQuest)
    {
        var npc = AssetDatabase.LoadAssetAtPath<EntityData>(NpcShopPath);
        if (npc == null || sampleQuest == null) return;

        var questModule = npc.modules.OfType<QuestModule>().FirstOrDefault();
        if (questModule == null)
        {
            questModule = new QuestModule();
            npc.modules.Add(questModule);
        }

        questModule.quests ??= new List<QuestGraphData>();
        if (!questModule.quests.Contains(sampleQuest))
            questModule.quests.Add(sampleQuest);

        EditorUtility.SetDirty(npc);
    }

    private static void EnsureNpcTemplates(DialogueGraphData sampleDialogue, QuestGraphData sampleQuest)
    {
        EnsureNpcEntity(
            NpcCraftPath,
            "npc_chetao",
            "npc_chetao_name",
            "npc_chetao_desc",
            sampleDialogue,
            includeShop: false,
            includeQuest: false,
            sampleQuest);

        EnsureNpcEntity(
            NpcEventPath,
            "npc_sukien",
            "npc_sukien_name",
            "npc_sukien_desc",
            sampleDialogue,
            includeShop: false,
            includeQuest: true,
            sampleQuest);
    }

    private static void EnsureNpcEntity(
        string path,
        string id,
        string nameKey,
        string descKey,
        DialogueGraphData dialogueGraph,
        bool includeShop,
        bool includeQuest,
        QuestGraphData sampleQuest)
    {
        var data = AssetDatabase.LoadAssetAtPath<EntityData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<EntityData>();
            data.name = System.IO.Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(data, path);
        }

        data.id = id;
        data.keyName = nameKey;
        data.descKey = descKey;
        data.maxStack = 1;
        data.category = ItemCategory.None;
        data.buyPrice = 0;
        data.sellPrice = 0;

        if (data.baseStats == null)
            data.baseStats = new StatsData();
        data.baseStats.baseStats ??= new List<StatEntry>();
        SetOrAddStat(data.baseStats.baseStats, StatType.Money, 5000f);

        data.placementRule = new PlacementRule
        {
            occupyLayer = EntityLayer.Ground,
            requireTags = PlacementTag.None,
            provideTags = PlacementTag.None,
            blockLayers = new EntityLayer[0]
        };

        var dialogue = EnsureModule<DialogueModule>(data);
        dialogue.graph = dialogueGraph;
        dialogue.optionTextKey = "ui.dialogue.talk";
        dialogue.priority = 10;

        EnsureModule<InventoryModule>(data).inventoryType = InventoryType.Backpack;
        EnsureModule<InventoryModule>(data).size = 20;

        if (includeShop)
        {
            var shop = EnsureModule<ShopModule>(data);
            shop.sellsToPlayer = true;
            shop.buysFromPlayer = true;
            shop.buysAllItems = false;
            shop.infiniteStock = false;
            shop.optionTextKey = "ui.shop.open";
            shop.priority = 30;
        }
        else
        {
            RemoveModule<ShopModule>(data);
        }

        if (includeQuest && sampleQuest != null)
        {
            var quest = EnsureModule<QuestModule>(data);
            quest.quests ??= new List<QuestGraphData>();
            if (!quest.quests.Contains(sampleQuest))
                quest.quests.Add(sampleQuest);
            quest.priority = 20;
        }
        else
        {
            RemoveModule<QuestModule>(data);
        }

        EditorUtility.SetDirty(data);
    }

    private static EntityData EnsurePickaxeItem()
    {
        var data = AssetDatabase.LoadAssetAtPath<EntityData>(PickaxePath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<EntityData>();
            data.name = "Pickaxe_01";
            AssetDatabase.CreateAsset(data, PickaxePath);
        }

        data.id = "Pickaxe01";
        data.keyName = "Pickaxe01_name";
        data.descKey = "Pickaxe01_desc";
        data.category = ItemCategory.Tool;
        data.maxStack = 1;
        data.buyPrice = 320;
        data.sellPrice = 140;

        data.baseStats ??= new StatsData();
        data.baseStats.baseStats ??= new List<StatEntry>();
        SetOrAddStat(data.baseStats.baseStats, StatType.Attack, 2f);
        SetOrAddStat(data.baseStats.baseStats, StatType.Range, 1.3f);
        SetOrAddStat(data.baseStats.baseStats, StatType.CoolDown, 0.35f);

        var tool = EnsureModule<ToolModule>(data);
        tool.toolType = ToolType.Pickaxe;
        tool.animTrigger = "Pickaxe";

        var appearance = EnsureModule<AppearanceModule>(data);
        appearance.equipmentPart = EquipmentPart.MeleeWeapon1H;
        if (string.IsNullOrWhiteSpace(appearance.spriteId))
            appearance.spriteId = "Tools.Pickaxe01";

        EditorUtility.SetDirty(data);
        return data;
    }

    private static EntityData EnsureOreChunkItem()
    {
        var data = AssetDatabase.LoadAssetAtPath<EntityData>(OreChunkPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<EntityData>();
            data.name = "OreChunk_01";
            AssetDatabase.CreateAsset(data, OreChunkPath);
        }

        data.id = "OreChunk01";
        data.keyName = "OreChunk01_name";
        data.descKey = "OreChunk01_desc";
        data.category = ItemCategory.Material;
        data.maxStack = 999;
        data.buyPrice = 0;
        data.sellPrice = 24;
        data.baseStats = data.baseStats ?? new StatsData { baseStats = new List<StatEntry>() };
        data.modules ??= new List<IModuleData>();
        EditorUtility.SetDirty(data);
        return data;
    }

    private static EntityData EnsureOreNodeEntity(EntityData oreChunk)
    {
        var data = AssetDatabase.LoadAssetAtPath<EntityData>(OreNodePath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<EntityData>();
            data.name = "OreNode_01";
            AssetDatabase.CreateAsset(data, OreNodePath);
        }

        data.id = "OreNode01";
        data.keyName = "ore_node_name";
        data.descKey = "ore_node_desc";
        data.category = ItemCategory.Placeable;
        data.maxStack = 1;
        data.buyPrice = 0;
        data.sellPrice = 0;

        data.baseStats ??= new StatsData();
        data.baseStats.baseStats ??= new List<StatEntry>();
        SetOrAddStat(data.baseStats.baseStats, StatType.MaxHp, 8f);
        SetOrAddStat(data.baseStats.baseStats, StatType.Hp, 8f);

        data.placementRule = new PlacementRule
        {
            occupyLayer = EntityLayer.Furniture,
            requireTags = PlacementTag.None,
            provideTags = PlacementTag.None,
            blockLayers = new EntityLayer[0]
        };

        var harvest = EnsureModule<HarvestModule>(data);
        harvest.harvestTool = ToolType.Pickaxe;

        var health = EnsureModule<HealthModule>(data);
        health.canTakeDamage = true;

        var drop = EnsureModule<DropModule>(data);
        drop.harvestDrops = oreChunk == null
            ? new DropEntry[0]
            : new[]
            {
                new DropEntry
                {
                    item = oreChunk,
                    minAmount = 1,
                    maxAmount = 3,
                    dropChance = 1f
                }
            };

        EnsureModule<MortalModule>(data);

        EditorUtility.SetDirty(data);
        return data;
    }

    private static EntityData EnsureEnemyEntity(EntityData oreChunk)
    {
        var data = AssetDatabase.LoadAssetAtPath<EntityData>(EnemyPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<EntityData>();
            data.name = "Enemy_01";
            AssetDatabase.CreateAsset(data, EnemyPath);
        }

        data.id = "Enemy01";
        data.keyName = "enemy_01_name";
        data.descKey = "enemy_01_desc";
        data.category = ItemCategory.None;
        data.maxStack = 1;
        data.buyPrice = 0;
        data.sellPrice = 0;

        data.baseStats ??= new StatsData();
        data.baseStats.baseStats ??= new List<StatEntry>();
        SetOrAddStat(data.baseStats.baseStats, StatType.MaxHp, 20f);
        SetOrAddStat(data.baseStats.baseStats, StatType.Hp, 20f);
        SetOrAddStat(data.baseStats.baseStats, StatType.Attack, 3f);
        SetOrAddStat(data.baseStats.baseStats, StatType.Speed, 1.8f);

        data.placementRule = new PlacementRule
        {
            occupyLayer = EntityLayer.Ground,
            requireTags = PlacementTag.None,
            provideTags = PlacementTag.None,
            blockLayers = new EntityLayer[0]
        };

        EnsureModule<HealthModule>(data).canTakeDamage = true;
        // Enemy mẫu dùng respawn loop, không destroy vĩnh viễn.
        RemoveModule<MortalModule>(data);

        var drop = EnsureModule<DropModule>(data);
        drop.harvestDrops = oreChunk == null
            ? new DropEntry[0]
            : new[]
            {
                new DropEntry
                {
                    item = oreChunk,
                    minAmount = 1,
                    maxAmount = 2,
                    dropChance = 0.5f
                }
            };

        var respawn = EnsureModule<RespawnModule>(data);
        respawn.defaultRespawnPosition = Vector2.zero;
        respawn.respawnDelay = 25f;
        respawn.restoreFullHp = true;
        respawn.respawnPrefabId = ObjectType.Enemy01;

        EditorUtility.SetDirty(data);
        return data;
    }

    private static void EnsureWorldObjectDefinitions(EntityData oreNode, EntityData enemy)
    {
        var npcTemplate = AssetDatabase.LoadAssetAtPath<WorldObjectDefinition>("Assets/Project/Resources/Data/WorldObjects/NPC01.asset");
        var plantTemplate = AssetDatabase.LoadAssetAtPath<WorldObjectDefinition>("Assets/Project/Resources/Data/WorldObjects/Plant01.asset");

        var npcPrefab = npcTemplate != null ? npcTemplate.prefab : null;
        var plantPrefab = plantTemplate != null ? plantTemplate.prefab : null;
        var enemyPrefab = npcPrefab != null ? npcPrefab : plantPrefab;

        EnsureWorldObjectDef("Assets/Project/Resources/Data/WorldObjects/NPCShop01.asset", ObjectType.NPCShop01, npcPrefab);
        EnsureWorldObjectDef("Assets/Project/Resources/Data/WorldObjects/NPCCrafting01.asset", ObjectType.NPCCrafting01, npcPrefab);
        EnsureWorldObjectDef("Assets/Project/Resources/Data/WorldObjects/NPCEvent01.asset", ObjectType.NPCEvent01, npcPrefab);
        EnsureWorldObjectDef("Assets/Project/Resources/Data/WorldObjects/Plant02.asset", ObjectType.Plant02, plantPrefab);
        EnsureWorldObjectDef("Assets/Project/Resources/Data/WorldObjects/OreNode01.asset", ObjectType.OreNode01, plantPrefab);
        EnsureWorldObjectDef("Assets/Project/Resources/Data/WorldObjects/Enemy01.asset", ObjectType.Enemy01, enemyPrefab);

        if (oreNode == null || enemy == null)
            return;
    }

    private static void EnsureWorldObjectDef(string path, ObjectType objectType, GameObject prefab)
    {
        var def = AssetDatabase.LoadAssetAtPath<WorldObjectDefinition>(path);
        if (def == null)
        {
            def = ScriptableObject.CreateInstance<WorldObjectDefinition>();
            def.name = System.IO.Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(def, path);
        }

        def.idObject = objectType;
        if (def.prefab == null)
            def.prefab = prefab;

        EditorUtility.SetDirty(def);
    }

    private static T EnsureModule<T>(EntityData data) where T : IModuleData
    {
        data.modules ??= new List<IModuleData>();

        var module = data.modules.OfType<T>().FirstOrDefault();
        if (module != null)
            return module;

        module = Activator.CreateInstance(typeof(T)) as T;
        if (module == null)
            throw new InvalidOperationException($"Cannot create module instance for type '{typeof(T).Name}'.");

        data.modules.Add(module);
        return module;
    }

    private static void RemoveModule<T>(EntityData data) where T : IModuleData
    {
        for (int i = data.modules.Count - 1; i >= 0; i--)
        {
            if (data.modules[i] is T)
                data.modules.RemoveAt(i);
        }
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

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
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
}
