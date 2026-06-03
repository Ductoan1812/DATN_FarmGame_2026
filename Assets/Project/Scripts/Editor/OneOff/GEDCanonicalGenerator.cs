#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class GEDCanonicalGenerator
{
    private const string ResourcesRoot = "Assets/Project/Resources/Data";
    private const string EntityRoot = ResourcesRoot + "/Entities";
    private const string RecipeRoot = ResourcesRoot + "/Recipes";
    private const string LocalizationRoot = "Assets/Resources/Localization";

    private const string ItemsRoot = EntityRoot + "/Items";
    private const string WorldRoot = EntityRoot + "/World";
    private const string CharactersRoot = EntityRoot + "/Characters";

    private static readonly Dictionary<string, EntityData> GeneratedEntities = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, RecipeData> GeneratedRecipes = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> ViEntries = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> EnEntries = new(StringComparer.Ordinal);

    [MenuItem("DATN/GED Canonical/Generate Core Playable Slice")]
    public static void GenerateCorePlayableSlice()
    {
        GeneratedEntities.Clear();
        GeneratedRecipes.Clear();
        ViEntries.Clear();
        EnEntries.Clear();

        try
        {
            AssetDatabase.StartAssetEditing();

            EnsureFolder(ItemsRoot);
            EnsureFolder(WorldRoot);
            EnsureFolder(CharactersRoot);
            EnsureFolder(RecipeRoot);
            EnsureFolder(LocalizationRoot);

            SeedExistingLocalization();
            GenerateLocalizationCommon();

            GenerateMaterials();
            GenerateTools();
            GenerateUtilities();
            GenerateFoods();
            GenerateCrops();
            GenerateFruitTrees();
            GenerateResourceNodes();
            GenerateAnimals();
            GenerateEnemies();
            GenerateCoreRecipes();
            GenerateSupportNPCs();
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            WriteLocalizationFile(Path.Combine(LocalizationRoot, "vi.json"), ViEntries);
            WriteLocalizationFile(Path.Combine(LocalizationRoot, "en.json"), EnEntries);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log("[GEDCanonicalGenerator] Generated core playable canonical slice.");
    }

    private static void GenerateLocalizationCommon()
    {
        AddLocalization("ui.crafting.open", "Chế tạo", "Craft");
        AddLocalization("ui.processor.open", "Vận hành", "Operate");
        AddLocalization("ui.storage.title", "Kho đồ", "Storage");
        AddLocalization("ui.processor.title", "Máy chế biến", "Processor");
        AddLocalization("ui.storage.open", "Mở kho", "Open Storage");
        AddLocalization("ui.processor.use", "Sử dụng máy", "Use Machine");
        AddLocalization("ui.bed.sleep", "Đi ngủ", "Sleep");
        AddLocalization("ui.animal.feed", "Cho ăn", "Feed");
        AddLocalization("ui.animal.collect", "Thu sản phẩm", "Collect");
        AddLocalization("ui.animal.status.hungry", "Đói", "Hungry");
        AddLocalization("ui.animal.status.fed", "Đã ăn", "Fed");
        AddLocalization("ui.animal.status.product_ready", "Sẵn sàng thu", "Product Ready");
        AddLocalization("ui.animal.status.dead", "Đã chết", "Dead");
    }

    private static void GenerateTools()
    {
        var specs = new[]
        {
            new ToolSpec("hoe", "Cuốc", "Hoe", ToolType.Hoe, 1, 2, 1, 1, 0.5f, 0f, 0, 0),
            new ToolSpec("hoe", "Cuốc Đồng", "Copper Hoe", ToolType.Hoe, 2, 2, 2, 1, 0.5f, 0f, 2000, 0),
            new ToolSpec("hoe", "Cuốc Sắt", "Iron Hoe", ToolType.Hoe, 3, 2, 3, 1, 0.5f, 0f, 5000, 0),
            new ToolSpec("hoe", "Cuốc Vàng", "Golden Hoe", ToolType.Hoe, 4, 1, 5, 1, 0.5f, 0f, 10000, 0),

            new ToolSpec("watering_can", "Bình Tưới", "Watering Can", ToolType.WateringCan, 1, 2, 1, 1, 0.4f, 40f, 0, 0),
            new ToolSpec("watering_can", "Bình Tưới Đồng", "Copper Watering Can", ToolType.WateringCan, 2, 2, 1, 3, 0.4f, 55f, 2000, 0),
            new ToolSpec("watering_can", "Bình Tưới Sắt", "Iron Watering Can", ToolType.WateringCan, 3, 2, 3, 3, 0.4f, 70f, 5000, 0),
            new ToolSpec("watering_can", "Bình Tưới Vàng", "Golden Watering Can", ToolType.WateringCan, 4, 1, 3, 3, 0.4f, 100f, 10000, 0),

            new ToolSpec("axe", "Rìu Đá", "Stone Axe", ToolType.Axe, 1, 3, 0, 0, 0.55f, 1f, 0, 0, attack: 1f),
            new ToolSpec("axe", "Rìu Đồng", "Copper Axe", ToolType.Axe, 2, 3, 0, 0, 0.55f, 1f, 2000, 0, attack: 2f),
            new ToolSpec("axe", "Rìu Sắt", "Iron Axe", ToolType.Axe, 3, 2, 0, 0, 0.55f, 1f, 5000, 0, attack: 4f),
            new ToolSpec("axe", "Rìu Vàng", "Golden Axe", ToolType.Axe, 4, 2, 0, 0, 0.55f, 1f, 10000, 0, attack: 6f),

            new ToolSpec("pickaxe", "Cuốc Chim Đá", "Stone Pickaxe", ToolType.Pickaxe, 1, 3, 0, 0, 0.6f, 1f, 0, 0, attack: 1f),
            new ToolSpec("pickaxe", "Cuốc Chim Đồng", "Copper Pickaxe", ToolType.Pickaxe, 2, 3, 0, 0, 0.6f, 1f, 2000, 0, attack: 2f),
            new ToolSpec("pickaxe", "Cuốc Chim Sắt", "Iron Pickaxe", ToolType.Pickaxe, 3, 2, 0, 0, 0.6f, 1f, 5000, 0, attack: 4f),
            new ToolSpec("pickaxe", "Cuốc Chim Vàng", "Golden Pickaxe", ToolType.Pickaxe, 4, 2, 0, 0, 0.6f, 1f, 10000, 0, attack: 8f),

            new ToolSpec("scythe", "Liềm Đá", "Stone Scythe", ToolType.Scythe, 1, 2, 1, 1, 0.5f, 1.5f, 0, 0, attack: 3f),
            new ToolSpec("scythe", "Liềm Đồng", "Copper Scythe", ToolType.Scythe, 2, 2, 3, 1, 0.5f, 1.5f, 2000, 0, attack: 5f),
            new ToolSpec("scythe", "Liềm Sắt", "Iron Scythe", ToolType.Scythe, 3, 2, 5, 1, 0.5f, 1.5f, 5000, 0, attack: 8f),
            new ToolSpec("scythe", "Liềm Vàng", "Golden Scythe", ToolType.Scythe, 4, 1, 7, 1, 0.5f, 1.5f, 10000, 0, attack: 12f),
        };

        for (int i = 0; i < specs.Length; i++)
        {
            var spec = specs[i];
            string id = $"item_tool_{spec.baseId}_t{spec.tier}";
            string keyRoot = $"item.tool.{spec.baseId}.t{spec.tier}";
            var data = EnsureEntity(
                $"{ItemsRoot}/Tools/{id}.asset",
                id,
                $"{keyRoot}.name",
                ItemCategory.Tool,
                1,
                spec.buyPrice,
                spec.sellPrice);

            data.baseStats = BuildStats(
                StatPair(StatType.Stamina, spec.staminaCost),
                StatPair(StatType.AreaX, spec.areaX),
                StatPair(StatType.AreaY, spec.areaY),
                StatPair(StatType.CoolDown, spec.cooldown),
                StatPair(StatType.Range, spec.range),
                StatPair(StatType.Attack, spec.attack));

            var toolModule = new ToolModule
            {
                toolType = spec.toolType,
                toolTier = spec.tier,
                refillAnimTrigger = spec.toolType == ToolType.WateringCan ? "Refill" : string.Empty
            };

            data.modules = new List<IModuleData> { toolModule };
            MarkDirty(data);

            AddLocalization($"{keyRoot}.name", spec.viName, spec.enName);
            AddLocalization(
                $"{keyRoot}.desc",
                BuildToolDescriptionVi(spec),
                BuildToolDescriptionEn(spec));
        }
    }

    private static void GenerateMaterials()
    {
        EnsureMaterial("item_mat_wood", "item.mat.wood", "Gỗ", "Wood", 99, 0, 10, "Vật liệu cơ bản lấy từ cây. Dùng để đóng rương, hàng rào và bàn chế tạo.", "Basic lumber from trees. Used for chests, fences, and crafting stations.");
        EnsureMaterial("item_mat_hardwood", "item.mat.hardwood", "Gỗ Cứng", "Hardwood", 99, 0, 15, "Gỗ chất lượng cao từ cây lớn. Dùng cho công trình bền và nâng cấp.", "Dense wood from large trees. Used for sturdy structures and upgrades.");
        EnsureMaterial("item_mat_softwood", "item.mat.softwood", "Gỗ Mềm", "Softwood", 99, 0, 5, "Gỗ nhẹ từ cây non và bạch dương.", "Lightweight wood from young trees and birch.");
        EnsureMaterial("item_mat_twig", "item.mat.twig", "Cành Khô", "Twig", 99, 0, 1, "Cành nhỏ dùng cho công thức cơ bản.", "Small branch used in basic recipes.");
        EnsureMaterial("item_mat_fiber", "item.mat.fiber", "Sợi Thực Vật", "Plant Fiber", 99, 0, 2, "Thu được từ bụi cây và cỏ rậm.", "Gathered from bushes and overgrowth.");
        EnsureMaterial("item_mat_stone", "item.mat.stone", "Đá", "Stone", 99, 0, 2, "Vật liệu khai thác cơ bản cho lò nung và hàng rào đá.", "Basic mined material for furnaces and stonework.");
        EnsureMaterial("item_mat_coal", "item.mat.coal", "Than", "Coal", 99, 0, 15, "Nhiên liệu cho lò nung và máy chế biến.", "Fuel for furnaces and processors.");
        EnsureMaterial("item_mat_copper_ore", "item.mat.copper_ore", "Quặng Đồng", "Copper Ore", 99, 0, 5, "Quặng thô dùng để luyện thỏi đồng.", "Raw ore used to smelt copper bars.");
        EnsureMaterial("item_mat_copper_bar", "item.mat.copper_bar", "Thỏi Đồng", "Copper Bar", 99, 0, 15, "Vật liệu nâng cấp công cụ cấp đầu.", "Upgrade material for early-tier tools.");
        EnsureMaterial("item_mat_iron_ore", "item.mat.iron_ore", "Quặng Sắt", "Iron Ore", 99, 0, 10, "Quặng cứng dùng để luyện thỏi sắt.", "Dense ore used to smelt iron bars.");
        EnsureMaterial("item_mat_iron_bar", "item.mat.iron_bar", "Thỏi Sắt", "Iron Bar", 99, 0, 30, "Vật liệu nâng cấp trung cấp cho công cụ và máy.", "Mid-tier upgrade material for tools and machines.");
        EnsureMaterial("item_mat_gold_ore", "item.mat.gold_ore", "Quặng Vàng", "Gold Ore", 99, 0, 25, "Quặng quý dùng để luyện thỏi vàng.", "Precious ore used to smelt gold bars.");
        EnsureMaterial("item_mat_gold_bar", "item.mat.gold_bar", "Thỏi Vàng", "Gold Bar", 99, 0, 75, "Vật liệu nâng cấp cuối cho tool và công trình cao cấp.", "Late-game material for premium tools and structures.");
        EnsureMaterial("item_mat_flour", "item.mat.flour", "Bột Mì", "Flour", 99, 0, 50, "Được xay từ lúa mì. Dùng cho bánh và pizza.", "Milled from wheat. Used for bread and pizza.");
        EnsureMaterial("item_mat_corn_oil", "item.mat.corn_oil", "Dầu Ngô", "Corn Oil", 99, 0, 60, "Chế biến từ ngô để nấu ăn hoặc làm thức ăn chăn nuôi.", "Processed from corn for cooking or feed.");
        EnsureMaterial("item_feed_hay", "item.feed.hay", "Cỏ Khô", "Hay", 99, 50, 0, "Thức ăn cơ bản cho vật nuôi trong chuồng.", "Basic feed for livestock.");
        EnsureMaterial("item_drop_rat_tail", "item.drop.rat_tail", "Đuôi Chuột", "Rat Tail", 99, 0, 10, "Nguyên liệu quái vật cấp thấp.", "Low-tier monster material.");
        EnsureMaterial("item_drop_snake_skin", "item.drop.snake_skin", "Da Rắn", "Snake Skin", 99, 0, 20, "Da rắn có thể dùng cho craft da hoặc bán.", "Snake skin for leatherwork or sale.");
        EnsureMaterial("item_drop_snake_venom", "item.drop.snake_venom", "Nọc Rắn", "Snake Venom", 99, 0, 40, "Độc dược hiếm từ rắn đồng.", "Rare toxin harvested from field snakes.");
        EnsureMaterial("item_drop_boar_meat", "item.drop.boar_meat", "Thịt Heo Rừng", "Boar Meat", 99, 0, 50, "Thịt tươi từ lợn rừng, dùng cho món ăn hoặc bán.", "Fresh boar meat for cooking or sale.");
        EnsureMaterial("item_drop_boar_tusk", "item.drop.boar_tusk", "Nanh Heo Rừng", "Boar Tusk", 99, 0, 80, "Chiến lợi phẩm từ lợn rừng, dùng cho đồ nghề cứng.", "Boar trophy material suited for rugged crafts.");
        EnsureMaterial("item_prod_egg", "item.prod.egg", "Trứng", "Egg", 99, 0, 50, "Sản phẩm hằng ngày của gà. Có thể ăn hoặc làm mayonnaise.", "Daily chicken product. Can be eaten or processed into mayonnaise.");
        EnsureMaterial("item_prod_milk", "item.prod.milk", "Sữa", "Milk", 99, 0, 125, "Sản phẩm hằng ngày của bò. Dùng làm phô mai.", "Daily cow product used for cheese.");
        EnsureMaterial("item_prod_mayonnaise", "item.prod.mayonnaise", "Mayonnaise", "Mayonnaise", 99, 0, 190, "Chế biến từ trứng. Giá trị cao hơn bán trứng thô.", "Processed from eggs. Worth more than selling raw eggs.");
        EnsureMaterial("item_prod_cheese", "item.prod.cheese", "Phô Mai", "Cheese", 99, 0, 230, "Chế biến từ sữa. Bán tốt và dùng trong món ăn cao cấp.", "Processed from milk. Valuable and useful in premium meals.");
    }

    private static void GenerateFoods()
    {
        var foods = new[]
        {
            new FoodSpec("roasted_corn", "Ngô Nướng", "Roasted Corn", 20f, 0, 35, "Món ăn đơn giản giúp hồi lại chút thể lực.", "A simple snack that restores a small amount of stamina."),
            new FoodSpec("veggie_soup", "Soup Rau Củ", "Veggie Soup", 55f, 0, 100, "Món hầm no bụng, hợp cho ngày làm việc dài.", "A hearty soup for long workdays."),
            new FoodSpec("pizza", "Pizza", "Pizza", 90f, 0, 200, "Món ăn đậm năng lượng, rất hợp trước khi đào mỏ hoặc chiến đấu.", "A dense meal ideal before mining or combat."),
            new FoodSpec("apple_pie", "Bánh Táo", "Apple Pie", 80f, 0, 175, "Bánh trái cây nướng thơm, hồi thể lực tốt.", "A fragrant baked dessert that restores a solid amount of stamina."),
            new FoodSpec("wine", "Rượu Nho", "Wine", 20f, 0, 160, "Thành phẩm lên men từ nho, dùng để bán hoặc nhâm nhi.", "Fermented grape product suited for sale or sipping.")
        };

        for (int i = 0; i < foods.Length; i++)
        {
            var spec = foods[i];
            string id = $"item_food_{spec.id}";
            string keyRoot = $"item.food.{spec.id}";
            var data = EnsureEntity(
                $"{ItemsRoot}/Food/{id}.asset",
                id,
                $"{keyRoot}.name",
                ItemCategory.Food,
                99,
                spec.buyPrice,
                spec.sellPrice);

            data.baseStats = BuildStats(StatPair(StatType.Stamina, spec.restoreStamina));
            data.modules = new List<IModuleData>
            {
                new ConsumableModule
                {
                    restoreStamina = spec.restoreStamina,
                    consumeAmount = 1,
                    destroyOnUse = true
                }
            };
            MarkDirty(data);

            AddLocalization($"{keyRoot}.name", spec.viName, spec.enName);
            AddLocalization($"{keyRoot}.desc", spec.viDesc, spec.enDesc);
        }
    }

    private static void GenerateUtilities()
    {
        var sprinklerT1World = EnsureEntity($"{WorldRoot}/Utility/world_utility_sprinkler_t1.asset", "world_utility_sprinkler_t1", "world.utility.sprinkler.t1.name", ItemCategory.Misc, 1, 0, 0);
        sprinklerT1World.modules = new List<IModuleData>
        {
            new SprinklerModule { waterRadius = 1 }
        };
        sprinklerT1World.baseStats = BuildStats();
        sprinklerT1World.placementRule = BuildFurniturePlacementRule();
        MarkDirty(sprinklerT1World);
        AddLocalization("world.utility.sprinkler.t1.name", "Vòi tưới T1", "Sprinkler T1");
        AddLocalization("world.utility.sprinkler.t1.desc", "Vòi tưới cơ bản, tự động tưới khu vực nhỏ vào đầu ngày mới.", "A basic sprinkler that waters a small area at the start of each day.");

        var sprinklerT1Item = EnsureEntity($"{ItemsRoot}/Placeables/item_place_sprinkler_t1.asset", "item_place_sprinkler_t1", "item.place.sprinkler.t1.name", ItemCategory.Placeable, 10, 2000, 300);
        sprinklerT1Item.modules = new List<IModuleData>
        {
            new PlacementModule
            {
                objectTypeToSpawn = ObjectType.Sprinkler01,
                placedEntityData = sprinklerT1World,
                centerTile = true,
                animTrigger = "PutDown"
            }
        };
        sprinklerT1Item.baseStats = BuildStats();
        MarkDirty(sprinklerT1Item);
        AddLocalization("item.place.sprinkler.t1.name", "Bản vẽ Vòi tưới T1", "Sprinkler T1");
        AddLocalization("item.place.sprinkler.t1.desc", "Đặt xuống để tự động tưới đất đã cuốc trong bán kính nhỏ vào đầu ngày.", "Place it to automatically water nearby tilled soil each morning.");

        var sprinklerT2World = EnsureEntity($"{WorldRoot}/Utility/world_utility_sprinkler_t2.asset", "world_utility_sprinkler_t2", "world.utility.sprinkler.t2.name", ItemCategory.Misc, 1, 0, 0);
        sprinklerT2World.modules = new List<IModuleData>
        {
            new SprinklerModule { waterRadius = 2 }
        };
        sprinklerT2World.baseStats = BuildStats();
        sprinklerT2World.placementRule = BuildFurniturePlacementRule();
        MarkDirty(sprinklerT2World);
        AddLocalization("world.utility.sprinkler.t2.name", "Vòi tưới T2", "Sprinkler T2");
        AddLocalization("world.utility.sprinkler.t2.desc", "Vòi tưới nâng cấp, bao phủ khu vực rộng hơn vào đầu ngày mới.", "An upgraded sprinkler that covers a wider area each morning.");

        var sprinklerT2Item = EnsureEntity($"{ItemsRoot}/Placeables/item_place_sprinkler_t2.asset", "item_place_sprinkler_t2", "item.place.sprinkler.t2.name", ItemCategory.Placeable, 10, 5000, 800);
        sprinklerT2Item.modules = new List<IModuleData>
        {
            new PlacementModule
            {
                objectTypeToSpawn = ObjectType.Sprinkler02,
                placedEntityData = sprinklerT2World,
                centerTile = true,
                animTrigger = "PutDown"
            }
        };
        sprinklerT2Item.baseStats = BuildStats();
        MarkDirty(sprinklerT2Item);
        AddLocalization("item.place.sprinkler.t2.name", "Bản vẽ Vòi tưới T2", "Sprinkler T2");
        AddLocalization("item.place.sprinkler.t2.desc", "Đặt xuống để tự động tưới khu vực lớn hơn, phù hợp cho ruộng giữa game.", "Place it to water a larger area automatically, ideal for mid-game fields.");

        var bedWorld = EnsureEntity($"{WorldRoot}/Utility/world_utility_bed_basic.asset", "world_utility_bed_basic", "world.utility.bed.basic.name", ItemCategory.Misc, 1, 0, 0);
        bedWorld.modules = new List<IModuleData>
        {
            new BedModule()
        };
        bedWorld.baseStats = BuildStats();
        bedWorld.placementRule = BuildFurniturePlacementRule();
        MarkDirty(bedWorld);
        AddLocalization("world.utility.bed.basic.name", "Giường Gỗ", "Wooden Bed");
        AddLocalization("world.utility.bed.basic.desc", "Ngủ để kết thúc ngày, hồi đầy thể lực và máu trước khi sang ngày mới.", "Sleep to end the day and fully restore stamina and health before the next morning.");

        var bedItem = EnsureEntity($"{ItemsRoot}/Placeables/item_place_bed_basic.asset", "item_place_bed_basic", "item.place.bed.basic.name", ItemCategory.Placeable, 1, 1500, 250);
        bedItem.modules = new List<IModuleData>
        {
            new PlacementModule
            {
                objectTypeToSpawn = ObjectType.Bed01,
                placedEntityData = bedWorld,
                centerTile = true,
                animTrigger = "PutDown"
            }
        };
        bedItem.baseStats = BuildStats();
        MarkDirty(bedItem);
        AddLocalization("item.place.bed.basic.name", "Giường Gỗ", "Wooden Bed");
        AddLocalization("item.place.bed.basic.desc", "Một chiếc giường đặt trong nhà để kết thúc ngày và bắt đầu buổi sáng tiếp theo trong trạng thái hồi phục.", "A house bed used to end the day and begin the next morning fully rested.");
    }

    private static void GenerateCrops()
    {
        var specs = new[]
        {
            new CropSpec("spring_lettuce", Season.Spring, "Xà Lách", "Lettuce", new []{1,2,0}, false, 0, -1, true, 15, 35, 1, 2, "Bán tươi hoặc nấu salad."),
            new CropSpec("spring_potato", Season.Spring, "Khoai Tây", "Potato", new []{1,2,2,1,0}, false, 0, -1, true, 40, 80, 1, 4, "Nguyên liệu tốt cho món chiên và bán sớm."),
            new CropSpec("spring_asparagus", Season.Spring, "Măng Tây", "Asparagus", new []{3,4,0,3}, true, 3, 2, false, 50, 45, 2, 4, "Cây tái thu hoạch, phù hợp tối ưu lợi nhuận đầu mùa."),
            new CropSpec("spring_strawberry", Season.Spring, "Dâu Tây", "Strawberry", new []{1,2,2,3,0}, true, 4, 3, false, 100, 60, 1, 3, "Cây lợi nhuận cao nếu trồng sớm trong mùa Xuân."),
            new CropSpec("summer_tomato", Season.Summer, "Cà Chua", "Tomato", new []{2,3,3,3,0}, true, 4, 3, false, 50, 60, 1, 4, "Nguyên liệu bếp quan trọng, tái thu hoạch đều."),
            new CropSpec("summer_corn", Season.Summer, "Ngô", "Corn", new []{2,3,3,2,0}, true, 4, 3, false, 75, 50, 1, 1, "Có thể ăn, bán, hoặc xay thành dầu và thức ăn."),
            new CropSpec("fall_pumpkin", Season.Fall, "Bí Ngô", "Pumpkin", new []{3,4,3,3,0}, false, 0, -1, true, 100, 320, 1, 1, "Cây lợi nhuận cao cuối mùa, thích hợp chuẩn bị vốn nâng cấp."),
            new CropSpec("winter_spinach", Season.Winter, "Rau Chân Vịt", "Spinach", new []{2,2,3,0}, false, 0, -1, true, 40, 70, 1, 3, "Rau mùa Đông ổn định, trưởng thành nhanh.")
        };

        for (int i = 0; i < specs.Length; i++)
            GenerateCrop(specs[i]);
    }

    private static void GenerateCrop(CropSpec spec)
    {
        string seasonSlug = spec.season.ToString().ToLowerInvariant();
        string baseKey = $"{seasonSlug}.{spec.id.Replace($"{seasonSlug}_", string.Empty)}";
        string seedId = $"item_seed_{spec.id}";
        string harvestId = $"item_crop_{spec.id}";
        string worldId = $"world_crop_{spec.id}";
        string seedKeyRoot = $"item.seed.{baseKey}";
        string cropKeyRoot = $"item.crop.{baseKey}";
        string worldKeyRoot = $"world.crop.{baseKey}";

        var harvestItem = EnsureEntity(
            $"{ItemsRoot}/Crops/{harvestId}.asset",
            harvestId,
            $"{cropKeyRoot}.name",
            ItemCategory.Crop,
            99,
            0,
            spec.cropSellPrice);
        harvestItem.modules = new List<IModuleData>();
        MarkDirty(harvestItem);

        var worldCrop = EnsureEntity(
            $"{WorldRoot}/Crops/{worldId}.asset",
            worldId,
            $"{worldKeyRoot}.name",
            ItemCategory.Misc,
            1,
            0,
            0);
        worldCrop.placementRule = BuildPlantPlacementRule();
        worldCrop.baseStats = BuildStats();
        worldCrop.modules = new List<IModuleData>
        {
            BuildCropStageModule(spec),
            new SeasonRuleModule
            {
                allowAllSeasons = false,
                allowedSeasons = new [] { spec.season },
                blockPlacementOutOfSeason = true,
                outOfSeasonBehavior = OutOfSeasonBehavior.Wilt
            },
            new HarvestModule
            {
                allowHandHarvest = true,
                harvestTool = ToolType.Scythe,
                destroyOnHarvest = !spec.regrowable,
                dropMode = HarvestDropMode.World
            },
            new DropModule
            {
                harvestDrops = new[]
                {
                    new DropEntry
                    {
                        item = harvestItem,
                        minAmount = spec.minDrop,
                        maxAmount = spec.maxDrop,
                        dropChance = 1f
                    }
                }
            },
            new QualityModule
            {
                minQuality = 1,
                maxQuality = 3,
                soilQualityPerStar = 1
            },
            new MortalModule()
        };
        MarkDirty(worldCrop);

        var seedItem = EnsureEntity(
            $"{ItemsRoot}/Seeds/{seedId}.asset",
            seedId,
            $"{seedKeyRoot}.name",
            ItemCategory.Seed,
            99,
            spec.seedBuyPrice,
            0);
        seedItem.baseStats = BuildStats();
        seedItem.modules = new List<IModuleData>
        {
            new PlacementModule
            {
                objectTypeToSpawn = ObjectType.Plant01,
                placedEntityData = worldCrop,
                centerTile = true,
                animTrigger = "Sow"
            },
            new SeasonRuleModule
            {
                allowAllSeasons = false,
                allowedSeasons = new [] { spec.season },
                blockPlacementOutOfSeason = true,
                outOfSeasonBehavior = OutOfSeasonBehavior.None
            }
        };
        MarkDirty(seedItem);

        AddLocalization($"{seedKeyRoot}.name", $"Hạt {spec.viName}", $"{spec.enName} Seeds");
        AddLocalization($"{seedKeyRoot}.desc", BuildSeedDescriptionVi(spec), BuildSeedDescriptionEn(spec));
        AddLocalization($"{cropKeyRoot}.name", spec.viName, spec.enName);
        AddLocalization($"{cropKeyRoot}.desc", BuildCropItemDescriptionVi(spec), BuildCropItemDescriptionEn(spec));
        AddLocalization($"{worldKeyRoot}.name", spec.viName, spec.enName);
        AddLocalization($"{worldKeyRoot}.desc", BuildWorldCropDescriptionVi(spec), BuildWorldCropDescriptionEn(spec));
    }

    private static void GenerateFruitTrees()
    {
        var harvestItem = EnsureEntity($"{ItemsRoot}/Crops/item_crop_tree_apple.asset", "item_crop_tree_apple", "item.crop.tree.apple.name", ItemCategory.Crop, 99, 0, 75);
        MarkDirty(harvestItem);

        var worldTree = EnsureEntity($"{WorldRoot}/FruitTrees/world_tree_apple.asset", "world_tree_apple", "world.tree.apple.name", ItemCategory.Misc, 1, 0, 0);
        worldTree.placementRule = BuildTreePlacementRule();
        worldTree.modules = new List<IModuleData>
        {
            new StageModule
            {
                requiresWater = false,
                wiltOnSeasonChange = false,
                harvestGoToStageIndex = 4,
                lastStageLoopToIndex = 4,
                daysToReturnAfterHarvest = 1,
                stages = BuildStages(7,7,7,7,0)
            },
            new SeasonRuleModule
            {
                allowAllSeasons = false,
                allowedSeasons = new [] { Season.Fall },
                blockPlacementOutOfSeason = false,
                outOfSeasonBehavior = OutOfSeasonBehavior.Dormant,
                dormantStageIndex = 3
            },
            new HarvestModule
            {
                allowHandHarvest = true,
                destroyOnHarvest = false,
                dropMode = HarvestDropMode.World
            },
            new DropModule
            {
                harvestDrops = new[]
                {
                    new DropEntry { item = harvestItem, minAmount = 1, maxAmount = 1, dropChance = 1f }
                },
                deathDrops = new[]
                {
                    new DropEntry { item = GetEntity("item_mat_hardwood"), minAmount = 5, maxAmount = 8, dropChance = 1f },
                    new DropEntry { item = GetEntity("item_mat_wood"), minAmount = 2, maxAmount = 4, dropChance = 1f }
                },
                includeHarvestDropsOnDestroyWhenHarvestable = true
            },
            new HealthModule(),
            new ToolRequirementModule
            {
                requiredToolType = ToolType.Axe,
                minimumToolTier = 2,
                blockDamageIfWrongTool = true,
                blockDamageIfBelowTier = false,
                wrongToolPenalty = 0.25f
            },
            new MortalModule(),
            new ResourceHitReactionModule()
        };
        MarkDirty(worldTree);

        var sapling = EnsureEntity($"{ItemsRoot}/Seeds/item_seed_tree_apple.asset", "item_seed_tree_apple", "item.seed.tree.apple.name", ItemCategory.Seed, 99, 1000, 0);
        sapling.modules = new List<IModuleData>
        {
            new PlacementModule
            {
                objectTypeToSpawn = ObjectType.TreeNode01,
                placedEntityData = worldTree,
                centerTile = true,
                animTrigger = "Sow"
            }
        };
        MarkDirty(sapling);

        AddLocalization("item.seed.tree.apple.name", "Cây Non Táo", "Apple Sapling");
        AddLocalization("item.seed.tree.apple.desc", "Cây ăn quả lâu năm. Mất 28 ngày để trưởng thành và cho táo vào mùa Thu. Cây không héo khi sang mùa mới.", "A perennial fruit tree. Takes 28 days to mature and bears apples in Fall. It will not wither when the season changes.");
        AddLocalization("item.crop.tree.apple.name", "Táo", "Apple");
        AddLocalization("item.crop.tree.apple.desc", "Quả ngọt từ cây táo. Bán tốt và dùng cho bánh hoặc đồ uống.", "A sweet tree fruit that sells well and works in pies or drinks.");
        AddLocalization("world.tree.apple.name", "Cây Táo", "Apple Tree");
        AddLocalization("world.tree.apple.desc", "Cây lâu năm cho quả vào mùa Thu và có thể chặt lấy gỗ.", "A perennial tree that fruits in Fall and can also be chopped for wood.");
    }

    private static void GenerateResourceNodes()
    {
        GenerateWoodTree(
            "oak",
            "Sồi",
            "Oak",
            12,
            2,
            new[]
            {
                new DropSpec("item_mat_hardwood", 5, 8, 1f),
                new DropSpec("item_mat_wood", 2, 4, 1f)
            });

        GenerateWoodTree(
            "maple",
            "Phong",
            "Maple",
            10,
            1,
            new[]
            {
                new DropSpec("item_mat_wood", 4, 6, 1f),
                new DropSpec("item_mat_softwood", 1, 2, 1f)
            });

        GenerateWoodTree(
            "pine",
            "Thông",
            "Pine",
            10,
            1,
            new[]
            {
                new DropSpec("item_mat_wood", 3, 5, 1f),
                new DropSpec("item_mat_twig", 2, 4, 1f)
            });

        GenerateOreNode("rock", "Đá Thường", "Stone Rock", 4, 1, new[] { new DropSpec("item_mat_stone", 3, 5, 1f), new DropSpec("item_mat_coal", 1, 1, 0.05f) }, ObjectType.RockNode01, 3);
        GenerateOreNode("copper", "Quặng Đồng", "Copper Node", 6, 1, new[] { new DropSpec("item_mat_copper_ore", 2, 4, 1f), new DropSpec("item_mat_stone", 1, 2, 0.5f) }, ObjectType.OreNode01, 5);
        GenerateOreNode("iron", "Quặng Sắt", "Iron Node", 10, 2, new[] { new DropSpec("item_mat_iron_ore", 2, 3, 1f), new DropSpec("item_mat_stone", 1, 1, 0.3f) }, ObjectType.OreNode01, 7);
        GenerateOreNode("gold", "Quặng Vàng", "Gold Node", 16, 3, new[] { new DropSpec("item_mat_gold_ore", 1, 2, 1f) }, ObjectType.OreNode01, 10);
    }

    private static void GenerateWoodTree(string id, string viName, string enName, int maxHp, int minTier, DropSpec[] drops)
    {
        string entityId = $"world_tree_{id}";
        string keyRoot = $"world.tree.{id}";
        var data = EnsureEntity($"{WorldRoot}/WoodTrees/{entityId}.asset", entityId, $"{keyRoot}.name", ItemCategory.Misc, 1, 0, 0);
        data.placementRule = BuildTreePlacementRule();
        data.baseStats = BuildStats(StatPair(StatType.MaxHp, maxHp));
        data.modules = new List<IModuleData>
        {
            new HealthModule(),
            new HarvestModule
            {
                harvestTool = ToolType.Axe,
                dropMode = HarvestDropMode.World,
                harvestCausesDamage = true,
                destroyOnHarvest = false,
                oneHitDestroy = false
            },
            new ToolRequirementModule
            {
                requiredToolType = ToolType.Axe,
                minimumToolTier = minTier,
                wrongToolPenalty = 0f,
                blockDamageIfWrongTool = true,
                blockDamageIfBelowTier = true
            },
            new DropModule { deathDrops = BuildDropEntries(drops) },
            new ResourceHitReactionModule(),
            new MortalModule()
        };
        MarkDirty(data);
        AddLocalization($"{keyRoot}.name", viName, enName);
        AddLocalization($"{keyRoot}.desc", $"Cây lấy gỗ có thể chặt bằng rìu cấp {minTier} trở lên.", $"A lumber tree that can be chopped with an Axe tier {minTier} or higher.");
    }

    private static void GenerateOreNode(string id, string viName, string enName, int maxHp, int minTier, DropSpec[] drops, ObjectType objectType, int respawnDays)
    {
        string entityId = $"world_node_{id}";
        string keyRoot = $"world.node.{id}";
        var data = EnsureEntity($"{WorldRoot}/Resources/{entityId}.asset", entityId, $"{keyRoot}.name", ItemCategory.Misc, 1, 0, 0);
        data.placementRule = BuildNodePlacementRule();
        data.baseStats = BuildStats(StatPair(StatType.MaxHp, maxHp));
        data.modules = new List<IModuleData>
        {
            new HealthModule(),
            new HarvestModule
            {
                harvestTool = ToolType.Pickaxe,
                dropMode = HarvestDropMode.World,
                harvestCausesDamage = true,
                destroyOnHarvest = false,
                oneHitDestroy = false
            },
            new ToolRequirementModule
            {
                requiredToolType = ToolType.Pickaxe,
                minimumToolTier = minTier,
                wrongToolPenalty = 0f,
                blockDamageIfWrongTool = true,
                blockDamageIfBelowTier = true
            },
            new DropModule { deathDrops = BuildDropEntries(drops) },
            new ResourceHitReactionModule(),
            new RespawnModule
            {
                respawnPrefabId = objectType,
                respawnDelay = 3f,
                restoreFullHp = true
            }
        };
        MarkDirty(data);
        AddLocalization($"{keyRoot}.name", viName, enName);
        AddLocalization($"{keyRoot}.desc", $"Node tài nguyên yêu cầu cuốc chim cấp {minTier} và hồi lại sau khoảng {respawnDays} ngày.", $"A resource node that requires Pickaxe tier {minTier} and returns after roughly {respawnDays} days.");
    }

    private static void GenerateAnimals()
    {
        GenerateAnimal("chicken", "Gà", "Chicken", 30f, "item_prod_egg", 1, 3, 800);
        GenerateAnimal("cow", "Bò", "Cow", 50f, "item_prod_milk", 1, 3, 1500);
    }

    private static void GenerateAnimal(string id, string viName, string enName, float maxHp, string productId, int productAmount, int daysWithoutFoodToDie, int buyPrice)
    {
        string entityId = $"world_animal_{id}";
        string keyRoot = $"world.animal.{id}";
        var data = EnsureEntity($"{WorldRoot}/Animals/{entityId}.asset", entityId, $"{keyRoot}.name", ItemCategory.Misc, 1, buyPrice, 0);
        data.placementRule = BuildAnimalPlacementRule();
        data.baseStats = BuildStats(StatPair(StatType.MaxHp, maxHp));
        data.modules = new List<IModuleData>
        {
            new HealthModule(),
            new AnimalModule
            {
                feedItem = GetEntity("item_feed_hay"),
                productItem = GetEntity(productId),
                productAmount = productAmount,
                daysWithoutFoodToDie = daysWithoutFoodToDie
            }
        };
        MarkDirty(data);
        AddLocalization($"{keyRoot}.name", viName, enName);
        AddLocalization($"{keyRoot}.desc", $"Vật nuôi cần cho ăn mỗi ngày để cho sản phẩm đều đặn.", $"A farm animal that must be fed daily to keep producing.");
    }

    private static void GenerateEnemies()
    {
        GenerateEnemy("rat", "Chuột Đồng", "Field Rat", 15f, 0f, 3f, 4.5f, 22, new[] { new DropSpec("item_drop_rat_tail", 1, 1, 0.7f) });
        GenerateEnemy("snake", "Rắn Đồng", "Field Snake", 25f, 2f, 6f, 3.5f, 37, new[] { new DropSpec("item_drop_snake_skin", 1, 1, 0.6f), new DropSpec("item_drop_snake_venom", 1, 1, 0.2f) });
        GenerateEnemy("boar", "Lợn Rừng", "Boar", 60f, 5f, 12f, 3f, 90, new[] { new DropSpec("item_drop_boar_meat", 1, 2, 0.9f), new DropSpec("item_drop_boar_tusk", 1, 1, 0.3f) });
    }

    private static void GenerateEnemy(string id, string viName, string enName, float maxHp, float defense, float attack, float speed, int expReward, DropSpec[] drops)
    {
        string entityId = $"enemy_{id}";
        string keyRoot = $"enemy.{id}";
        var data = EnsureEntity($"{CharactersRoot}/Enemies/{entityId}.asset", entityId, $"{keyRoot}.name", ItemCategory.Misc, 1, 0, 0);
        data.placementRule = BuildEnemyPlacementRule();
        data.baseStats = BuildStats(
            StatPair(StatType.MaxHp, maxHp),
            StatPair(StatType.Defense, defense),
            StatPair(StatType.Attack, attack),
            StatPair(StatType.Speed, speed));
        data.modules = new List<IModuleData>
        {
            new HealthModule(),
            new DropModule { deathDrops = BuildDropEntries(drops) },
            new ExpRewardModule { rewardExp = expReward, sourceType = ExpSourceType.Combat, requireKiller = true },
            new MortalModule()
        };
        MarkDirty(data);

        AddLocalization($"{keyRoot}.name", viName, enName);
        AddLocalization($"{keyRoot}.desc", $"Kẻ địch ngoài thiên nhiên với {Mathf.RoundToInt(maxHp)} HP, phù hợp cho vòng chiến đấu đầu game.", $"A wild enemy with {Mathf.RoundToInt(maxHp)} HP suited for early combat loops.");
    }

    private static void GenerateSupportNPCs()
    {
        var shopkeeper = EnsureEntity($"{CharactersRoot}/NPCs/npc_shop_bac_ba.asset", "npc_shop_bac_ba", "npc.shop.bac_ba.name", ItemCategory.Misc, 1, 0, 0);
        shopkeeper.placementRule = BuildNPCPlacementRule();
        shopkeeper.modules = new List<IModuleData>
        {
            new InventoryModule { inventoryType = InventoryType.Backpack, size = 24 },
            new ShopModule
            {
                optionTextKey = "ui.shop.open",
                priority = 30,
                sellsToPlayer = true,
                buysFromPlayer = true,
                infiniteStock = true,
                initialStock = new List<ShopStockEntry>
                {
                    new ShopStockEntry { itemData = GetEntity("item_seed_spring_lettuce"), amount = 20 },
                    new ShopStockEntry { itemData = GetEntity("item_seed_spring_potato"), amount = 20 },
                    new ShopStockEntry { itemData = GetEntity("item_seed_summer_tomato"), amount = 20 },
                    new ShopStockEntry { itemData = GetEntity("item_seed_summer_corn"), amount = 20 },
                    new ShopStockEntry { itemData = GetEntity("item_seed_fall_pumpkin"), amount = 10 },
                    new ShopStockEntry { itemData = GetEntity("item_seed_winter_spinach"), amount = 20 }
                }
            }
        };
        MarkDirty(shopkeeper);
        AddLocalization("npc.shop.bac_ba.name", "Bác Ba", "Uncle Ba");
        AddLocalization("npc.shop.bac_ba.desc", "Chủ tiệm nông cụ và hạt giống theo mùa.", "A general seed and farm goods merchant.");
    }

    private static void GenerateCoreRecipes()
    {
        EnsureRecipe(
            "recipe_furnace_copper_bar",
            "recipe.furnace.copper_bar",
            ingredients: new[] { MakeRecipeIngredientSpec("item_mat_copper_ore", 5), MakeRecipeIngredientSpec("item_mat_coal", 1) },
            outputs: new[] { MakeRecipeIngredientSpec("item_mat_copper_bar", 1) },
            craftExp: 5);

        EnsureRecipe(
            "recipe_furnace_iron_bar",
            "recipe.furnace.iron_bar",
            ingredients: new[] { MakeRecipeIngredientSpec("item_mat_iron_ore", 5), MakeRecipeIngredientSpec("item_mat_coal", 1) },
            outputs: new[] { MakeRecipeIngredientSpec("item_mat_iron_bar", 1) },
            craftExp: 8);

        EnsureRecipe(
            "recipe_furnace_gold_bar",
            "recipe.furnace.gold_bar",
            ingredients: new[] { MakeRecipeIngredientSpec("item_mat_gold_ore", 5), MakeRecipeIngredientSpec("item_mat_coal", 1) },
            outputs: new[] { MakeRecipeIngredientSpec("item_mat_gold_bar", 1) },
            craftExp: 10);

        EnsureRecipe(
            "recipe_mill_corn_oil",
            "recipe.mill.corn_oil",
            ingredients: new[] { MakeRecipeIngredientSpec("item_crop_summer_corn", 1) },
            outputs: new[] { MakeRecipeIngredientSpec("item_mat_corn_oil", 1) },
            craftExp: 4);

        EnsureRecipe(
            "recipe_mayo_basic",
            "recipe.machine.mayo",
            ingredients: new[] { MakeRecipeIngredientSpec("item_prod_egg", 1) },
            outputs: new[] { MakeRecipeIngredientSpec("item_prod_mayonnaise", 1) },
            craftExp: 4);

        EnsureRecipe(
            "recipe_cheese_basic",
            "recipe.machine.cheese",
            ingredients: new[] { MakeRecipeIngredientSpec("item_prod_milk", 3) },
            outputs: new[] { MakeRecipeIngredientSpec("item_prod_cheese", 1) },
            craftExp: 6);
    }

    private static EntityData EnsureEntity(string assetPath, string id, string keyName, ItemCategory category, int maxStack, int buyPrice, int sellPrice)
    {
        EnsureFolder(Path.GetDirectoryName(assetPath).Replace('\\', '/'));
        var data = AssetDatabase.LoadAssetAtPath<EntityData>(assetPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<EntityData>();
            AssetDatabase.CreateAsset(data, assetPath);
        }

        data.id = id;
        data.keyName = keyName;
        data.descKey = keyName.Replace(".name", ".desc");
        data.category = category;
        data.maxStack = Mathf.Max(1, maxStack);
        data.buyPrice = buyPrice;
        data.sellPrice = sellPrice;
        data.icon = null;
        GeneratedEntities[id] = data;
        return data;
    }

    private static RecipeData EnsureRecipe(string id, string keyRoot, RecipeIngredientSpec[] ingredients, RecipeIngredientSpec[] outputs, int craftExp)
    {
        string assetPath = $"{RecipeRoot}/{id}.asset";
        EnsureFolder(Path.GetDirectoryName(assetPath).Replace('\\', '/'));
        var recipe = AssetDatabase.LoadAssetAtPath<RecipeData>(assetPath);
        if (recipe == null)
        {
            recipe = ScriptableObject.CreateInstance<RecipeData>();
            AssetDatabase.CreateAsset(recipe, assetPath);
        }

        recipe.id = id;
        recipe.titleKey = $"{keyRoot}.name";
        recipe.ingredients = BuildRecipeEntries(ingredients);
        recipe.outputs = BuildRecipeEntries(outputs);
        recipe.craftExp = craftExp;
        EditorUtility.SetDirty(recipe);
        GeneratedRecipes[id] = recipe;

        AddLocalization($"{keyRoot}.name", BuildRecipeNameVi(id), BuildRecipeNameEn(id));
        AddLocalization($"{keyRoot}.desc", "Công thức chế tạo chuẩn hóa từ GED.", "A GED-normalized crafting recipe.");
        return recipe;
    }

    private static void EnsureMaterial(string id, string keyRoot, string viName, string enName, int maxStack, int buyPrice, int sellPrice, string viDesc, string enDesc)
    {
        var data = EnsureEntity($"{ItemsRoot}/Materials/{id}.asset", id, $"{keyRoot}.name", ItemCategory.Material, maxStack, buyPrice, sellPrice);
        data.modules = new List<IModuleData>();
        data.baseStats = BuildStats();
        MarkDirty(data);
        AddLocalization($"{keyRoot}.name", viName, enName);
        AddLocalization($"{keyRoot}.desc", viDesc, enDesc);
    }

    private static StageModule BuildCropStageModule(CropSpec spec)
    {
        var stages = BuildStages(spec.stageDays);
        return new StageModule
        {
            requiresWater = true,
            wiltOnSeasonChange = true,
            harvestGoToStageIndex = spec.regrowable ? spec.stageDays.Length - 1 : -1,
            lastStageLoopToIndex = spec.regrowable ? Mathf.Clamp(spec.loopToStageIndex, 0, spec.stageDays.Length - 1) : -1,
            daysToReturnAfterHarvest = spec.regrowable ? spec.daysToReturnAfterHarvest : 0,
            stages = stages
        };
    }

    private static GrowthStage[] BuildStages(params int[] daysToGrow)
    {
        var stages = new GrowthStage[daysToGrow.Length];
        for (int i = 0; i < daysToGrow.Length; i++)
        {
            stages[i] = new GrowthStage
            {
                daysToGrow = Mathf.Max(0, daysToGrow[i]),
                canHarvest = i == daysToGrow.Length - 1 && daysToGrow[i] == 0
            };
        }
        return stages;
    }

    private static DropEntry[] BuildDropEntries(DropSpec[] drops)
    {
        var result = new List<DropEntry>();
        if (drops == null)
            return result.ToArray();

        for (int i = 0; i < drops.Length; i++)
        {
            var item = GetEntity(drops[i].itemId);
            if (item == null)
                continue;

            result.Add(new DropEntry
            {
                item = item,
                minAmount = drops[i].minAmount,
                maxAmount = drops[i].maxAmount,
                dropChance = drops[i].chance
            });
        }

        return result.ToArray();
    }

    private static StatsData BuildStats(params StatEntry[] entries)
    {
        var data = new StatsData();
        data.baseStats = entries != null ? new List<StatEntry>(entries) : new List<StatEntry>();
        return data;
    }

    private static StatEntry StatPair(StatType statType, float baseValue)
    {
        return new StatEntry
        {
            statType = statType,
            value = baseValue
        };
    }

    private static List<RecipeIngredient> BuildRecipeEntries(RecipeIngredientSpec[] specs)
    {
        var result = new List<RecipeIngredient>();
        if (specs == null)
            return result;

        for (int i = 0; i < specs.Length; i++)
        {
            var item = GetEntity(specs[i].itemId);
            if (item == null)
                continue;

            result.Add(new RecipeIngredient
            {
                item = item,
                amount = Mathf.Max(1, specs[i].amount)
            });
        }

        return result;
    }

    private static RecipeIngredientSpec MakeRecipeIngredientSpec(string itemId, int amount)
    {
        return new RecipeIngredientSpec(itemId, amount);
    }

    private static PlacementRule BuildPlantPlacementRule()
    {
        return new PlacementRule
        {
            occupyLayer = EntityLayer.Plant,
            requireTags = PlacementTag.Plantable,
            provideTags = PlacementTag.None,
            blockLayers = new[] { EntityLayer.Plant, EntityLayer.Furniture }
        };
    }

    private static PlacementRule BuildTreePlacementRule()
    {
        return new PlacementRule
        {
            occupyLayer = EntityLayer.Plant,
            requireTags = PlacementTag.Buildable | PlacementTag.Plantable,
            provideTags = PlacementTag.None,
            blockLayers = new[] { EntityLayer.Plant, EntityLayer.Furniture }
        };
    }

    private static PlacementRule BuildNodePlacementRule()
    {
        return new PlacementRule
        {
            occupyLayer = EntityLayer.Decoration,
            requireTags = PlacementTag.None,
            provideTags = PlacementTag.None,
            blockLayers = new[] { EntityLayer.Decoration, EntityLayer.Furniture }
        };
    }

    private static PlacementRule BuildAnimalPlacementRule()
    {
        return new PlacementRule
        {
            occupyLayer = EntityLayer.Plant,
            requireTags = PlacementTag.Walkable,
            provideTags = PlacementTag.None,
            blockLayers = new[] { EntityLayer.Plant, EntityLayer.Furniture }
        };
    }

    private static PlacementRule BuildEnemyPlacementRule()
    {
        return new PlacementRule
        {
            occupyLayer = EntityLayer.Plant,
            requireTags = PlacementTag.Walkable,
            provideTags = PlacementTag.None,
            blockLayers = new[] { EntityLayer.Plant, EntityLayer.Furniture }
        };
    }

    private static PlacementRule BuildNPCPlacementRule()
    {
        return new PlacementRule
        {
            occupyLayer = EntityLayer.Furniture,
            requireTags = PlacementTag.Walkable,
            provideTags = PlacementTag.None,
            blockLayers = new[] { EntityLayer.Furniture, EntityLayer.Plant }
        };
    }

    private static PlacementRule BuildFurniturePlacementRule()
    {
        return new PlacementRule
        {
            occupyLayer = EntityLayer.Furniture,
            requireTags = PlacementTag.Buildable | PlacementTag.Walkable,
            provideTags = PlacementTag.None,
            blockLayers = new[] { EntityLayer.Furniture, EntityLayer.Plant }
        };
    }

    private static EntityData GetEntity(string id)
    {
        if (GeneratedEntities.TryGetValue(id, out var entity))
            return entity;

        string[] guids = AssetDatabase.FindAssets($"t:EntityData {id}");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var data = AssetDatabase.LoadAssetAtPath<EntityData>(path);
            if (data != null && string.Equals(data.id, id, StringComparison.Ordinal))
            {
                GeneratedEntities[id] = data;
                return data;
            }
        }

        return null;
    }

    private static void AddLocalization(string key, string vi, string en)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        ViEntries[key] = vi ?? string.Empty;
        EnEntries[key] = en ?? string.Empty;
    }

    private static void SeedExistingLocalization()
    {
        MergeLocalizationFile(Path.Combine(LocalizationRoot, "vi.json"), ViEntries);
        MergeLocalizationFile(Path.Combine(LocalizationRoot, "en.json"), EnEntries);
    }

    private static void MergeLocalizationFile(string path, Dictionary<string, string> target)
    {
        if (!File.Exists(path))
            return;

        var json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json))
            return;

        var file = JsonUtility.FromJson<LocalizationFile>(json);
        if (file?.entries == null)
            return;

        for (int i = 0; i < file.entries.Length; i++)
        {
            var entry = file.entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                continue;

            target[entry.key] = entry.value ?? string.Empty;
        }
    }

    private static void WriteLocalizationFile(string path, Dictionary<string, string> entries)
    {
        var file = new LocalizationFile();
        var list = new List<LocalizationEntry>();

        foreach (var pair in entries)
            list.Add(new LocalizationEntry { key = pair.Key, value = pair.Value });

        list.Sort((a, b) => string.CompareOrdinal(a.key, b.key));
        file.entries = list.ToArray();

        File.WriteAllText(path, JsonUtility.ToJson(file, true));
    }

    private static void MarkDirty(UnityEngine.Object obj)
    {
        if (obj != null)
            EditorUtility.SetDirty(obj);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string folder = Path.GetFileName(path);
        if (!string.IsNullOrWhiteSpace(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        if (!string.IsNullOrWhiteSpace(parent) && !string.IsNullOrWhiteSpace(folder) && !AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, folder);
    }

    private static string BuildToolDescriptionVi(ToolSpec spec)
    {
        if (spec.toolType == ToolType.WateringCan)
            return $"{spec.viName} cấp {spec.tier}. Tưới vùng {Mathf.RoundToInt(spec.areaX)}x{Mathf.RoundToInt(spec.areaY)} và chứa khoảng {Mathf.RoundToInt(spec.range)} lần dùng trước khi phải lấy nước lại.";

        if (spec.toolType == ToolType.Hoe)
            return $"{spec.viName} cấp {spec.tier}. Cuốc vùng {Mathf.RoundToInt(spec.areaX)}x{Mathf.RoundToInt(spec.areaY)} với chi phí {spec.staminaCost} thể lực mỗi lần.";

        return $"{spec.viName} cấp {spec.tier}. Gây {spec.attack:0.#} sát thương cơ bản với chi phí {spec.staminaCost} thể lực mỗi lần dùng.";
    }

    private static string BuildToolDescriptionEn(ToolSpec spec)
    {
        if (spec.toolType == ToolType.WateringCan)
            return $"{spec.enName} tier {spec.tier}. Waters a {Mathf.RoundToInt(spec.areaX)}x{Mathf.RoundToInt(spec.areaY)} area and holds about {Mathf.RoundToInt(spec.range)} charges before refilling.";

        if (spec.toolType == ToolType.Hoe)
            return $"{spec.enName} tier {spec.tier}. Tills a {Mathf.RoundToInt(spec.areaX)}x{Mathf.RoundToInt(spec.areaY)} area at {spec.staminaCost} stamina per swing.";

        return $"{spec.enName} tier {spec.tier}. Deals {spec.attack:0.#} base damage at a stamina cost of {spec.staminaCost} per use.";
    }

    private static string BuildSeedDescriptionVi(CropSpec spec)
    {
        int totalDays = GetInitialGrowthDays(spec);
        string harvestRule = spec.regrowable
            ? $"Sau vụ đầu, cây cho thu hoạch lại mỗi {spec.daysToReturnAfterHarvest} ngày."
            : "Thu hoạch một lần rồi cây biến mất.";
        return $"Hạt giống mùa {SeasonToVi(spec.season)}. Mất {totalDays} ngày để trưởng thành. {harvestRule} Nếu qua mùa mới cây sẽ héo.";
    }

    private static string BuildSeedDescriptionEn(CropSpec spec)
    {
        int totalDays = GetInitialGrowthDays(spec);
        string harvestRule = spec.regrowable
            ? $"After the first harvest, it produces again every {spec.daysToReturnAfterHarvest} days."
            : "Single harvest crop.";
        return $"{SeasonToEn(spec.season)} seed. Takes {totalDays} days to mature. {harvestRule} It withers when the season changes.";
    }

    private static string BuildCropItemDescriptionVi(CropSpec spec)
    {
        return $"{spec.viName} tươi. {spec.viUseHint}";
    }

    private static string BuildCropItemDescriptionEn(CropSpec spec)
    {
        return $"Fresh {spec.enName}. Useful for cooking, processing, or sale.";
    }

    private static string BuildWorldCropDescriptionVi(CropSpec spec)
    {
        return $"Cây {spec.viName} ngoài ruộng. Cần tưới nước hằng ngày để phát triển trong mùa {SeasonToVi(spec.season)}.";
    }

    private static string BuildWorldCropDescriptionEn(CropSpec spec)
    {
        return $"{spec.enName} growing in the field. Needs daily watering and thrives during {SeasonToEn(spec.season)}.";
    }

    private static int GetInitialGrowthDays(CropSpec spec)
    {
        int total = 0;
        for (int i = 0; i < spec.stageDays.Length; i++)
            total += Mathf.Max(0, spec.stageDays[i]);
        return total;
    }

    private static string SeasonToVi(Season season)
    {
        return season switch
        {
            Season.Spring => "Xuân",
            Season.Summer => "Hạ",
            Season.Fall => "Thu",
            Season.Winter => "Đông",
            _ => season.ToString()
        };
    }

    private static string SeasonToEn(Season season) => season.ToString();

    private static string BuildRecipeNameVi(string id)
    {
        return id switch
        {
            "recipe_furnace_copper_bar" => "Luyện Thỏi Đồng",
            "recipe_furnace_iron_bar" => "Luyện Thỏi Sắt",
            "recipe_furnace_gold_bar" => "Luyện Thỏi Vàng",
            "recipe_mill_corn_oil" => "Xay Dầu Ngô",
            "recipe_mayo_basic" => "Làm Mayonnaise",
            "recipe_cheese_basic" => "Ép Phô Mai",
            _ => id
        };
    }

    private static string BuildRecipeNameEn(string id)
    {
        return id switch
        {
            "recipe_furnace_copper_bar" => "Smelt Copper Bar",
            "recipe_furnace_iron_bar" => "Smelt Iron Bar",
            "recipe_furnace_gold_bar" => "Smelt Gold Bar",
            "recipe_mill_corn_oil" => "Mill Corn Oil",
            "recipe_mayo_basic" => "Make Mayonnaise",
            "recipe_cheese_basic" => "Press Cheese",
            _ => id
        };
    }

    private readonly struct ToolSpec
    {
        public readonly string baseId;
        public readonly string viName;
        public readonly string enName;
        public readonly ToolType toolType;
        public readonly int tier;
        public readonly float staminaCost;
        public readonly float areaX;
        public readonly float areaY;
        public readonly float cooldown;
        public readonly float range;
        public readonly int buyPrice;
        public readonly int sellPrice;
        public readonly float attack;

        public ToolSpec(string baseId, string viName, string enName, ToolType toolType, int tier, float staminaCost, float areaX, float areaY, float cooldown, float range, int buyPrice, int sellPrice, float attack = 0f)
        {
            this.baseId = baseId;
            this.viName = viName;
            this.enName = enName;
            this.toolType = toolType;
            this.tier = tier;
            this.staminaCost = staminaCost;
            this.areaX = areaX;
            this.areaY = areaY;
            this.cooldown = cooldown;
            this.range = range;
            this.buyPrice = buyPrice;
            this.sellPrice = sellPrice;
            this.attack = attack;
        }
    }

    private readonly struct CropSpec
    {
        public readonly string id;
        public readonly Season season;
        public readonly string viName;
        public readonly string enName;
        public readonly int[] stageDays;
        public readonly bool regrowable;
        public readonly int daysToReturnAfterHarvest;
        public readonly int loopToStageIndex;
        public readonly bool destroyOnHarvest;
        public readonly int seedBuyPrice;
        public readonly int cropSellPrice;
        public readonly int minDrop;
        public readonly int maxDrop;
        public readonly string viUseHint;

        public CropSpec(string id, Season season, string viName, string enName, int[] stageDays, bool regrowable, int daysToReturnAfterHarvest, int loopToStageIndex, bool destroyOnHarvest, int seedBuyPrice, int cropSellPrice, int minDrop, int maxDrop, string viUseHint)
        {
            this.id = id;
            this.season = season;
            this.viName = viName;
            this.enName = enName;
            this.stageDays = stageDays;
            this.regrowable = regrowable;
            this.daysToReturnAfterHarvest = daysToReturnAfterHarvest;
            this.loopToStageIndex = loopToStageIndex;
            this.destroyOnHarvest = destroyOnHarvest;
            this.seedBuyPrice = seedBuyPrice;
            this.cropSellPrice = cropSellPrice;
            this.minDrop = minDrop;
            this.maxDrop = maxDrop;
            this.viUseHint = viUseHint;
        }
    }

    private readonly struct DropSpec
    {
        public readonly string itemId;
        public readonly int minAmount;
        public readonly int maxAmount;
        public readonly float chance;

        public DropSpec(string itemId, int minAmount, int maxAmount, float chance)
        {
            this.itemId = itemId;
            this.minAmount = minAmount;
            this.maxAmount = maxAmount;
            this.chance = chance;
        }
    }

    private readonly struct FoodSpec
    {
        public readonly string id;
        public readonly string viName;
        public readonly string enName;
        public readonly float restoreStamina;
        public readonly int buyPrice;
        public readonly int sellPrice;
        public readonly string viDesc;
        public readonly string enDesc;

        public FoodSpec(string id, string viName, string enName, float restoreStamina, int buyPrice, int sellPrice, string viDesc, string enDesc)
        {
            this.id = id;
            this.viName = viName;
            this.enName = enName;
            this.restoreStamina = restoreStamina;
            this.buyPrice = buyPrice;
            this.sellPrice = sellPrice;
            this.viDesc = viDesc;
            this.enDesc = enDesc;
        }
    }

    private readonly struct RecipeIngredientSpec
    {
        public readonly string itemId;
        public readonly int amount;

        public RecipeIngredientSpec(string itemId, int amount)
        {
            this.itemId = itemId;
            this.amount = amount;
        }
    }
}
#endif
