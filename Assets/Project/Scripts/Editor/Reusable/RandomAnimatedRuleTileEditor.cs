using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[CustomEditor(typeof(RandomAnimatedRuleTile))]
[CanEditMultipleObjects]
public class RandomAnimatedRuleTileEditor : RuleTileEditor
{
    private static readonly GUIContent RandomVariantCountLabel = new("Variants", "Số lượng biến thể random trong rule này.");
    private static readonly GUIContent RandomFramesPerVariantLabel = new("Frames", "Số frame cho mỗi biến thể random. Nếu = 1 thì đây là random sprite thường.");

    private RandomAnimatedRuleTile randomTile => target as RandomAnimatedRuleTile;

    public override void OnInspectorGUI()
    {
        randomTile?.SyncRandomSettingsWithRules();
        base.OnInspectorGUI();
        randomTile?.SyncRandomSettingsWithRules();
    }

    protected override void OnDrawElement(Rect rect, int index, bool isactive, bool isfocused)
    {
        RuleTile.TilingRule rule = tile.m_TilingRules[index];
        BoundsInt bounds = GetRuleGUIBounds(rule.GetBounds(), rule);

        float yPos = rect.yMin + 2f;
        float height = rect.height - k_PaddingBetweenRules;
        Vector2 matrixSize = GetMatrixSize(bounds);

        Rect previewRect = new Rect(rect.xMax - k_DefaultElementHeight - 5f, yPos, k_DefaultElementHeight, k_DefaultElementHeight);
        Rect matrixRect = new Rect(rect.xMax - matrixSize.x - previewRect.width - 10f, yPos, matrixSize.x, matrixSize.y);
        Rect inspectorRect = new Rect(rect.xMin, yPos, rect.width - matrixSize.x - previewRect.width - 20f, height);

        DrawRuleInspector(inspectorRect, rule);
        RuleMatrixOnGUI(tile, matrixRect, bounds, rule);
        DrawRulePreview(previewRect, rule);
    }

