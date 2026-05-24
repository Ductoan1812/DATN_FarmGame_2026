using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

public class CreateWateredTilemap
{
    public static void Execute()
    {
        // Tìm Grid object
        var grid = GameObject.Find("_____Environment______/Environment /Grid");
        if (grid == null)
        {
            Debug.LogError("Không tìm thấy Grid!");
            return;
        }

        // Check đã có Tm_Watered chưa
        var existing = grid.transform.Find("Tm_Watered");
        if (existing != null)
        {
            Debug.Log("Tm_Watered đã tồn tại!");
            return;
        }

        // Tạo GameObject mới
        var go = new GameObject("Tm_Watered");
        go.transform.SetParent(grid.transform, false);

        // Thêm Tilemap + TilemapRenderer
        var tilemap = go.AddComponent<Tilemap>();
        var renderer = go.AddComponent<TilemapRenderer>();

        // Set sorting order giữa Ground (0) và Building
        renderer.sortingOrder = 1; // Ground = 0, Watered = 1, Building sẽ cao hơn

        // Set layer giống Tm_Ground
        go.layer = LayerMask.NameToLayer("Ground  ");

        // Đặt sibling index = 1 (ngay sau Tm_Ground ở index 0)
        go.transform.SetSiblingIndex(1);

        // Gán reference vào GameManager
        var gm = Object.FindAnyObjectByType<GameManager>();
        if (gm != null)
        {
            var so = new SerializedObject(gm);
            var prop = so.FindProperty("tmWatered");
            if (prop != null)
            {
                prop.objectReferenceValue = tilemap;
                so.ApplyModifiedProperties();
                Debug.Log("Đã gán tmWatered vào GameManager!");
            }
            else
            {
                Debug.LogWarning("Không tìm thấy field tmWatered trên GameManager. Hãy gán thủ công.");
            }
        }

        EditorUtility.SetDirty(go);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("Đã tạo Tm_Watered tilemap thành công! SortingOrder=1, SiblingIndex=1");
    }
}
