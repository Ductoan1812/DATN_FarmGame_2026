using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System.IO;

public class GenerateResourceNodes
{
    [MenuItem("Tools/DATN/Generate Resource Nodes")]
    public static void Execute()
    {
        string spritePath = "Assets/Project/Art/Farming/Ore/Ore.png";
        string dropsFolder = "Assets/Project/Resources/Data/Entities/Items/Resources";
        string nodesFolder = "Assets/Project/Resources/Data/Entities/World/Resources";
        
        if (!Directory.Exists(dropsFolder)) Directory.CreateDirectory(dropsFolder);
        if (!Directory.Exists(nodesFolder)) Directory.CreateDirectory(nodesFolder);

        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(spritePath);
        List<Sprite> sprites = allAssets.OfType<Sprite>().ToList();
        
        if (sprites.Count == 0)
        {
            Debug.LogError("No sprites found in Ore.png");
            return;
        }

        // --- 1. Generate Drop Items ---
        Dictionary<string, EntityData> dropItems = new Dictionary<string, EntityData>();
        
        // Find all sprites that represent dropped resources (like Item_resource_stone)
        var dropSprites = sprites.Where(x => x.name.StartsWith("Item_resource_")).ToList();
        
        foreach (Sprite s in dropSprites)
        {
            dropItems[s.name] = CreateDropItem(s.name, s, dropsFolder);
        }

        // Create Coal placeholder using stone sprite (or any sprite) since user said no coal sprite yet
        if (!dropItems.ContainsKey("Item_resource_coal"))
        {
            Sprite coalFallback = dropSprites.FirstOrDefault(x => x.name == "Item_resource_stone") ?? sprites.FirstOrDefault();
            dropItems["Item_resource_coal"] = CreateDropItem("Item_resource_coal", coalFallback, dropsFolder);
        }

        // Create Copper placeholder if not exist
        if (!dropItems.ContainsKey("Item_resource_copper_ore"))
        {
            Sprite copperFallback = dropSprites.FirstOrDefault(x => x.name == "Item_resource_stone") ?? sprites.FirstOrDefault();
            dropItems["Item_resource_copper_ore"] = CreateDropItem("Item_resource_copper_ore", copperFallback, dropsFolder);
        }

        // --- 2. Generate Nodes ---
        var nodeSprites = sprites.Where(x => !x.name.StartsWith("Item_resource_")).ToList();
        int nodeCount = 0;

        foreach (Sprite s in nodeSprites)
        {
            string nodeName = s.name.ToLower();
            
            EntityData nodeData = ScriptableObject.CreateInstance<EntityData>();
            nodeData.id = "node_" + s.name;
            nodeData.keyName = nodeData.id;
            nodeData.category = ItemCategory.Placeable;
            
            nodeData.modules = new List<IModuleData>();

            // Determine Tier and Health
            int tier = 1;
            int hp = 10;
            
            if (nodeName.Contains("big")) { tier = 3; hp = 30; }
            else if (nodeName.Contains("ruby") || nodeName.Contains("sapphire") || nodeName.Contains("emerald")) { tier = 2; hp = 20; }
            else if (nodeName.Contains("gold")) { tier = 2; hp = 20; }
            else if (nodeName.Contains("iron")) { tier = 1; hp = 15; }
            else { tier = 1; hp = 10; }

            // Stats (MaxHp & Hp)
            nodeData.baseStats = new StatsData();
            nodeData.baseStats.baseStats = new List<StatEntry>
            {
                new StatEntry { statType = StatType.MaxHp, value = hp },
                new StatEntry { statType = StatType.Hp, value = hp }
            };

            // StageModule (Only 1 stage as requested)
            StageModule stageModule = new StageModule();
            stageModule.stages = new GrowthStage[] 
            { 
                new GrowthStage { sprite = s, daysToGrow = 0, canHarvest = true }
            };
            nodeData.modules.Add(stageModule);

            nodeData.modules.Add(new HealthModule());
            nodeData.modules.Add(new MortalModule());
            nodeData.modules.Add(new ResourceHitReactionModule());

            // Harvest Requirement
            HarvestModule harvestModule = new HarvestModule();
            harvestModule.harvestTool = ToolType.Pickaxe;
            nodeData.modules.Add(harvestModule);

            ToolRequirementModule toolReq = new ToolRequirementModule();
            toolReq.requiredToolType = ToolType.Pickaxe;
            toolReq.minimumToolTier = tier;
            nodeData.modules.Add(toolReq);

            // DropModule
            DropModule dropModule = new DropModule();
            List<DropEntry> drops = new List<DropEntry>();
            
            if (nodeName.Contains("gold") && dropItems.ContainsKey("Item_resource_gold_ore"))
                drops.Add(new DropEntry { item = dropItems["Item_resource_gold_ore"], minAmount = 1, maxAmount = 3, dropChance = 1f });
            else if (nodeName.Contains("iron") && dropItems.ContainsKey("Item_resource_iron_ore"))
                drops.Add(new DropEntry { item = dropItems["Item_resource_iron_ore"], minAmount = 1, maxAmount = 3, dropChance = 1f });
            else if (nodeName.Contains("sapphire") && dropItems.ContainsKey("Item_resource_gem_sapphire"))
                drops.Add(new DropEntry { item = dropItems["Item_resource_gem_sapphire"], minAmount = 1, maxAmount = 2, dropChance = 1f });
            else if (nodeName.Contains("ruby") && dropItems.ContainsKey("Item_resource_gem_ruby"))
                drops.Add(new DropEntry { item = dropItems["Item_resource_gem_ruby"], minAmount = 1, maxAmount = 2, dropChance = 1f });
            else if (nodeName.Contains("emerald") && dropItems.ContainsKey("Item_resource_gem_emerald"))
                drops.Add(new DropEntry { item = dropItems["Item_resource_gem_emerald"], minAmount = 1, maxAmount = 2, dropChance = 1f });
            else 
            {
                // Đá thường: Đập rớt Đá (100%), Tỉ lệ nhỏ rớt Đồng (20%) và Than (30%)
                if (dropItems.ContainsKey("Item_resource_stone"))
                    drops.Add(new DropEntry { item = dropItems["Item_resource_stone"], minAmount = 1, maxAmount = 3, dropChance = 1f });
                if (dropItems.ContainsKey("Item_resource_copper_ore"))
                    drops.Add(new DropEntry { item = dropItems["Item_resource_copper_ore"], minAmount = 1, maxAmount = 1, dropChance = 0.2f });
                if (dropItems.ContainsKey("Item_resource_coal"))
                    drops.Add(new DropEntry { item = dropItems["Item_resource_coal"], minAmount = 1, maxAmount = 1, dropChance = 0.3f });
            }

            dropModule.harvestDrops = drops.ToArray();
            nodeData.modules.Add(dropModule);

            string assetPath = $"{nodesFolder}/node_{s.name}.asset";
            AssetDatabase.CreateAsset(nodeData, assetPath);
            nodeCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Successfully created {nodeCount} Resource Nodes and {dropItems.Count} Item Drops!");
    }

    private static EntityData CreateDropItem(string id, Sprite icon, string folder)
    {
        string assetPath = $"{folder}/{id}.asset";
        EntityData existing = AssetDatabase.LoadAssetAtPath<EntityData>(assetPath);
        if (existing != null) return existing;

        EntityData itemData = ScriptableObject.CreateInstance<EntityData>();
        itemData.id = id;
        itemData.keyName = id;
        itemData.category = ItemCategory.Material;
        itemData.maxStack = 99;
        itemData.icon = icon;
        itemData.modules = new List<IModuleData>();

        AssetDatabase.CreateAsset(itemData, assetPath);
        return itemData;
    }
}
