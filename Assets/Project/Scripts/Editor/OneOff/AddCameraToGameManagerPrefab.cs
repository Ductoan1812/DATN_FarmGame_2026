using UnityEditor;
using UnityEngine;

/// <summary>
/// Thêm Main Camera + SceneCameraFollower vào GameManager prefab.
/// Chạy 1 lần qua: Tools > DATN > Add Camera to GameManager Prefab
/// </summary>
public static class AddCameraToGameManagerPrefab
{
    private const string PrefabPath = "Assets/Project/Prefabs/Systems/_____GameManager____.prefab";

    [MenuItem("Tools/DATN/Add Camera to GameManager Prefab")]
    public static void Execute()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[AddCameraToGM] Prefab not found at {PrefabPath}");
            return;
        }

        // Mở prefab để edit
        var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);

        try
        {
            // Tìm hoặc tạo Cameras container
            var camerasGO = root.transform.Find("Cameras")?.gameObject;
            if (camerasGO == null)
            {
                camerasGO = new GameObject("Cameras");
                camerasGO.transform.SetParent(root.transform, false);
                Debug.Log("[AddCameraToGM] Created 'Cameras' child.");
            }

            // Tìm hoặc tạo Main Camera
            var camTransform = camerasGO.transform.Find("Main Camera");
            GameObject camGO;
            Camera cam;

            if (camTransform == null)
            {
                camGO = new GameObject("Main Camera");
                camGO.transform.SetParent(camerasGO.transform, false);
                camGO.transform.localPosition = new Vector3(0f, 0f, -10f);
                camGO.tag = "MainCamera";
                Debug.Log("[AddCameraToGM] Created 'Main Camera' child.");
            }
            else
            {
                camGO = camTransform.gameObject;
                camGO.tag = "MainCamera";
                Debug.Log("[AddCameraToGM] Found existing 'Main Camera'.");
            }

            // Thêm Camera component nếu chưa có
            cam = camGO.GetComponent<Camera>();
            if (cam == null)
            {
                cam = camGO.AddComponent<Camera>();
                Debug.Log("[AddCameraToGM] Added Camera component.");
            }

            // Cấu hình Camera
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 100f;
            cam.cullingMask = -1; // Everything

            // Xóa CinemachineBrain nếu còn
            var brain = camGO.GetComponent<Cinemachine.CinemachineBrain>();
            if (brain != null)
            {
                Object.DestroyImmediate(brain);
                Debug.Log("[AddCameraToGM] Removed CinemachineBrain.");
            }

            // Thêm SceneCameraFollower nếu chưa có
            if (camGO.GetComponent<SceneCameraFollower>() == null)
            {
                camGO.AddComponent<SceneCameraFollower>();
                Debug.Log("[AddCameraToGM] Added SceneCameraFollower.");
            }
            else
            {
                Debug.Log("[AddCameraToGM] SceneCameraFollower already present.");
            }

            // Xóa CinemachineVirtualCamera objects nếu còn
            for (int i = camerasGO.transform.childCount - 1; i >= 0; i--)
            {
                var child = camerasGO.transform.GetChild(i);
                if (child.GetComponent<Cinemachine.CinemachineVirtualCamera>() != null)
                {
                    Object.DestroyImmediate(child.gameObject);
                    Debug.Log($"[AddCameraToGM] Removed CinemachineVirtualCamera object '{child.name}'.");
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log($"[AddCameraToGM] Done. GameManager prefab updated with Main Camera + SceneCameraFollower.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.Refresh();
    }
}
