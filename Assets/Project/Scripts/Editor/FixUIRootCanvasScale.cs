using UnityEditor;
using UnityEngine;

public static class FixUIRootCanvasScaleRunner
{
    public static void Execute()
    {
        const string prefabPath = "Assets/Project/Prefabs/Systems/UIRoot.prefab";

        var editRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        if (editRoot == null)
        {
            Debug.LogError("[FixUIRootCanvasScale] Không load được UIRoot prefab!");
            return;
        }

        string[] canvasNames = { "Canvas_HUD", "Canvas_Windows", "Canvas_Overlay", "Canvas_Debug" };
        int fixCount = 0;

        foreach (var canvasName in canvasNames)
        {
            var t = editRoot.transform.Find(canvasName);
            if (t == null)
            {
                Debug.LogWarning($"[FixUIRootCanvasScale] Không tìm thấy: {canvasName}");
                continue;
            }

            if (t.localScale != Vector3.one)
            {
                Debug.Log($"[FixUIRootCanvasScale] Fix scale '{canvasName}': {t.localScale} -> (1,1,1)");
                t.localScale = Vector3.one;
                fixCount++;
            }
            else
            {
                Debug.Log($"[FixUIRootCanvasScale] '{canvasName}' scale OK.");
            }
        }

        if (fixCount > 0)
        {
            PrefabUtility.SaveAsPrefabAsset(editRoot, prefabPath);
            Debug.Log($"[FixUIRootCanvasScale] Saved UIRoot. Fixed {fixCount} Canvas(es).");
        }
        else
        {
            Debug.Log("[FixUIRootCanvasScale] Không cần fix gì.");
        }

        PrefabUtility.UnloadPrefabContents(editRoot);
    }
}
