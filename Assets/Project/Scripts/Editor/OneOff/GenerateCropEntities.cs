using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System.IO;

public class GenerateCropEntities
{
    [MenuItem("Tools/DATN/Generate Crop Entities")]
    public static void Execute()
    {
        string spritePath = "Assets/Project/Art/Farming/Crops/Cl_Crops1.png";
        string outputFolder = "Assets/Project/Resources/Data/Entities/World/Crops";
        
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(spritePath);
        if (allAssets == null || allAssets.Length == 0)
        {
            Debug.LogError("No sprites found at " + spritePath);
            return;
        }

        List<Sprite> sprites = new List<Sprite>();
        foreach (var asset in allAssets)
        {
            if (asset is Sprite s)
            {
                sprites.Add(s);
            }
        }

        string[] cropNames = new string[]
        {
            "wheat", "potato", "turnip", "garlic", "radish", "carrot", "cauliflower", "cabbage",
            "asparagus", "corn", "tomato", "pepper_red", "pepper_yellow", "pepper_green", "cucumber", "strawberry",
            "grape", "bean", "pea", "yam", "melon", "blueberry"
        };

        foreach (string cropName in cropNames)
        {
            List<Sprite> stageSprites = new List<Sprite>();
            Sprite wiltSprite = sprites.FirstOrDefault(x => x.name == $"wilt_{cropName}");

            int stageIndex = 1;
            while (true)
            {
                string stageName = $"{cropName}_stage{stageIndex}";
                Sprite s = sprites.FirstOrDefault(x => x.name == stageName);
                if (s != null)
                {
                    stageSprites.Add(s);
                    stageIndex++;
                }
                else
                {
                    break;
                }
            }

            EntityData data = ScriptableObject.CreateInstance<EntityData>();
            data.id = "crop_" + cropName;
            data.keyName = "crop_" + cropName;
            data.category = ItemCategory.Placeable;
            data.maxStack = 1;
            
            // Stats
            data.baseStats = new StatsData();
            data.baseStats.baseStats = new List<StatEntry>
            {
                new StatEntry { statType = StatType.MaxHp, value = 10f },
                new StatEntry { statType = StatType.Hp, value = 10f }
            };

            // Modules
            data.modules = new List<IModuleData>();

            // 1. StageModule
            StageModule stageModule = new StageModule();
            stageModule.wiltSprite = wiltSprite;
            stageModule.stages = new GrowthStage[stageSprites.Count];
            for (int i = 0; i < stageSprites.Count; i++)
            {
                stageModule.stages[i] = new GrowthStage 
                { 
                    sprite = stageSprites[i],
                    daysToGrow = 1,
                    canHarvest = (i == stageSprites.Count - 1)
                };
            }
            data.modules.Add(stageModule);

            // 2. HarvestModule
            HarvestModule harvestModule = new HarvestModule();
            harvestModule.harvestTool = ToolType.Scythe;
            data.modules.Add(harvestModule);

            // 3. DropModule
            DropModule dropModule = new DropModule();
            dropModule.harvestDrops = new DropEntry[0];
            data.modules.Add(dropModule);

            // 4. HealthModule
            data.modules.Add(new HealthModule());

            // 5. ResourceHitReactionModule
            data.modules.Add(new ResourceHitReactionModule());

            // 6. ExpRewardModule
            ExpRewardModule expModule = new ExpRewardModule();
            expModule.rewardExp = 10;
            expModule.sourceType = ExpSourceType.Harvest;
            data.modules.Add(expModule);

            // 7. MortalModule
            data.modules.Add(new MortalModule());

            string assetPath = $"{outputFolder}/crop_{cropName}.asset";
            AssetDatabase.CreateAsset(data, assetPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Successfully created " + cropNames.Length + " EntityData objects for crops!");
    }
}
