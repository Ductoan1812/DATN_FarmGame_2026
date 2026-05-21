using System;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    private WorldEntityService _worldService;
    private readonly SpawnSystem _spawnSystem;
    private readonly EventBus _eventBus;
    private TimeManager _timeManager;

    // Config
    private readonly ObjectType _playerPrefabId;
    private readonly string _playerEntityDataId;
    private readonly Vector2 _defaultPlayerPos;
    private readonly StarterLoadoutData _starterLoadout;

    public SaveLoadManager(
        EntityService entityService,
        EntityDataRegistry entityDataRegistry,
        WorldEntityService worldService,
        SpawnSystem spawnSystem,
        EventBus eventBus,
        ObjectType playerPrefabId = ObjectType.Player01,
        string playerEntityDataId = "player",
        Vector2 defaultPlayerPos = default,
        StarterLoadoutData starterLoadout = null)
    {
        _entityService = entityService;
        _entityDataRegistry = entityDataRegistry;
        _worldService = worldService;
        _spawnSystem = spawnSystem;
        _eventBus = eventBus;
        _playerPrefabId = playerPrefabId;
        _playerEntityDataId = playerEntityDataId;
        _defaultPlayerPos = defaultPlayerPos;
        _starterLoadout = starterLoadout;
    }

    /// <summary>Gọi sau khi TimeManager được tạo (trong GameManager.Start hoặc Awake).</summary>
    public void SetTimeManager(TimeManager tm) => _timeManager = tm;

    public void SetWorldService(WorldEntityService worldService) => _worldService = worldService;

    /// <summary>Public wrapper for testing — reloads system save (time + watered tiles).</summary>
    public void LoadSystemDataPublic() => LoadSystemData();

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
        GameManager.Instance?.ProgressionService?.EnsureInitialized(playerEntity);
        Debug.Log($"[SaveLoadManager] Created Player entity: {playerEntity.id}");

        // Spawn Player qua SpawnSystem event
        Vector2 spawnPosition = SceneSpawnResolver.Resolve(
            _starterLoadout != null ? _starterLoadout.startSpawnPointId : SceneSpawnResolver.DefaultPlayerSpawnPointId,
            _defaultPlayerPos);

        _eventBus.Publish(new SpawnRequestPublish(
            spawnPosition,
            _playerPrefabId,
            playerEntity,
            payload: new SceneSpawnPayload { savePolicy = SceneEntitySavePolicy.Temporary }
        ));

        StarterLoadoutService.Apply(_entityService, _eventBus, playerEntity, _starterLoadout);
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
        EnsureLoadedPlayerProgression();

        // Phase 2: Load SpatialEntityRegistry + TileRegistry dirty + Spawn GameObjects
        // → EntityRoot.Add → inv.Container được set
        _worldService.Load(GetCurrentWorldSaveFile(), ep =>
        {
            var runtime = _entityService.Get(ep.idRuntime);
            _spawnSystem.ReinstantiateFromSave(ep, runtime);
        });

        EnsurePlayerSpawnedAt();
    }

    // ══════════════════════════════════════
    //  SAVE
    // ══════════════════════════════════════

    public void SaveAll()
    {
        _entityService.SaveData(EntitiesSaveFile, true);
        _worldService.Save(GetCurrentWorldSaveFile());
        SaveSystemData();
        Debug.Log("[SaveLoadManager] All data saved.");
    }

    public void LoadCurrentSceneWorld()
    {
        if (_worldService == null || _spawnSystem == null) return;
        string worldFile = GetCurrentWorldSaveFile();
        string path = System.IO.Path.Combine(Application.persistentDataPath, worldFile);
        if (!System.IO.File.Exists(path))
        {
            Debug.Log($"[SaveLoadManager] No scene world save for '{SceneManager.GetActiveScene().name}'. Marker scanner can seed defaults.");
            return;
        }

        _worldService.Load(worldFile, ep =>
        {
            var runtime = _entityService.Get(ep.idRuntime);
            _spawnSystem.ReinstantiateFromSave(ep, runtime);
        });
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

        var tracker = GameManager.Instance?.WateredTileTracker;
        if (tracker != null)
            data.wateredCells = tracker.ExportWateredCells();

        var soil = GameManager.Instance?.SoilQualityTracker;
        if (soil != null)
            data.soilCells = soil.ExportSoilCells();

        var clearZones = GameManager.Instance?.ClearZoneTracker;
        if (clearZones != null)
            data.clearZones = clearZones.ExportClearZones();

        var weather = GameManager.Instance?.WeatherSystem;
        if (weather != null)
            data.currentWeather = weather.CurrentWeather;

        var json = JsonUtility.ToJson(data, true);
        var path = System.IO.Path.Combine(Application.persistentDataPath, SystemSaveFile);
        System.IO.File.WriteAllText(path, json);
        Debug.Log($"[SaveLoadManager] System data saved: {data.time}, watered={data.wateredCells?.Count ?? 0}, soil={data.soilCells?.Count ?? 0}, clearZones={data.clearZones?.Count ?? 0}");
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

        var tracker = GameManager.Instance?.WateredTileTracker;
        if (tracker != null)
            tracker.ImportWateredCells(data.wateredCells);

        var soil = GameManager.Instance?.SoilQualityTracker;
        if (soil != null)
            soil.ImportSoilCells(data.soilCells);

        var clearZones = GameManager.Instance?.ClearZoneTracker;
        if (clearZones != null)
            clearZones.ImportClearZones(data.clearZones);

        var weather = GameManager.Instance?.WeatherSystem;
        if (weather != null)
            weather.SetWeather(data.currentWeather);

        Debug.Log($"[SaveLoadManager] System data loaded: {data.time}, watered={data.wateredCells?.Count ?? 0}, soil={data.soilCells?.Count ?? 0}, clearZones={data.clearZones?.Count ?? 0}, weather={data.currentWeather}");
    }

    // ══════════════════════════════════════
    //  UTILS
    // ══════════════════════════════════════

    private bool HasSaveFile()
    {
        var path = System.IO.Path.Combine(Application.persistentDataPath, EntitiesSaveFile);
        return System.IO.File.Exists(path);
    }

    private static string GetCurrentWorldSaveFile()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (string.IsNullOrWhiteSpace(sceneName))
            return WorldSaveFile;

        foreach (var invalid in System.IO.Path.GetInvalidFileNameChars())
            sceneName = sceneName.Replace(invalid, '_');

        return $"world_{sceneName}.json";
    }

    private void EnsureLoadedPlayerProgression()
    {
        var progressionService = GameManager.Instance?.ProgressionService;
        if (progressionService == null) return;

        foreach (var entity in _entityService.GetAll())
        {
            if (entity?.entityData == null) continue;
            if (entity.entityData.id == _playerEntityDataId)
                progressionService.EnsureInitialized(entity);
        }
    }

    public void EnsurePlayerSpawnedAt(string spawnPointId = null)
    {
        if (UnityEngine.Object.FindAnyObjectByType<PlayerControler>() != null)
            return;

        var playerEntity = FindPlayerEntity();
        if (playerEntity == null)
        {
            var playerData = _entityDataRegistry.Find(_playerEntityDataId);
            if (playerData == null)
            {
                Debug.LogError($"[SaveLoadManager] Cannot spawn player: EntityData '{_playerEntityDataId}' not found.");
                return;
            }

            playerEntity = _entityService.Create(playerData);
        }

        GameManager.Instance?.ProgressionService?.EnsureInitialized(playerEntity);
        string resolvedSpawnPointId = string.IsNullOrWhiteSpace(spawnPointId)
            ? SceneSpawnResolver.DefaultPlayerSpawnPointId
            : spawnPointId.Trim();
        Vector2 spawnPosition = SceneSpawnResolver.Resolve(resolvedSpawnPointId, _defaultPlayerPos);

        _eventBus.Publish(new SpawnRequestPublish(
            spawnPosition,
            _playerPrefabId,
            playerEntity,
            payload: new SceneSpawnPayload { savePolicy = SceneEntitySavePolicy.Temporary }));

        Debug.Log($"[SaveLoadManager] Player spawned at '{resolvedSpawnPointId}' position {spawnPosition}.");
    }

    private EntityRuntime FindPlayerEntity()
    {
        foreach (var entity in _entityService.GetAll())
        {
            if (entity?.entityData == null) continue;
            if (entity.entityData.id == _playerEntityDataId)
                return entity;
        }

        return null;
    }
}
