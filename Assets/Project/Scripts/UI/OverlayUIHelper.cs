using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Resolves overlay UI parent: prefers UIRoot/Canvas_Overlay, falls back to runtime canvas.
/// </summary>
public static class OverlayUIHelper
{
    public static Transform GetOrCreateOverlayRoot(GameObject owner, int sortingOrder)
    {
        var uiRoot = GameObject.Find("UIRoot");
        if (uiRoot != null)
        {
            var overlay = uiRoot.transform.Find("Canvas_Overlay");
            if (overlay != null)
                return overlay;
        }

        var canvas = owner.GetComponent<Canvas>();
        if (canvas == null)
            canvas = owner.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = owner.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = owner.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        if (!owner.GetComponent<GraphicRaycaster>())
            owner.AddComponent<GraphicRaycaster>();

        return owner.transform;
    }
}
