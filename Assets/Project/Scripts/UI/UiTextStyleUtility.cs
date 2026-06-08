using TMPro;
using UnityEngine;

public static class UiTextStyleUtility
{
    public const string RobotoResourcePath = "Fonts & Materials/Roboto-Bold SDF";

    private static TMP_FontAsset cachedRoboto;

    public static TMP_FontAsset RobotoFont
    {
        get
        {
            if (cachedRoboto == null)
                cachedRoboto = Resources.Load<TMP_FontAsset>(RobotoResourcePath);

            return cachedRoboto;
        }
    }

    public static void ApplyRoboto(TMP_Text text)
    {
        if (text == null)
            return;

        var font = RobotoFont;
        if (font != null)
            text.font = font;
    }

    public static void ApplyRobotoToChildren(Transform root)
    {
        if (root == null)
            return;

        var font = RobotoFont;
        if (font == null)
            return;

        var texts = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null)
                texts[i].font = font;
        }
    }

    public static void ApplyRobotoAndColor(TMP_Text text, Color color)
    {
        if (text == null)
            return;

        ApplyRoboto(text);
        text.color = color;
    }

    public static void ApplyRobotoAndColorToChildren(Transform root, Color color)
    {
        if (root == null)
            return;

        var texts = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
            ApplyRobotoAndColor(texts[i], color);
    }
}
