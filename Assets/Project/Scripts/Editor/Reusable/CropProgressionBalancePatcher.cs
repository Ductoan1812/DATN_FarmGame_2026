#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class CropProgressionBalancePatcher
{
    private const string EntityRoot = "Assets/Project/Resources/Data/Entities";
    private const string ShopPath = EntityRoot + "/Characters/NPCs/npc_shop_bac_ba.asset";

    [MenuItem("FarmGame/Content/Apply Crop Progression Balance")]
    public static void Execute()
    {
        var specs = BuildSpecs();
        var changedAssets = new HashSet<UnityEngine.Object>();
        var seedSet = new HashSet<EntityData>();
        var vi = new Dictionary<string, string>(StringComparer.Ordinal);
        var en = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var spec in specs)
        {
            var seed = LoadEntity(spec.SeedPath);
            var crop = LoadEntity(spec.WorldCropPath);
            var product = LoadEntity(spec.ProductPath);

            if (seed == null || crop == null || product == null)
            {
                Debug.LogWarning($"[CropProgressionBalancePatcher] Missing asset for crop '{spec.Id}'.");
                continue;
            }

            seedSet.Add(seed);
            ConfigureSeed(seed, crop, spec);
            ConfigureProduct(product, spec);
            ConfigureWorldCrop(crop, product, spec);
            AddLocalization(vi, en, spec);

            changedAssets.Add(seed);
            changedAssets.Add(crop);
            changedAssets.Add(product);
        }

        ConfigureShop(specs, seedSet, changedAssets);
        PatchLocalization("Assets/Project/Resources/Localization/vi.json", vi);
        PatchLocalization("Assets/Project/Resources/Localization/en.json", en);

        foreach (var asset in changedAssets)
            EditorUtility.SetDirty(asset);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CropProgressionBalancePatcher] Applied crop progression balance for {specs.Length} crops.");
    }

    private static CropSpec[] BuildSpecs()
    {
        return new[]
        {
            new CropSpec("wheat", "Lua mi", "Wheat", "item_seed_wheat", "world_crop_wheat", 1, 4, 0, 20, 32, 1, 1, 3),
            new CropSpec("turnip", "Cu cai trang", "Turnip", "item_seed_turnip", "world_crop_turnip", 1, 3, 0, 24, 40, 1, 1, 4),
            new CropSpec("carrot", "Ca rot", "Carrot", "seed_carrot_crop", "crop_carrot", 2, 4, 0, 28, 45, 1, 1, 4),
            new CropSpec("potato", "Khoai tay", "Potato", "item_seed_potato", "world_crop_potato", 2, 5, 0, 34, 55, 1, 2, 5),
            new CropSpec("radish", "Cu cai do", "Radish", "seed_radish_crop", "crop_radish", 2, 5, 0, 36, 58, 1, 1, 5),
            new CropSpec("cucumber", "Dua leo", "Cucumber", "seed_cucumber_crop", "crop_cucumber", 3, 6, 3, 70, 44, 1, 2, 5),
            new CropSpec("garlic", "Toi", "Garlic", "seed_garlic_crop", "crop_garlic", 3, 5, 0, 42, 70, 1, 1, 6),
            new CropSpec("pea", "Dau Ha Lan", "Pea", "seed_pea_crop", "crop_pea", 3, 6, 3, 80, 48, 1, 2, 5),
            new CropSpec("bean", "Dau que", "Bean", "seed_bean_crop", "crop_bean", 4, 6, 3, 85, 50, 1, 2, 6),
            new CropSpec("cabbage", "Bap cai", "Cabbage", "seed_cabbage_crop", "crop_cabbage", 4, 6, 0, 55, 90, 1, 1, 7),
            new CropSpec("tomato", "Ca chua", "Tomato", "item_seed_tomato", "world_crop_tomato", 4, 7, 3, 100, 62, 1, 3, 6),
            new CropSpec("pepper_green", "Ot chuong xanh", "Green Pepper", "seed_pepper_green_crop", "crop_pepper_green", 4, 6, 3, 110, 58, 1, 2, 6),
            new CropSpec("corn", "Bap ngo", "Corn", "item_seed_corn", "world_crop_corn", 5, 8, 4, 145, 78, 1, 3, 7),
            new CropSpec("asparagus", "Mang tay", "Asparagus", "seed_asparagus_crop", "crop_asparagus", 5, 7, 3, 130, 70, 1, 2, 7),
            new CropSpec("pepper_red", "Ot chuong do", "Red Pepper", "seed_pepper_red_crop", "crop_pepper_red", 5, 7, 3, 135, 64, 1, 2, 7),
            new CropSpec("cauliflower", "Bong cai trang", "Cauliflower", "seed_cauliflower_crop", "crop_cauliflower", 6, 7, 0, 75, 120, 1, 1, 8),
            new CropSpec("pepper_yellow", "Ot chuong vang", "Yellow Pepper", "seed_pepper_yellow_crop", "crop_pepper_yellow", 6, 7, 3, 145, 68, 1, 2, 7),
            new CropSpec("yam", "Khoai mo", "Yam", "seed_yam_crop", "crop_yam", 6, 6, 0, 60, 96, 1, 2, 8),
            new CropSpec("strawberry", "Dau tay", "Strawberry", "item_seed_strawberry", "world_crop_strawberry", 7, 8, 3, 180, 85, 1, 3, 8),
            new CropSpec("blueberry", "Viet quat", "Blueberry", "seed_blueberry_crop", "crop_blueberry", 7, 9, 4, 210, 75, 2, 4, 8),
            new CropSpec("melon", "Dua gang", "Melon", "seed_melon_crop", "crop_melon", 8, 8, 0, 100, 160, 1, 1, 10),
            new CropSpec("grape", "Nho", "Grape", "seed_grape_crop", "crop_grape", 9, 9, 4, 260, 110, 1, 3, 10),
        };
    }

    private static EntityData LoadEntity(string path)
    {
        return AssetDatabase.LoadAssetAtPath<EntityData>(path);
    }

    private static void ConfigureSeed(EntityData seed, EntityData crop, CropSpec spec)
    {
        seed.id = spec.SeedAssetName;
        seed.keyName = $"item.seed.{spec.Id}.name";
        seed.descKey = $"item.seed.{spec.Id}.desc";
        seed.category = ItemCategory.Seed;
        seed.maxStack = 99;
        seed.buyPrice = spec.SeedPrice;
        seed.sellPrice = Mathf.Max(1, Mathf.FloorToInt(spec.SeedPrice * 0.25f));

        var placement = GetOrAddModule<PlacementModule>(seed);
        placement.objectTypeToSpawn = ObjectType.Plant01;
        placement.placedEntityData = crop;
        placement.centerTile = true;
        placement.animTrigger = "PutDown";

        if (seed.icon == null)
            seed.icon = GetSeedIcon(crop);
    }

    private static void ConfigureProduct(EntityData product, CropSpec spec)
    {
        product.keyName = $"item.crop.{spec.Id}.name";
        product.descKey = $"item.crop.{spec.Id}.desc";
        product.category = ItemCategory.Crop;
        product.maxStack = 99;
        product.buyPrice = 0;
        product.sellPrice = spec.SellPrice;
    }

    private static void ConfigureWorldCrop(EntityData crop, EntityData product, CropSpec spec)
    {
        crop.keyName = $"world.crop.{spec.Id}.name";
        crop.descKey = $"world.crop.{spec.Id}.desc";
        crop.category = ItemCategory.Misc;
        crop.buyPrice = 0;
        crop.sellPrice = 0;

        var stage = GetOrAddModule<StageModule>(crop);
        ConfigureStages(stage, spec);

        var harvest = GetOrAddModule<HarvestModule>(crop);
        harvest.harvestTool = ToolType.Scythe;
        harvest.allowHandHarvest = true;
        harvest.additionalHarvestTools = Array.Empty<HarvestToolOption>();
        harvest.dropMode = HarvestDropMode.DirectToInteractor;
        harvest.harvestCausesDamage = false;
        harvest.destroyOnHarvest = !spec.IsRegrowable;
        harvest.oneHitDestroy = false;

        var drop = GetOrAddModule<DropModule>(crop);
        var productDrop = new[] { NewDrop(product, spec.MinDrop, spec.MaxDrop) };
        if (spec.IsRegrowable)
        {
            drop.harvestDrops = productDrop;
            drop.deathDrops = Array.Empty<DropEntry>();
        }
        else
        {
            drop.harvestDrops = Array.Empty<DropEntry>();
            drop.deathDrops = productDrop;
        }

        drop.includeHarvestDropsOnDestroyWhenHarvestable = false;

        var exp = GetOrAddModule<ExpRewardModule>(crop);
        exp.rewardExp = spec.Exp;
        exp.sourceType = ExpSourceType.Harvest;
        exp.requireKiller = true;

        if (product.icon == null)
            product.icon = GetHarvestIcon(stage);
        if (crop.icon == null)
            crop.icon = product.icon != null ? product.icon : GetHarvestIcon(stage);
    }

    private static void ConfigureStages(StageModule stage, CropSpec spec)
    {
        if (stage.stages == null || stage.stages.Length == 0)
            return;

        int harvestIndex = Mathf.Clamp(FindHarvestStageIndex(stage), 0, stage.stages.Length - 1);
        for (int i = 0; i < stage.stages.Length; i++)
        {
            if (stage.stages[i] == null)
                stage.stages[i] = new GrowthStage();

            stage.stages[i].canHarvest = i == harvestIndex;
        }

        DistributeGrowthDays(stage.stages, harvestIndex, spec.GrowDays);

        if (spec.IsRegrowable)
        {
            int recoveryIndex = Mathf.Max(0, harvestIndex - 1);
            stage.stages[recoveryIndex].canHarvest = false;
            stage.regrowStageIndex = recoveryIndex;
            stage.harvestGoToStageIndex = recoveryIndex;
            stage.lastStageLoopToIndex = harvestIndex;
            stage.daysToReturnAfterHarvest = Mathf.Max(1, spec.RegrowDays);
        }
        else
        {
            stage.regrowStageIndex = -1;
            stage.harvestGoToStageIndex = -1;
            stage.lastStageLoopToIndex = -1;
            stage.daysToReturnAfterHarvest = -1;
        }

        stage.requiresWater = true;
    }

    private static int FindHarvestStageIndex(StageModule stage)
    {
        if (stage.stages == null || stage.stages.Length == 0)
            return 0;

        for (int i = stage.stages.Length - 1; i >= 0; i--)
        {
            if (stage.stages[i] != null && stage.stages[i].canHarvest)
                return i;
        }

        return stage.stages.Length - 1;
    }

    private static void DistributeGrowthDays(GrowthStage[] stages, int harvestIndex, int growDays)
    {
        int growStageCount = Mathf.Max(1, harvestIndex);
        int remainingDays = Mathf.Max(1, growDays);

        for (int i = 0; i < stages.Length; i++)
        {
            if (i >= harvestIndex)
            {
                stages[i].daysToGrow = 1;
                continue;
            }

            int remainingStages = growStageCount - i;
            int days = Mathf.Max(1, Mathf.CeilToInt(remainingDays / (float)remainingStages));
            stages[i].daysToGrow = days;
            remainingDays -= days;
        }
    }

    private static Sprite GetSeedIcon(EntityData crop)
    {
        var stage = GetModule<StageModule>(crop);
        if (stage?.stages == null)
            return crop.icon;

        foreach (var growthStage in stage.stages)
        {
            if (growthStage?.sprite != null)
                return growthStage.sprite;
        }

        return crop.icon;
    }

    private static Sprite GetHarvestIcon(StageModule stage)
    {
        if (stage?.stages == null || stage.stages.Length == 0)
            return null;

        int harvestIndex = FindHarvestStageIndex(stage);
        var harvestSprite = stage.stages[harvestIndex]?.sprite;
        if (harvestSprite != null)
            return harvestSprite;

        for (int i = stage.stages.Length - 1; i >= 0; i--)
        {
            if (stage.stages[i]?.sprite != null)
                return stage.stages[i].sprite;
        }

        return null;
    }

    private static DropEntry NewDrop(EntityData item, int min, int max)
    {
        min = Mathf.Max(1, min);
        max = Mathf.Max(min, max);
        return new DropEntry
        {
            item = item,
            minAmount = min,
            maxAmount = max,
            dropChance = 1f
        };
    }

    private static void ConfigureShop(CropSpec[] specs, HashSet<EntityData> cropSeeds, HashSet<UnityEngine.Object> changedAssets)
    {
        var shopOwner = LoadEntity(ShopPath);
        if (shopOwner == null)
        {
            Debug.LogWarning("[CropProgressionBalancePatcher] Shop owner not found.");
            return;
        }

        var shop = GetOrAddModule<ShopModule>(shopOwner);
        shop.sellsToPlayer = true;
        shop.buysFromPlayer = true;
        shop.buysAllItems = true;
        shop.infiniteStock = true;

        var preservedStock = new List<ShopStockEntry>();
        if (shop.initialStock != null)
        {
            foreach (var entry in shop.initialStock)
            {
                if (entry?.itemData == null || cropSeeds.Contains(entry.itemData))
                    continue;

                if (entry.unlockRequirement == null)
                    entry.unlockRequirement = new UnlockRequirementData();
                if (entry.requiredLevel < 1)
                    entry.requiredLevel = 1;
                entry.unlockRequirement.requiredLevel = Mathf.Max(1, entry.requiredLevel);
                entry.unlockRequirement.requiredQuestIds ??= Array.Empty<string>();
                preservedStock.Add(entry);
            }
        }

        var cropStock = new List<ShopStockEntry>();
        foreach (var spec in specs.OrderBy(s => s.Level).ThenBy(s => s.SeedAssetName, StringComparer.Ordinal))
        {
            var seed = LoadEntity(spec.SeedPath);
            if (seed == null) continue;

            cropStock.Add(new ShopStockEntry
            {
                itemData = seed,
                amount = 99,
                requiredLevel = spec.Level,
                unlockRequirement = new UnlockRequirementData
                {
                    requiredLevel = spec.Level,
                    requiredQuestIds = Array.Empty<string>()
                }
            });
        }

        shop.initialStock = cropStock.Concat(preservedStock).ToList();
        changedAssets.Add(shopOwner);
    }

    private static void AddLocalization(Dictionary<string, string> vi, Dictionary<string, string> en, CropSpec spec)
    {
        string regrowVi = spec.IsRegrowable
            ? $" Sau lan dau, cay cho thu lai moi {spec.RegrowDays} ngay."
            : string.Empty;
        string regrowEn = spec.IsRegrowable
            ? $" After the first harvest, it produces again every {spec.RegrowDays} days."
            : string.Empty;

        vi[$"item.seed.{spec.Id}.name"] = $"Hat giong {spec.ViName}";
        vi[$"item.seed.{spec.Id}.desc"] = $"Mo khoa cap {spec.Level}. Mat khoang {spec.GrowDays} ngay tuoi nuoc de truong thanh.{regrowVi}";
        vi[$"item.crop.{spec.Id}.name"] = spec.ViName;
        vi[$"item.crop.{spec.Id}.desc"] = $"{spec.ViName} thu hoach tu ruong. Ban duoc gia hon khi mo khoa o cap cao.";
        vi[$"world.crop.{spec.Id}.name"] = $"Cay {spec.ViName}";
        vi[$"world.crop.{spec.Id}.desc"] = $"Cay trong cap {spec.Level}, truong thanh sau khoang {spec.GrowDays} ngay tuoi nuoc.{regrowVi}";

        en[$"item.seed.{spec.Id}.name"] = $"{spec.EnName} Seeds";
        en[$"item.seed.{spec.Id}.desc"] = $"Unlocked at Lv.{spec.Level}. The crop matures after about {spec.GrowDays} watered days.{regrowEn}";
        en[$"item.crop.{spec.Id}.name"] = spec.EnName;
        en[$"item.crop.{spec.Id}.desc"] = $"{spec.EnName} harvested from the field. Higher level crops sell for better value.";
        en[$"world.crop.{spec.Id}.name"] = $"{spec.EnName} Plant";
        en[$"world.crop.{spec.Id}.desc"] = $"A Lv.{spec.Level} crop that matures after about {spec.GrowDays} watered days.{regrowEn}";
    }

    private static void PatchLocalization(string path, Dictionary<string, string> patch)
    {
        if (patch.Count == 0)
            return;

        var file = File.Exists(path)
            ? JsonUtility.FromJson<LocalizationFile>(File.ReadAllText(path)) ?? new LocalizationFile()
            : new LocalizationFile();

        var entries = new Dictionary<string, string>(StringComparer.Ordinal);
        if (file.entries != null)
        {
            foreach (var entry in file.entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                    continue;

                entries[entry.key] = entry.value ?? string.Empty;
            }
        }

        foreach (var pair in patch)
            entries[pair.Key] = pair.Value;

        file.entries = entries
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new LocalizationEntry { key = pair.Key, value = pair.Value })
            .ToArray();

        File.WriteAllText(path, JsonUtility.ToJson(file, true) + Environment.NewLine);
    }

    private static T GetModule<T>(EntityData data) where T : IModuleData
    {
        if (data?.modules == null)
            return null;

        foreach (var module in data.modules)
        {
            if (module is T typed)
                return typed;
        }

        return null;
    }

    private static T GetOrAddModule<T>(EntityData data) where T : IModuleData, new()
    {
        if (data.modules == null)
            data.modules = new List<IModuleData>();

        var module = GetModule<T>(data);
        if (module != null)
            return module;

        module = new T();
        data.modules.Add(module);
        return module;
    }

    private sealed class CropSpec
    {
        public readonly string Id;
        public readonly string ViName;
        public readonly string EnName;
        public readonly string SeedAssetName;
        public readonly string WorldAssetName;
        public readonly int Level;
        public readonly int GrowDays;
        public readonly int RegrowDays;
        public readonly int SeedPrice;
        public readonly int SellPrice;
        public readonly int MinDrop;
        public readonly int MaxDrop;
        public readonly int Exp;

        public bool IsRegrowable => RegrowDays > 0;
        public string SeedPath => $"{EntityRoot}/Items/Seeds/{SeedAssetName}.asset";
        public string WorldCropPath => $"{EntityRoot}/World/Crops/{WorldAssetName}.asset";
        public string ProductPath => $"{EntityRoot}/Items/Crops/item_crop_{Id}.asset";

        public CropSpec(
            string id,
            string viName,
            string enName,
            string seedAssetName,
            string worldAssetName,
            int level,
            int growDays,
            int regrowDays,
            int seedPrice,
            int sellPrice,
            int minDrop,
            int maxDrop,
            int exp)
        {
            Id = id;
            ViName = viName;
            EnName = enName;
            SeedAssetName = seedAssetName;
            WorldAssetName = worldAssetName;
            Level = Mathf.Max(1, level);
            GrowDays = Mathf.Max(1, growDays);
            RegrowDays = Mathf.Max(0, regrowDays);
            SeedPrice = Mathf.Max(1, seedPrice);
            SellPrice = Mathf.Max(1, sellPrice);
            MinDrop = Mathf.Max(1, minDrop);
            MaxDrop = Mathf.Max(MinDrop, maxDrop);
            Exp = Mathf.Max(0, exp);
        }
    }
}
#endif
