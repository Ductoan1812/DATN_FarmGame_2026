using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System.IO;

public class GenerateFruitEntities
{
    [MenuItem("Tools/DATN/Generate Fruit Entities")]
    public static void Execute()
    {
        string cropsSpritePath = "Assets/Project/Art/Farming/Crops/Cl_Crops1.png";
        string treesSpritePath = "Assets/Project/Art/Farming/Crops/CL_Crops_Mining.png";
        string outputFolder = "Assets/Project/Resources/Data/Entities/Items/Crops";
        
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        // --- 1. Process 22 regular crops ---
        Object[] cropAssets = AssetDatabase.LoadAllAssetsAtPath(cropsSpritePath);
        string[] regularCropNames = new string[]
        {
            "wheat", "potato", "turnip", "garlic", "radish", "carrot", "cauliflower", "cabbage",
            "asparagus", "corn", "tomato", "pepper_red", "pepper_yellow", "pepper_green", "cucumber", "strawberry",
            "grape", "bean", "pea", "yam", "melon", "blueberry"
        };

        int count = 0;
        foreach (string cropName in regularCropNames)
        {
            Sprite itemSprite = cropAssets.OfType<Sprite>().FirstOrDefault(x => x.name == $"item_{cropName}");
            if (itemSprite == null) continue;

            EntityData itemData = ScriptableObject.CreateInstance<EntityData>();
            string itemId = $"Item_fruit_{cropName}_Crops";
            
            itemData.id = itemId;
            itemData.keyName = itemId;
            itemData.category = ItemCategory.Crop;
            itemData.maxStack = 99;
            itemData.icon = itemSprite;
            itemData.modules = new List<IModuleData>();

            string assetPath = $"{outputFolder}/{itemId}.asset";
            AssetDatabase.CreateAsset(itemData, assetPath);
            count++;
        }

        // --- 2. Process 4 tree fruits ---
        Object[] treeAssets = AssetDatabase.LoadAllAssetsAtPath(treesSpritePath);
        string[] treeFruitNames = new string[] { "apple", "Cherry Tree", "plum", "pear" };
        
        foreach (string treeFruit in treeFruitNames)
        {
            Sprite itemSprite = treeAssets.OfType<Sprite>().FirstOrDefault(x => x.name == $"fruit_{treeFruit}");
            if (itemSprite == null) continue;

            string formattedName = treeFruit.Replace(" ", "_");

            EntityData itemData = ScriptableObject.CreateInstance<EntityData>();
            string itemId = $"Item_fruit_{formattedName}_fruitTree";
            
            itemData.id = itemId;
            itemData.keyName = itemId;
            itemData.category = ItemCategory.Crop;
            itemData.maxStack = 99;
            itemData.icon = itemSprite;
            itemData.modules = new List<IModuleData>();

            string assetPath = $"{outputFolder}/{itemId}.asset";
            AssetDatabase.CreateAsset(itemData, assetPath);
            count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Successfully created {count} Fruit EntityData objects!");
    }
}
