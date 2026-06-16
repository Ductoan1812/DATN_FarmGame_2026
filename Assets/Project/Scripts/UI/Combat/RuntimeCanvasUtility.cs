using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class RuntimeCanvasUtility
{
    private static Sprite defaultUiSprite;

    public static Canvas CreateOverlayCanvas(string name, Transform parent, int sortingOrder)
    {
        var go = new GameObject(name);
        if (parent != null)
            go.transform.SetParent(parent, false);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    public static TextMeshProUGUI CreateText(Transform parent, string name, int fontSize, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.enableWordWrapping = false;

        var rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(520f, 120f);
        return text;
    }

    public static Image CreateImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.sprite = GetDefaultUiSprite();
        image.type = Image.Type.Simple;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Sprite GetDefaultUiSprite()
    {
        if (defaultUiSprite != null)
            return defaultUiSprite;

        defaultUiSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        if (defaultUiSprite != null)
            return defaultUiSprite;

        defaultUiSprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
            new Vector2(0.5f, 0.5f),
            100f);
        defaultUiSprite.name = "RuntimeCanvasUtility_DefaultSprite";
        return defaultUiSprite;
    }
}
