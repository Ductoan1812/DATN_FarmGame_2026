using UnityEngine;
using UnityEditor;
using System.Linq;

public class CropBalancer
{
    [MenuItem("Tools/Balance Crops")]
    public static void Execute()
    {
        ProcessCrop("Turnip", "Crop_T1_Turnip", "Seed_T1_Turnip", "CropPlant_T1_Turnip", 20, 60, 4, -1, 1, 1);
        ProcessCrop("Potato", "Potato", "seed_potato", "CropPlant_Potato", 50, 80, 6, -1, 1, 1, true); // potato has 20% for extra
        ProcessCrop("Strawberry", "Strawberry", "Seed_Strawberry", "CropPlant_Strawberry", 100, 120, 8, 4, 1, 1);
        
        ProcessCrop("Tomato", "Crop_T2_Tomato", "Seed_T2_Tomato", "CropPlant_T2_Tomato", 50, 60, 11, 4, 1, 1);
        ProcessCrop("Corn", "Crop_Corn", "Seed_Corn", "CropPlant_Corn", 150, 50, 14, 4, 1, 1);
        ProcessCrop("Melon", "Crop_T4_Melon", "Seed_T4_Melon", "CropPlant_T4_Melon", 80, 250, 12, -1, 1, 1);
        
        ProcessCrop("Carrot", "Crop_Carrot", "seed_carrot", "CropPlant_Carrot", 50, 120, 8, -1, 1, 1);
        ProcessCrop("Pumpkin", "Crop_T3_Pumpkin", "Seed_T3_Pumpkin", "CropPlant_T3_Pumpkin", 100, 320, 13, -1, 1, 1);
        ProcessCrop("Eggplant", "Crop_Eggplant", "Seed_Eggplant", "CropPlant_Eggplant", 20, 60, 5, 5, 1, 1);
        
        ProcessCrop("WinterRoot", "Crop_WinterRoot", "Seed_WinterRoot", "CropPlant_WinterRoot", 70, 150, 7, -1, 1, 1);
        
        AssetDatabase.SaveAssets();
        Debug.Log("CropBalancer executed successfully.");
    }
    
    static void ProcessCrop(string baseName, string cropName, string seedName, string plantName, int seedPrice, int sellPrice, int growDays, int regrowDays, int minYield, int maxYield, bool isPotato = false)
    {
        string cropsDir = "Assets/Project/ScriptableObjects/Items/Crops";
        string seedsDir = "Assets/Project/ScriptableObjects/Items/Seeds";
        string plantsDir = "Assets/Project/ScriptableObjects/WorldObjects/Plants/Placed";
        
        // Find or create Crop
        EntityData crop = FindOrCreateAsset(cropName, cropsDir, "Assets/Project/ScriptableObjects/Items/Crops/Crop_T1_Turnip.asset");
        if (crop != null) {
            crop.sellPrice = sellPrice;
            crop.id = cropName.ToLower();
            EditorUtility.SetDirty(crop);
        }
        
        // Find or create Plant
        EntityData plant = FindOrCreateAsset(plantName, plantsDir, "Assets/Project/ScriptableObjects/WorldObjects/Plants/Placed/CropPlant_T1_Turnip.asset");
        if (plant != null) {
            plant.id = plantName.ToLower();
            
            // Update StageModule
            var stageModule = plant.modules.FirstOrDefault(m => m is StageModule) as StageModule;
            if (stageModule != null) {
                // If it's 4 days, divide by 2 stages -> 2 days each
                int stage1Days = growDays / 2;
                int stage2Days = growDays - stage1Days;
                
                if (stageModule.stages.Length >= 3) {
                    stageModule.stages[0].daysToGrow = stage1Days;
                    stageModule.stages[1].daysToGrow = stage2Days;
                    stageModule.stages[2].daysToGrow = 999; // harvest stage
                }
                
                if (regrowDays > 0) {
                    stageModule.regrowStageIndex = -1; 
                    stageModule.harvestGoToStageIndex = 1; // goto stage 1 after harvest? Usually it goes to some regrow stage. Let's just set harvestGoToStageIndex to 1, and daysToReturnAfterHarvest to regrowDays
                    stageModule.daysToReturnAfterHarvest = regrowDays;
                    stageModule.lastStageLoopToIndex = 2; // back to harvest stage
                } else {
                    stageModule.regrowStageIndex = -1;
                    stageModule.harvestGoToStageIndex = -1;
                    stageModule.daysToReturnAfterHarvest = -1;
                    stageModule.lastStageLoopToIndex = -1;
                }
            }
            
            // Update DropModule
            var dropModule = plant.modules.FirstOrDefault(m => m is DropModule) as DropModule;
            if (dropModule != null) {
                if (isPotato) {
                    dropModule.harvestDrops = new DropEntry[] {
                        new DropEntry { item = crop, minAmount = 1, maxAmount = 1, dropChance = 1f },
                        new DropEntry { item = crop, minAmount = 1, maxAmount = 1, dropChance = 0.2f }
                    };
                } else {
                    dropModule.harvestDrops = new DropEntry[] {
                        new DropEntry { item = crop, minAmount = minYield, maxAmount = maxYield, dropChance = 1f }
                    };
                }
            }
            EditorUtility.SetDirty(plant);
        }
        
        // Find or create Seed
        EntityData seed = FindOrCreateAsset(seedName, seedsDir, "Assets/Project/ScriptableObjects/WorldObjects/Plants/Seed_T1_Turnip.asset");
        if (seed != null) {
            seed.buyPrice = seedPrice;
            seed.id = seedName.ToLower();
            
            // Update PlacementModule
            var placement = seed.modules.FirstOrDefault(m => m is PlacementModule) as PlacementModule;
            if (placement != null) {
                placement.placedEntityData = plant;
            }
            EditorUtility.SetDirty(seed);
        }
    }
    
    static EntityData FindOrCreateAsset(string name, string targetDir, string templatePath)
    {
        string[] searchPaths = new[] { "Assets/Project/ScriptableObjects" };
        var guids = AssetDatabase.FindAssets(name + " t:EntityData", searchPaths);
        
        if (guids.Length > 0) {
            string existingPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            // Make sure it's the right one (name match)
            var asset = AssetDatabase.LoadAssetAtPath<EntityData>(existingPath);
            if (asset.name == name) return asset;
        }
        
        // Ensure dir exists
        if (!AssetDatabase.IsValidFolder(targetDir)) {
            // Need to create folder recursively or assume it exists. Since targetDirs are existing or simple children, let's assume they exist or we create it.
            // Items/Seeds might not exist, so let's create it.
            if (!AssetDatabase.IsValidFolder("Assets/Project/ScriptableObjects/Items/Seeds")) {
                AssetDatabase.CreateFolder("Assets/Project/ScriptableObjects/Items", "Seeds");
            }
        }
        
        string newPath = targetDir + "/" + name + ".asset";
        if (AssetDatabase.CopyAsset(templatePath, newPath)) {
            var newAsset = AssetDatabase.LoadAssetAtPath<EntityData>(newPath);
            newAsset.name = name;
            return newAsset;
        }
        
        return null;
    }
}
