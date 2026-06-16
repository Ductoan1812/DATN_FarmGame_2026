using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using DialogueGraphTool;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    private static GameObject persistentUIRoot;
    private static bool shuttingDownForMainMenu;

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
    public ProgressionService   ProgressionService { get; private set; }
    public CraftingService      CraftingService    { get; private set; }
    public SpawnSystem          SpawnSystem        { get; private set; }
    public SaveLoadManager      SaveLoadManager    { get; private set; }
    public WateredTileTracker   WateredTileTracker { get; private set; }
    public WeatherSystem        WeatherSystem      { get; private set; }
    public SprinklerRegistry    SprinklerRegistry  { get; private set; }
    public SoilQualityTracker   SoilQualityTracker { get; private set; }
    public ClearZoneTracker     ClearZoneTracker   { get; private set; }
    public NarrativeService     NarrativeService   { get; private set; }
    public ResearchService      ResearchService    { get; private set; }
    public DailyTracker         DailyTracker       { get; private set; }

    // ── Shared ────────────────────────────────────────────
    public EventBus             EventBus           { get; private set; }
    public TimeManager          TimeManager        { get; private set; }

    [SerializeField] private WeatherConfig weatherConfig;
    [SerializeField] private TileData tileData;
    public TileData TileData => tileData;
    public Tilemap TmGround => tmGround;

    [Header("Tilemap References")]
    [SerializeField] private Tilemap tmGround;
    [SerializeField] private Tilemap tmWatered;
    [SerializeField] private Tilemap tmGroundDetail;
    [SerializeField] private Tilemap tmCollision;
    [SerializeField] private Tilemap tmDecoration;
    [SerializeField] private Tilemap tmOverlay;

    [Header("Player Config")]
    [SerializeField] private ObjectType playerPrefabId = ObjectType.Player01;
    [SerializeField] private string playerEntityDataId = "player";
    [SerializeField] private Vector2 defaultPlayerPos = Vector2.zero;
    [SerializeField] private StarterLoadoutData starterLoadout;
    private bool isReloadingScene;
    private bool bootCompleted;
    private bool isRestoringScene;

    private void Awake()
    {
        shuttingDownForMainMenu = false;

        if (Instance != null && Instance != this)
        {
            DestroyDuplicateRuntimeRoot();
            return;
        }

        Instance = this;
        DontDestroyOnLoad(transform.root.gameObject);
        EnsurePersistentUIRoot();
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Phase 1: Init tất cả systems
        InitEventBus();
        InitEntityDataRegistry();
        InitEntitySystem();
        InitWorldObjects();
        InitTileRegistry();
        InitWorldEntitySystem();
        InitClearZoneTracker();
        InitSpawnSystem();
        InitTimeManager();
        InitWateredTileTracker();
        InitWeatherSystem();
        InitSprinklerRegistry();
        InitSoilQualityTracker();
        InitNarrativeService();
        InitResearchService();
        InitDialogueGameplayBridge();
        InitSceneTransitionBridge();
        InitInteractionPreviewBridge();
        InitPlayerDeathHandler();
        InitCombatProgressionPolish();
        InitLoadingScreenUI();
        InitNarrativeUI();
        InitDailyTracker();
        InitEndOfDaySummaryUI();
        InitPickupNotificationUI();
        InitSaveLoadManager();

        // Subscribe save/load events
        EventBus.Subscribe<SaveGameRequestPublish>(OnSaveRequest);
        EventBus.Subscribe<LoadGameRequestPublish>(OnLoadRequest);
        EventBus.Subscribe<PlayerReadyPublish>(OnPlayerReady);
        EventBus.Subscribe<StoryEventUnlockedPublish>(OnStoryEventUnlocked);
    }

    private void Start()
    {
        // Boot sequence: DataReady → WorldObjectsSpawned → InventoryDataRestored → PlayerReady → GameReady
        SaveLoadManager.Boot();
        bootCompleted = true;
    }

    private void OnDestroy()
    {
        bool wasInstance = Instance == this;

        if (wasInstance)
            SceneManager.sceneLoaded -= OnSceneLoaded;

        if (EventBus != null)
        {
            EventBus.Unsubscribe<SaveGameRequestPublish>(OnSaveRequest);
            EventBus.Unsubscribe<LoadGameRequestPublish>(OnLoadRequest);
            EventBus.Unsubscribe<PlayerReadyPublish>(OnPlayerReady);
            EventBus.Unsubscribe<StoryEventUnlockedPublish>(OnStoryEventUnlocked);
        }

        NarrativeService?.Shutdown();
        ResearchService?.Shutdown();
        DailyTracker?.Shutdown();

        if (wasInstance)
            Instance = null;

        if (wasInstance)
            persistentUIRoot = null;
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
        ProgressionService = new ProgressionService(EventBus);
        CraftingService = new CraftingService(EntityService, InventoryService, ProgressionService, EventBus);
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
        RegisterExtraNamedTilemaps();

        // Quét baseline (snapshot gốc từ Editor)
        TileRegistry.ScanBaseline();
    }

    private void RegisterExtraNamedTilemaps()
    {
        // Nếu có SceneTilemapRegistry — dùng luôn, không cần scan Grid
        var reg = SceneTilemapRegistry.Current;
        if (reg != null)
        {
            if (reg.TryGet("Tm_Ground2", out var tm2))
                TileRegistry.RegisterTilemap("Tm_Ground2", tm2);
            return;
        }

        // Fallback: scan Grid children
        var grid = FindAnyObjectByType<Grid>();
        if (grid == null) return;

        foreach (var tm in grid.GetComponentsInChildren<Tilemap>())
        {
            switch (tm.gameObject.name)
            {
                case "Tm_Ground2":
                    TileRegistry.RegisterTilemap("Tm_Ground2", tm);
                    break;
            }
        }
    }

    private void AutoFindTilemaps()
    {
        // 1. Ưu tiên SceneTilemapRegistry — O(1), không scan scene
        var reg = SceneTilemapRegistry.Current;
        if (reg != null)
        {
            if (tmGround       == null) tmGround       = reg.Ground;
            if (tmGroundDetail == null) tmGroundDetail = reg.GroundDetail;
            if (tmWatered      == null) tmWatered      = reg.Watered;
            if (tmCollision    == null) tmCollision    = reg.Collision;
            if (tmDecoration   == null) tmDecoration   = reg.Decoration;
            if (tmOverlay      == null) tmOverlay      = reg.Overlay;

            if (tmGround != null)
            {
                Debug.Log("[GameManager] AutoFindTilemaps: bound from SceneTilemapRegistry.");
                return;
            }
        }

        // 2. Fallback: scan Grid children (chỉ khi scene chưa có SceneTilemapRegistry)
        var grid = FindAnyObjectByType<Grid>();
        if (grid == null) return;

        foreach (var tm in grid.GetComponentsInChildren<Tilemap>())
        {
            switch (tm.gameObject.name)
            {
                case "Tm_Ground":
                case "Tm_Ground1":      if (tmGround       == null) tmGround       = tm; break;
                case "Tm_GroundDetail": if (tmGroundDetail == null) tmGroundDetail = tm; break;
                case "Tm_Watered":      if (tmWatered      == null) tmWatered      = tm; break;
                case "Tm_Collision":    if (tmCollision    == null) tmCollision    = tm; break;
                case "Tm_Decoration":   if (tmDecoration   == null) tmDecoration   = tm; break;
                case "Tm_Overlay":      if (tmOverlay      == null) tmOverlay      = tm; break;
            }
        }
    }

    private void InitWorldEntitySystem()
    {
        // Dùng tmGround trực tiếp — GridSystem.Instance có thể chưa sẵn sàng lúc Awake
        var tilemap = tmGround;
        if (tilemap == null)
        {
            // Fallback: thử auto-find
            var grid = FindAnyObjectByType<Grid>();
            if (grid != null)
            {
                foreach (var tm in grid.GetComponentsInChildren<Tilemap>())
                    if (tm.gameObject.name == "Tm_Ground" || tm.gameObject.name == "Tm_Ground1") { tilemap = tm; break; }
            }
        }

        if (tilemap == null)
            Debug.LogWarning("[GameManager] Tilemap not found – WorldEntityService running headless.");

        SpatialRegistry = new SpatialEntityRegistry();
        WorldService    = new WorldEntityService(SpatialRegistry, TileRegistry, tileData, tilemap);
    }

    private void InitTimeManager()
    {
        TimeManager = GetComponent<TimeManager>();
        if (TimeManager == null)
            TimeManager = FindAnyObjectByType<TimeManager>();
        if (TimeManager == null)
            Debug.LogWarning("[GameManager] TimeManager not found.");
    }

    private void InitWateredTileTracker()
    {
        if (tmWatered == null)
        {
            Debug.LogWarning("[GameManager] tmWatered tilemap chưa gán; dùng watered tracker in-memory.");
        }

        WateredTileTracker = new WateredTileTracker(tmWatered, tmGround, tileData);

        // Thứ tự đúng: NextDayEventPublish (sprinklers water → cây grow) → DayChangedPublish (roll weather → reset watered → rain waters plowed)
        EventBus.Subscribe<NextDayEventPublish>(_ =>
        {
            SprinklerRegistry?.TickAll();
        });

        EventBus.Subscribe<DayChangedPublish>(_ =>
        {
            WeatherSystem?.RollNextDayWeather();
            WateredTileTracker?.ResetAll();
            WeatherSystem?.ApplyRainForNewDay(WateredTileTracker);
        });
    }

    private void InitSprinklerRegistry()
    {
        SprinklerRegistry = new SprinklerRegistry();
    }

    private void InitSoilQualityTracker()
    {
        SoilQualityTracker = new SoilQualityTracker();
    }

    private void InitClearZoneTracker()
    {
        ClearZoneTracker = new ClearZoneTracker(WorldService, tileData);
    }

    private void InitNarrativeService()
    {
#if UNITY_EDITOR
        var guids = UnityEditor.AssetDatabase.FindAssets("t:StoryEventData");
        var events = new System.Collections.Generic.List<StoryEventData>();
        foreach (var guid in guids)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var data = UnityEditor.AssetDatabase.LoadAssetAtPath<StoryEventData>(path);
            if (data != null) events.Add(data);
        }
        NarrativeService = new NarrativeService(EventBus, events);
