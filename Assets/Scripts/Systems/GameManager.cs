using UnityEngine;
using UnityEngine.Tilemaps;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ── Registries ────────────────────────────────────────
    public EntityRegistry          EntityRegistry     { get; private set; }
    public EntityDataRegistry      EntityDataRegistry { get; private set; }
    public WorldObjectRegistry     WorldObjects       { get; private set; }
    public SpatialEntityRegistry   SpatialRegistry    { get; private set; }
    public TileRegistry            TileRegistry       { get; private set; }

    // ── Services ──────────────────────────────────────────
    public EntityService        EntityService      { get; private set; }
    public InventoryService     InventoryService   { get; private set; }
    public WorldEntityService   WorldService       { get; private set; }
    public SpawnSystem          SpawnSystem        { get; private set; }
    public SaveLoadManager      SaveLoadManager    { get; private set; }

    // ── Shared ────────────────────────────────────────────
    public EventBus             EventBus           { get; private set; }

    [SerializeField] private TileData tileData;
    public TileData TileData => tileData;

    [Header("Tilemap References")]
    [SerializeField] private Tilemap tmGround;
    [SerializeField] private Tilemap tmGroundDetail;
    [SerializeField] private Tilemap tmCollision;
    [SerializeField] private Tilemap tmDecoration;
    [SerializeField] private Tilemap tmOverlay;

    [Header("Player Config")]
    [SerializeField] private ObjectType playerPrefabId = ObjectType.Player01;
    [SerializeField] private string playerEntityDataId = "player";
    [SerializeField] private Vector2 defaultPlayerPos = Vector2.zero;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Phase 1: Init tất cả systems
        InitEventBus();
        InitEntityDataRegistry();
        InitEntitySystem();
        InitWorldObjects();
        InitTileRegistry();
        InitWorldEntitySystem();
        InitSpawnSystem();
        InitSaveLoadManager();

        // Subscribe save/load events
        EventBus.Subscribe<SaveGameRequestPublish>(OnSaveRequest);
        EventBus.Subscribe<LoadGameRequestPublish>(OnLoadRequest);
    }

    private void Start()
    {
        // Phase 2-4: Boot sequence (load data → spawn objects → WorldReady)
        SaveLoadManager.Boot();
    }

    private void OnDestroy()
    {
        if (EventBus != null)
        {
            EventBus.Unsubscribe<SaveGameRequestPublish>(OnSaveRequest);
            EventBus.Unsubscribe<LoadGameRequestPublish>(OnLoadRequest);
        }
    }

    // ── Init methods ──────────────────────────────────────

    private void InitEventBus()
    {
        EventBus = FindAnyObjectByType<EventBus>();
        if (EventBus == null)
            EventBus = new GameObject("EventBus").AddComponent<EventBus>();
    }

    private void InitEntityDataRegistry()
    {
        EntityDataRegistry = new EntityDataRegistry();

#if UNITY_EDITOR
        var guids = UnityEditor.AssetDatabase.FindAssets("t:EntityData");
        foreach (var guid in guids)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var data = UnityEditor.AssetDatabase.LoadAssetAtPath<EntityData>(path);
            if (data != null) EntityDataRegistry.Register(data);
        }
#else
        var allData = Resources.LoadAll<EntityData>("");
        EntityDataRegistry.RegisterAll(allData);
#endif

        Debug.Log($"[GameManager] EntityDataRegistry loaded {EntityDataRegistry.Count} EntityData assets.");
    }

    private void InitEntitySystem()
    {
        EntityRegistry   = new EntityRegistry();
        EntityService    = new EntityService(EntityRegistry);
        InventoryService = new InventoryService(EntityService);
    }

    private void InitWorldObjects()
    {
        WorldObjects = new WorldObjectRegistry();
        WorldObjects.RegisterAll(Resources.LoadAll<WorldObjectDefinition>("Data/WorldObjects"));
    }

    private void InitTileRegistry()
    {
        TileRegistry = new TileRegistry();

        // Auto-find tilemaps nếu chưa gán trong Inspector
        if (tmGround == null || tmGroundDetail == null)
            AutoFindTilemaps();

        // Đăng ký tất cả tilemap
        if (tmGround       != null) TileRegistry.RegisterTilemap("Tm_Ground",       tmGround);
        if (tmGroundDetail != null) TileRegistry.RegisterTilemap("Tm_GroundDetail",  tmGroundDetail);
        if (tmCollision    != null) TileRegistry.RegisterTilemap("Tm_Collision",     tmCollision);
        if (tmDecoration   != null) TileRegistry.RegisterTilemap("Tm_Decoration",    tmDecoration);
        if (tmOverlay      != null) TileRegistry.RegisterTilemap("Tm_Overlay",       tmOverlay);

        // Quét baseline (snapshot gốc từ Editor)
        TileRegistry.ScanBaseline();
    }

    private void AutoFindTilemaps()
    {
        var grid = FindAnyObjectByType<Grid>();
        if (grid == null) return;

        foreach (var tm in grid.GetComponentsInChildren<Tilemap>())
        {
            switch (tm.gameObject.name)
            {
                case "Tm_Ground":       if (tmGround       == null) tmGround       = tm; break;
                case "Tm_GroundDetail": if (tmGroundDetail == null) tmGroundDetail = tm; break;
                case "Tm_Collision":    if (tmCollision    == null) tmCollision    = tm; break;
                case "Tm_Decoration":   if (tmDecoration   == null) tmDecoration   = tm; break;
                case "Tm_Overlay":      if (tmOverlay      == null) tmOverlay      = tm; break;
            }
        }
    }

    private void InitWorldEntitySystem()
    {
        var tilemap = GridSystem.GetTilemap();
        if (tilemap == null)
            Debug.LogWarning("[GameManager] Tilemap not found – WorldEntityService running headless.");

        SpatialRegistry = new SpatialEntityRegistry();
        WorldService    = new WorldEntityService(SpatialRegistry, TileRegistry, tileData, tilemap);
    }

    private void InitSpawnSystem()
    {
        SpawnSystem = gameObject.AddComponent<SpawnSystem>();
        SpawnSystem.Init(EventBus, WorldService, WorldObjects, EntityService);
    }

    private void InitSaveLoadManager()
    {
        SaveLoadManager = new SaveLoadManager(
            EntityService,
            EntityDataRegistry,
            WorldService,
            SpawnSystem,
            EventBus,
            playerPrefabId,
            playerEntityDataId,
            defaultPlayerPos
        );
    }

    // ── Event handlers ────────────────────────────────────

    private void OnSaveRequest(SaveGameRequestPublish req)
    {
        SaveLoadManager.SaveAll();
    }

    private void OnLoadRequest(LoadGameRequestPublish req)
    {
        // TODO: full reload (destroy all → re-boot)
        Debug.LogWarning("[GameManager] Runtime reload not yet implemented. Restart the game to load.");
    }
}
