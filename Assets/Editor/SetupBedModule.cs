using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SetupBedModule
{
    public static void Execute()
    {
        var bed = AssetDatabase.LoadAssetAtPath<EntityData>("Assets/Project/ScriptableObjects/WorldObjects/Utility/Bed_01.asset");
        if (bed == null) { Debug.LogError("Bed_01 not found!"); return; }

        // Check nếu đã có BedModule
        foreach (var m in bed.modules)
        {
            if (m is BedModule)
            {
                Debug.Log("BedModule đã có trên Bed_01!");
                return;
            }
        }

        // Thêm BedModule
        bed.modules.Add(new BedModule());
        EditorUtility.SetDirty(bed);
        AssetDatabase.SaveAssets();
        Debug.Log("Đã thêm BedModule vào Bed_01!");
    }
}