#else
        var events = Resources.LoadAll<StoryEventData>("");
        NarrativeService = new NarrativeService(EventBus, events);
#endif
    }

    private void InitWeatherSystem()
    {
        if (weatherConfig == null)
            weatherConfig = Resources.Load<WeatherConfig>("Data/WeatherConfig");

        WeatherSystem = new WeatherSystem(EventBus, weatherConfig);
        // Roll weather for day 1 on new game (sunny default is fine; save/load will restore)
        Debug.Log($"[GameManager] WeatherSystem initialized. Weather={WeatherSystem.CurrentWeather}");
    }

    private void InitDialogueGameplayBridge()
    {
        if (GetComponent<DialogueGameplayBridge>() == null)
            gameObject.AddComponent<DialogueGameplayBridge>();
    }

    private void InitSceneTransitionBridge()
    {
        if (GetComponent<SceneTransitionBridge>() == null)
            gameObject.AddComponent<SceneTransitionBridge>();
    }

    private void InitInteractionPreviewBridge()
    {
        if (GetComponent<InteractionPreviewSystem>() == null)
            gameObject.AddComponent<InteractionPreviewSystem>();
    }

    private void InitPlayerDeathHandler()
    {
        if (GetComponent<PlayerDeathHandler>() == null)
            gameObject.AddComponent<PlayerDeathHandler>();
    }

    private void InitCombatProgressionPolish()
    {
        EnsureRuntimeComponent<FloatingTextPool>();
        EnsureRuntimeComponent<FloatingCombatTextSpawner>();
        EnsureRuntimeComponent<HitStopManager>();
        EnsureRuntimeComponent<PlayerCombatPolishBinder>();
        EnsureRuntimeComponent<LowHpVignetteUI>();
        EnsureRuntimeComponent<ExpPopupSpawner>();
        EnsureRuntimeComponent<LevelUpEffect>();
        EnsureRuntimeComponent<ToastUI>();
        EnsureRuntimeComponent<DeathScreenUI>();
        EnsureRuntimeComponent<QuestProgressionSystem>();
        EnsureRuntimeComponent<NightEnemySpawnManager>();
        EnsureRuntimeComponent<AutoSaveOnDayChanged>();
    }

    private void InitNarrativeUI()
    {
        EnsureNarrativeUIComponent<MessageNotificationUI>("NarrativeUI_MessageNotification");
        EnsureNarrativeUIComponent<NewsBroadcastUI>("NarrativeUI_NewsBroadcast");
        EnsureNarrativeUIComponent<DiaryUI>("NarrativeUI_Diary");
        EnsureNarrativeUIComponent<CalendarUI>("CalendarUI");
    }

    private void InitLoadingScreenUI()
    {
        EnsureNarrativeUIComponent<LoadingScreenUI>("LoadingScreenUI");
    }

    private void InitResearchService()
    {
#if UNITY_EDITOR
        var guids = UnityEditor.AssetDatabase.FindAssets("t:ResearchData");
        var research = new System.Collections.Generic.List<ResearchData>();
        foreach (var guid in guids)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var data = UnityEditor.AssetDatabase.LoadAssetAtPath<ResearchData>(path);
            if (data != null) research.Add(data);
        }
        ResearchService = new ResearchService(EventBus, TimeManager, research);
