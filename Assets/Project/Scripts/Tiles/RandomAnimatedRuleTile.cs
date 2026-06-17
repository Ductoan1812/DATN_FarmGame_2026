using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityTileAnimationData = UnityEngine.Tilemaps.TileAnimationData;
using UnityTileData = UnityEngine.Tilemaps.TileData;

[CreateAssetMenu(fileName = "NewRandomAnimatedRuleTile", menuName = "2D/Tiles/Random Animated Rule Tile")]
public class RandomAnimatedRuleTile : RuleTile
{
    [Serializable]
    public class RandomAnimationSettings
    {
        public int ruleId;
        public int variantCount = 1;
        public int framesPerVariant = 1;
    }

    [HideInInspector] public List<RandomAnimationSettings> m_RandomAnimationSettings = new();

    public RandomAnimationSettings GetRandomAnimationSettings(int ruleId, bool createIfMissing = false)
    {
        for (int i = 0; i < m_RandomAnimationSettings.Count; i++)
        {
            if (m_RandomAnimationSettings[i].ruleId == ruleId)
                return m_RandomAnimationSettings[i];
        }

        if (!createIfMissing)
            return null;

        var settings = new RandomAnimationSettings
        {
            ruleId = ruleId,
            variantCount = 1,
            framesPerVariant = 1
        };
        m_RandomAnimationSettings.Add(settings);
        return settings;
    }

    public void ConfigureRandomRule(TilingRule rule, int variantCount, int framesPerVariant)
    {
        if (rule == null)
            return;

        var settings = GetRandomAnimationSettings(rule.m_Id, true);
        settings.variantCount = Mathf.Max(1, variantCount);
        settings.framesPerVariant = Mathf.Max(1, framesPerVariant);
        Array.Resize(ref rule.m_Sprites, settings.variantCount * settings.framesPerVariant);
    }

    public void SyncRandomSettingsWithRules()
    {
        var validRuleIds = new HashSet<int>();
        for (int i = 0; i < m_TilingRules.Count; i++)
        {
            var rule = m_TilingRules[i];
            validRuleIds.Add(rule.m_Id);

            if (rule.m_Output != TilingRuleOutput.OutputSprite.Random)
                continue;

            var settings = GetRandomAnimationSettings(rule.m_Id, true);
            settings.framesPerVariant = Mathf.Max(1, settings.framesPerVariant);
            settings.variantCount = Mathf.Max(1, settings.variantCount);

            int spriteCount = rule.m_Sprites?.Length ?? 0;
            if (spriteCount <= 0)
            {
                Array.Resize(ref rule.m_Sprites, settings.variantCount * settings.framesPerVariant);
                continue;
            }

            int expectedCount = settings.variantCount * settings.framesPerVariant;
            if (spriteCount != expectedCount)
            {
                if (spriteCount % settings.framesPerVariant == 0)
                {
                    settings.variantCount = Mathf.Max(1, spriteCount / settings.framesPerVariant);
                }
                else
                {
                    Array.Resize(ref rule.m_Sprites, expectedCount);
                }
            }
        }

        m_RandomAnimationSettings.RemoveAll(settings => !validRuleIds.Contains(settings.ruleId));
    }

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref UnityTileData tileData)
    {
        if (TryGetRandomVariant(position, tilemap, out var variant, out var rule, out var transform))
        {
            tileData.sprite = variant.frames.Length > 0 ? variant.frames[0] : m_DefaultSprite;
            tileData.gameObject = rule.m_GameObject != null ? rule.m_GameObject : m_DefaultGameObject;
            tileData.colliderType = rule.m_ColliderType;
            tileData.flags = TileFlags.LockTransform;
            tileData.transform = transform;

            if (rule.m_RandomTransform != TilingRuleOutput.Transform.Fixed)
                tileData.transform = ApplyRandomTransform(rule.m_RandomTransform, transform, rule.m_PerlinScale, position);

            return;
        }

        base.GetTileData(position, tilemap, ref tileData);
    }

    public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref UnityTileAnimationData tileAnimationData)
    {
        if (TryGetRandomVariant(position, tilemap, out var variant, out var rule, out _))
        {
            if (variant.frames.Length <= 1)
                return false;

            tileAnimationData.animatedSprites = variant.frames;
            tileAnimationData.animationSpeed = UnityEngine.Random.Range(rule.m_MinAnimationSpeed, rule.m_MaxAnimationSpeed);
            return true;
        }

        return base.GetTileAnimationData(position, tilemap, ref tileAnimationData);
    }

    private bool TryGetRandomVariant(Vector3Int position, ITilemap tilemap, out RandomVariant variant, out TilingRule matchedRule, out Matrix4x4 transform)
    {
        variant = default;
        matchedRule = null;
        transform = Matrix4x4.identity;

        for (int i = 0; i < m_TilingRules.Count; i++)
        {
            var rule = m_TilingRules[i];
            var matchedTransform = Matrix4x4.identity;
            if (!RuleMatches(rule, position, tilemap, ref matchedTransform))
                continue;

            if (rule.m_Output != TilingRuleOutput.OutputSprite.Random)
                return false;

            var settings = GetRandomAnimationSettings(rule.m_Id);
            if (settings == null || rule.m_Sprites == null || rule.m_Sprites.Length == 0)
                return false;

            int framesPerVariant = Mathf.Max(1, settings.framesPerVariant);
            int variantCount = Mathf.Max(1, Math.Min(settings.variantCount, rule.m_Sprites.Length / framesPerVariant));
            if (variantCount <= 0)
                return false;

            int index = Mathf.Clamp(
                Mathf.FloorToInt(GetPerlinValue(position, rule.m_PerlinScale, 100000f) * variantCount),
                0,
                variantCount - 1);

            variant = ExtractVariant(rule.m_Sprites, index, framesPerVariant);
            matchedRule = rule;
            transform = matchedTransform;
            return variant.frames.Length > 0;
        }

        return false;
    }

    private RandomVariant ExtractVariant(Sprite[] sprites, int variantIndex, int framesPerVariant)
    {
        int startIndex = variantIndex * framesPerVariant;
        int safeLength = Mathf.Max(0, Mathf.Min(framesPerVariant, sprites.Length - startIndex));

        if (safeLength <= 0)
            return default;

        var frames = new List<Sprite>(safeLength);
        for (int i = 0; i < safeLength; i++)
        {
            var frame = sprites[startIndex + i];
            if (frame != null)
                frames.Add(frame);
        }

        return new RandomVariant { frames = frames.ToArray() };
    }

    private struct RandomVariant
    {
        public Sprite[] frames;
    }
}
