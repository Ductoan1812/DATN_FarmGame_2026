using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class GenerateMineSpawnAssets
{
    private const string ResourceEntityRoot = "Assets/Project/Resources/Data/Entities/World/Resources";
    private const string MarkerRoot = "Assets/Project/Resources/Data/SceneMarkers/Mine/SpawnTiles";
    private const string RegionRoot = "Assets/Project/Resources/Data/SceneMarkers/Mine/RuleRegions";

    private enum MarkerCategory
    {
        Stone,
        Iron,
        Gold,
        Emerald,
        Ruby,
        Sapphire,
        StoneEmerald,
        StoneRuby,
        StoneSapphire
    }

    private sealed class NodeInfo
    {
        public string assetPath;
        public string assetName;
        public EntityData entityData;
        public MarkerCategory category;
        public bool isLargeNode;
    }

    private readonly struct PoolRequest
    {
        public PoolRequest(string poolKey, int variantCount, int minCount, int maxCount, int poolOffset = 0)
        {
            PoolKey = poolKey;
            VariantCount = variantCount;
            MinCount = minCount;
            MaxCount = maxCount;
            PoolOffset = poolOffset;
        }

        public string PoolKey { get; }
        public int VariantCount { get; }
        public int MinCount { get; }
        public int MaxCount { get; }
        public int PoolOffset { get; }
    }

    private readonly struct LevelDefinition
    {
        public LevelDefinition(int levelIndex, params PoolRequest[] requests)
        {
            LevelIndex = levelIndex;
            Requests = requests ?? Array.Empty<PoolRequest>();
        }

        public int LevelIndex { get; }
        public PoolRequest[] Requests { get; }
    }

    [MenuItem("Tools/DATN/Content/Generate Mine Spawn Assets")]
    public static void GenerateAssets()
    {
        var nodes = LoadNodes();
        if (nodes.Count == 0)
        {
            Debug.LogWarning("[GenerateMineSpawnAssets] No mine resource EntityData assets were found.");
            return;
        }

        var markerByAssetName = GenerateSpawnTiles(nodes);
        GenerateRuleRegions(markerByAssetName);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[GenerateMineSpawnAssets] Generated/updated {markerByAssetName.Count} mine spawn tiles and 20 mine rule-region presets.");
    }

    public static void Execute()
    {
        GenerateAssets();
    }

    private static List<NodeInfo> LoadNodes()
    {
        var guids = AssetDatabase.FindAssets("t:EntityData", new[] { ResourceEntityRoot });
        var results = new List<NodeInfo>(guids.Length);

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var entityData = AssetDatabase.LoadAssetAtPath<EntityData>(path);
            if (entityData == null)
                continue;

            string assetName = Path.GetFileNameWithoutExtension(path);
            results.Add(new NodeInfo
            {
                assetPath = path,
                assetName = assetName,
                entityData = entityData,
                category = ResolveCategory(assetName),
                isLargeNode = assetName.IndexOf("big", StringComparison.OrdinalIgnoreCase) >= 0
            });
        }

        results.Sort((a, b) => string.Compare(a.assetName, b.assetName, StringComparison.OrdinalIgnoreCase));
        return results;
    }

    private static Dictionary<string, SceneSpawnTile> GenerateSpawnTiles(IEnumerable<NodeInfo> nodes)
    {
        EnsureFolder(MarkerRoot);

        var markerByAssetName = new Dictionary<string, SceneSpawnTile>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in nodes)
        {
            string folder = Path.Combine(MarkerRoot, ResolveFolderName(node.category)).Replace("\\", "/");
            EnsureFolder(folder);

            string assetPath = $"{folder}/Marker_Mine_{node.assetName}.asset";
            var marker = LoadOrCreateAsset<SceneSpawnTile>(assetPath);
            marker.markerKind = ResolveMarkerKind(node.category);
            marker.objectType = ResolveObjectType(node.category);
            marker.entityData = node.entityData;
            marker.savePolicy = SceneEntitySavePolicy.Persistent;
            marker.spawnGroupId = string.Empty;
            marker.spawnPointId = string.Empty;
            marker.respawnMinutes = 0;
            marker.initialAmount = 1;
            marker.bypassPlacementValidation = false;
            marker.stageSpawnMode = MarkerStageSpawnMode.Default;
            marker.fixedStartStageIndex = 0;
            marker.randomStartStageMin = 0;
            marker.randomStartStageMax = 0;
            marker.editorSprite = ResolvePreviewSprite(node.entityData);
            marker.editorColor = ResolveMarkerColor(node.category, node.isLargeNode);
            EditorUtility.SetDirty(marker);

            markerByAssetName[node.assetName] = marker;
        }

        return markerByAssetName;
    }

    private static void GenerateRuleRegions(IReadOnlyDictionary<string, SceneSpawnTile> markerByAssetName)
    {
        EnsureFolder(RegionRoot);

        var pools = BuildPools(markerByAssetName);
        var levels = BuildLevelDefinitions();

        foreach (var level in levels)
        {
            string assetPath = $"{RegionRoot}/RuleRegion_Mine_Level_{level.LevelIndex:00}.asset";
            var region = LoadOrCreateAsset<SceneSpawnRuleRegionTile>(assetPath);
            region.regionKey = $"mine_level_{level.LevelIndex:00}";
            region.entries = BuildEntriesForLevel(level, pools);
            region.editorSprite = ResolveRegionPreviewSprite(region.entries);
            region.editorColor = ResolveLevelColor(level.LevelIndex);
            EditorUtility.SetDirty(region);
        }
    }

    private static SceneSpawnRuleEntry[] BuildEntriesForLevel(
        LevelDefinition level,
        IReadOnlyDictionary<string, List<SceneSpawnTile>> pools)
    {
        var entries = new List<SceneSpawnRuleEntry>();

        foreach (var request in level.Requests)
        {
            if (!pools.TryGetValue(request.PoolKey, out var pool) || pool == null || pool.Count == 0)
                continue;

            int variantCount = Mathf.Clamp(request.VariantCount, 1, pool.Count);
            int offset = (level.LevelIndex - 1 + request.PoolOffset) % pool.Count;

            for (int i = 0; i < variantCount; i++)
            {
                var marker = pool[(offset + i) % pool.Count];
                entries.Add(new SceneSpawnRuleEntry
                {
                    entryId = BuildEntryId(level.LevelIndex, marker, i),
                    markerTile = marker,
                    initialCountMin = Mathf.Max(0, request.MinCount),
                    initialCountMax = Mathf.Max(request.MinCount, request.MaxCount),
                    spawnMode = SceneSpawnRuleMode.SpawnFreshOnSceneLoad,
                    respawnDelaySeconds = 45f,
                    respawnCountMin = 1,
                    respawnCountMax = 1,
                    spawnGroupOverride = string.Empty
                });
            }
        }

        return entries.ToArray();
    }

    private static IReadOnlyDictionary<string, List<SceneSpawnTile>> BuildPools(
        IReadOnlyDictionary<string, SceneSpawnTile> markerByAssetName)
    {
        var pools = new Dictionary<string, List<SceneSpawnTile>>(StringComparer.OrdinalIgnoreCase)
        {
            ["CommonStone"] = ResolvePool(markerByAssetName, "world_node_rock", "node_stone8", "node_stone9", "node_stone10", "node_stone11", "node_stone12"),
            ["RareStone"] = ResolvePool(markerByAssetName, "node_BigStone1"),
            ["Iron"] = ResolvePool(markerByAssetName, "node_iron2", "node_iron3", "node_iron4", "node_iron5", "node_iron6", "world_node_iron"),
            ["BigIron"] = ResolvePool(markerByAssetName, "node_bigiron1"),
            ["Gold"] = ResolvePool(markerByAssetName, "node_gold2", "node_gold3", "node_gold4", "node_gold5", "node_gold6"),
            ["BigGold"] = ResolvePool(markerByAssetName, "world_node_gold", "node_biggold1"),
            ["EmeraldPure"] = ResolvePool(markerByAssetName, "node_Emerald1", "node_Emerald2", "node_Emerald3", "node_Emerald4"),
            ["RubyPure"] = ResolvePool(markerByAssetName, "node_ruby1", "node_ruby2", "node_ruby3", "node_ruby4"),
            ["SapphirePure"] = ResolvePool(markerByAssetName, "node_sapphire1", "node_sapphire2", "node_sapphire3", "node_sapphire4"),
            ["EmeraldHybrid"] = ResolvePool(markerByAssetName, "node_StoneEmerald", "node_StoneEmerald1", "node_StoneEmerald2", "node_StoneEmerald3", "node_StoneEmerald4", "node_StoneEmerald6"),
            ["RubyHybrid"] = ResolvePool(markerByAssetName, "node_stoneRuby1", "node_stoneRuby2", "node_stoneRuby3", "node_stoneRuby4", "node_stoneRuby5", "node_stoneRuby6"),
            ["SapphireHybrid"] = ResolvePool(markerByAssetName, "node_StoneSapphire1", "node_StoneSapphire2", "node_StoneSapphire3", "node_StoneSapphire4", "node_StoneSapphire5", "node_StoneSapphire6"),
            ["BigEmeraldHybrid"] = ResolvePool(markerByAssetName, "node_BigStoneEmerald1"),
            ["BigRubyHybrid"] = ResolvePool(markerByAssetName, "node_BigstoneRuby1"),
            ["BigSapphireHybrid"] = ResolvePool(markerByAssetName, "node_BigStooneSapphire1")
        };

        return pools;
    }

    private static LevelDefinition[] BuildLevelDefinitions()
    {
        return new[]
        {
            new LevelDefinition(1,
                new PoolRequest("CommonStone", 4, 1, 3)),
            new LevelDefinition(2,
                new PoolRequest("CommonStone", 5, 1, 3),
                new PoolRequest("Iron", 1, 0, 1)),
            new LevelDefinition(3,
                new PoolRequest("CommonStone", 5, 1, 3),
                new PoolRequest("Iron", 2, 1, 2)),
            new LevelDefinition(4,
                new PoolRequest("CommonStone", 4, 1, 2),
                new PoolRequest("Iron", 3, 1, 2)),
            new LevelDefinition(5,
                new PoolRequest("CommonStone", 4, 1, 2),
                new PoolRequest("Iron", 4, 1, 2),
                new PoolRequest("RareStone", 1, 0, 1)),
            new LevelDefinition(6,
                new PoolRequest("CommonStone", 3, 1, 2),
                new PoolRequest("Iron", 4, 1, 2),
                new PoolRequest("Gold", 1, 0, 1)),
            new LevelDefinition(7,
                new PoolRequest("CommonStone", 3, 1, 2),
                new PoolRequest("Iron", 4, 1, 2),
                new PoolRequest("Gold", 2, 1, 2)),
            new LevelDefinition(8,
                new PoolRequest("CommonStone", 2, 1, 2),
                new PoolRequest("Iron", 4, 1, 2),
                new PoolRequest("Gold", 3, 1, 2),
                new PoolRequest("EmeraldHybrid", 1, 0, 1)),
            new LevelDefinition(9,
                new PoolRequest("Iron", 4, 1, 2),
                new PoolRequest("Gold", 3, 1, 2),
                new PoolRequest("EmeraldHybrid", 2, 0, 1),
                new PoolRequest("RubyHybrid", 1, 0, 1),
                new PoolRequest("EmeraldPure", 1, 0, 1)),
            new LevelDefinition(10,
                new PoolRequest("Iron", 3, 1, 2),
                new PoolRequest("Gold", 4, 1, 2),
                new PoolRequest("EmeraldHybrid", 1, 0, 1),
                new PoolRequest("RubyHybrid", 1, 0, 1),
                new PoolRequest("SapphireHybrid", 1, 0, 1),
                new PoolRequest("EmeraldPure", 1, 0, 1)),
            new LevelDefinition(11,
                new PoolRequest("Iron", 2, 1, 2),
                new PoolRequest("Gold", 4, 1, 2),
                new PoolRequest("EmeraldHybrid", 2, 0, 1),
                new PoolRequest("RubyHybrid", 1, 0, 1),
                new PoolRequest("SapphireHybrid", 1, 0, 1),
                new PoolRequest("RareStone", 1, 0, 1),
                new PoolRequest("BigIron", 1, 0, 1)),
            new LevelDefinition(12,
                new PoolRequest("Gold", 4, 1, 2),
                new PoolRequest("BigGold", 1, 0, 1),
                new PoolRequest("EmeraldPure", 2, 0, 1),
                new PoolRequest("RubyPure", 1, 0, 1),
                new PoolRequest("SapphirePure", 1, 0, 1),
                new PoolRequest("EmeraldHybrid", 1, 0, 1),
                new PoolRequest("RubyHybrid", 1, 0, 1)),
            new LevelDefinition(13,
                new PoolRequest("Gold", 3, 1, 2),
                new PoolRequest("BigGold", 1, 0, 1),
                new PoolRequest("EmeraldPure", 2, 1, 2),
                new PoolRequest("EmeraldHybrid", 2, 1, 2),
                new PoolRequest("RubyPure", 1, 0, 1),
                new PoolRequest("SapphirePure", 1, 0, 1)),
            new LevelDefinition(14,
                new PoolRequest("Gold", 3, 1, 2),
                new PoolRequest("RubyPure", 2, 1, 2),
                new PoolRequest("RubyHybrid", 2, 1, 2),
                new PoolRequest("EmeraldPure", 1, 0, 1),
                new PoolRequest("SapphirePure", 1, 0, 1),
                new PoolRequest("BigRubyHybrid", 1, 0, 1)),
            new LevelDefinition(15,
                new PoolRequest("Gold", 3, 1, 2),
                new PoolRequest("SapphirePure", 2, 1, 2),
                new PoolRequest("SapphireHybrid", 2, 1, 2),
                new PoolRequest("EmeraldPure", 1, 0, 1),
                new PoolRequest("RubyPure", 1, 0, 1),
                new PoolRequest("BigSapphireHybrid", 1, 0, 1)),
            new LevelDefinition(16,
                new PoolRequest("BigGold", 1, 0, 1),
                new PoolRequest("Gold", 3, 1, 2),
                new PoolRequest("EmeraldPure", 1, 1, 2),
                new PoolRequest("RubyPure", 1, 1, 2),
                new PoolRequest("SapphirePure", 1, 1, 2),
                new PoolRequest("EmeraldHybrid", 1, 1, 2),
                new PoolRequest("RubyHybrid", 1, 1, 2),
                new PoolRequest("SapphireHybrid", 1, 1, 2),
                new PoolRequest("BigEmeraldHybrid", 1, 0, 1)),
            new LevelDefinition(17,
                new PoolRequest("BigIron", 1, 0, 1),
                new PoolRequest("BigGold", 1, 0, 1),
                new PoolRequest("Gold", 3, 1, 2),
                new PoolRequest("EmeraldPure", 2, 1, 2),
                new PoolRequest("RubyPure", 2, 1, 2),
                new PoolRequest("SapphirePure", 2, 1, 2)),
            new LevelDefinition(18,
                new PoolRequest("BigIron", 1, 0, 1),
                new PoolRequest("BigGold", 2, 0, 1),
                new PoolRequest("EmeraldHybrid", 2, 1, 2),
                new PoolRequest("RubyHybrid", 2, 1, 2),
                new PoolRequest("SapphireHybrid", 2, 1, 2),
                new PoolRequest("BigEmeraldHybrid", 1, 0, 1),
                new PoolRequest("BigRubyHybrid", 1, 0, 1),
                new PoolRequest("BigSapphireHybrid", 1, 0, 1)),
            new LevelDefinition(19,
                new PoolRequest("BigIron", 1, 0, 1),
                new PoolRequest("BigGold", 2, 0, 1),
                new PoolRequest("EmeraldPure", 2, 1, 2),
                new PoolRequest("RubyPure", 2, 1, 2),
                new PoolRequest("SapphirePure", 2, 1, 2),
                new PoolRequest("EmeraldHybrid", 2, 1, 2),
                new PoolRequest("RubyHybrid", 2, 1, 2),
                new PoolRequest("SapphireHybrid", 2, 1, 2),
                new PoolRequest("RareStone", 1, 0, 1)),
            new LevelDefinition(20,
                new PoolRequest("BigGold", 2, 1, 1),
                new PoolRequest("BigIron", 1, 1, 1),
                new PoolRequest("BigEmeraldHybrid", 1, 1, 1),
                new PoolRequest("BigRubyHybrid", 1, 1, 1),
                new PoolRequest("BigSapphireHybrid", 1, 1, 1),
                new PoolRequest("Gold", 3, 1, 2),
                new PoolRequest("EmeraldPure", 2, 1, 2),
                new PoolRequest("RubyPure", 2, 1, 2),
                new PoolRequest("SapphirePure", 2, 1, 2))
        };
    }

    private static List<SceneSpawnTile> ResolvePool(
        IReadOnlyDictionary<string, SceneSpawnTile> markerByAssetName,
        params string[] assetNames)
    {
        var result = new List<SceneSpawnTile>();
        foreach (var assetName in assetNames)
        {
            if (markerByAssetName.TryGetValue(assetName, out var marker) && marker != null)
                result.Add(marker);
        }

        return result;
    }

    private static string BuildEntryId(int levelIndex, SceneSpawnTile marker, int variantIndex)
    {
        string markerName = marker != null ? marker.name : "marker";
        return $"mine_l{levelIndex:00}_{markerName}_{variantIndex + 1}";
    }

    private static Sprite ResolveRegionPreviewSprite(SceneSpawnRuleEntry[] entries)
    {
        if (entries == null)
            return null;

        for (int i = 0; i < entries.Length; i++)
        {
            var sprite = entries[i]?.markerTile?.editorSprite;
            if (sprite != null)
                return sprite;
        }

        return null;
    }

    private static Color ResolveLevelColor(int levelIndex)
    {
        float t = Mathf.InverseLerp(1f, 20f, levelIndex);
        return Color.HSVToRGB(Mathf.Lerp(0.13f, 0.78f, t), Mathf.Lerp(0.45f, 0.8f, t), 1f);
    }

    private static Sprite ResolvePreviewSprite(EntityData entityData)
    {
        if (entityData?.modules != null)
        {
            var stageModule = entityData.modules.OfType<StageModule>().FirstOrDefault();
            if (stageModule?.stages != null && stageModule.stages.Length > 0)
                return stageModule.stages[0].sprite;
        }

        return entityData != null ? entityData.icon : null;
    }

    private static SceneMarkerKind ResolveMarkerKind(MarkerCategory category)
    {
        return category == MarkerCategory.Stone ? SceneMarkerKind.ResourceNode : SceneMarkerKind.Ore;
    }

    private static ObjectType ResolveObjectType(MarkerCategory category)
    {
        switch (category)
        {
            case MarkerCategory.Stone:
            case MarkerCategory.StoneEmerald:
            case MarkerCategory.StoneRuby:
            case MarkerCategory.StoneSapphire:
                return ObjectType.RockNode01;

            default:
                return ObjectType.OreNode01;
        }
    }

    private static string ResolveFolderName(MarkerCategory category)
    {
        return category switch
        {
            MarkerCategory.Stone => "Stone",
            MarkerCategory.Iron => "Iron",
            MarkerCategory.Gold => "Gold",
            MarkerCategory.Emerald => "Emerald",
            MarkerCategory.Ruby => "Ruby",
            MarkerCategory.Sapphire => "Sapphire",
            MarkerCategory.StoneEmerald => "StoneEmerald",
            MarkerCategory.StoneRuby => "StoneRuby",
            MarkerCategory.StoneSapphire => "StoneSapphire",
            _ => "Misc"
        };
    }

    private static Color ResolveMarkerColor(MarkerCategory category, bool isLargeNode)
    {
        Color baseColor = category switch
        {
            MarkerCategory.Stone => new Color(0.78f, 0.78f, 0.78f, 1f),
            MarkerCategory.Iron => new Color(0.82f, 0.62f, 0.38f, 1f),
            MarkerCategory.Gold => new Color(1f, 0.84f, 0.2f, 1f),
            MarkerCategory.Emerald => new Color(0.35f, 0.9f, 0.45f, 1f),
            MarkerCategory.Ruby => new Color(0.92f, 0.3f, 0.38f, 1f),
            MarkerCategory.Sapphire => new Color(0.34f, 0.56f, 1f, 1f),
            MarkerCategory.StoneEmerald => new Color(0.48f, 0.84f, 0.5f, 1f),
            MarkerCategory.StoneRuby => new Color(0.88f, 0.46f, 0.48f, 1f),
            MarkerCategory.StoneSapphire => new Color(0.48f, 0.7f, 0.96f, 1f),
            _ => Color.white
        };

        return isLargeNode ? Color.Lerp(baseColor, Color.white, 0.18f) : baseColor;
    }

    private static MarkerCategory ResolveCategory(string assetName)
    {
        if (assetName.IndexOf("emerald", StringComparison.OrdinalIgnoreCase) >= 0)
            return assetName.IndexOf("stone", StringComparison.OrdinalIgnoreCase) >= 0
                ? MarkerCategory.StoneEmerald
                : MarkerCategory.Emerald;

        if (assetName.IndexOf("ruby", StringComparison.OrdinalIgnoreCase) >= 0)
            return assetName.IndexOf("stone", StringComparison.OrdinalIgnoreCase) >= 0
                ? MarkerCategory.StoneRuby
                : MarkerCategory.Ruby;

        if (assetName.IndexOf("sapphire", StringComparison.OrdinalIgnoreCase) >= 0)
            return assetName.IndexOf("stone", StringComparison.OrdinalIgnoreCase) >= 0
                ? MarkerCategory.StoneSapphire
                : MarkerCategory.Sapphire;

        if (assetName.IndexOf("iron", StringComparison.OrdinalIgnoreCase) >= 0)
            return MarkerCategory.Iron;

        if (assetName.IndexOf("gold", StringComparison.OrdinalIgnoreCase) >= 0)
            return MarkerCategory.Gold;

        return MarkerCategory.Stone;
    }

    private static T LoadOrCreateAsset<T>(string assetPath) where T : ScriptableObject
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (existing != null)
            return existing;

        var created = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(created, assetPath);
        return created;
    }

    private static void EnsureFolder(string folderPath)
    {
        folderPath = folderPath.Replace("\\", "/");
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string parent = Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
        string folderName = Path.GetFileName(folderPath);
        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folderName))
            return;

        EnsureFolder(parent);
        if (!AssetDatabase.IsValidFolder(folderPath))
            AssetDatabase.CreateFolder(parent, folderName);
    }
}
