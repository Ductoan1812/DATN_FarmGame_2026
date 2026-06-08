using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class MenuWindowShellUI
{
    public static readonly Color RootColor = new(0.96f, 0.82f, 0.52f, 0.96f);
    public static readonly Color OutlineColor = new(0.78f, 0.54f, 0.20f, 1f);
    public static readonly Color HeaderColor = new(0.30f, 0.17f, 0.07f, 0.95f);
    public static readonly Color HeaderTextColor = new(0.98f, 0.88f, 0.55f, 1f);
    public static readonly Color BodyTextColor = new(0.14f, 0.08f, 0.03f, 1f);
    public static readonly Color SurfaceColor = new(0.34f, 0.20f, 0.08f, 1f);
    public static readonly Color SurfaceAltColor = new(0.25f, 0.14f, 0.06f, 0.85f);
    public static readonly Color AccentColor = new(0.86f, 0.55f, 0.18f, 1f);
    public static readonly Color AccentSoftColor = new(1f, 0.90f, 0.66f, 1f);

    public static RectTransform BuildShell(Transform root, string title, Vector2 bodyAnchoredPosition, Vector2 bodySizeDelta)
    {
        if (root == null)
            return null;

        var rootImage = root.GetComponent<Image>() ?? root.gameObject.AddComponent<Image>();
        rootImage.color = RootColor;

        var outline = root.GetComponent<Outline>() ?? root.gameObject.AddComponent<Outline>();
        outline.effectColor = OutlineColor;
        outline.effectDistance = new Vector2(3f, -3f);

        var header = CreateUiObject("Header", root);
        SetRect(header, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 72f));
        var headerImage = header.gameObject.AddComponent<Image>();
        headerImage.color = HeaderColor;

        var titleText = CreateText("TitleText", header, title, 28f, TextAlignmentOptions.Center, HeaderTextColor);
        Stretch(titleText.rectTransform, new Vector2(24f, 0f), new Vector2(-24f, 0f));

        var body = CreateUiObject("Body", root);
        SetRect(body, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), bodyAnchoredPosition, bodySizeDelta);
        return body;
    }

    public static void ClearChildren(Transform root)
    {
        if (root == null)
            return;

        for (int i = root.childCount - 1; i >= 0; i--)
            DestroyChild(root.GetChild(i).gameObject);
    }

    public static Transform FindDeepChild(Transform root, string name)
    {
        if (root == null)
            return null;
        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindDeepChild(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }

    public static RectTransform CreateUiObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    public static TextMeshProUGUI CreateText(string name, Transform parent, string value, float size, TextAlignmentOptions alignment, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.alignment = alignment;
        text.color = color;
        text.enableWordWrapping = true;
        UiTextStyleUtility.ApplyRoboto(text);
        return text;
    }

    public static Image CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    public static Button CreateButton(string name, Transform parent, Color color)
    {
        var image = CreateImage(name, parent, color);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        return button;
    }

    public static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    public static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private static void DestroyChild(GameObject child)
    {
        if (child == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(child);
        else
            Object.DestroyImmediate(child);
    }
}
