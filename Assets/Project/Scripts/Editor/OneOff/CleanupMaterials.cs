using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class CleanupMaterials
{
    [MenuItem("Tools/DATN/Cleanup Materials")]
    public static void Execute()
    {
        string materialsFolder = "Assets/Project/Resources/Data/Entities/Items/Materials";
        string resourcesFolder = "Assets/Project/Resources/Data/Entities/Items/Resources";
        
        string[] oldFilesToDelete = new string[]
        {
            "item_mat_coal.asset",
            "item_mat_copper_bar.asset",
            "item_mat_copper_ore.asset",
            "item_mat_gold_bar.asset",
            "item_mat_gold_ore.asset",
            "item_mat_iron_bar.asset",
            "item_mat_iron_ore.asset",
            "item_mat_stone.asset"
        };

        // 1. Delete old files
        foreach (string file in oldFilesToDelete)
        {
            string path = $"{materialsFolder}/{file}";
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
                Debug.Log("Deleted old entity: " + path);
            }
        }

        // 2. Move files from Resources to Materials
        if (AssetDatabase.IsValidFolder(resourcesFolder))
        {
            string[] newAssets = Directory.GetFiles(resourcesFolder, "*.asset");
            foreach (string file in newAssets)
            {
                string fileName = Path.GetFileName(file);
                // Convert OS path to Unity project path for MoveAsset
                string sourcePath = file.Replace('\\', '/');
                string destPath = $"{materialsFolder}/{fileName}";
                
                string moveError = AssetDatabase.MoveAsset(sourcePath, destPath);
                if (string.IsNullOrEmpty(moveError))
                {
                    Debug.Log("Moved to Materials: " + fileName);
                }
                else
                {
                    Debug.LogWarning("Failed to move: " + fileName + " - " + moveError);
                }
            }
            
            // Cleanup the empty folder
            if (Directory.GetFiles(resourcesFolder).Length == 0)
            {
                AssetDatabase.DeleteAsset(resourcesFolder);
            }
        }

        // 3. Create missing Item_resource_copper_bar placeholder
        string copperBarPath = $"{materialsFolder}/Item_resource_copper_bar.asset";
        if (AssetDatabase.LoadAssetAtPath<EntityData>(copperBarPath) == null)
        {
            EntityData ironBar = AssetDatabase.LoadAssetAtPath<EntityData>($"{materialsFolder}/Item_resource_iron_bar.asset");
            
            EntityData copperBar = ScriptableObject.CreateInstance<EntityData>();
            copperBar.id = "Item_resource_copper_bar";
            copperBar.keyName = "Item_resource_copper_bar";
            copperBar.category = ItemCategory.Material;
            copperBar.maxStack = 99;
            copperBar.icon = ironBar != null ? ironBar.icon : null;
            copperBar.modules = new List<IModuleData>();

            AssetDatabase.CreateAsset(copperBar, copperBarPath);
            Debug.Log("Created placeholder Item_resource_copper_bar");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Cleanup complete!");
    }
}
