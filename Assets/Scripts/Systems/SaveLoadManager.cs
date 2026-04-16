using System;
using UnityEngine;

/// <summary>
/// Orchestrator cho boot sequence:
///   Phase 1: Load EntityRegistry (tất cả EntityRuntime)
///   Phase 2: Load SpatialEntityRegistry (positions, spatial) + TileRegistry dirty
///   Phase 3: Spawn GameObjects từ SpatialEntityRegistry (bao gồm Player)
///   Phase 4: Publish WorldReady → UI bind data
///
/// New Game: không có save file → tạo Player entity mới + spawn tại vị trí mặc định.
///
/// Save format mới:
///   - entities: chỉ entity đang tồn tại (bỏ ô trống)
///   - tileChanges: chỉ tile thay đổi so với baseline (dirty)
/// </summary>
public class SaveLoadManager
{
    // ── File names ────────────────────────────────────────
    public const string EntitiesSaveFile = "entities_save.json";
    public const string WorldSaveFile    = "world_save.json";

    private readonly EntityService _entityService;
    private readonly EntityDataRegistry _entityDataRegistry;
    private readonly WorldEntityService _worldService;
    private readonly SpawnSystem _spawnSystem;
    private readonly EventBus _eventBus;

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

        // Phase 4: Broadcast WorldReady
        _eventBus.Publish(new WorldReady());
        Debug.Log("[SaveLoadManager] WorldReady published.");
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
        Debug.Log($"[SaveLoadManager] Created Player entity: {playerEntity.Id}");

        // Spawn Player qua SpawnSystem event
        _eventBus.Publish(new SpawnRequest(
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

        // Phase 2+3: Load SpatialEntityRegistry + TileRegistry dirty + Spawn GameObjects
        // → EntityRoot.Add → inv.Container được set
        _worldService.Load(WorldSaveFile, ep =>
        {
            var runtime = _entityService.Get(ep.idRuntime);
            _spawnSystem.ReinstantiateFromSave(ep, runtime);
        });

        // Phase 4: Sau khi spawn xong, Container đã có → RestoreSlots → item.Owner đúng
        _entityService.RestoreAllInventories();
    }

    // ══════════════════════════════════════
    //  SAVE
    // ══════════════════════════════════════

    public void SaveAll()
    {
        _entityService.SaveData(EntitiesSaveFile, true);
        _worldService.Save(WorldSaveFile);
        Debug.Log("[SaveLoadManager] All data saved.");
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
