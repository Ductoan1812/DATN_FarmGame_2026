using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

public class FormatCropsSprites
{
    [MenuItem("Tools/DATN/Format Crops Sprites")]
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
                // Lấy tọa độ tâm của sprite hiện tại
                float cx = Mathf.Round(sprite.rect.x + sprite.rect.width / 2f);
                float cy = Mathf.Round(sprite.rect.y + sprite.rect.height / 2f);
                
                // Mở rộng/thu hẹp thành 32x32 với tâm không đổi
                sprite.rect = new Rect(cx - 16f, cy - 16f, 32f, 32f);
                
                // Đặt pivot ở Center
                sprite.alignment = (int)SpriteAlignment.Center;
                sprite.pivot = new Vector2(0.5f, 0.5f);
                
                sprites[i] = sprite;
                changed = true;
            }
        }

        if (changed)
        {
            importer.spritesheet = sprites.ToArray();
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            Debug.Log("Successfully formatted seed and item sprites to 32x32 with Center pivot!");
        }
        else
        {
            Debug.Log("No seed_ or item_ sprites found to format. Did you run the rename script first?");
        }
    }
}