#else
        var research = Resources.LoadAll<ResearchData>("");
        ResearchService = new ResearchService(EventBus, TimeManager, research);
#endif
    }

    private void InitDailyTracker()
    {
        DailyTracker = new DailyTracker(EventBus, TimeManager);
    }

    private void InitEndOfDaySummaryUI()
    {
        EnsureNarrativeUIComponent<EndOfDaySummaryUI>("EndOfDaySummaryUI");
    }

    private void InitPickupNotificationUI()
    {
        EnsureNarrativeUIComponent<PickupNotificationUI>("PickupNotificationUI");
    }

    private T EnsureNarrativeUIComponent<T>(string childName) where T : Component
    {
        var child = transform.Find(childName);
        var go = child != null ? child.gameObject : new GameObject(childName);
        go.transform.SetParent(transform, false);
        return go.GetComponent<T>() ?? go.AddComponent<T>();
    }

    private T EnsureRuntimeComponent<T>() where T : Component
    {
        return GetComponent<T>() ?? gameObject.AddComponent<T>();
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
            defaultPlayerPos,
            ResolveStarterLoadout()
        );
        SaveLoadManager.SetTimeManager(TimeManager);
    }

    private StarterLoadoutData ResolveStarterLoadout()
    {
        if (starterLoadout != null)
            return starterLoadout;

        return Resources.Load<StarterLoadoutData>("Data/StarterLoadouts/DefaultStarterLoadout");
    }

    // ── Event handlers ────────────────────────────────────

    private void OnSaveRequest(SaveGameRequestPublish req)
    {
        SaveLoadManager.SaveAll();
    }

    private void OnLoadRequest(LoadGameRequestPublish req)
    {
        if (isReloadingScene) return;
        isReloadingScene = true;

        string currentSceneName = SceneManager.GetActiveScene().name;
        if (string.IsNullOrWhiteSpace(currentSceneName))
        {
            Debug.LogError("[GameManager] Cannot reload: active scene name is empty.");
            isReloadingScene = false;
            return;
        }

        Debug.Log($"[GameManager] Load requested. Reloading scene '{currentSceneName}'...");
        SceneManager.LoadScene(currentSceneName);
    }

    private void OnPlayerReady(PlayerReadyPublish _)
    {
        EventBus.Publish(new GameReadyPublish());
        Debug.Log("[GameManager] GameReadyPublish published — boot sequence complete.");
    }

    private void OnStoryEventUnlocked(StoryEventUnlockedPublish evt)
    {
        if (evt.data == null || evt.data.id != "story_day10_mutant_threat")
            return;

        SpawnNarrativeMutant();
    }

    private void SpawnNarrativeMutant()
    {
        var enemyData = EntityDataRegistry?.GetById("enemy_orc1")
                     ?? EntityDataRegistry?.GetById("enemy_slime3")
                     ?? EntityDataRegistry?.GetById("enemy_slime2")
                     ?? EntityDataRegistry?.GetById("enemy_slime1");
        if (enemyData == null)
        {
            Debug.LogWarning("[GameManager] Narrative mutant spawn skipped: no enemy data found.");
            return;
        }

        var objectType = NightEnemySpawnManager.ResolveObjectType(enemyData.id);

        var spawnPos = defaultPlayerPos + new Vector2(3f, 0f);
        var player = FindAnyObjectByType<PlayerControler>();
        if (player != null)
            spawnPos = (Vector2)player.transform.position + new Vector2(3f, 0f);

        var sceneName = SceneManager.GetActiveScene().name;
        var cell = new Vector3Int(Mathf.FloorToInt(spawnPos.x), Mathf.FloorToInt(spawnPos.y), 0);
        var payload = new SceneSpawnPayload
        {
            sceneName = sceneName,
            markerKind = SceneMarkerKind.Enemy,
            objectType = objectType,
            cell = cell,
            spawnGroupId = "narrative_day10_mutant",
            persistentId = SceneSpawnPayload.BuildPersistentId(sceneName, SceneMarkerKind.Enemy, objectType, cell, "narrative_day10_mutant"),
            savePolicy = SceneEntitySavePolicy.Persistent,
            initialAmount = 1
        };

        EventBus.Publish(new SpawnRequestPublish(spawnPos, objectType, enemyData, 1, true, payload));
        Debug.Log($"[GameManager] Narrative mutant spawn requested: {enemyData.id} at {spawnPos}");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (shuttingDownForMainMenu)
            return;

        if (SceneTransitionService.ShouldDeferAutoRestore(scene.name))
            return;

        BeginRestoreActiveSceneAfterLoad(scene.name);
    }

    public void BeginRestoreActiveSceneAfterLoad(string sceneName)
    {
        if (!bootCompleted || Instance != this || isRestoringScene)
            return;

        isRestoringScene = true;
        StartCoroutine(RestoreActiveSceneAfterLoad(sceneName));
    }

    private IEnumerator RestoreActiveSceneAfterLoad(string sceneName)
    {
        // Wait one frame so SceneContext/SceneContentScanner in the new scene can run Awake/Start.
        yield return null;
        bool saveArrivalSceneState = false;

        EventBus?.Publish(new LoadingScreenProgressPublish(0.88f));
        EnsurePersistentUIRoot();
        RebindSceneScopedServices();
        EventBus?.Publish(new LoadingScreenProgressPublish(0.92f));

        EnsureMainCameraExists();

        if (SaveLoadManager != null && SaveLoadManager.IsBootingFromSave)
        {
            Debug.Log("[GameManager] Scene loaded is target saved scene. Performing full saved-state restore...");
            SaveLoadManager.CompleteBootFromSavedScene();
        }
        else
        {
            SaveLoadManager.LoadCurrentSceneWorld();
            EventBus?.Publish(new LoadingScreenProgressPublish(0.96f));

            bool arrivedFromTransition = SceneTransitionService.TryPeekPendingSpawnPointForCurrentScene(out var spawnPointId);
            SaveLoadManager.EnsurePlayerSpawnedAt(spawnPointId);
            if (arrivedFromTransition)
                SceneTransitionService.SuppressPortalTriggersAfterArrival();
            saveArrivalSceneState = arrivedFromTransition;
            EventBus?.Publish(new LoadingScreenProgressPublish(0.99f));

            EventBus.Publish(new WorldObjectsSpawnedPublish());
            EventBus.Publish(new InventoryDataRestoredPublish());
        }

        // Camera rebind sau khi player đã spawn trong scene mới
        RebindCamera();

        if (saveArrivalSceneState)
        {
            yield return null;
            SaveLoadManager?.SaveSystemSnapshot();
        }

        EventBus?.Publish(new LoadingScreenProgressPublish(1f));
        EventBus.Publish(new LoadingScreenHidePublish());
        SceneTransitionService.NotifyRestoreCompleted(sceneName);

        isReloadingScene = false;
        isRestoringScene = false;
        Debug.Log($"[GameManager] Restored scene-scoped services and player for scene '{sceneName}'.");
    }

    private void RebindSceneScopedServices()
    {
        // Reset tất cả tilemap ref — SceneTilemapRegistry.Current sẽ được set
        // bởi Awake() của SceneTilemapRegistry trong scene mới trước khi chạy đến đây
        tmGround      = null;
        tmGroundDetail = null;
        tmWatered     = null;
        tmCollision   = null;
        tmDecoration  = null;
        tmOverlay     = null;

        // Rebind GridSystem ngay nếu có registry
        if (SceneTilemapRegistry.Current != null)
        {
            var gs = GridSystem.Instance;
            if (gs != null) gs.AutoRebind();
        }

        InitTileRegistry();
        InitWorldEntitySystem();

        // WateredTileTracker dùng readonly fields — tạo mới với tilemap từ scene mới.
        // Các lambda event trong EventBus đọc WateredTileTracker qua field (không phải capture),
        // nên chỉ cần reassign field là đủ.
        if (tmWatered != null || tmGround != null)
            WateredTileTracker = new WateredTileTracker(tmWatered, tmGround, tileData);

        ClearZoneTracker?.RebindWorldService(WorldService, tileData);
        SprinklerRegistry?.Clear();
        SpawnSystem?.RebindWorldService(WorldService);
        SaveLoadManager?.SetWorldService(WorldService);
    }

    private void DestroyDuplicateRuntimeRoot()
    {
        // Only destroy the duplicate GameManager object itself.
        // Scene-local cameras and other siblings may live under the same scene root,
        // so destroying transform.root here can wipe out the destination scene camera
        // during additive transitions.
        Destroy(gameObject);
    }

    public static void PrepareReturnToMainMenu()
    {
        shuttingDownForMainMenu = true;
        SceneTransitionService.ClearPendingTransitionState();

        var manager = Instance;
        if (manager != null)
        {
            SceneManager.sceneLoaded -= manager.OnSceneLoaded;
            manager.EventBus?.Publish(new LoadingScreenHidePublish());

            var managerRoot = manager.transform.root != null
                ? manager.transform.root.gameObject
                : manager.gameObject;

            Destroy(managerRoot);
        }

        foreach (var root in FindNamedGameObjects("UIRoot"))
        {
            if (root != null)
                Destroy(root);
        }

        persistentUIRoot = null;
    }

    private void EnsurePersistentUIRoot()
    {
        var roots = FindNamedGameObjects("UIRoot");

        if (persistentUIRoot == null)
        {
            foreach (var root in roots)
            {
                if (root == null) continue;
                persistentUIRoot = root;
                DontDestroyOnLoad(persistentUIRoot);
                break;
            }
        }

        // Fallback: nếu không tìm thấy UIRoot trong scene, thử instantiate từ BootstrapConfig
        // (trường hợp game start từ scene không phải FarmScene mà BootstrapLoader chưa chạy)
        if (persistentUIRoot == null)
        {
            var config = Resources.Load<BootstrapConfig>(BootstrapConfig.ResourcePath);
            if (config != null && config.uiRootPrefab != null)
            {
                persistentUIRoot = Instantiate(config.uiRootPrefab);
                persistentUIRoot.name = "UIRoot";
                DontDestroyOnLoad(persistentUIRoot);
                Debug.Log("[GameManager] UIRoot instantiated from BootstrapConfig as fallback.");
            }
            else
            {
                Debug.LogError("[GameManager] UIRoot not found and BootstrapConfig fallback unavailable! " +
                               "UI sẽ không hoạt động.");
            }
        }

        // Destroy các UIRoot duplicate (chỉ giữ lại persistentUIRoot)
        foreach (var root in roots)
        {
            if (root == null || root == persistentUIRoot)
                continue;

            Destroy(root);
        }
    }


    private static GameObject[] FindNamedGameObjects(string objectName)
    {
        var transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int count = 0;
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null && transforms[i].name == objectName)
                count++;
        }

        var results = new GameObject[count];
        int index = 0;
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null && transforms[i].name == objectName)
                results[index++] = transforms[i].gameObject;
        }

        return results;
    }

    private void EnsureMainCameraExists()
    {
        var mainCam = Camera.main;

        if (mainCam == null)
        {
            // Tìm hoặc tạo Cameras container
            var cameras = GameObject.Find("Cameras");
            if (cameras == null)
                cameras = new GameObject("Cameras");

            var camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            camObj.transform.SetParent(cameras.transform);
            camObj.transform.position = new Vector3(0f, 0f, -10f);

            mainCam = camObj.AddComponent<Camera>();
            mainCam.orthographic = true;
            mainCam.orthographicSize = 6f;

            // SceneCameraFollower thay thế Cinemachine — đơn giản, không bị lỗi sau scene transition
            camObj.AddComponent<SceneCameraFollower>();

            Debug.Log("[GameManager] Created Main Camera with SceneCameraFollower.");
        }
        else
        {
            // Camera đã tồn tại — đảm bảo có SceneCameraFollower
            if (mainCam.GetComponent<SceneCameraFollower>() == null)
            {
                mainCam.gameObject.AddComponent<SceneCameraFollower>();
                Debug.Log("[GameManager] Added SceneCameraFollower to existing Main Camera.");
            }
        }
    }

    /// <summary>Gọi sau khi scene mới load xong để camera rebind player mới.</summary>
    private static void RebindCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;

        var follower = cam.GetComponent<SceneCameraFollower>();
        follower?.ForceRebind();
    }
}