    private void DrawRuleInspector(Rect rect, RuleTile.TilingRule rule)
    {
        float y = rect.yMin;

        GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), "GameObject");
        rule.m_GameObject = (GameObject)EditorGUI.ObjectField(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), "", rule.m_GameObject, typeof(GameObject), false);
        y += k_SingleLineHeight;

        GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), "Collider");
        rule.m_ColliderType = (Tile.ColliderType)EditorGUI.EnumPopup(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), rule.m_ColliderType);
        y += k_SingleLineHeight;

        GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), "Output");
        rule.m_Output = (RuleTile.TilingRuleOutput.OutputSprite)EditorGUI.EnumPopup(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), rule.m_Output);
        y += k_SingleLineHeight;

        if (rule.m_Output == RuleTile.TilingRuleOutput.OutputSprite.Animation)
        {
            GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), "Min Speed");
            rule.m_MinAnimationSpeed = EditorGUI.FloatField(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), rule.m_MinAnimationSpeed);
            y += k_SingleLineHeight;

            GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), "Max Speed");
            rule.m_MaxAnimationSpeed = EditorGUI.FloatField(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), rule.m_MaxAnimationSpeed);
            y += k_SingleLineHeight;

            GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), "Size");
            EditorGUI.BeginChangeCheck();
            int newLength = EditorGUI.DelayedIntField(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), rule.m_Sprites.Length);
            if (EditorGUI.EndChangeCheck())
                Array.Resize(ref rule.m_Sprites, Math.Max(newLength, 1));
            y += k_SingleLineHeight;

            for (int i = 0; i < rule.m_Sprites.Length; i++)
            {
                rule.m_Sprites[i] = EditorGUI.ObjectField(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), rule.m_Sprites[i], typeof(Sprite), false) as Sprite;
                y += k_SingleLineHeight;
            }

            return;
        }

        if (rule.m_Output == RuleTile.TilingRuleOutput.OutputSprite.Random)
        {
            var settings = randomTile.GetRandomAnimationSettings(rule.m_Id, true);
            settings.variantCount = Mathf.Max(1, settings.variantCount);
            settings.framesPerVariant = Mathf.Max(1, settings.framesPerVariant);

            int expectedSpriteCount = settings.variantCount * settings.framesPerVariant;
            if (rule.m_Sprites == null || rule.m_Sprites.Length != expectedSpriteCount)
                randomTile.ConfigureRandomRule(rule, settings.variantCount, settings.framesPerVariant);

            GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), "Noise");
            rule.m_PerlinScale = EditorGUI.Slider(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), rule.m_PerlinScale, 0.001f, 0.999f);
            y += k_SingleLineHeight;

            GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), "Shuffle");
            rule.m_RandomTransform = (RuleTile.TilingRuleOutput.Transform)EditorGUI.EnumPopup(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), rule.m_RandomTransform);
            y += k_SingleLineHeight;

            GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), RandomVariantCountLabel);
            EditorGUI.BeginChangeCheck();
            int variantCount = EditorGUI.DelayedIntField(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), settings.variantCount);
            if (EditorGUI.EndChangeCheck())
            {
                settings.variantCount = Mathf.Max(1, variantCount);
                randomTile.ConfigureRandomRule(rule, settings.variantCount, settings.framesPerVariant);
            }
            y += k_SingleLineHeight;

            GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), RandomFramesPerVariantLabel);
            EditorGUI.BeginChangeCheck();
            int framesPerVariant = EditorGUI.DelayedIntField(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), settings.framesPerVariant);
            if (EditorGUI.EndChangeCheck())
            {
                settings.framesPerVariant = Mathf.Max(1, framesPerVariant);
                randomTile.ConfigureRandomRule(rule, settings.variantCount, settings.framesPerVariant);
            }
            y += k_SingleLineHeight;

            DrawRandomVariantGrid(rect, ref y, rule, settings);
        }
    }

    private void DrawRandomVariantGrid(Rect rect, ref float y, RuleTile.TilingRule rule, RandomAnimatedRuleTile.RandomAnimationSettings settings)
    {
        settings.variantCount = Mathf.Max(1, settings.variantCount);
        settings.framesPerVariant = Mathf.Max(1, settings.framesPerVariant);
        randomTile.ConfigureRandomRule(rule, settings.variantCount, settings.framesPerVariant);

        float availableWidth = Mathf.Max(80f, rect.width - k_LabelWidth);
        float cellGap = 4f;
        float labelWidth = 56f;
        float cellWidth = Mathf.Max(48f, (availableWidth - labelWidth - cellGap * Mathf.Max(0, settings.framesPerVariant - 1)) / settings.framesPerVariant);

        for (int variantIndex = 0; variantIndex < settings.variantCount; variantIndex++)
        {
            GUI.Label(new Rect(rect.xMin, y, labelWidth, k_SingleLineHeight), $"Set {variantIndex + 1}");

            for (int frameIndex = 0; frameIndex < settings.framesPerVariant; frameIndex++)
            {
                int spriteIndex = (variantIndex * settings.framesPerVariant) + frameIndex;
                Rect fieldRect = new Rect(
                    rect.xMin + k_LabelWidth + frameIndex * (cellWidth + cellGap),
                    y,
                    cellWidth,
                    k_SingleLineHeight);

                rule.m_Sprites[spriteIndex] = EditorGUI.ObjectField(fieldRect, rule.m_Sprites[spriteIndex], typeof(Sprite), false) as Sprite;
            }

            y += k_SingleLineHeight;
        }
    }

    private void DrawRulePreview(Rect rect, RuleTile.TilingRule rule)
    {
        if (rule.m_Output == RuleTile.TilingRuleOutput.OutputSprite.Random)
        {
            var settings = randomTile.GetRandomAnimationSettings(rule.m_Id);
            if (settings == null || rule.m_Sprites == null || rule.m_Sprites.Length == 0)
            {
                base.SpriteOnGUI(rect, rule);
                return;
            }

            int frameCount = Mathf.Max(1, settings.framesPerVariant);
            int previewIndex = FindPreviewSpriteIndex(rule.m_Sprites, frameCount);
            if (previewIndex >= 0)
            {
                EditorGUI.ObjectField(rect, rule.m_Sprites[previewIndex], typeof(Sprite), false);
                return;
            }

            base.SpriteOnGUI(rect, rule);
            return;
        }

        base.SpriteOnGUI(rect, rule);
    }

    private int FindPreviewSpriteIndex(Sprite[] sprites, int framesPerVariant)
    {
        int safeFrameCount = Mathf.Max(1, framesPerVariant);
        for (int variantIndex = 0; variantIndex < sprites.Length; variantIndex += safeFrameCount)
        {
            for (int frameIndex = 0; frameIndex < safeFrameCount && variantIndex + frameIndex < sprites.Length; frameIndex++)
            {
                if (sprites[variantIndex + frameIndex] != null)
                    return variantIndex + frameIndex;
            }
        }

        return -1;
    }
}
