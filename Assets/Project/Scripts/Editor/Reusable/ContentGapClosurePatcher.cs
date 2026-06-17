using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Assets.HeroEditor4D.Common.Scripts.Enums;
using UnityEditor;
using UnityEngine;

public static class ContentGapClosurePatcher
{
    private const string CropItemRoot = "Assets/Project/Resources/Data/Entities/Items/Crops";
    private const string FoodItemRoot = "Assets/Project/Resources/Data/Entities/Items/Food";
    private const string MaterialItemRoot = "Assets/Project/Resources/Data/Entities/Items/Materials";
    private const string ToolItemRoot = "Assets/Project/Resources/Data/Entities/Items/Tools";
    private const string PlaceableItemRoot = "Assets/Project/Resources/Data/Entities/Items/Placeables";
    private const string AnimalProductItemRoot = "Assets/Project/Resources/Data/Entities/Items/AnimalProducts";
    private const string WorldCropRoot = "Assets/Project/Resources/Data/Entities/World/Crops";
    private const string FruitTreeRoot = "Assets/Project/Resources/Data/Entities/World/FruitTrees";
    private const string WoodTreeRoot = "Assets/Project/Resources/Data/Entities/World/WoodTrees";
    private const string EnemyRoot = "Assets/Project/Resources/Data/Entities/Characters/Enemies";
    private const string RecipeRoot = "Assets/Project/Resources/Data/Recipes";
    private const string PendingMarkerRelativePath = "Library/ContentGapClosure.pending";

    private static readonly string[] LocalizationEnPaths =
    {
        "Assets/Project/Resources/Localization/en.json",
        "Assets/Resources/Localization/en.json"
    };

    private static readonly string[] LocalizationViPaths =
    {
        "Assets/Project/Resources/Localization/vi.json",
        "Assets/Resources/Localization/vi.json"
    };

    private static readonly CropItemSpec[] CropSpecs =
    {
        new CropItemSpec("asparagus", "crop_asparagus", "seed_asparagus_crop", "Asparagus", "Măng tây", 38, 1, 2),
        new CropItemSpec("bean", "crop_bean", "seed_bean_crop", "Green Bean", "Đậu que", 30, 1, 2),
        new CropItemSpec("blueberry", "crop_blueberry", "seed_blueberry_crop", "Blueberry", "Việt quất", 54, 2, 3),
        new CropItemSpec("cabbage", "crop_cabbage", "seed_cabbage_crop", "Cabbage", "Bắp cải", 46, 1, 1),
        new CropItemSpec("carrot", "crop_carrot", "seed_carrot_crop", "Carrot", "Cà rốt", 32, 1, 2),
        new CropItemSpec("cauliflower", "crop_cauliflower", "seed_cauliflower_crop", "Cauliflower", "Súp lơ trắng", 62, 1, 1),
        new CropItemSpec("cucumber", "crop_cucumber", "seed_cucumber_crop", "Cucumber", "Dưa leo", 34, 1, 2),
        new CropItemSpec("garlic", "crop_garlic", "seed_garlic_crop", "Garlic", "Tỏi", 42, 1, 2),
        new CropItemSpec("grape", "crop_grape", "seed_grape_crop", "Grape", "Nho", 68, 2, 3),
        new CropItemSpec("melon", "crop_melon", "seed_melon_crop", "Melon", "Dưa lưới", 80, 1, 1),
        new CropItemSpec("pea", "crop_pea", "seed_pea_crop", "Pea Pod", "Đậu Hà Lan", 30, 1, 2),
        new CropItemSpec("pepper_green", "crop_pepper_green", "seed_pepper_green_crop", "Green Pepper", "Ớt xanh", 36, 1, 2),
        new CropItemSpec("pepper_red", "crop_pepper_red", "seed_pepper_red_crop", "Red Pepper", "Ớt đỏ", 38, 1, 2),
        new CropItemSpec("pepper_yellow", "crop_pepper_yellow", "seed_pepper_yellow_crop", "Yellow Pepper", "Ớt vàng", 38, 1, 2),
        new CropItemSpec("radish", "crop_radish", "seed_radish_crop", "Radish", "Củ cải đỏ", 34, 1, 2),
        new CropItemSpec("yam", "crop_yam", "seed_yam_crop", "Yam", "Khoai mỡ", 45, 1, 2)
    };

    private static readonly FruitItemSpec[] FruitSpecs =
    {
        new FruitItemSpec("apple", "apple_fruitTree", "seed_apple_fruitree", "Apple", "Táo", 70, 2, 3),
        new FruitItemSpec("cherry", "Cherry_fruitTree", "seed_cherry_fruitree", "Cherry", "Anh đào", 65, 2, 3),
        new FruitItemSpec("pear", "pear_fruiTree", "seed_pear_fruitree", "Pear", "Lê", 72, 2, 3),
        new FruitItemSpec("plum", "plum_fruitTree", "seed_plum_fruitree", "Plum", "Mận", 78, 2, 3)
    };

    private static readonly WoodTreeSpec[] WoodTreeSpecs =
    {
        new WoodTreeSpec("WoodTree_brech", 2, 3),
        new WoodTreeSpec("WoodTree_fir", 2, 3),
        new WoodTreeSpec("WoodTree_maple", 3, 4),
        new WoodTreeSpec("WoodTree_pine", 3, 4),
        new WoodTreeSpec("WoodTree_oak", 4, 5)
    };

    private static readonly EnemyDropSpec[] EnemyDropSpecs =
    {
        new EnemyDropSpec("Slime1", new[]
        {
            new DropSpecRef(MaterialAsset("item_mat_stone.asset"), 1, 2, 1f),
            new DropSpecRef(MaterialAsset("Item_resource_coal.asset"), 1, 1, 0.3f)
        }),
        new EnemyDropSpec("Slime2", new[]
        {
            new DropSpecRef(MaterialAsset("item_mat_stone.asset"), 1, 3, 1f),
            new DropSpecRef(MaterialAsset("Item_resource_copper_ore.asset"), 1, 2, 0.45f)
        }),
        new EnemyDropSpec("Slime3", new[]
        {
            new DropSpecRef(MaterialAsset("Item_resource_copper_ore.asset"), 1, 2, 1f),
            new DropSpecRef(MaterialAsset("Item_resource_coal.asset"), 1, 2, 0.55f),
            new DropSpecRef(MaterialAsset("item_mat_iron_ore.asset"), 1, 1, 0.2f)
        }),
        new EnemyDropSpec("Orc1", new[]
        {
            new DropSpecRef(MaterialAsset("item_mat_wood.asset"), 1, 2, 1f),
            new DropSpecRef(MaterialAsset("Item_resource_copper_bar.asset"), 1, 1, 0.35f)
        }),
        new EnemyDropSpec("Orc2", new[]
        {
            new DropSpecRef(MaterialAsset("item_mat_wood.asset"), 2, 3, 1f),
            new DropSpecRef(MaterialAsset("item_mat_iron_ore.asset"), 1, 2, 0.6f),
            new DropSpecRef(MaterialAsset("Item_resource_coal.asset"), 1, 2, 0.45f)
        }),
        new EnemyDropSpec("Orc3", new[]
        {
            new DropSpecRef(MaterialAsset("item_mat_wood.asset"), 2, 4, 1f),
            new DropSpecRef(MaterialAsset("Item_resource_iron_bar.asset"), 1, 1, 0.45f),
            new DropSpecRef(MaterialAsset("item_mat_gold_ore.asset"), 1, 2, 0.35f)
        })
    };

