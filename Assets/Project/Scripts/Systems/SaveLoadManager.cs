using System;
using System.IO;
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
    public const string SaveRootFolder   = "Saves";
    public const int DefaultSlotCount    = 8;

    private const string ActiveSlotPref = "save_active_slot";
    private static int activeSlotIndex;

    [Serializable]
    public class SaveSlotSummary
    {
        public int slotIndex;
        public bool hasSave;
        public string displayName;
        public string detailText;
        public string lastActiveSceneName;
        public TimeState time;
        public DateTime lastWriteTime;
    }

    private readonly EntityService _entityService;
    private readonly EntityDataRegistry _entityDataRegistry;
    private WorldEntityService _worldService;
    private readonly SpawnSystem _spawnSystem;
    private readonly EventBus _eventBus;
    private TimeManager _timeManager;

    public bool IsBootingFromSave { get; private set; }

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
            var systemData = ReadSystemSave();
            if (systemData != null && !string.IsNullOrEmpty(systemData.lastActiveSceneName))
            {
                string currentScene = SceneManager.GetActiveScene().name;
                if (!string.Equals(currentScene, systemData.lastActiveSceneName, StringComparison.Ordinal))
                {
                    Debug.Log($"[SaveLoadManager] Saved scene is '{systemData.lastActiveSceneName}', current is '{currentScene}'. Loading saved scene and setting IsBootingFromSave...");
                    IsBootingFromSave = true;
                    SceneManager.LoadScene(systemData.lastActiveSceneName);
                    return; // Let the next scene boot
                }
            }

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
            GetSaveFilePath(EntitiesSaveFile),
            true
        );
        EnsureLoadedPlayerProgression();

        // Phase 2: Load SpatialEntityRegistry + TileRegistry dirty + Spawn GameObjects
        // → EntityRoot.Add → inv.Container được set
        _worldService.Load(GetSaveFilePath(GetCurrentWorldSaveFile(), createDirectory: false), ep =>
        {
            var runtime = _entityService.Get(ep.idRuntime);
            _spawnSystem.ReinstantiateFromSave(ep, runtime);
        });

        var systemData = ReadSystemSave();
        if (systemData != null && systemData.hasSavedPlayerPosition)
        {
            Vector2 savedPos = new Vector2(systemData.playerPosX, systemData.playerPosY);
            SpawnPlayerAtPosition(savedPos);
        }
        else
        {
            EnsurePlayerSpawnedAt();
        }
    }

    // ══════════════════════════════════════
    //  SAVE
    // ══════════════════════════════════════

    public void SaveAll()
    {
        _entityService.SaveData(GetSaveFilePath(EntitiesSaveFile), true);
        _worldService.Save(GetSaveFilePath(GetCurrentWorldSaveFile()));
        SaveSystemData();
        Debug.Log($"[SaveLoadManager] All data saved to slot {ActiveSlotIndex}.");
    }

    public void LoadCurrentSceneWorld()
    {
        if (_worldService == null || _spawnSystem == null) return;
        string worldFile = GetCurrentWorldSaveFile();
        string path = GetSaveFilePath(worldFile, createDirectory: false);
        if (!File.Exists(path))
        {
            Debug.Log($"[SaveLoadManager] No scene world save for '{SceneManager.GetActiveScene().name}'. Marker scanner can seed defaults.");
            return;
        }

        _worldService.Load(path, ep =>
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

        var narrative = GameManager.Instance?.NarrativeService;
        if (narrative != null)
            data.triggeredStoryEventIds = narrative.ExportTriggered();

        var research = GameManager.Instance?.ResearchService;
        if (research != null)
            data.unlockedResearch = research.ExportUnlocked();

        // Save active scene and player position
        var player = FindBestPlayerController();
        if (player != null)
        {
            data.lastActiveSceneName = SceneManager.GetActiveScene().name;
            data.playerPosX = player.transform.position.x;
            data.playerPosY = player.transform.position.y;
            data.hasSavedPlayerPosition = true;
        }

        var json = JsonUtility.ToJson(data, true);
        var path = GetSaveFilePath(SystemSaveFile);
        File.WriteAllText(path, json);
        Debug.Log($"[SaveLoadManager] System data saved to slot {ActiveSlotIndex}: {data.time}, scene={data.lastActiveSceneName}, playerPos=({data.playerPosX},{data.playerPosY}), watered={data.wateredCells?.Count ?? 0}, soil={data.soilCells?.Count ?? 0}, clearZones={data.clearZones?.Count ?? 0}, narrative={data.triggeredStoryEventIds?.Count ?? 0}, research={data.unlockedResearch?.Count ?? 0}");
    }

    private void LoadSystemData()
    {
        var path = GetSaveFilePath(SystemSaveFile, createDirectory: false);
        if (!File.Exists(path)) return;

        var json = File.ReadAllText(path);
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

        var narrative = GameManager.Instance?.NarrativeService;
        if (narrative != null)
            narrative.ImportTriggered(data.triggeredStoryEventIds);

        var research = GameManager.Instance?.ResearchService;
        if (research != null)
            research.ImportUnlocked(data.unlockedResearch);

        Debug.Log($"[SaveLoadManager] System data loaded: {data.time}, watered={data.wateredCells?.Count ?? 0}, soil={data.soilCells?.Count ?? 0}, clearZones={data.clearZones?.Count ?? 0}, weather={data.currentWeather}, narrative={data.triggeredStoryEventIds?.Count ?? 0}, research={data.unlockedResearch?.Count ?? 0}");
    }

    // ══════════════════════════════════════
    //  UTILS
    // ══════════════════════════════════════

    private bool HasSaveFile()
    {
        return HasAnySaveData(ActiveSlotIndex);
    }

    public static int ActiveSlotIndex
    {
        get
        {
            if (activeSlotIndex <= 0)
                activeSlotIndex = Mathf.Max(1, PlayerPrefs.GetInt(ActiveSlotPref, 1));

            return activeSlotIndex;
        }
    }

    public static void SelectSlot(int slotIndex)
    {
        activeSlotIndex = Mathf.Max(1, slotIndex);
        PlayerPrefs.SetInt(ActiveSlotPref, activeSlotIndex);
        PlayerPrefs.Save();
        Debug.Log($"[SaveLoadManager] Active save slot set to save{activeSlotIndex}.");
    }

    /// <summary>Public API: check if any save data exists.</summary>
    public static bool HasAnySaveData()
    {
        return HasAnySaveData(ActiveSlotIndex);
    }

    public static bool HasAnySaveData(int slotIndex)
    {
        var path = GetSaveFilePath(EntitiesSaveFile, slotIndex, createDirectory: false);
        return File.Exists(path);
    }

    /// <summary>Public API: delete all save files in the active slot. Logs warnings and does not throw.</summary>
    public static void DeleteAllSaveData()
    {
        DeleteAllSaveData(ActiveSlotIndex);
    }

    public static void DeleteAllSaveData(int slotIndex)
    {
        var dir = GetSlotDirectory(slotIndex);
        if (!Directory.Exists(dir))
            return;

        try
        {
            Directory.Delete(dir, recursive: true);
            Debug.Log($"[SaveLoadManager] Save data deleted for slot {slotIndex}: {dir}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SaveLoadManager] Failed to delete slot {slotIndex}: {ex.Message}");
        }
    }

    public static SaveSlotSummary[] GetSaveSlots(int count = DefaultSlotCount)
    {
        count = Mathf.Max(1, count);
        var result = new SaveSlotSummary[count];
        for (int i = 0; i < count; i++)
            result[i] = BuildSlotSummary(i + 1);

        return result;
    }

    public static SaveSlotSummary BuildSlotSummary(int slotIndex)
    {
        slotIndex = Mathf.Max(1, slotIndex);
        var summary = new SaveSlotSummary
        {
            slotIndex = slotIndex,
            displayName = $"Bản Save {slotIndex}",
            detailText = "Chưa có dữ liệu lưu.",
            hasSave = HasAnySaveData(slotIndex),
            lastActiveSceneName = "FarmScene"
        };

        var systemData = ReadSystemSave(slotIndex);
        if (systemData != null)
        {
            summary.time = systemData.time;
            summary.lastActiveSceneName = string.IsNullOrWhiteSpace(systemData.lastActiveSceneName)
                ? "FarmScene"
                : systemData.lastActiveSceneName;
            summary.detailText = BuildSlotDetailText(systemData);
        }
        else if (summary.hasSave)
        {
            summary.detailText = "Đã có dữ liệu lưu.";
        }

        var entitiesPath = GetSaveFilePath(EntitiesSaveFile, slotIndex, createDirectory: false);
        var systemPath = GetSaveFilePath(SystemSaveFile, slotIndex, createDirectory: false);
        if (File.Exists(systemPath))
            summary.lastWriteTime = File.GetLastWriteTime(systemPath);
        else if (File.Exists(entitiesPath))
            summary.lastWriteTime = File.GetLastWriteTime(entitiesPath);

        return summary;
    }

    public static string GetSaveFilePath(string fileName, bool createDirectory = true)
    {
        return GetSaveFilePath(fileName, ActiveSlotIndex, createDirectory);
    }

    public static string GetSaveFilePath(string fileName, int slotIndex, bool createDirectory = true)
    {
        var dir = GetSlotDirectory(slotIndex);
        if (createDirectory)
            Directory.CreateDirectory(dir);

        return Path.Combine(dir, fileName);
    }

    public static string GetSlotDirectory(int slotIndex)
    {
        slotIndex = Mathf.Max(1, slotIndex);
        return Path.Combine(Application.persistentDataPath, SaveRootFolder, $"save{slotIndex}");
    }

    public static string GetSaveRootDirectory()
    {
        return Path.Combine(Application.persistentDataPath, SaveRootFolder);
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

    private static string BuildSlotDetailText(SystemSaveData data)
    {
        if (data == null)
            return "Đã có dữ liệu lưu.";

        string season = data.time.season switch
        {
            Season.Spring => "Xuân",
            Season.Summer => "Hạ",
            Season.Fall => "Thu",
            Season.Winter => "Đông",
            _ => data.time.season.ToString()
        };

        int day = Mathf.Max(1, data.time.day);
        int hour = Mathf.Clamp(data.time.hour, 0, 23);
        int minute = Mathf.Clamp(data.time.minute, 0, 59);
        string scene = string.IsNullOrWhiteSpace(data.lastActiveSceneName)
            ? "FarmScene"
            : data.lastActiveSceneName;

        return $"Ngày {day} - {season} - {hour:D2}:{minute:D2} - {scene}";
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
        var existingPlayer = FindBestPlayerController();
        if (existingPlayer != null && existingPlayer.gameObject.scene == SceneManager.GetActiveScene())
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

    private static PlayerControler FindBestPlayerController()
    {
        var players = UnityEngine.Object.FindObjectsByType<PlayerControler>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (players == null || players.Length == 0)
            return null;

        var activeScene = SceneManager.GetActiveScene();
        for (int i = 0; i < players.Length; i++)
        {
            var player = players[i];
            if (player != null && player.gameObject.scene == activeScene)
                return player;
        }

        return players[0];
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

    private SystemSaveData ReadSystemSave()
    {
        return ReadSystemSave(ActiveSlotIndex);
    }

    private static SystemSaveData ReadSystemSave(int slotIndex)
    {
        var path = GetSaveFilePath(SystemSaveFile, slotIndex, createDirectory: false);
        if (!File.Exists(path)) return null;
        try
        {
            var json = File.ReadAllText(path);
            return JsonUtility.FromJson<SystemSaveData>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveLoadManager] Failed to read system save: {ex.Message}");
            return null;
        }
    }

    private void SpawnPlayerAtPosition(Vector2 spawnPosition)
    {
        var existingPlayer = FindBestPlayerController();
        if (existingPlayer != null && existingPlayer.gameObject.scene == SceneManager.GetActiveScene())
        {
            existingPlayer.transform.position = spawnPosition;
            return;
        }

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

        _eventBus.Publish(new SpawnRequestPublish(
            spawnPosition,
            _playerPrefabId,
            playerEntity,
            payload: new SceneSpawnPayload { savePolicy = SceneEntitySavePolicy.Temporary }));

        Debug.Log($"[SaveLoadManager] Player spawned at saved position {spawnPosition}.");
    }

    public void CompleteBootFromSavedScene()
    {
        IsBootingFromSave = false;
        Debug.Log("[SaveLoadManager] Completing boot sequence from saved scene...");

        // Load tất cả entities và spawn player tại vị trí đã lưu
        LoadAll();

        // Phase 1: Entity data loaded
        _eventBus.Publish(new DataReadyPublish());

        // Phase 2: World objects spawned (đã spawn trong LoadAll)
        _eventBus.Publish(new WorldObjectsSpawnedPublish());

        // Phase 3: Restore inventories + system data (time, watered cells, weather, v.v.)
        _entityService.RestoreAllInventories();
        LoadSystemData();

        _eventBus.Publish(new InventoryDataRestoredPublish());
        Debug.Log("[SaveLoadManager] Boot sequence from saved scene complete.");
    }
}
