using System;
using UnityEngine;

/// <summary>
/// Orchestrator cho boot sequence:
///   Phase 1: Load EntityRegistry → publish DataReadyPublish
///   Phase 2: Spawn GameObjects → publish WorldObjectsSpawnedPublish (SpawnSystem)
///   Phase 3: Restore inventories → publish InventoryDataRestoredPublish
///   Phase 4: PlayerBridge bind → publish PlayerReadyPublish
///   Phase 5: GameManager publish GameReadyPublish
///
/// New Game: không có save file → tạo Player entity mới + spawn tại vị trí mặc định.
///
/// Save format:
///   - entities: chỉ entity đang tồn tại (bỏ ô trống)
///   - tileChanges: chỉ tile thay đổi so với baseline (dirty)
/// </summary>
public class SaveLoadManager
{
    // ── File names ────────────────────────────────────────
    public const string EntitiesSaveFile = "entities_save.json";
    public const string WorldSaveFile    = "world_save.json";
    public const string SystemSaveFile   = "system_save.json";

    private readonly EntityService _entityService;
    private readonly EntityDataRegistry _entityDataRegistry;
    private readonly WorldEntityService _worldService;
    private readonly SpawnSystem _spawnSystem;
    private readonly EventBus _eventBus;
    private TimeManager _timeManager;

    // Config
    private readonly ObjectType _playerPrefabId;
    private readonly string _playerEntityDataId;
    private readonly Vector2 _defaultPlayerPos;

    public SaveLoadManager(
        EntityService entityService,
        EntityDataRegistry entityDataRegistry,
        WorldEntityService worldService,
        SpawnSystem spawnSystem,
        EventBus eventBus,
        ObjectType playerPrefabId = ObjectType.Player01,
        string playerEntityDataId = "player",
        Vector2 defaultPlayerPos = default)
    {
        _entityService = entityService;
        _entityDataRegistry = entityDataRegistry;
        _worldService = worldService;
        _spawnSystem = spawnSystem;
        _eventBus = eventBus;
        _playerPrefabId = playerPrefabId;
        _playerEntityDataId = playerEntityDataId;
        _defaultPlayerPos = defaultPlayerPos;
    }

    /// <summary>Gọi sau khi TimeManager được tạo (trong GameManager.Start hoặc Awake).</summary>
    public void SetTimeManager(TimeManager tm) => _timeManager = tm;

    // ══════════════════════════════════════
    //  BOOT (gọi 1 lần khi game start)
    // ══════════════════════════════════════

    public void Boot()
    {
        bool hasSave = HasSaveFile();

        if (hasSave)
        {
            Debug.Log("[SaveLoadManager] Save detected → Loading...");
            LoadAll();
        }
        else
        {
            Debug.Log("[SaveLoadManager] No save → New Game");
            NewGame();
        }

        // Phase 1: Entity data loaded
        _eventBus.Publish(new DataReadyPublish());
        Debug.Log("[SaveLoadManager] DataReadyPublish published.");

        // Phase 2: World objects spawned (đã spawn trong LoadAll/NewGame)
        _eventBus.Publish(new WorldObjectsSpawnedPublish());
        Debug.Log("[SaveLoadManager] WorldObjectsSpawnedPublish published.");

        // Phase 3: Restore inventories (chỉ khi load save)
        if (hasSave)
        {
            _entityService.RestoreAllInventories();
            LoadSystemData();
        }

        _eventBus.Publish(new InventoryDataRestoredPublish());
        Debug.Log("[SaveLoadManager] InventoryDataRestoredPublish published.");
    }

    // ══════════════════════════════════════
    //  NEW GAME
    // ══════════════════════════════════════

    private void NewGame()
    {
        // Tạo Player EntityRuntime
        var playerData = _entityDataRegistry.Find(_playerEntityDataId);
        if (playerData == null)
        {
            Debug.LogError($"[SaveLoadManager] EntityData '{_playerEntityDataId}' not found! Cannot create Player.");
            return;
        }

        var playerEntity = _entityService.Create(playerData);
        Debug.Log($"[SaveLoadManager] Created Player entity: {playerEntity.id}");

        // Spawn Player qua SpawnSystem event
        _eventBus.Publish(new SpawnRequestPublish(
            _defaultPlayerPos,
            _playerPrefabId,
            playerEntity
        ));
    }

    // ══════════════════════════════════════
    //  LOAD
    // ══════════════════════════════════════

    private void LoadAll()
    {
        // Phase 1: Load tất cả EntityRuntime vào EntityRegistry (chưa RestoreSlots)
        _entityService.LoadData(
            id => _entityDataRegistry.FindById(id),
            EntitiesSaveFile,
            true
        );

        // Phase 2: Load SpatialEntityRegistry + TileRegistry dirty + Spawn GameObjects
        // → EntityRoot.Add → inv.Container được set
        _worldService.Load(WorldSaveFile, ep =>
        {
            var runtime = _entityService.Get(ep.idRuntime);
            _spawnSystem.ReinstantiateFromSave(ep, runtime);
        });
    }

    // ══════════════════════════════════════
    //  SAVE
    // ══════════════════════════════════════

    public void SaveAll()
    {
        _entityService.SaveData(EntitiesSaveFile, true);
        _worldService.Save(WorldSaveFile);
        SaveSystemData();
        Debug.Log("[SaveLoadManager] All data saved.");
    }

    // ══════════════════════════════════════
    //  UTILS
    // ══════════════════════════════════════

    // ══════════════════════════════════════
    //  SYSTEM SAVE / LOAD
    // ══════════════════════════════════════

    private void SaveSystemData()
    {
        var data = new SystemSaveData();
        if (_timeManager != null)
            data.time = _timeManager.GetSaveState();

        var json = JsonUtility.ToJson(data, true);
        var path = System.IO.Path.Combine(Application.persistentDataPath, SystemSaveFile);
        System.IO.File.WriteAllText(path, json);
        Debug.Log($"[SaveLoadManager] System data saved: {data.time}");
    }

    private void LoadSystemData()
    {
        var path = System.IO.Path.Combine(Application.persistentDataPath, SystemSaveFile);
        if (!System.IO.File.Exists(path)) return;

        var json = System.IO.File.ReadAllText(path);
        var data = JsonUtility.FromJson<SystemSaveData>(json);
        if (data == null) return;

        if (_timeManager != null)
            _timeManager.ApplySaveState(data.time);

        Debug.Log($"[SaveLoadManager] System data loaded: {data.time}");
    }

    // ══════════════════════════════════════
    //  UTILS
    // ══════════════════════════════════════

    private bool HasSaveFile()
    {
        var path = System.IO.Path.Combine(Application.persistentDataPath, EntitiesSaveFile);
        return System.IO.File.Exists(path);
    }
}