/// <summary>
/// Service chuyển scene + giữ spawn point đích tạm thời giữa 2 scene.
/// </summary>
public static class SceneTransitionService
{
    private static string pendingSpawnPointId;
    private static string pendingSceneName;
    private static float portalTriggerSuppressedUntilRealtime;
    private const float ArrivalPortalSuppressSeconds = 0.75f;
    private static bool deferAutoRestoreForPendingScene;
    private static Camera transitionCamera;
    private static GameObject transitionCameraObject;

    public static void ClearPendingTransitionState()
    {
        pendingSpawnPointId = null;
        pendingSceneName = null;
        deferAutoRestoreForPendingScene = false;
        portalTriggerSuppressedUntilRealtime = 0f;
        DisableTransitionCamera();
        if (transitionCameraObject != null)
        {
            UnityEngine.Object.Destroy(transitionCameraObject);
            transitionCameraObject = null;
            transitionCamera = null;
        }
    }

    public static bool RequestTransition(
        EntityRuntime interactor,
        string targetSceneName,
        string targetSpawnPointId,
        bool saveBeforeTransition)
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogWarning("[SceneTransitionService] targetSceneName is empty.");
            return false;
        }

        EnsureTransitionCameraActive();

        var eventBus = GameManager.Instance?.EventBus;
        if (saveBeforeTransition && eventBus != null)
            eventBus.Publish(new SaveGameRequestPublish());

        pendingSceneName = targetSceneName.Trim();
        pendingSpawnPointId = string.IsNullOrWhiteSpace(targetSpawnPointId)
            ? string.Empty
            : targetSpawnPointId.Trim();

        try
        {
            if (eventBus != null)
                eventBus.Publish(new LoadingScreenShowPublish(pendingSceneName));

            var manager = GameManager.Instance;
            if (manager != null)
                manager.StartCoroutine(LoadSceneAsyncRoutine(pendingSceneName, eventBus));
            else
                SceneManager.LoadScene(pendingSceneName);

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SceneTransitionService] Failed to load scene '{pendingSceneName}': {ex.Message}");
            if (eventBus != null)
                eventBus.Publish(new LoadingScreenHidePublish());
            pendingSceneName = null;
            pendingSpawnPointId = null;
            return false;
        }
    }

    public static bool ArePortalTriggersSuppressed()
    {
        return Time.realtimeSinceStartup < portalTriggerSuppressedUntilRealtime;
    }

    public static void SuppressPortalTriggersAfterArrival(float seconds = ArrivalPortalSuppressSeconds)
    {
        portalTriggerSuppressedUntilRealtime = Time.realtimeSinceStartup + Mathf.Max(0.05f, seconds);
    }

    public static bool ShouldDeferAutoRestore(string sceneName)
    {
        return deferAutoRestoreForPendingScene
               && !string.IsNullOrWhiteSpace(pendingSceneName)
               && string.Equals(sceneName, pendingSceneName, StringComparison.Ordinal);
    }

    public static void NotifyRestoreCompleted(string sceneName)
    {
        if (!string.IsNullOrWhiteSpace(pendingSceneName)
            && string.Equals(sceneName, pendingSceneName, StringComparison.Ordinal))
        {
            deferAutoRestoreForPendingScene = false;
        }

        DisableTransitionCamera();
    }

    private static IEnumerator LoadSceneAsyncRoutine(string sceneName, EventBus eventBus)
    {
        yield return null;

        var sourceScene = SceneManager.GetActiveScene();
        deferAutoRestoreForPendingScene = true;

        var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (operation == null)
        {
            deferAutoRestoreForPendingScene = false;
            SceneManager.LoadScene(sceneName);
            yield break;
        }

        while (!operation.isDone)
        {
            float progress = Mathf.Lerp(0.05f, 0.6f, Mathf.Clamp01(operation.progress / 0.9f));
            eventBus?.Publish(new LoadingScreenProgressPublish(progress));
            yield return null;
        }

        var targetScene = SceneManager.GetSceneByName(sceneName);
        if (targetScene.IsValid())
            SceneManager.SetActiveScene(targetScene);

        eventBus?.Publish(new LoadingScreenProgressPublish(0.68f));

        if (sourceScene.IsValid() && sourceScene.isLoaded && !string.Equals(sourceScene.name, sceneName, StringComparison.Ordinal))
        {
            var unloadOperation = SceneManager.UnloadSceneAsync(sourceScene);
            if (unloadOperation != null)
            {
                while (!unloadOperation.isDone)
                {
                    eventBus?.Publish(new LoadingScreenProgressPublish(0.68f));
                    yield return null;
                }
            }
        }

        eventBus?.Publish(new LoadingScreenProgressPublish(0.82f));
        GameManager.Instance?.BeginRestoreActiveSceneAfterLoad(sceneName);
    }

    public static bool TryConsumePendingSpawnPointForCurrentScene(out string spawnPointId)
    {
        spawnPointId = null;

        if (string.IsNullOrWhiteSpace(pendingSceneName))
            return false;

        string currentScene = SceneManager.GetActiveScene().name;
        if (!string.Equals(currentScene, pendingSceneName, StringComparison.Ordinal))
            return false;

        spawnPointId = pendingSpawnPointId;
        pendingSceneName = null;
        pendingSpawnPointId = null;
        return true;
    }

    public static bool TryPeekPendingSpawnPointForCurrentScene(out string spawnPointId)
    {
        spawnPointId = null;

        if (string.IsNullOrWhiteSpace(pendingSceneName))
            return false;

        string currentScene = SceneManager.GetActiveScene().name;
        if (!string.Equals(currentScene, pendingSceneName, StringComparison.Ordinal))
            return false;

        spawnPointId = pendingSpawnPointId;
        return true;
    }

    private static void EnsureTransitionCameraActive()
    {
        if (transitionCameraObject == null)
        {
            transitionCameraObject = new GameObject("__TransitionCamera");
            UnityEngine.Object.DontDestroyOnLoad(transitionCameraObject);
            transitionCamera = transitionCameraObject.AddComponent<Camera>();
            transitionCamera.clearFlags = CameraClearFlags.SolidColor;
            transitionCamera.backgroundColor = Color.black;
            transitionCamera.cullingMask = 0;
            transitionCamera.orthographic = true;
            transitionCamera.depth = -1000f;
            transitionCamera.nearClipPlane = 0.01f;
            transitionCamera.farClipPlane = 1f;
        }

        if (transitionCamera != null)
            transitionCamera.enabled = true;
    }

    private static void DisableTransitionCamera()
    {
        if (transitionCamera == null)
            return;

        transitionCamera.enabled = false;
    }
}