    [MenuItem("Tools/DATN/Content/Apply Content Gap Closure Patch", priority = 220)]
    public static void ApplyMenu()
    {
        Run(validationOnly: false, failOnError: false);
    }

    [MenuItem("Tools/DATN/Content/Validate Content Gap Closure Patch", priority = 221)]
    public static void ValidateMenu()
    {
        Run(validationOnly: true, failOnError: false);
    }

    public static void ApplyBatch()
    {
        Run(validationOnly: false, failOnError: true);
    }

    public static void ValidateBatch()
    {
        Run(validationOnly: true, failOnError: true);
    }

    [MenuItem("Tools/DATN/Content/Queue Content Gap Closure Auto Patch", priority = 219)]
    public static void QueueAutoPatch()
    {
        string markerPath = GetPendingMarkerPath();
        Directory.CreateDirectory(Path.GetDirectoryName(markerPath) ?? string.Empty);
        File.WriteAllText(markerPath, "pending");
        Debug.Log("[ContentGapClosurePatcher] Queued one-shot auto patch on next domain reload.");
        AssetDatabase.Refresh();
    }

    [InitializeOnLoadMethod]
    private static void TryRunQueuedAutoPatch()
    {
        if (Application.isBatchMode)
            return;

        string markerPath = GetPendingMarkerPath();
        if (!File.Exists(markerPath))
            return;

        try
        {
            File.Delete(markerPath);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[ContentGapClosurePatcher] Failed to clear pending auto patch marker: {exception.Message}");
            return;
        }

        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            try
            {
                Run(validationOnly: false, failOnError: false);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[ContentGapClosurePatcher] Auto patch failed: {exception}");
            }
        };
    }

    private static void Run(bool validationOnly, bool failOnError)
    {
        var context = new PatchContext(validationOnly);

        try
        {
            if (!validationOnly)
            {
                EnsureFolder(CropItemRoot);
                EnsureFolder(FoodItemRoot);
                EnsureFolder(MaterialItemRoot);
                EnsureFolder(ToolItemRoot);
                EnsureFolder(PlaceableItemRoot);
                EnsureFolder(AnimalProductItemRoot);
            }

            Dictionary<string, EntityData> createdItems = validationOnly
                ? LoadExpectedItems(context)
                : UpsertItems(context);

            if (validationOnly)
            {
                ValidateHarvestDrops(createdItems, context);
                ValidateEnemyDrops(context);
                ValidateRecipes(context);
            }
            else
            {
                PatchHarvestDrops(createdItems, context);
                PatchEnemyDrops(createdItems, context);
                PatchRecipes(createdItems, context);
            }
            ValidateLocalizationKeys(context);

            if (!validationOnly)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
        catch (Exception exception)
        {
            context.Error($"Unhandled patch exception: {exception}");
        }

        context.Flush();

        if (failOnError && context.ErrorCount > 0)
            throw new InvalidOperationException($"Content gap patch finished with {context.ErrorCount} errors.");
    }

    private static Dictionary<string, EntityData> UpsertItems(PatchContext context)
    {
        var result = new Dictionary<string, EntityData>(StringComparer.OrdinalIgnoreCase);

        foreach (CropItemSpec spec in CropSpecs)
        {
            EntityData asset = UpsertCropItem(spec, context);
            if (asset != null)
                result[spec.itemId] = asset;
        }

        foreach (FruitItemSpec spec in FruitSpecs)
        {
            EntityData asset = UpsertFruitItem(spec, context);
            if (asset != null)
                result[spec.itemId] = asset;
        }

        EntityData wood = UpsertSimpleItem(new SimpleItemSpec(
            path: MaterialAsset("item_mat_wood.asset"),
            id: "item_mat_wood",
            keyBase: "item.mat.wood",
            englishName: "Wood",
            vietnameseName: "Gỗ",
            englishDesc: "Basic lumber from trees. Used for fences, chests, and tool upgrades.",
            vietnameseDesc: "Gỗ cơ bản thu từ cây. Dùng để làm hàng rào, rương và nâng cấp dụng cụ.",
            category: ItemCategory.Material,
            maxStack: 99,
            sellPrice: 16,
            icon: LoadSprite("Assets/Project/Generated/Icons/mat_wood_01.png")));
        result["item_mat_wood"] = wood;

        EntityData egg = UpsertSimpleItem(new SimpleItemSpec(
            path: $"{AnimalProductItemRoot}/item_animal_egg.asset",
            id: "item_animal_egg",
            keyBase: "item.animal.egg",
            englishName: "Egg",
            vietnameseName: "Trứng",
            englishDesc: "An animal product used in cooking or turned into mayonnaise.",
            vietnameseDesc: "Sản phẩm chăn nuôi dùng để nấu ăn hoặc làm mayonnaise.",
            category: ItemCategory.AnimalProduct,
            maxStack: 99,
            sellPrice: 24,
            icon: LoadSprite("Assets/Project/Generated/Icons/egg.png")));
        result["item_animal_egg"] = egg;

        EntityData milk = UpsertSimpleItem(new SimpleItemSpec(
            path: $"{AnimalProductItemRoot}/item_animal_milk.asset",
            id: "item_animal_milk",
            keyBase: "item.animal.milk",
            englishName: "Milk",
            vietnameseName: "Sữa",
            englishDesc: "Fresh milk used for recipes and cheese processing.",
            vietnameseDesc: "Sữa tươi dùng cho nấu ăn và ép phô mai.",
            category: ItemCategory.AnimalProduct,
            maxStack: 99,
            sellPrice: 40,
            icon: LoadSprite("Assets/Project/Generated/Icons/milk.png")));
        result["item_animal_milk"] = milk;

        EntityData cheese = UpsertFoodItem(
            assetPath: $"{FoodItemRoot}/item_food_cheese.asset",
            id: "item_food_cheese",
            keyBase: "item.food.cheese",
            englishName: "Cheese",
            vietnameseName: "Phô mai",
            englishDesc: "A processed dairy food that restores stamina and sells better than raw milk.",
            vietnameseDesc: "Món sữa chế biến giúp hồi thể lực và bán lời hơn sữa tươi.",
            sellPrice: 85,
            restoreHp: 5f,
            restoreStamina: 24f,
            icon: LoadSprite("Assets/Project/Generated/Icons/milk.png"));
        result["item_food_cheese"] = cheese;

        EntityData mayonnaise = UpsertFoodItem(
            assetPath: $"{FoodItemRoot}/item_food_mayonnaise.asset",
            id: "item_food_mayonnaise",
            keyBase: "item.food.mayonnaise",
            englishName: "Mayonnaise",
            vietnameseName: "Mayonnaise",
            englishDesc: "A rich egg-based food that restores a bit of stamina.",
            vietnameseDesc: "Món sốt làm từ trứng giúp hồi một ít thể lực.",
            sellPrice: 60,
            restoreHp: 3f,
            restoreStamina: 16f,
            icon: LoadSprite("Assets/Project/Generated/Icons/egg.png"));
        result["item_food_mayonnaise"] = mayonnaise;

        EntityData sprinklerT1 = UpsertPlaceableItem(
            assetPath: $"{PlaceableItemRoot}/item_place_sprinkler_t1.asset",
            id: "item_place_sprinkler_t1",
            keyBase: "item.place.sprinkler.t1",
            englishName: "Sprinkler T1",
            vietnameseName: "Sprinkler T1",
            englishDesc: "Place it to automatically water a small area every morning.",
            vietnameseDesc: "Đặt xuống để tự động tưới một vùng nhỏ vào mỗi buổi sáng.",
            sellPrice: 80,
            objectType: ObjectType.Sprinkler01,
            worldEntityPath: "Assets/Project/Resources/Data/Entities/World/Utility/world_utility_sprinkler_t1.asset",
            icon: LoadSprite("Assets/Project/Generated/Icons/tool_sprinkler_t1.png"),
            context: context);
        result["item_place_sprinkler_t1"] = sprinklerT1;

        EntityData sprinklerT2 = UpsertPlaceableItem(
            assetPath: $"{PlaceableItemRoot}/item_place_sprinkler_t2.asset",
            id: "item_place_sprinkler_t2",
            keyBase: "item.place.sprinkler.t2",
            englishName: "Sprinkler T2",
            vietnameseName: "Sprinkler T2",
            englishDesc: "Place it to automatically water a larger field every morning.",
            vietnameseDesc: "Đặt xuống để tự động tưới một khu lớn hơn vào mỗi buổi sáng.",
            sellPrice: 120,
            objectType: ObjectType.Sprinkler02,
            worldEntityPath: "Assets/Project/Resources/Data/Entities/World/Utility/world_utility_sprinkler_t2.asset",
            icon: LoadSprite("Assets/Project/Generated/Icons/tool_sprinkler_t2.png"),
            context: context);
        result["item_place_sprinkler_t2"] = sprinklerT2;

        EntityData pickaxeT2 = UpsertToolItem(
            assetPath: $"{ToolItemRoot}/item_tool_pickaxe_t2.asset",
            id: "item_tool_pickaxe_t2",
            keyBase: "item.tool.pickaxe.t2",
            englishName: "Copper Pickaxe",
            vietnameseName: "Cuốc chim Đồng",
            englishDesc: "A stronger pickaxe that mines faster than the starter tool.",
            vietnameseDesc: "Cuốc chim mạnh hơn giúp đào nhanh hơn bản cơ bản.",
            sellPrice: 240,
            icon: LoadSprite("Assets/Project/Generated/Icons/tool_pickaxe_t2.png"),
            toolType: ToolType.Pickaxe,
            toolTier: 2,
            animTrigger: "Pickaxe",
            appearanceSpriteId: "Art.Equipment.MeleeWeapon1H.Pickaxe",
            staminaCost: 3f,
            attack: 4f,
            areaX: 1f,
            areaY: 1f,
            cooldown: 0.28f,
            range: 1.35f);
        result["item_tool_pickaxe_t2"] = pickaxeT2;

        EntityData scytheT2 = UpsertToolItem(
            assetPath: $"{ToolItemRoot}/item_tool_scythe_t2.asset",
            id: "item_tool_scythe_t2",
            keyBase: "item.tool.scythe.t2",
            englishName: "Copper Scythe",
            vietnameseName: "Liềm Đồng",
            englishDesc: "An upgraded scythe that harvests a wider area per swing.",
            vietnameseDesc: "Liềm nâng cấp giúp thu hoạch rộng hơn trong mỗi lần quét.",
            sellPrice: 210,
            icon: LoadSprite("Assets/Project/Generated/Icons/tool_scythe_t2.png"),
            toolType: ToolType.Scythe,
            toolTier: 2,
            animTrigger: "Scythe",
            appearanceSpriteId: "Art.Equipment.MeleeWeapon1H.Scyther",
            staminaCost: 3f,
            attack: 7f,
            areaX: 3f,
            areaY: 1f,
            cooldown: 0.55f,
            range: 1.4f);
        result["item_tool_scythe_t2"] = scytheT2;

        return result;
    }

    private static Dictionary<string, EntityData> LoadExpectedItems(PatchContext context)
    {
        var result = new Dictionary<string, EntityData>(StringComparer.OrdinalIgnoreCase);

        foreach (CropItemSpec spec in CropSpecs)
            result[spec.itemId] = LoadEntity($"{CropItemRoot}/{spec.itemId}.asset", context);

        foreach (FruitItemSpec spec in FruitSpecs)
            result[spec.itemId] = LoadEntity($"{CropItemRoot}/{spec.itemId}.asset", context);

        string[] requiredPaths =
        {
            MaterialAsset("item_mat_wood.asset"),
            $"{AnimalProductItemRoot}/item_animal_egg.asset",
            $"{AnimalProductItemRoot}/item_animal_milk.asset",
            $"{FoodItemRoot}/item_food_cheese.asset",
            $"{FoodItemRoot}/item_food_mayonnaise.asset",
            $"{PlaceableItemRoot}/item_place_sprinkler_t1.asset",
            $"{PlaceableItemRoot}/item_place_sprinkler_t2.asset",
            $"{ToolItemRoot}/item_tool_pickaxe_t2.asset",
            $"{ToolItemRoot}/item_tool_scythe_t2.asset"
        };

        foreach (string path in requiredPaths)
        {
            EntityData asset = LoadEntity(path, context);
            if (asset != null)
                result[asset.id] = asset;
        }

        return result;
    }

    private static void PatchHarvestDrops(Dictionary<string, EntityData> items, PatchContext context)
    {
        foreach (CropItemSpec spec in CropSpecs)
        {
            EntityData item = GetItem(items, spec.itemId, context);
            EntityData crop = LoadEntity($"{WorldCropRoot}/{spec.worldAssetName}.asset", context);
            if (item == null || crop == null)
                continue;

            DropModule dropModule = GetOrCreateModule<DropModule>(crop.modules);
            dropModule.harvestDrops = new[]
            {
                new DropEntry
                {
                    item = item,
                    minAmount = spec.minDrop,
                    maxAmount = spec.maxDrop,
                    dropChance = 1f
                }
            };
            dropModule.deathDrops = Array.Empty<DropEntry>();
            dropModule.includeHarvestDropsOnDestroyWhenHarvestable = false;
            EditorUtility.SetDirty(crop);
            context.Info($"Patched crop drops: {crop.name}");
        }

        foreach (FruitItemSpec spec in FruitSpecs)
        {
            EntityData item = GetItem(items, spec.itemId, context);
            EntityData tree = LoadEntity($"{FruitTreeRoot}/{spec.worldAssetName}.asset", context);
            if (item == null || tree == null)
                continue;

            DropModule dropModule = GetOrCreateModule<DropModule>(tree.modules);
            dropModule.harvestDrops = new[]
            {
                new DropEntry
                {
                    item = item,
                    minAmount = spec.minDrop,
                    maxAmount = spec.maxDrop,
                    dropChance = 1f
                }
            };
            dropModule.deathDrops = Array.Empty<DropEntry>();
            dropModule.includeHarvestDropsOnDestroyWhenHarvestable = true;
            EditorUtility.SetDirty(tree);
            context.Info($"Patched fruit tree drops: {tree.name}");
        }

        EntityData woodItem = GetItem(items, "item_mat_wood", context);
        foreach (WoodTreeSpec spec in WoodTreeSpecs)
        {
            EntityData tree = LoadEntity($"{WoodTreeRoot}/{spec.assetName}.asset", context);
            if (tree == null || woodItem == null)
                continue;

            DropModule dropModule = GetOrCreateModule<DropModule>(tree.modules);
            dropModule.harvestDrops = new[]
            {
                new DropEntry
                {
                    item = woodItem,
                    minAmount = spec.minDrop,
                    maxAmount = spec.maxDrop,
                    dropChance = 1f
                }
            };
            dropModule.deathDrops = Array.Empty<DropEntry>();
            dropModule.includeHarvestDropsOnDestroyWhenHarvestable = false;
            tree.modules = ReorderModules(tree.modules,
                GetModule<HarvestModule>(tree),
                dropModule,
                GetModule<HealthModule>(tree),
                GetModule<MortalModule>(tree));
            EditorUtility.SetDirty(tree);
            context.Info($"Patched wood tree drops: {tree.name}");
        }
    }

    private static void PatchEnemyDrops(Dictionary<string, EntityData> items, PatchContext context)
    {
        foreach (EnemyDropSpec spec in EnemyDropSpecs)
        {
            EntityData enemy = LoadEntity($"{EnemyRoot}/{spec.enemyAssetName}.asset", context);
            if (enemy == null)
                continue;

            DropModule dropModule = GetOrCreateModule<DropModule>(enemy.modules);
            dropModule.harvestDrops = Array.Empty<DropEntry>();
            dropModule.deathDrops = BuildDropEntries(spec.drops, items, context);
            dropModule.includeHarvestDropsOnDestroyWhenHarvestable = false;
            EditorUtility.SetDirty(enemy);
            context.Info($"Patched enemy drops: {enemy.name}");
        }
    }

    private static void PatchRecipes(Dictionary<string, EntityData> items, PatchContext context)
    {
        PatchRecipe(
            "Recipe_Sprinkler_T1.asset",
            new[]
            {
                new RecipeSpecRef(MaterialAsset("item_mat_stone.asset"), 4),
                new RecipeSpecRef(MaterialAsset("Item_resource_copper_bar.asset"), 2),
                new RecipeSpecRef(MaterialAsset("Item_resource_coal.asset"), 1)
            },
            new[]
            {
                new RecipeSpecRef($"{PlaceableItemRoot}/item_place_sprinkler_t1.asset", 1)
            },
            context);

        PatchRecipe(
            "Recipe_Sprinkler_T2.asset",
            new[]
            {
                new RecipeSpecRef($"{PlaceableItemRoot}/item_place_sprinkler_t1.asset", 1),
                new RecipeSpecRef(MaterialAsset("Item_resource_iron_bar.asset"), 2),
                new RecipeSpecRef(MaterialAsset("item_mat_stone.asset"), 6)
            },
            new[]
            {
                new RecipeSpecRef($"{PlaceableItemRoot}/item_place_sprinkler_t2.asset", 1)
            },
            context);

        PatchRecipe(
            "Recipe_Pickaxe_T2.asset",
            new[]
            {
                new RecipeSpecRef($"{ToolItemRoot}/item_tool_pickaxe_t1.asset", 1),
                new RecipeSpecRef(MaterialAsset("item_mat_stone.asset"), 18),
                new RecipeSpecRef(MaterialAsset("Item_resource_copper_bar.asset"), 5)
            },
            new[]
            {
                new RecipeSpecRef($"{ToolItemRoot}/item_tool_pickaxe_t2.asset", 1)
            },
            context);

        PatchRecipe(
            "Recipe_Scythe_T2.asset",
            new[]
            {
                new RecipeSpecRef($"{ToolItemRoot}/item_tool_scythe_t1.asset", 1),
                new RecipeSpecRef(MaterialAsset("item_mat_stone.asset"), 12),
                new RecipeSpecRef(MaterialAsset("Item_resource_copper_bar.asset"), 4)
            },
            new[]
            {
                new RecipeSpecRef($"{ToolItemRoot}/item_tool_scythe_t2.asset", 1)
            },
            context);

        PatchRecipe(
            "recipe_cheese_basic.asset",
            new[]
            {
                new RecipeSpecRef($"{CropItemRoot}/item_crop_wheat.asset", 3)
            },
            new[]
            {
                new RecipeSpecRef($"{FoodItemRoot}/item_food_cheese.asset", 1)
            },
            context);

        PatchRecipe(
            "recipe_mayo_basic.asset",
            new[]
            {
                new RecipeSpecRef($"{CropItemRoot}/item_crop_corn.asset", 1)
            },
            new[]
            {
                new RecipeSpecRef($"{FoodItemRoot}/item_food_mayonnaise.asset", 1)
            },
            context);
    }

    private static void PatchRecipe(string recipeFileName, RecipeSpecRef[] ingredients, RecipeSpecRef[] outputs, PatchContext context)
    {
        string assetPath = $"{RecipeRoot}/{recipeFileName}";
        RecipeData recipe = AssetDatabase.LoadAssetAtPath<RecipeData>(assetPath);
        if (recipe == null)
        {
            context.Error($"Missing recipe asset: {assetPath}");
            return;
        }

        recipe.ingredients = BuildRecipeIngredients(ingredients, context);
        recipe.outputs = BuildRecipeIngredients(outputs, context);
        EditorUtility.SetDirty(recipe);
        context.Info($"Patched recipe: {recipeFileName}");
    }

    private static void ValidateHarvestDrops(Dictionary<string, EntityData> items, PatchContext context)
    {
        foreach (CropItemSpec spec in CropSpecs)
        {
            EntityData item = GetItem(items, spec.itemId, context);
            EntityData crop = LoadEntity($"{WorldCropRoot}/{spec.worldAssetName}.asset", context);
            DropModule dropModule = GetModule<DropModule>(crop);
            if (crop == null || item == null)
                continue;

            ValidateDropEntries(
                $"{crop.name}.harvestDrops",
                dropModule?.harvestDrops,
                new[] { new DropSpecRef($"{CropItemRoot}/{spec.itemId}.asset", spec.minDrop, spec.maxDrop, 1f) },
                context);
        }

        foreach (FruitItemSpec spec in FruitSpecs)
        {
            EntityData tree = LoadEntity($"{FruitTreeRoot}/{spec.worldAssetName}.asset", context);
            DropModule dropModule = GetModule<DropModule>(tree);
            if (tree == null)
                continue;

            ValidateDropEntries(
                $"{tree.name}.harvestDrops",
                dropModule?.harvestDrops,
                new[] { new DropSpecRef($"{CropItemRoot}/{spec.itemId}.asset", spec.minDrop, spec.maxDrop, 1f) },
                context);
        }

        foreach (WoodTreeSpec spec in WoodTreeSpecs)
        {
            EntityData tree = LoadEntity($"{WoodTreeRoot}/{spec.assetName}.asset", context);
            DropModule dropModule = GetModule<DropModule>(tree);
            if (tree == null)
                continue;

            ValidateDropEntries(
                $"{tree.name}.harvestDrops",
                dropModule?.harvestDrops,
                new[] { new DropSpecRef(MaterialAsset("item_mat_wood.asset"), spec.minDrop, spec.maxDrop, 1f) },
                context);
        }
    }

    private static void ValidateEnemyDrops(PatchContext context)
    {
        foreach (EnemyDropSpec spec in EnemyDropSpecs)
        {
            EntityData enemy = LoadEntity($"{EnemyRoot}/{spec.enemyAssetName}.asset", context);
            DropModule dropModule = GetModule<DropModule>(enemy);
            if (enemy == null)
                continue;

            ValidateDropEntries($"{enemy.name}.deathDrops", dropModule?.deathDrops, spec.drops, context);
        }
    }

    private static void ValidateRecipes(PatchContext context)
    {
        ValidateRecipe(
            "Recipe_Sprinkler_T1.asset",
            new[]
            {
                new RecipeSpecRef(MaterialAsset("item_mat_stone.asset"), 4),
                new RecipeSpecRef(MaterialAsset("Item_resource_copper_bar.asset"), 2),
                new RecipeSpecRef(MaterialAsset("Item_resource_coal.asset"), 1)
            },
            new[]
            {
                new RecipeSpecRef($"{PlaceableItemRoot}/item_place_sprinkler_t1.asset", 1)
            },
            context);

        ValidateRecipe(
            "Recipe_Sprinkler_T2.asset",
            new[]
            {
                new RecipeSpecRef($"{PlaceableItemRoot}/item_place_sprinkler_t1.asset", 1),
                new RecipeSpecRef(MaterialAsset("Item_resource_iron_bar.asset"), 2),
                new RecipeSpecRef(MaterialAsset("item_mat_stone.asset"), 6)
            },
            new[]
            {
                new RecipeSpecRef($"{PlaceableItemRoot}/item_place_sprinkler_t2.asset", 1)
            },
            context);

        ValidateRecipe(
            "Recipe_Pickaxe_T2.asset",
            new[]
            {
                new RecipeSpecRef($"{ToolItemRoot}/item_tool_pickaxe_t1.asset", 1),
                new RecipeSpecRef(MaterialAsset("item_mat_stone.asset"), 18),
                new RecipeSpecRef(MaterialAsset("Item_resource_copper_bar.asset"), 5)
            },
            new[]
            {
                new RecipeSpecRef($"{ToolItemRoot}/item_tool_pickaxe_t2.asset", 1)
            },
            context);

        ValidateRecipe(
            "Recipe_Scythe_T2.asset",
            new[]
            {
                new RecipeSpecRef($"{ToolItemRoot}/item_tool_scythe_t1.asset", 1),
                new RecipeSpecRef(MaterialAsset("item_mat_stone.asset"), 12),
                new RecipeSpecRef(MaterialAsset("Item_resource_copper_bar.asset"), 4)
            },
            new[]
            {
                new RecipeSpecRef($"{ToolItemRoot}/item_tool_scythe_t2.asset", 1)
            },
            context);

        ValidateRecipe(
            "recipe_cheese_basic.asset",
            new[]
            {
                new RecipeSpecRef($"{CropItemRoot}/item_crop_wheat.asset", 3)
            },
            new[]
            {
                new RecipeSpecRef($"{FoodItemRoot}/item_food_cheese.asset", 1)
            },
            context);

        ValidateRecipe(
            "recipe_mayo_basic.asset",
            new[]
            {
                new RecipeSpecRef($"{CropItemRoot}/item_crop_corn.asset", 1)
            },
            new[]
            {
                new RecipeSpecRef($"{FoodItemRoot}/item_food_mayonnaise.asset", 1)
            },
            context);
    }

    private static void ValidateRecipe(string recipeFileName, RecipeSpecRef[] ingredients, RecipeSpecRef[] outputs, PatchContext context)
    {
        string assetPath = $"{RecipeRoot}/{recipeFileName}";
        RecipeData recipe = AssetDatabase.LoadAssetAtPath<RecipeData>(assetPath);
        if (recipe == null)
        {
            context.Error($"Missing recipe asset: {assetPath}");
            return;
        }

        ValidateRecipeIngredients($"{recipeFileName}.ingredients", recipe.ingredients, ingredients, context);
        ValidateRecipeIngredients($"{recipeFileName}.outputs", recipe.outputs, outputs, context);
    }

    private static List<RecipeIngredient> BuildRecipeIngredients(RecipeSpecRef[] specs, PatchContext context)
    {
        var list = new List<RecipeIngredient>(specs.Length);
        foreach (RecipeSpecRef spec in specs)
        {
            EntityData item = LoadEntity(spec.assetPath, context);
            if (item == null)
                continue;

            list.Add(new RecipeIngredient
            {
                item = item,
                amount = spec.amount
            });
        }

        return list;
    }

    private static void ValidateRecipeIngredients(string label, List<RecipeIngredient> actual, RecipeSpecRef[] expected, PatchContext context)
    {
        if (actual == null)
        {
            context.Error($"Missing recipe list: {label}");
            return;
        }

        if (actual.Count != expected.Length)
        {
            context.Error($"Recipe list count mismatch for {label}. Expected {expected.Length}, found {actual.Count}.");
            return;
        }

        for (int index = 0; index < expected.Length; index++)
        {
            RecipeIngredient entry = actual[index];
            RecipeSpecRef spec = expected[index];
            EntityData expectedItem = LoadEntity(spec.assetPath, context);
            if (entry == null)
            {
                context.Error($"Null recipe entry at {label}[{index}]");
                continue;
            }

            if (entry.item == null)
            {
                context.Error($"Missing recipe item at {label}[{index}]");
                continue;
            }

            if (expectedItem != null && entry.item != expectedItem)
                context.Error($"Recipe item mismatch at {label}[{index}]. Expected {expectedItem.name}, found {entry.item.name}.");

            if (entry.amount != spec.amount)
                context.Error($"Recipe amount mismatch at {label}[{index}]. Expected {spec.amount}, found {entry.amount}.");
        }
    }

    private static void ValidateDropEntries(string label, DropEntry[] actual, DropSpecRef[] expected, PatchContext context)
    {
        if (actual == null)
        {
            context.Error($"Missing drop list: {label}");
            return;
        }

        if (actual.Length != expected.Length)
        {
            context.Error($"Drop count mismatch for {label}. Expected {expected.Length}, found {actual.Length}.");
            return;
        }

        for (int index = 0; index < expected.Length; index++)
        {
            DropEntry entry = actual[index];
            DropSpecRef spec = expected[index];
            EntityData expectedItem = LoadEntity(spec.assetPath, context);

            if (entry.item == null)
            {
                context.Error($"Missing drop item at {label}[{index}]");
                continue;
            }

            if (expectedItem != null && entry.item != expectedItem)
                context.Error($"Drop item mismatch at {label}[{index}]. Expected {expectedItem.name}, found {entry.item.name}.");

            if (entry.minAmount != spec.minAmount || entry.maxAmount != spec.maxAmount)
                context.Error($"Drop amount mismatch at {label}[{index}]. Expected {spec.minAmount}-{spec.maxAmount}, found {entry.minAmount}-{entry.maxAmount}.");

            if (!Mathf.Approximately(entry.dropChance, spec.dropChance))
                context.Error($"Drop chance mismatch at {label}[{index}]. Expected {spec.dropChance}, found {entry.dropChance}.");
        }
    }

    private static void ValidateLocalizationKeys(PatchContext context)
    {
        var keys = new List<string>();
        keys.AddRange(CropSpecs.Select(spec => $"item.crop.{spec.keyToken}.name"));
        keys.AddRange(CropSpecs.Select(spec => $"item.crop.{spec.keyToken}.desc"));
        keys.AddRange(FruitSpecs.Select(spec => $"item.crop.{spec.keyToken}.name"));
        keys.AddRange(FruitSpecs.Select(spec => $"item.crop.{spec.keyToken}.desc"));
        keys.AddRange(new[]
        {
            "item.mat.wood.name",
            "item.mat.wood.desc",
            "item.animal.egg.name",
            "item.animal.egg.desc",
            "item.animal.milk.name",
            "item.animal.milk.desc",
            "item.food.cheese.name",
            "item.food.cheese.desc",
            "item.food.mayonnaise.name",
            "item.food.mayonnaise.desc",
            "item.place.sprinkler.t1.name",
            "item.place.sprinkler.t1.desc",
            "item.place.sprinkler.t2.name",
            "item.place.sprinkler.t2.desc",
            "item.tool.pickaxe.t2.name",
            "item.tool.pickaxe.t2.desc",
            "item.tool.scythe.t2.name",
            "item.tool.scythe.t2.desc"
        });

        foreach (string path in LocalizationEnPaths.Concat(LocalizationViPaths))
        {
            if (!File.Exists(ToAbsoluteProjectPath(path)))
            {
                context.Warning($"Localization file not found for validation: {path}");
                continue;
            }

            string json = File.ReadAllText(ToAbsoluteProjectPath(path));
            foreach (string key in keys)
            {
                if (!json.Contains($"\"{key}\"", StringComparison.Ordinal))
                    context.Error($"Missing localization key '{key}' in {path}");
            }
        }
    }

    private static EntityData UpsertCropItem(CropItemSpec spec, PatchContext context)
    {
        Sprite icon = ResolveSeedIcon(spec.seedAssetName, spec.worldAssetName);
        return UpsertSimpleItem(new SimpleItemSpec(
            path: $"{CropItemRoot}/{spec.itemId}.asset",
            id: spec.itemId,
            keyBase: $"item.crop.{spec.keyToken}",
            englishName: spec.englishName,
            vietnameseName: spec.vietnameseName,
            englishDesc: $"{spec.englishName} harvested from the field. Useful for selling, cooking, or quest turn-ins.",
            vietnameseDesc: $"{spec.vietnameseName} thu hoạch từ ruộng. Dùng để bán, nấu ăn hoặc nộp nhiệm vụ.",
            category: ItemCategory.Crop,
            maxStack: 99,
            sellPrice: spec.sellPrice,
            icon: icon));
    }

    private static EntityData UpsertFruitItem(FruitItemSpec spec, PatchContext context)
    {
        Sprite icon = ResolveSeedIcon(spec.seedAssetName, spec.worldAssetName);
        return UpsertSimpleItem(new SimpleItemSpec(
            path: $"{CropItemRoot}/{spec.itemId}.asset",
            id: spec.itemId,
            keyBase: $"item.crop.{spec.keyToken}",
            englishName: spec.englishName,
            vietnameseName: spec.vietnameseName,
            englishDesc: $"{spec.englishName} picked from a fruit tree. Great for cooking or profitable sales.",
            vietnameseDesc: $"{spec.vietnameseName} hái từ cây ăn quả. Phù hợp để nấu ăn hoặc bán có lời.",
            category: ItemCategory.Crop,
            maxStack: 99,
            sellPrice: spec.sellPrice,
            icon: icon));
    }

    private static EntityData UpsertSimpleItem(SimpleItemSpec spec)
    {
        EntityData item = LoadOrCreateEntity(spec.path);
        item.id = spec.id;
        item.keyName = spec.keyBase + ".name";
        item.descKey = spec.keyBase + ".desc";
        item.icon = spec.icon;
        item.category = spec.category;
        item.maxStack = spec.maxStack;
        item.buyPrice = 0;
        item.sellPrice = spec.sellPrice;
        item.baseStats ??= new StatsData();
        item.baseStats.baseStats ??= new List<StatEntry>();
        item.placementRule = CreateEmptyPlacementRule();
        item.modules ??= new List<IModuleData>();
        item.modules = item.modules.Where(module => module != null).ToList();

        EditorUtility.SetDirty(item);
        UpsertLocalization(spec.keyBase, spec.englishName, spec.englishDesc, spec.vietnameseName, spec.vietnameseDesc);
        return item;
    }

    private static EntityData UpsertFoodItem(
        string assetPath,
        string id,
        string keyBase,
        string englishName,
        string vietnameseName,
        string englishDesc,
        string vietnameseDesc,
        int sellPrice,
        float restoreHp,
        float restoreStamina,
        Sprite icon)
    {
        EntityData item = LoadOrCreateEntity(assetPath);
        item.id = id;
        item.keyName = keyBase + ".name";
        item.descKey = keyBase + ".desc";
        item.icon = icon;
        item.category = ItemCategory.Food;
        item.maxStack = 99;
        item.buyPrice = 0;
        item.sellPrice = sellPrice;
        item.baseStats ??= new StatsData();
        item.baseStats.baseStats ??= new List<StatEntry>();
        item.placementRule = CreateEmptyPlacementRule();
        item.modules ??= new List<IModuleData>();

        ConsumableModule consumable = GetOrCreateModule<ConsumableModule>(item.modules);
        consumable.restoreHp = restoreHp;
        consumable.restoreStamina = restoreStamina;
        consumable.restoreMp = 0f;
        consumable.consumeAmount = 1;
        consumable.destroyOnUse = true;

        item.modules = ReorderModules(item.modules, consumable);
        EditorUtility.SetDirty(item);
        UpsertLocalization(keyBase, englishName, englishDesc, vietnameseName, vietnameseDesc);
        return item;
    }

    private static EntityData UpsertPlaceableItem(
        string assetPath,
        string id,
        string keyBase,
        string englishName,
        string vietnameseName,
        string englishDesc,
        string vietnameseDesc,
        int sellPrice,
        ObjectType objectType,
        string worldEntityPath,
        Sprite icon,
        PatchContext context)
    {
        EntityData worldEntity = LoadEntity(worldEntityPath, context);
        EntityData item = LoadOrCreateEntity(assetPath);
        item.id = id;
        item.keyName = keyBase + ".name";
        item.descKey = keyBase + ".desc";
        item.icon = icon;
        item.category = ItemCategory.Placeable;
        item.maxStack = 99;
        item.buyPrice = 0;
        item.sellPrice = sellPrice;
        item.baseStats ??= new StatsData();
        item.baseStats.baseStats ??= new List<StatEntry>();
        item.placementRule = CreateEmptyPlacementRule();
        item.modules ??= new List<IModuleData>();

        PlacementModule placement = GetOrCreateModule<PlacementModule>(item.modules);
        placement.objectTypeToSpawn = objectType;
        placement.placedEntityData = worldEntity;
        placement.centerTile = true;
        placement.animTrigger = "PutDown";

        item.modules = ReorderModules(item.modules, placement);
        EditorUtility.SetDirty(item);
        UpsertLocalization(keyBase, englishName, englishDesc, vietnameseName, vietnameseDesc);
        return item;
    }

    private static EntityData UpsertToolItem(
        string assetPath,
        string id,
        string keyBase,
        string englishName,
        string vietnameseName,
        string englishDesc,
        string vietnameseDesc,
        int sellPrice,
        Sprite icon,
        ToolType toolType,
        int toolTier,
        string animTrigger,
        string appearanceSpriteId,
        float staminaCost,
        float attack,
        float areaX,
        float areaY,
        float cooldown,
        float range)
    {
        EntityData item = LoadOrCreateEntity(assetPath);
        item.id = id;
        item.keyName = keyBase + ".name";
        item.descKey = keyBase + ".desc";
        item.icon = icon;
        item.category = ItemCategory.Tool;
        item.maxStack = 1;
        item.buyPrice = 0;
        item.sellPrice = sellPrice;
        item.placementRule = CreateEmptyPlacementRule();
        item.baseStats ??= new StatsData();
        item.baseStats.baseStats ??= new List<StatEntry>();
        UpsertStat(item.baseStats.baseStats, StatType.Stamina, staminaCost);
        UpsertStat(item.baseStats.baseStats, StatType.AreaX, areaX);
        UpsertStat(item.baseStats.baseStats, StatType.AreaY, areaY);
        UpsertStat(item.baseStats.baseStats, StatType.CoolDown, cooldown);
        UpsertStat(item.baseStats.baseStats, StatType.Range, range);
        UpsertStat(item.baseStats.baseStats, StatType.Attack, attack);

        item.modules ??= new List<IModuleData>();
        ToolModule tool = GetOrCreateModule<ToolModule>(item.modules);
        tool.toolType = toolType;
        tool.toolTier = toolTier;
        tool.animTrigger = animTrigger;
        tool.refillAnimTrigger = string.Empty;

        StaminaCostModule stamina = GetOrCreateModule<StaminaCostModule>(item.modules);
        stamina.cost = staminaCost;

        ToolRequirementModule requirement = GetOrCreateModule<ToolRequirementModule>(item.modules);
        requirement.requiredToolType = toolType;
        requirement.minimumToolTier = toolTier;
        requirement.wrongToolPenalty = 0f;
        requirement.blockDamageIfWrongTool = true;
        requirement.blockDamageIfBelowTier = true;

        AppearanceModule appearance = GetOrCreateModule<AppearanceModule>(item.modules);
        appearance.spriteId = appearanceSpriteId;
        appearance.equipmentPart = EquipmentPart.MeleeWeapon1H;

        item.modules = ReorderModules(item.modules, tool, stamina, requirement, appearance);
        EditorUtility.SetDirty(item);
        UpsertLocalization(keyBase, englishName, englishDesc, vietnameseName, vietnameseDesc);
        return item;
    }

    private static EntityData LoadEntity(string assetPath, PatchContext context)
    {
        EntityData entity = AssetDatabase.LoadAssetAtPath<EntityData>(assetPath);
        if (entity == null)
            context.Error($"Missing EntityData asset: {assetPath}");
        return entity;
    }

    private static EntityData LoadOrCreateEntity(string assetPath)
    {
        EntityData entity = AssetDatabase.LoadAssetAtPath<EntityData>(assetPath);
        if (entity != null)
            return entity;

        EntityData created = ScriptableObject.CreateInstance<EntityData>();
        created.baseStats = new StatsData { baseStats = new List<StatEntry>() };
        created.modules = new List<IModuleData>();
        created.placementRule = CreateEmptyPlacementRule();
        AssetDatabase.CreateAsset(created, assetPath);
        return created;
    }

    private static DropEntry[] BuildDropEntries(DropSpecRef[] specs, Dictionary<string, EntityData> items, PatchContext context)
    {
        var list = new List<DropEntry>(specs.Length);
        foreach (DropSpecRef spec in specs)
        {
            EntityData item = LoadEntity(spec.assetPath, context);
            if (item == null)
                continue;

            list.Add(new DropEntry
            {
                item = item,
                minAmount = spec.minAmount,
                maxAmount = spec.maxAmount,
                dropChance = spec.dropChance
            });
        }

        return list.ToArray();
    }

    private static EntityData GetItem(Dictionary<string, EntityData> items, string itemId, PatchContext context)
    {
        if (items.TryGetValue(itemId, out EntityData asset) && asset != null)
            return asset;

        context.Error($"Expected item missing from patch scope: {itemId}");
        return null;
    }

    private static Sprite ResolveSeedIcon(string seedAssetName, string worldAssetName)
    {
        string[] candidates =
        {
            $"{CropItemRoot}/{seedAssetName}.asset",
            $"Assets/Project/Resources/Data/Entities/Items/Seeds/{seedAssetName}.asset"
        };

        foreach (string path in candidates)
        {
            EntityData seed = AssetDatabase.LoadAssetAtPath<EntityData>(path);
            if (seed != null && seed.icon != null)
                return seed.icon;
        }

        string worldCropPath = $"{WorldCropRoot}/{worldAssetName}.asset";
        EntityData worldCrop = AssetDatabase.LoadAssetAtPath<EntityData>(worldCropPath);
        if (worldCrop != null && worldCrop.icon != null)
            return worldCrop.icon;

        string fruitPath = $"{FruitTreeRoot}/{worldAssetName}.asset";
        EntityData fruitTree = AssetDatabase.LoadAssetAtPath<EntityData>(fruitPath);
        if (fruitTree != null && fruitTree.icon != null)
            return fruitTree.icon;

        return null;
    }

    private static Sprite LoadSprite(string assetPath)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite != null)
            return sprite;

        return AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().FirstOrDefault();
    }

    private static string MaterialAsset(string fileName)
    {
        return $"{MaterialItemRoot}/{fileName}";
    }

    private static PlacementRule CreateEmptyPlacementRule()
    {
        return new PlacementRule
        {
            occupyLayer = 0,
            requireTags = 0,
            provideTags = 0,
            blockLayers = Array.Empty<EntityLayer>()
        };
    }

    private static void UpsertLocalization(
        string keyBase,
        string englishName,
        string englishDesc,
        string vietnameseName,
        string vietnameseDesc)
    {
        foreach (string path in LocalizationEnPaths)
            TryUpsertLocalizationFile(path, keyBase + ".name", englishName, keyBase + ".desc", englishDesc);

        foreach (string path in LocalizationViPaths)
            TryUpsertLocalizationFile(path, keyBase + ".name", vietnameseName, keyBase + ".desc", vietnameseDesc);
    }

    private static void TryUpsertLocalizationFile(
        string assetPath,
        string nameKey,
        string nameValue,
        string descKey,
        string descValue)
    {
        string absolutePath = ToAbsoluteProjectPath(assetPath);
        if (!File.Exists(absolutePath))
            return;

        string json = File.ReadAllText(absolutePath);
        if (JsonUtility.FromJson<LocalizationFile>(json) == null)
            return;

        string lineEnding = json.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        json = UpsertLocalizationJsonEntry(json, nameKey, nameValue, lineEnding);
        json = UpsertLocalizationJsonEntry(json, descKey, descValue, lineEnding);
        File.WriteAllText(absolutePath, json);
        AssetDatabase.ImportAsset(assetPath);
    }

    private static string UpsertLocalizationJsonEntry(string json, string key, string value, string lineEnding)
    {
        string serialized = JsonUtility.ToJson(new LocalizationEntry { key = key, value = value });
        serialized = serialized
            .Replace("{\"key\":", "{ \"key\": ")
            .Replace(",\"value\":", ", \"value\": ");
        serialized = serialized.Substring(0, serialized.Length - 1) + " }";

        string escapedKey = Regex.Escape(key);
        string entryPattern =
            $@"(?<indent>^[ \t]*)\{{\s*""key""\s*:\s*""{escapedKey}""\s*,\s*""value""\s*:\s*""(?:\\.|[^""\\])*""\s*\}}";
        Match existing = Regex.Match(json, entryPattern, RegexOptions.Multiline);
        if (existing.Success)
        {
            string indent = existing.Groups["indent"].Value;
            return json.Substring(0, existing.Index)
                   + indent
                   + serialized
                   + json.Substring(existing.Index + existing.Length);
        }

        int arrayEnd = json.LastIndexOf(']');
        if (arrayEnd < 0)
            return json;

        string before = json.Substring(0, arrayEnd).TrimEnd();
        bool hasEntries = before.LastIndexOf('{') > before.LastIndexOf('[');
        string separator = hasEntries ? "," : string.Empty;
        return before
               + separator
               + lineEnding
               + "    "
               + serialized
               + lineEnding
               + "  "
               + json.Substring(arrayEnd);
    }

    private static void UpsertStat(List<StatEntry> stats, StatType statType, float value)
    {
        StatEntry existing = stats.FirstOrDefault(entry => entry != null && entry.statType == statType);
        if (existing == null)
        {
            stats.Add(new StatEntry
            {
                statType = statType,
                value = value
            });
            return;
        }

        existing.value = value;
    }

    private static T GetOrCreateModule<T>(List<IModuleData> modules) where T : IModuleData, new()
    {
        T existing = modules.OfType<T>().FirstOrDefault();
        if (existing != null)
            return existing;

        var created = new T();
        modules.Add(created);
        return created;
    }

    private static T GetModule<T>(EntityData data) where T : IModuleData
    {
        return data?.modules?.OfType<T>().FirstOrDefault();
    }

    private static List<IModuleData> ReorderModules(List<IModuleData> modules, params IModuleData[] preferredOrder)
    {
        var reordered = new List<IModuleData>();
        foreach (IModuleData module in preferredOrder)
        {
            if (module != null && modules.Contains(module) && !reordered.Contains(module))
                reordered.Add(module);
        }

        foreach (IModuleData module in modules)
        {
            if (module != null && !reordered.Contains(module))
                reordered.Add(module);
        }

        return reordered;
    }

    private static void EnsureFolder(string folderPath)
    {
        string normalized = folderPath.Replace("\\", "/");
        if (AssetDatabase.IsValidFolder(normalized))
            return;

        string parent = Path.GetDirectoryName(normalized)?.Replace("\\", "/");
        string leaf = Path.GetFileName(normalized);
        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf))
            return;

        EnsureFolder(parent);
        if (!AssetDatabase.IsValidFolder(normalized))
            AssetDatabase.CreateFolder(parent, leaf);
    }

    private static string ToAbsoluteProjectPath(string assetPath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
        string relative = assetPath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(projectRoot, relative);
    }

    private static string GetPendingMarkerPath()
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
        return Path.Combine(projectRoot, PendingMarkerRelativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private readonly struct CropItemSpec
    {
        public CropItemSpec(
            string keyToken,
            string worldAssetName,
            string seedAssetName,
            string englishName,
            string vietnameseName,
            int sellPrice,
            int minDrop,
            int maxDrop)
        {
            this.keyToken = keyToken;
            this.worldAssetName = worldAssetName;
            this.seedAssetName = seedAssetName;
            this.englishName = englishName;
            this.vietnameseName = vietnameseName;
            this.sellPrice = sellPrice;
            this.minDrop = minDrop;
            this.maxDrop = maxDrop;
        }

        public readonly string keyToken;
        public readonly string worldAssetName;
        public readonly string seedAssetName;
        public readonly string englishName;
        public readonly string vietnameseName;
        public readonly int sellPrice;
        public readonly int minDrop;
        public readonly int maxDrop;
        public string itemId => $"item_crop_{keyToken}";
    }

    private readonly struct FruitItemSpec
    {
        public FruitItemSpec(
            string keyToken,
            string worldAssetName,
            string seedAssetName,
            string englishName,
            string vietnameseName,
            int sellPrice,
            int minDrop,
            int maxDrop)
        {
            this.keyToken = keyToken;
            this.worldAssetName = worldAssetName;
            this.seedAssetName = seedAssetName;
            this.englishName = englishName;
            this.vietnameseName = vietnameseName;
            this.sellPrice = sellPrice;
            this.minDrop = minDrop;
            this.maxDrop = maxDrop;
        }

        public readonly string keyToken;
        public readonly string worldAssetName;
        public readonly string seedAssetName;
        public readonly string englishName;
        public readonly string vietnameseName;
        public readonly int sellPrice;
        public readonly int minDrop;
        public readonly int maxDrop;
        public string itemId => $"item_crop_{keyToken}";
    }

    private readonly struct WoodTreeSpec
    {
        public WoodTreeSpec(string assetName, int minDrop, int maxDrop)
        {
            this.assetName = assetName;
            this.minDrop = minDrop;
            this.maxDrop = maxDrop;
        }

        public readonly string assetName;
        public readonly int minDrop;
        public readonly int maxDrop;
    }

    private readonly struct SimpleItemSpec
    {
        public SimpleItemSpec(
            string path,
            string id,
            string keyBase,
            string englishName,
            string vietnameseName,
            string englishDesc,
            string vietnameseDesc,
            ItemCategory category,
            int maxStack,
            int sellPrice,
            Sprite icon)
        {
            this.path = path;
            this.id = id;
            this.keyBase = keyBase;
            this.englishName = englishName;
            this.vietnameseName = vietnameseName;
            this.englishDesc = englishDesc;
            this.vietnameseDesc = vietnameseDesc;
            this.category = category;
            this.maxStack = maxStack;
            this.sellPrice = sellPrice;
            this.icon = icon;
        }

        public readonly string path;
        public readonly string id;
        public readonly string keyBase;
        public readonly string englishName;
        public readonly string vietnameseName;
        public readonly string englishDesc;
        public readonly string vietnameseDesc;
        public readonly ItemCategory category;
        public readonly int maxStack;
        public readonly int sellPrice;
        public readonly Sprite icon;
    }

    private readonly struct DropSpecRef
    {
        public DropSpecRef(string assetPath, int minAmount, int maxAmount, float dropChance)
        {
            this.assetPath = assetPath;
            this.minAmount = minAmount;
            this.maxAmount = maxAmount;
            this.dropChance = dropChance;
        }

        public readonly string assetPath;
        public readonly int minAmount;
        public readonly int maxAmount;
        public readonly float dropChance;
    }

    private readonly struct EnemyDropSpec
    {
        public EnemyDropSpec(string enemyAssetName, DropSpecRef[] drops)
        {
            this.enemyAssetName = enemyAssetName;
            this.drops = drops;
        }

        public readonly string enemyAssetName;
        public readonly DropSpecRef[] drops;
    }

    private readonly struct RecipeSpecRef
    {
        public RecipeSpecRef(string assetPath, int amount)
        {
            this.assetPath = assetPath;
            this.amount = amount;
        }

        public readonly string assetPath;
        public readonly int amount;
    }

    private sealed class PatchContext
    {
        private readonly List<string> infos = new List<string>();
        private readonly List<string> warnings = new List<string>();
        private readonly List<string> errors = new List<string>();

        public PatchContext(bool validationOnly)
        {
            ValidationOnly = validationOnly;
        }

        public bool ValidationOnly { get; }
        public int ErrorCount => errors.Count;

        public void Info(string message) => infos.Add(message);
        public void Warning(string message) => warnings.Add(message);
        public void Error(string message) => errors.Add(message);

        public void Flush()
        {
            foreach (string message in infos)
                Debug.Log($"[ContentGapClosurePatcher] {message}");
            foreach (string message in warnings)
                Debug.LogWarning($"[ContentGapClosurePatcher] {message}");
            foreach (string message in errors)
                Debug.LogError($"[ContentGapClosurePatcher] {message}");

            string mode = ValidationOnly ? "Validation" : "Patch";
            if (errors.Count == 0)
                Debug.Log($"[ContentGapClosurePatcher] {mode} completed with {warnings.Count} warnings.");
            else
                Debug.LogError($"[ContentGapClosurePatcher] {mode} completed with {errors.Count} errors and {warnings.Count} warnings.");
        }
    }
}
