using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class SetupTownShopRoles
{
    private const string TownScenePath = "Assets/Project/Scenes/Coreplay/TownScene.unity";
    private const string NpcFolder = "Assets/Project/ScriptableObjects/Characters/NPCs";
    private const string MarkerFolder = "Assets/Project/ScriptableObjects/SceneMarkers/MVP";
    private const string ShopNpcPath = NpcFolder + "/NPC_Banhang.asset";
    private const string CraftNpcPath = NpcFolder + "/NPC_Chetao.asset";
    private const string AnimalNpcPath = NpcFolder + "/NPC_ChanNuoi.asset";
    private const string AnimalMarkerPath = MarkerFolder + "/Marker_NPC_AnimalShop.asset";

    [MenuItem("Tools/DATN/One-off Setup/Bootstraps/Setup Town Shop Roles")]
    public static void Execute()
    {
        var cropShop = AssetDatabase.LoadAssetAtPath<EntityData>(ShopNpcPath);
        var toolShop = AssetDatabase.LoadAssetAtPath<EntityData>(CraftNpcPath);
        var animalShop = LoadOrCreateAnimalNpc();

        ConfigureCropShop(cropShop);
        ConfigureToolAndMechanicShop(toolShop);
        ConfigureAnimalShop(animalShop);
        EnsureAnimalShopMarker(animalShop);
        StampTownAnimalShopMarker();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SetupTownShopRoles] Town shop roles configured: crop, tool/mechanic, animal.");
    }

    public static void Verify()
    {
        var animalNpc = AssetDatabase.LoadAssetAtPath<EntityData>(AnimalNpcPath);
        var animalMarker = AssetDatabase.LoadAssetAtPath<SceneSpawnTile>(AnimalMarkerPath);
        var markerMapObject = GameObject.Find(SceneContext.RuntimeMarkersTilemapName);
        var markerMap = markerMapObject != null ? markerMapObject.GetComponent<Tilemap>() : null;
        var tile = markerMap != null ? markerMap.GetTile(new Vector3Int(3, 5, 0)) : null;

        bool ok = animalNpc != null && animalMarker != null && tile == animalMarker;
        if (ok)
            Debug.Log("[TEST PASS] Animal shop NPC asset and TownScene marker are configured.");
        else
            Debug.LogError($"[TEST FAIL] Animal shop setup incomplete. npc={animalNpc != null}, marker={animalMarker != null}, tileSet={tile == animalMarker}.");
    }

    public static void VerifyRuntime()
    {
        bool foundAnimal = false;
        foreach (var root in Object.FindObjectsByType<EntityRoot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            var entity = root.GetEntity();
            if (entity?.entityData == null)
                continue;

            Debug.Log($"[TEST INFO] Runtime entity {root.name}: {entity.entityData.id}");
            if (entity.entityData.id == "npc_channuoi")
                foundAnimal = true;
        }

        if (foundAnimal)
            Debug.Log("[TEST PASS] Runtime animal shop NPC is spawned.");
        else
            Debug.LogError("[TEST FAIL] Runtime animal shop NPC not found.");
    }

    private static void ConfigureCropShop(EntityData npc)
    {
        if (npc == null) return;

        SetMerchantDefaults(npc, "npc_banhang_name", "npc_banhang_desc");
        var shop = EnsureModule<ShopModule>(npc);
        ConfigureInfiniteShop(shop, buysAllItems: false);

        shop.initialStock.Clear();
        foreach (var seed in LoadAll("Assets/Project/ScriptableObjects/Items/Seeds"))
            AddStock(shop, seed, 10, 1);
        AddStock(shop, Load("Assets/Project/ScriptableObjects/Items/Tools/MVP/Fertilizer_T1.asset"), 10, 1);

        shop.buyWhitelist.Clear();
        foreach (var crop in LoadAll("Assets/Project/ScriptableObjects/Items/Crops"))
            AddWhitelist(shop, crop);
        AddWhitelist(shop, Load("Assets/Project/ScriptableObjects/Items/Materials/Forage_01.asset"));

        EditorUtility.SetDirty(npc);
    }

    private static void ConfigureToolAndMechanicShop(EntityData npc)
    {
        if (npc == null) return;

        SetMerchantDefaults(npc, "npc_chetao_name", "npc_chetao_desc");
        EnsureModule<CraftingModule>(npc);
        var shop = EnsureModule<ShopModule>(npc);
        ConfigureInfiniteShop(shop, buysAllItems: false);

        shop.initialStock.Clear();
        AddStock(shop, Load("Assets/Project/ScriptableObjects/Items/Tools/Hoe_01.asset"), 1, 1);
        AddStock(shop, Load("Assets/Project/ScriptableObjects/Items/Tools/Axe_01.asset"), 1, 1);
        AddStock(shop, Load("Assets/Project/ScriptableObjects/Items/Tools/Pickaxe_01.asset"), 1, 1);
        AddStock(shop, Load("Assets/Project/ScriptableObjects/Items/Tools/Scythe_01.asset"), 1, 1);
        AddStock(shop, Load("Assets/Project/ScriptableObjects/Items/Tools/WateringCan_01.asset"), 1, 1);
        AddStock(shop, Load("Assets/Project/ScriptableObjects/Items/Weapons/Sword_T1.asset"), 1, 1);
        AddStock(shop, Load("Assets/Project/ScriptableObjects/Items/Weapons/Spear_T1.asset"), 1, 1);
        AddStock(shop, Load("Assets/Project/ScriptableObjects/Items/Tools/MVP/Sprinkler_T1.asset"), 2, 5);
        AddStock(shop, Load("Assets/Project/ScriptableObjects/Items/Tools/MVP/Sprinkler_T2.asset"), 1, 15);
        AddStock(shop, Load("Assets/Project/ScriptableObjects/Items/Materials/Coal_01.asset"), 10, 1);
        AddStock(shop, Load("Assets/Project/ScriptableObjects/Items/Materials/Ore_T1_Copper.asset"), 8, 5);
        AddStock(shop, Load("Assets/Project/ScriptableObjects/Items/Materials/Wood_01.asset"), 20, 1);
        AddStock(shop, Load("Assets/Project/ScriptableObjects/Items/Materials/Stone_01.asset"), 20, 1);

        shop.buyWhitelist.Clear();
        foreach (var material in LoadAll("Assets/Project/ScriptableObjects/Items/Materials"))
            AddWhitelist(shop, material);
        foreach (var tool in LoadAll("Assets/Project/ScriptableObjects/Items/Tools"))
            AddWhitelist(shop, tool);
        foreach (var weapon in LoadAll("Assets/Project/ScriptableObjects/Items/Weapons"))
            AddWhitelist(shop, weapon);

        EditorUtility.SetDirty(npc);
    }

    private static void ConfigureAnimalShop(EntityData npc)
    {
        if (npc == null) return;

        SetMerchantDefaults(npc, "npc_channuoi_name", "npc_channuoi_desc");
        var shop = EnsureModule<ShopModule>(npc);
        ConfigureInfiniteShop(shop, buysAllItems: false);

        shop.initialStock.Clear();
        AddStock(shop, Load("Assets/Project/ScriptableObjects/WorldObjects/Animals/Animal_Chicken_01.asset"), 1, 1);
        AddStock(shop, Load("Assets/Project/ScriptableObjects/WorldObjects/Animals/Animal_Cow_01.asset"), 1, 8);
        AddStock(shop, Load("Assets/Project/ScriptableObjects/WorldObjects/Animals/Animal_Sheep_01.asset"), 1, 12);
        AddStock(shop, Load("Assets/Project/ScriptableObjects/Items/Animal/AnimalFeed.asset"), 20, 1);

        shop.buyWhitelist.Clear();
        AddWhitelist(shop, Load("Assets/Project/ScriptableObjects/Items/Animal/Egg.asset"));
        AddWhitelist(shop, Load("Assets/Project/ScriptableObjects/Items/Animal/Milk.asset"));
        AddWhitelist(shop, Load("Assets/Project/ScriptableObjects/Items/Animal/Wool.asset"));

        EnsureBuyPrice(Load("Assets/Project/ScriptableObjects/WorldObjects/Animals/Animal_Chicken_01.asset"), 350);
        EnsureBuyPrice(Load("Assets/Project/ScriptableObjects/WorldObjects/Animals/Animal_Cow_01.asset"), 1600);
        EnsureBuyPrice(Load("Assets/Project/ScriptableObjects/WorldObjects/Animals/Animal_Sheep_01.asset"), 1200);

        EditorUtility.SetDirty(npc);
    }

    private static EntityData LoadOrCreateAnimalNpc()
    {
        var npc = AssetDatabase.LoadAssetAtPath<EntityData>(AnimalNpcPath);
        if (npc != null)
            return npc;

        var template = AssetDatabase.LoadAssetAtPath<EntityData>(ShopNpcPath);
        npc = ScriptableObject.CreateInstance<EntityData>();
        npc.id = "npc_channuoi";
        npc.keyName = "npc_channuoi_name";
        npc.descKey = "npc_channuoi_desc";
        npc.icon = template != null ? template.icon : null;
        npc.category = template != null ? template.category : default;
        npc.maxStack = 1;
        npc.buyPrice = 0;
        npc.sellPrice = 0;
        npc.baseStats = new StatsData();
        npc.modules = new List<IModuleData>();
        AssetDatabase.CreateAsset(npc, AnimalNpcPath);

        var dialogue = template?.modules?.OfType<DialogueModule>().FirstOrDefault();
        if (dialogue != null)
        {
            var clone = new DialogueModule
            {
                graph = dialogue.graph,
                optionTextKey = dialogue.optionTextKey,
                priority = dialogue.priority
            };
            npc.modules.Add(clone);
        }

        return npc;
    }

    private static void EnsureAnimalShopMarker(EntityData animalShop)
    {
        var marker = AssetDatabase.LoadAssetAtPath<SceneSpawnTile>(AnimalMarkerPath);
        if (marker == null)
        {
            marker = ScriptableObject.CreateInstance<SceneSpawnTile>();
            AssetDatabase.CreateAsset(marker, AnimalMarkerPath);
        }

        marker.name = "Marker_NPC_AnimalShop";
        marker.markerKind = SceneMarkerKind.Npc;
        marker.objectType = ObjectType.NPCShop01;
        marker.entityData = animalShop;
        marker.savePolicy = SceneEntitySavePolicy.Persistent;
        marker.spawnGroupId = "town_animal_shop";
        marker.respawnMinutes = 0;
        marker.initialAmount = 1;
        marker.bypassPlacementValidation = true;
        marker.editorColor = new Color(1f, 0.72f, 0.28f, 0.85f);
        EditorUtility.SetDirty(marker);
    }

    private static void StampTownAnimalShopMarker()
    {
        var scene = EditorSceneManager.OpenScene(TownScenePath, OpenSceneMode.Single);
        var markerMapObject = GameObject.Find(SceneContext.RuntimeMarkersTilemapName);
        var markerMap = markerMapObject != null ? markerMapObject.GetComponent<Tilemap>() : null;
        if (markerMap == null)
        {
            Debug.LogWarning("[SetupTownShopRoles] RuntimeMarkerTilemap not found; animal shop marker asset was still created.");
            return;
        }

        var marker = AssetDatabase.LoadAssetAtPath<SceneSpawnTile>(AnimalMarkerPath);
        markerMap.SetTile(new Vector3Int(3, 5, 0), marker);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigureInfiniteShop(ShopModule shop, bool buysAllItems)
    {
        shop.optionTextKey = "ui.shop.open";
        shop.priority = 30;
        shop.sellsToPlayer = true;
        shop.buysFromPlayer = true;
        shop.buysAllItems = buysAllItems;
        shop.infiniteStock = true;
        shop.stockInventoryType = InventoryType.Backpack;
        shop.initialStock ??= new List<ShopStockEntry>();
        shop.buyWhitelist ??= new List<EntityData>();
    }

    private static void SetMerchantDefaults(EntityData npc, string keyName, string descKey)
    {
        npc.keyName = keyName;
        npc.descKey = descKey;
        npc.baseStats ??= new StatsData();
        npc.baseStats.baseStats ??= new List<StatEntry>();
        SetOrAddStat(npc.baseStats.baseStats, StatType.Money, 999999f);
        EnsureModule<InventoryModule>(npc).size = 80;
    }

    private static void AddStock(ShopModule shop, EntityData item, int amount, int requiredLevel)
    {
        if (shop == null || item == null)
            return;

        if (item.buyPrice < 0)
            item.buyPrice = Mathf.Max(1, item.sellPrice * 2);

        shop.initialStock.Add(new ShopStockEntry
        {
            itemData = item,
            amount = Mathf.Max(1, amount),
            requiredLevel = Mathf.Max(1, requiredLevel),
            unlockRequirement = new UnlockRequirementData { requiredLevel = Mathf.Max(1, requiredLevel) }
        });
        EditorUtility.SetDirty(item);
    }

    private static void AddWhitelist(ShopModule shop, EntityData item)
    {
        if (shop == null || item == null || shop.buyWhitelist.Contains(item))
            return;

        shop.buyWhitelist.Add(item);
    }

    private static void EnsureBuyPrice(EntityData item, int buyPrice)
    {
        if (item == null)
            return;

        if (item.buyPrice < 0)
            item.buyPrice = buyPrice;
        EditorUtility.SetDirty(item);
    }

    private static EntityData Load(string path) => AssetDatabase.LoadAssetAtPath<EntityData>(path);

    private static List<EntityData> LoadAll(string folder)
    {
        return AssetDatabase.FindAssets("t:EntityData", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<EntityData>)
            .Where(data => data != null)
            .ToList();
    }

    private static T EnsureModule<T>(EntityData data) where T : IModuleData, new()
    {
        data.modules ??= new List<IModuleData>();
        var module = data.modules.OfType<T>().FirstOrDefault();
        if (module != null)
            return module;

        module = new T();
        data.modules.Add(module);
        return module;
    }

    private static void SetOrAddStat(List<StatEntry> stats, StatType type, float value)
    {
        for (int i = 0; i < stats.Count; i++)
        {
            if (stats[i].statType != type)
                continue;

            stats[i] = new StatEntry { statType = type, value = value };
            return;
        }

        stats.Add(new StatEntry { statType = type, value = value });
    }
}
