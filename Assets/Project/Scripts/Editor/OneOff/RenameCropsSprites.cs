using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

public class RenameCropsSprites : EditorWindow
{
    [MenuItem("Tools/DATN/Rename Crops Sprites")]
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

        // Sort by yMin descending (top to bottom)
        var sortedByY = sprites.OrderByDescending(s => s.rect.yMin).ToList();
        
        List<List<SpriteMetaData>> rows = new List<List<SpriteMetaData>>();
        List<SpriteMetaData> currentRow = new List<SpriteMetaData>();
        
        float currentY = -1;
        float rowTolerance = 16f; // pixels

        foreach (var sprite in sortedByY)
        {
            if (currentY == -1 || Mathf.Abs(sprite.rect.yMin - currentY) > rowTolerance)
            {
                if (currentRow.Count > 0)
                {
                    rows.Add(currentRow.OrderBy(s => s.rect.xMin).ToList());
                }
                currentRow = new List<SpriteMetaData>();
                currentY = sprite.rect.yMin;
            }
            currentRow.Add(sprite);
        }
        if (currentRow.Count > 0)
        {
            rows.Add(currentRow.OrderBy(s => s.rect.xMin).ToList());
        }

        string[] cropNames = new string[]
        {
            "wheat", "potato", "turnip", "garlic", "radish", "carrot", "cauliflower", "cabbage",
            "asparagus", "corn", "tomato", "pepper_red", "pepper_yellow", "pepper_green", "cucumber", "strawberry",
            "grape", "bean", "pea", "yam", "melon", "blueberry"
        };

        if (rows.Count != cropNames.Length)
        {
            Debug.LogError($"Expected {cropNames.Length} rows, but found {rows.Count} rows.");
            for (int i = 0; i < rows.Count; i++) {
                Debug.Log($"Row {i} has {rows[i].Count} sprites. Approx Y: {rows[i][0].rect.yMin}");
            }
            return;
        }

        List<SpriteMetaData> newSprites = new List<SpriteMetaData>();

        for (int i = 0; i < rows.Count; i++)
        {
            string cropName = cropNames[i];
            var row = rows[i];
            
            for (int j = 0; j < row.Count; j++)
            {
                var sprite = row[j];
                
                if (j == 0)
                {
                    sprite.name = $"seed_{cropName}";
                }
                else if (j == row.Count - 2)
                {
                    sprite.name = $"item_{cropName}";
                }
                else if (j == row.Count - 1)
                {
                    sprite.name = $"wilt_{cropName}";
                }
                else
                {
                    sprite.name = $"{cropName}_stage{j}"; // j starts at 1
                }
                
                newSprites.Add(sprite);
            }
        }

        importer.spritesheet = newSprites.ToArray();
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
        
        Debug.Log("Successfully renamed all sprites!");
    }
}
