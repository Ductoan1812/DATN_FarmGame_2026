using UnityEngine;
using UnityEditor;

public class SetupWateringCan
{
    public static void Execute()
    {
        // Load WateringCan asset
        var asset = AssetDatabase.LoadAssetAtPath<EntityData>("Assets/Project/ScriptableObjects/Items/Tools/WateringCan_01.asset");
        if (asset == null)
        {
            Debug.LogError("Không tìm thấy WateringCan_01.asset!");
            return;
        }

        // Sửa basic fields
        asset.id = "WateringCan01";
        asset.keyName = "WateringCanBasic";
        asset.descKey = "Bình tưới cơ bản";
        // icon giữ nguyên tạm (dùng icon Hoe, sẽ thay sau)

        // Sửa ToolModule: toolType = WateringCan
        if (asset.modules != null && asset.modules.Count > 0)
        {
            foreach (var module in asset.modules)
            {
                if (module is ToolModule toolModule)
                {
                    toolModule.toolType = ToolType.WateringCan;
                    toolModule.animTrigger = "Hoe"; // Dùng tạm anim Hoe để không miss
                    Debug.Log("Đã set ToolModule.toolType = WateringCan, animTrigger = Hoe (tạm)");
                    break;
                }
            }
        }

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        Debug.Log($"WateringCan_01 setup xong: id={asset.id}, keyName={asset.keyName}, toolType=WateringCan");
    }
}
