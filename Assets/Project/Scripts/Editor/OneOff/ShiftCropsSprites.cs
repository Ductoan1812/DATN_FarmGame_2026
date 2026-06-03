using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

public class ShiftCropsSprites
{
    [MenuItem("Tools/DATN/Shift Seed and Item Sprites Down")]
    public static void Execute()
    {
        string path = "Assets/Project/Art/Farming/Crops/Cl_Crops1.png";
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("Could not find TextureImporter at " + path);
            return;
        }

        var sprites = importer.spritesheet.ToList();
        bool changed = false;

        for (int i = 0; i < sprites.Count; i++)
        {
            var sprite = sprites[i];
            if (sprite.name.StartsWith("seed_") || sprite.name.StartsWith("item_"))
            {
                // Shift rect.y down by 16
                sprite.rect = new Rect(sprite.rect.x, sprite.rect.y - 16f, sprite.rect.width, sprite.rect.height);
                
                sprites[i] = sprite;
                changed = true;
            }
        }

        if (changed)
        {
            importer.spritesheet = sprites.ToArray();
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            Debug.Log("Successfully shifted seed and item sprites down by 16 pixels!");
        }
    }
}
