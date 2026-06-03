using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System.IO;

public class GenerateSeedEntities
{
    [MenuItem("Tools/DATN/Generate Seed Entities")]
    public static void Execute()
    {
        string spritePath = "Assets/Project/Art/Farming/Crops/Cl_Crops1.png";
        string outputFolder = "Assets/Project/Resources/Data/Entities/Items/Seeds";
        string cropsFolder = "Assets/Project/Resources/Data/Entities/World/Crops";
        
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
            Sprite seedSprite = sprites.FirstOrDefault(x => x.name == $"seed_{cropName}");

            string cropAssetPath = $"{cropsFolder}/crop_{cropName}.asset";
            EntityData cropEntity = AssetDatabase.LoadAssetAtPath<EntityData>(cropAssetPath);
            
            if (cropEntity == null)
            {
                Debug.LogWarning($"Could not find crop EntityData at {cropAssetPath}");
            }

            EntityData seedData = ScriptableObject.CreateInstance<EntityData>();
            string seedId = $"seed_{cropName}_crop";
            
            seedData.id = seedId;
            seedData.keyName = seedId;
            seedData.category = ItemCategory.Seed;
            seedData.maxStack = 99;
            seedData.icon = seedSprite;

            seedData.modules = new List<IModuleData>();

            PlacementModule placementModule = new PlacementModule();
            placementModule.objectTypeToSpawn = ObjectType.Plant01;
            placementModule.placedEntityData = cropEntity;
            placementModule.centerTile = true;
            placementModule.animTrigger = "PutDown";

            seedData.modules.Add(placementModule);

            string assetPath = $"{outputFolder}/{seedId}.asset";
            AssetDatabase.CreateAsset(seedData, assetPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Successfully created {cropNames.Length} Seed EntityData objects!");
    }
}