/// <summary>
/// Lắng nghe PlayerReadyPublish và áp spawn point pending sau scene transition.
/// </summary>
public class SceneTransitionBridge : MonoBehaviour
{
    private EventBus eventBus;
    private bool subscribed;

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Update()
    {
        if (!subscribed)
            TrySubscribe();
    }

    private void OnDisable()
    {
        if (subscribed && eventBus != null)
        {
            eventBus.Unsubscribe<PlayerReadyPublish>(OnPlayerReady);
            subscribed = false;
        }
    }

    private void TrySubscribe()
    {
        if (subscribed) return;
        eventBus = GameManager.Instance?.EventBus;
        if (eventBus == null) return;

        eventBus.Subscribe<PlayerReadyPublish>(OnPlayerReady);
        subscribed = true;
    }

    private void OnPlayerReady(PlayerReadyPublish _)
    {
        if (!SceneTransitionService.TryConsumePendingSpawnPointForCurrentScene(out var spawnPointId))
            return;

        var player = FindBestPlayerForActiveScene();
        if (player == null) return;

        Vector2 fallback = player.transform.position;
        player.transform.position = SceneSpawnResolver.Resolve(spawnPointId, fallback);
    }

    private static PlayerControler FindBestPlayerForActiveScene()
    {
        var players = FindObjectsByType<PlayerControler>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
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
}

[DefaultExecutionOrder(-200)]
public class DialogueGameplayBridge : MonoBehaviour
{
    private EventBus eventBus;
    private bool subscribed;
    private static readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.HashSet<string>> PlayerFlags = new();

