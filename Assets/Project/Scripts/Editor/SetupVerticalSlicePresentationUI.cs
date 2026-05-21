using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

#if UNITY_EDITOR
/// <summary>
/// Editor utility: setup vertical slice presentation UI in scene.
/// MenuItem: Tools/DATN/UI/Setup Vertical Slice Presentation UI
/// Ensures UIRoot → Canvas_Overlay → VerticalSliceMenuUI hierarchy.
/// Idempotent, marks scene dirty.
/// </summary>
public class SetupVerticalSlicePresentationUI
{
    [MenuItem("Tools/DATN/UI/Setup Vertical Slice Presentation UI")]
    public static void Setup()
    {
        // Find or create UIRoot
        var uiRoot = GameObject.Find("UIRoot");
        if (uiRoot == null)
        {
            uiRoot = new GameObject("UIRoot");
            Debug.Log("[Setup] Created UIRoot.");
        }

        // Find or create Canvas_Overlay under UIRoot
        Transform overlayTransform = uiRoot.transform.Find("Canvas_Overlay");
        GameObject canvasOverlay;
        if (overlayTransform == null)
        {
            canvasOverlay = new GameObject("Canvas_Overlay");
            canvasOverlay.transform.SetParent(uiRoot.transform, false);

            var canvas = canvasOverlay.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasOverlay.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasOverlay.AddComponent<GraphicRaycaster>();
            Debug.Log("[Setup] Created Canvas_Overlay with sortingOrder=100.");
        }
        else
        {
            canvasOverlay = overlayTransform.gameObject;
            var canvas = canvasOverlay.GetComponent<Canvas>();
            if (canvas != null) canvas.sortingOrder = 100;
        }

        // Find or create VerticalSliceMenuUI under Canvas_Overlay
        Transform menuTransform = canvasOverlay.transform.Find("VerticalSliceMenuUI");
        GameObject menuObj;
        if (menuTransform == null)
        {
            menuObj = new GameObject("VerticalSliceMenuUI");
            menuObj.transform.SetParent(canvasOverlay.transform, false);

            var rect = menuObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            menuObj.AddComponent<VerticalSliceMenuUI>();
            Debug.Log("[Setup] Created VerticalSliceMenuUI.");
        }
        else
        {
            menuObj = menuTransform.gameObject;
            if (menuObj.GetComponent<VerticalSliceMenuUI>() == null)
            {
                menuObj.AddComponent<VerticalSliceMenuUI>();
                Debug.Log("[Setup] Added VerticalSliceMenuUI component.");
            }
        }

        // Mark scene dirty; saving can be handled separately to avoid editor stalls.
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[Setup] Vertical Slice Presentation UI setup complete.");
    }

    public static void SetupAndSave()
    {
        Setup();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Setup] Vertical Slice Presentation UI saved.");
    }
}
#endif