    private void Awake()
    {
        DialogueService.ConditionEvaluator = EvaluateCondition;
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Update()
    {
        if (!subscribed)
            TrySubscribe();
    }

    private void OnDisable()
    {
        if (subscribed && eventBus != null)
        {
            eventBus.Unsubscribe<DialogueEventNodePublish>(OnDialogueEventNode);
            subscribed = false;
        }

        if (DialogueService.ConditionEvaluator == EvaluateCondition)
            DialogueService.ConditionEvaluator = null;
    }

    private void TrySubscribe()
    {
        if (subscribed) return;

        eventBus = GameManager.Instance?.EventBus;
        if (eventBus == null) return;

        eventBus.Subscribe<DialogueEventNodePublish>(OnDialogueEventNode);
        subscribed = true;
    }

    private static bool EvaluateCondition(DialogueConditionContext context)
    {
        if (context == null || string.IsNullOrWhiteSpace(context.ConditionKey))
            return false;

        string condition = context.ConditionKey.Trim();
        if (TryEvaluateQuestState(context, condition, out bool questResult))
            return questResult;

        if (TryEvaluateHasItem(context, condition, out bool hasItemResult))
            return hasItemResult;

        if (TryEvaluateStat(context, condition, out bool statResult))
            return statResult;

        if (TryEvaluateFlag(context, condition, out bool flagResult))
            return flagResult;

        return false;
    }

    private void OnDialogueEventNode(DialogueEventNodePublish e)
    {
        string eventKey = e.eventKey?.Trim();
        if (string.IsNullOrWhiteSpace(eventKey))
            return;

        var speaker = e.context?.Speaker;
        var player = e.context?.Listener;
        if (player == null)
        {
            Debug.LogWarning($"[DialogueGameplayBridge] Event '{eventKey}' ignored: listener/player is null.");
            return;
        }

        string lower = eventKey.ToLowerInvariant();

        if (lower.StartsWith("quest.accept"))
        {
            ExecuteQuestAction(player, speaker, ResolveArgument(eventKey, e.payload), QuestService.AcceptQuest);
            return;
        }

        if (lower.StartsWith("quest.complete"))
        {
            ExecuteQuestAction(player, speaker, ResolveArgument(eventKey, e.payload), QuestService.CompleteQuest);
            return;
        }

        if (lower.StartsWith("quest.show"))
        {
            ExecuteQuestAction(player, speaker, ResolveArgument(eventKey, e.payload), (p, owner, graph) =>
            {
                QuestService.ShowQuest(p, owner, graph);
                return true;
            });
            return;
        }

        if (lower.StartsWith("shop.open"))
        {
            if (speaker == null) return;
            var shop = speaker.entityData?.modules?.OfType<ShopModule>().FirstOrDefault();
            if (shop == null)
            {
                Debug.LogWarning($"[DialogueGameplayBridge] Event '{eventKey}' failed: speaker has no ShopModule.");
                return;
            }

            ShopService.Open(player, speaker, shop);
            return;
        }

        if (lower == "sample.set_flag")
        {
            SetPlayerFlag(player, e.payload);
            return;
        }

        if (lower == "sample.accept_farmer_work")
        {
            ExecuteQuestAction(player, speaker, e.payload, QuestService.AcceptQuest);
            return;
        }

        Debug.Log($"[DialogueGameplayBridge] Unhandled dialogue event key '{eventKey}'.");
    }

    private void ExecuteQuestAction(
        EntityRuntime player,
        EntityRuntime questOwner,
        string questId,
        Func<EntityRuntime, EntityRuntime, QuestGraphData, bool> questAction)
    {
        if (string.IsNullOrWhiteSpace(questId) || questAction == null)
            return;

        var questGraph = FindQuestGraph(questOwner, questId);
        if (questGraph == null)
        {
            Debug.LogWarning($"[DialogueGameplayBridge] Quest '{questId}' not found on '{questOwner?.entityData?.keyName}'.");
            return;
        }

        questAction(player, questOwner, questGraph);
    }

    private static QuestGraphData FindQuestGraph(EntityRuntime questOwner, string questId)
    {
        if (questOwner?.entityData?.modules == null || string.IsNullOrWhiteSpace(questId))
            return null;

        foreach (var module in questOwner.entityData.modules.OfType<QuestModule>())
        {
            if (module?.quests == null) continue;
            foreach (var quest in module.quests)
            {
                if (quest != null && string.Equals(quest.id, questId, StringComparison.OrdinalIgnoreCase))
                    return quest;
            }
        }

        return null;
    }

    private static string ResolveArgument(string eventKey, string payload)
    {
        if (!string.IsNullOrWhiteSpace(payload))
            return payload.Trim();

        if (string.IsNullOrWhiteSpace(eventKey))
            return string.Empty;

        int index = eventKey.IndexOf(':');
        return index < 0 ? string.Empty : eventKey[(index + 1)..].Trim();
    }

    private static bool TryEvaluateQuestState(DialogueConditionContext context, string condition, out bool result)
    {
        result = false;
        const string prefix = "quest_state:";
        if (!condition.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        string questId = condition[prefix.Length..].Trim();
        if (string.IsNullOrWhiteSpace(questId))
            return true;

        var log = context.Listener?.GetModule<QuestLogRuntime>();
        var state = log?.GetState(questId) ?? QuestState.NotStarted;

        if (context.CompareMode == DialogueCompareMode.Exists)
        {
            result = state != QuestState.NotStarted;
            return true;
        }

        if (!Enum.TryParse(context.CompareValue, true, out QuestState expected))
            expected = QuestState.NotStarted;

        result = context.CompareMode switch
        {
            DialogueCompareMode.Equals => state == expected,
            DialogueCompareMode.NotEquals => state != expected,
            _ => state == expected
        };

        return true;
    }

    private static bool TryEvaluateHasItem(DialogueConditionContext context, string condition, out bool result)
    {
        result = false;
        const string prefix = "has_item:";
        if (!condition.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        string itemId = condition[prefix.Length..].Trim();
        if (string.IsNullOrWhiteSpace(itemId))
            return true;

        var inventoryService = GameManager.Instance?.InventoryService;
        if (inventoryService == null || context.Listener == null)
            return true;

        int count = inventoryService.CountEntity(context.Listener, itemId);
        int required = ParseIntOrDefault(context.CompareValue, 1);
        result = CompareNumeric(count, required, context.CompareMode);
        return true;
    }

    private static bool TryEvaluateStat(DialogueConditionContext context, string condition, out bool result)
    {
        result = false;
        const string prefix = "stat:";
        if (!condition.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        string statRaw = condition[prefix.Length..].Trim();
        if (!Enum.TryParse(statRaw, true, out StatType statType))
            return true;

        float value = context.Listener?.stats?.Get(statType) ?? 0f;
        float comparedValue = ParseFloatOrDefault(context.CompareValue, 0f);
        result = CompareNumeric(value, comparedValue, context.CompareMode);
        return true;
    }

    private static bool TryEvaluateFlag(DialogueConditionContext context, string condition, out bool result)
    {
        result = false;
        if (!condition.StartsWith("sample.", StringComparison.OrdinalIgnoreCase))
            return false;

        var playerId = context.Listener?.id;
        if (string.IsNullOrWhiteSpace(playerId))
            return true;

        result = TryGetFlags(playerId, out var flags) && flags.Contains(condition);
        return true;
    }

    private static void SetPlayerFlag(EntityRuntime player, string flag)
    {
        if (player == null || string.IsNullOrWhiteSpace(flag))
            return;

        if (!TryGetFlags(player.id, out var flags))
        {
            flags = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            PlayerFlags[player.id] = flags;
        }

        flags.Add(flag.Trim());
    }

    private static bool TryGetFlags(string playerId, out System.Collections.Generic.HashSet<string> flags)
    {
        flags = null;
        if (string.IsNullOrWhiteSpace(playerId))
            return false;

        return PlayerFlags.TryGetValue(playerId, out flags);
    }

    private static bool CompareNumeric(float left, float right, DialogueCompareMode mode)
    {
        return mode switch
        {
            DialogueCompareMode.Equals => Mathf.Approximately(left, right),
            DialogueCompareMode.NotEquals => !Mathf.Approximately(left, right),
            DialogueCompareMode.GreaterOrEqual => left >= right,
            DialogueCompareMode.LessOrEqual => left <= right,
            DialogueCompareMode.Exists => left > 0f,
            _ => false
        };
    }

    private static int ParseIntOrDefault(string value, int fallback)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
            ? parsed
            : fallback;
    }

    private static float ParseFloatOrDefault(string value, float fallback)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
            ? parsed
            : fallback;
    }
}
