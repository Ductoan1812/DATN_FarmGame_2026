using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using static GameManager;

/// <summary>
/// In-game Debug Console. Mở bằng phím ` (BackQuote).
/// UI được build sẵn trong scene (Tools > Setup Debug Console UI).
/// Hỗ trợ autocomplete, history, và command registry mở rộng.
/// </summary>
public class DebugConsole : MonoBehaviour
{
    // ── Config ──
    [SerializeField] private KeyCode toggleKey = KeyCode.BackQuote;
    [SerializeField] private int maxLogLines = 50;

    // ── UI Refs (gán từ Inspector) ──
    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TextMeshProUGUI logText;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private GameObject suggestionsPanel;

    // ── Runtime ──
    private List<TextMeshProUGUI> suggestionTexts = new();
    private bool isOpen;
    private readonly List<string> logLines = new();
    private readonly List<string> history = new();
    private int historyIndex = -1;
    private readonly List<string> currentSuggestions = new();
    private int selectedSuggestion = -1;

    // ── Commands ──
    private readonly Dictionary<string, Action<string[]>> commands = new();
    private readonly Dictionary<string, string> commandHelp = new();

    // ── Colors ──
    private static readonly string hexWhite = "FFFFFF";
    private static readonly string hexError = "FF6666";
    private static readonly string hexOK = "66FF66";
    private static readonly Color colHighlight = new(0.3f, 0.6f, 1f);

    #region Lifecycle

    private void Awake()
    {
        AutoFindRefs();
        CacheSuggestionTexts();
        RegisterBuiltInCommands();
        if (panel != null) panel.SetActive(false);
        if (inputField != null) inputField.onValueChanged.AddListener(OnInputChanged);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey)) Toggle();
        if (!isOpen) return;

        if (Input.GetKeyDown(KeyCode.UpArrow))   { if (currentSuggestions.Count > 0) NavSuggestion(-1); else NavHistory(-1); }
        if (Input.GetKeyDown(KeyCode.DownArrow))  { if (currentSuggestions.Count > 0) NavSuggestion(1);  else NavHistory(1);  }
        if (Input.GetKeyDown(KeyCode.Tab) && currentSuggestions.Count > 0) ApplySuggestion();
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (selectedSuggestion >= 0 && selectedSuggestion < currentSuggestions.Count) ApplySuggestion();
            else Submit();
        }
        if (Input.GetKeyDown(KeyCode.Escape)) Toggle();
    }

    #endregion

    #region Public API

    /// <summary>Mở/đóng console.</summary>
    public void Toggle()
    {   
        isOpen = !isOpen;
        panel.SetActive(isOpen);

        // Tắt/bật input của Player khi console mở/đóng
        var playerCtl = FindAnyObjectByType<PlayerControler>();
        if (playerCtl != null) playerCtl.InputEnabled = !isOpen;

        if (isOpen) { inputField.ActivateInputField(); inputField.text = ""; }
    }

    /// <summary>Đăng ký command từ bên ngoài.</summary>
    public void AddCommand(string name, string help, Action<string[]> action)
    {
        var key = name.ToLowerInvariant();
        commands[key] = action;
        commandHelp[key] = help;
    }

    public void Log(string msg) => Append(msg, hexWhite);
    public void LogError(string msg) => Append($"[ERROR] {msg}", hexError);
    public void LogSuccess(string msg) => Append($"[OK] {msg}", hexOK);

    #endregion

    #region Commands

    private void RegisterBuiltInCommands()
    {
        AddCommand("help", "Hiển thị danh sách lệnh", _ =>
        {
            Log("── Commands ──");
            foreach (var kv in commandHelp.OrderBy(k => k.Key))
                Log($"  {kv.Key} — {kv.Value}");
        });

        AddCommand("clear", "Xóa log", _ => { logLines.Clear(); logText.text = ""; });

        AddCommand("save", "save — Lưu game ngay", _ =>
        {
            var gm = GM(); if (gm == null) return;
            gm.EventBus.Publish(new SaveGameRequestPublish());
            LogSuccess("Đã gửi yêu cầu SaveGameRequestPublish.");
        });

        AddCommand("load", "load — Load save (reload scene hiện tại)", _ =>
        {
            var gm = GM(); if (gm == null) return;
            gm.EventBus.Publish(new LoadGameRequestPublish());
            LogSuccess("Đã gửi yêu cầu LoadGameRequestPublish.");
        });

        AddCommand("scene", "scene <sceneName> [spawnPointId] — Chuyển scene qua SceneTransitionService", args =>
        {
            if (args.Length < 1) { LogError("scene <sceneName> [spawnPointId]"); return; }

            var player = FindAnyObjectByType<PlayerControler>();
            var playerEntity = player != null ? player.GetComponent<EntityRoot>()?.GetEntity() : null;

            string sceneName = args[0];
            string spawnPointId = args.Length > 1 ? args[1] : string.Empty;

            bool ok = SceneTransitionService.RequestTransition(
                playerEntity,
                sceneName,
                spawnPointId,
                saveBeforeTransition: true);

            if (ok) LogSuccess($"Đang chuyển scene '{sceneName}' (spawn='{spawnPointId}').");
            else LogError($"Không thể chuyển scene '{sceneName}'.");
        });

        AddCommand("set", "set <target> <stat> <value> — Set stat runtime của EntityRoot", args =>
        {
            if (args.Length < 3) { LogError("set <target> <stat> <value>"); return; }

            var root = FindEntityRoot(args[0]);
            if (root == null) { LogError($"EntityRoot '{args[0]}' không tìm thấy. Dùng 'entities'."); return; }

            var entity = root.GetEntity();
            if (entity == null) { LogError($"'{root.gameObject.name}' chưa có EntityRuntime."); return; }

            if (!System.Enum.TryParse(args[1], true, out StatType statType))
            {
                LogError($"Stat '{args[1]}' không hợp lệ. Ví dụ: Money, Hp, MaxHp, Speed.");
                return;
            }

            if (!TryParseFloat(args[2], out float value))
            {
                LogError($"Value '{args[2]}' không hợp lệ.");
                return;
            }

            entity.stats.Set(statType, value);
            LogSuccess($"{root.gameObject.name}.{statType} = {entity.stats.Get(statType):0.###}");
        });

        AddCommand("entities", "Liệt kê EntityRoot runtime trong scene", _ =>
        {
            var roots = FindObjectsOfType<EntityRoot>();
            int count = 0;
            foreach (var root in roots)
            {
                var entity = root.GetEntity();
                if (entity == null) continue;

                Log($"  {root.gameObject.name} — runtime:{entity.id} data:{entity.entityData?.keyName ?? "-"} id:{entity.entityData?.id ?? "-"}");
                count++;
            }

            if (count == 0) Log("Không có EntityRoot runtime nào.");
        });

        // ── give <target> <item> [amount] ──
        AddCommand("give", "give <target> <item> [amount]", args =>
        {
            if (args.Length < 2) { LogError("give <target> <item> [amount]"); return; }
            var gm = GM(); if (gm == null) return;

            int amount = args.Length >= 3 ? ParseInt(args[2], 1) : 1;
            var root = FindContainer(args[0]);
            if (root == null) { LogError($"Container '{args[0]}' không tìm thấy. Dùng 'containers'."); return; }

            var data = FindEntityData(args[1]); if (data == null) return;
            var entity = gm.EntityService.Create(data, amount);
            int got = gm.InventoryService.Pickup(entity, root.GetEntity());
            if (got > 0) LogSuccess($"+{got}x {data.keyName} → {root.gameObject.name}");
            else LogError($"Inventory đầy.");
        });

        AddCommand("exp", "exp <amount> [target] — Cộng EXP cho player hoặc EntityRoot", args =>
        {
            if (args.Length < 1) { LogError("exp <amount> [target]"); return; }
            var gm = GM(); if (gm == null) return;

            int amount = ParseInt(args[0], 0);
            if (amount <= 0) { LogError("amount phải > 0."); return; }

            EntityRuntime target = null;
            if (args.Length >= 2)
            {
                var root = FindEntityRoot(args[1]);
                target = root != null ? root.GetEntity() : null;
            }
            else
            {
                var player = FindAnyObjectByType<PlayerControler>();
                target = player != null ? player.GetComponent<EntityRoot>()?.GetEntity() : null;
            }

            if (target == null) { LogError("Không tìm thấy target nhận EXP."); return; }

            gm.ProgressionService.GrantExp(target, amount, ExpSourceType.Debug);
            LogSuccess($"{target.entityData?.keyName ?? target.id}: Lv {target.stats.Get(StatType.Level):0}, EXP {target.stats.Get(StatType.Exp):0}/{target.stats.Get(StatType.MaxExp):0}");
        });
        AddCommand("NextDay", "Tiến tới ngày tiếp theo", _ =>
        {
            var tm = FindAnyObjectByType<TimeManager>();
            if (tm != null)
            {
                tm.SkipToNextDay();
                Log($"Sang ngày mới: {tm.CurrentState}");
            }
            else
            {
                var gm = GM(); if (gm == null) return;
                gm.EventBus.Publish(new NextDayEventPublish());
                Log("NextDayEventPublish đã được phát (no TimeManager).");
            }
        });

        // ── water <x> <y> — Tưới nước tại cell ──
        AddCommand("water", "water <x> <y> — Tưới nước tại cell (x,y)", args =>
        {
            if (args.Length < 2) { LogError("water <x> <y>"); return; }
            var gm = GM(); if (gm == null) return;
            if (!int.TryParse(args[0], out int x) || !int.TryParse(args[1], out int y))
            { LogError("Tọa độ không hợp lệ."); return; }

            var tracker = gm.WateredTileTracker;
            if (tracker == null) { LogError("WateredTileTracker null!"); return; }

            var cell = new Vector2Int(x, y);
            tracker.SetWatered(cell);
            LogSuccess($"Đã tưới cell ({x},{y}). IsWatered={tracker.IsWatered(cell)}");
        });

        // ── hoe <x> <y> — Cuốc đất tại cell ──
        AddCommand("hoe", "hoe <x> <y> — Cuốc đất tại cell (x,y)", args =>
        {
            if (args.Length < 2) { LogError("hoe <x> <y>"); return; }
            var gm = GM(); if (gm == null) return;
            if (!int.TryParse(args[0], out int x) || !int.TryParse(args[1], out int y))
            { LogError("Tọa độ không hợp lệ."); return; }

            var ws = gm.WorldService;
            var tileData = gm.TileData;
            if (tileData?.plowedTile == null) { LogError("plowedTile null!"); return; }

            var cell = new Vector2Int(x, y);
            ws.SetGround(cell, tileData.plowedTile);
            LogSuccess($"Đã cuốc đất tại ({x},{y})");
        });

        // ── sleep — Ngủ: restore stamina/HP + NextDay ──
        AddCommand("sleep", "sleep — Ngủ: restore stamina/HP + sang ngày mới", _ =>
        {
            var gm = GM(); if (gm == null) return;
            var player = FindAnyObjectByType<PlayerControler>();
            var playerEntity = player?.GetComponent<EntityRoot>()?.GetEntity();
            if (playerEntity == null) { LogError("Player entity null!"); return; }

            float maxSta = playerEntity.stats.Get(StatType.MaxStamina);
            float maxHp = playerEntity.stats.Get(StatType.MaxHp);
            if (maxSta > 0) playerEntity.stats.Set(StatType.Stamina, maxSta);
            if (maxHp > 0) playerEntity.stats.Set(StatType.Hp, maxHp);

            gm.TimeManager?.SkipToNextDay();
            LogSuccess($"Đã ngủ! Stamina={maxSta}, HP={maxHp}. Sang ngày mới.");
        });

        // ── inspect <x> <y> — Kiểm tra state tại cell ──
        AddCommand("inspect", "inspect <x> <y> — Kiểm tra state tại cell (x,y)", args =>
        {
            if (args.Length < 2) { LogError("inspect <x> <y>"); return; }
            var gm = GM(); if (gm == null) return;
            if (!int.TryParse(args[0], out int x) || !int.TryParse(args[1], out int y))
            { LogError("Tọa độ không hợp lệ."); return; }

            var cell = new Vector2Int(x, y);
            var tracker = gm.WateredTileTracker;
            var ws = gm.WorldService;

            bool watered = tracker?.IsWatered(cell) ?? false;
            bool hasPlant = ws?.HasBlockerAt(cell, EntityLayer.Plant) ?? false;
            bool hasGround = ws?.HasBlockerAt(cell, EntityLayer.Ground) ?? false;
            var groundTile = ws?.GetGround(cell);

            Log($"Cell ({x},{y}): watered={watered}, hasPlant={hasPlant}, hasGround={hasGround}, groundTile={groundTile?.name ?? "null"}");

            // Find plant entity at this cell
            foreach (var root in FindObjectsOfType<EntityRoot>())
            {
                var entity = root.GetEntity();
                if (entity == null) continue;
                var stage = entity.GetModule<StageRuntime>();
                if (stage == null) continue;

                var pos = root.gameObject.transform.position;
                var entityCell3 = GridSystem.WorldToCell(pos);
                var entityCell = new Vector2Int(entityCell3.x, entityCell3.y);
                if (entityCell == cell)
                {
                    Log($"  Plant: {root.gameObject.name} | stage={stage.currentStageIndex} | daysWithoutWater={stage.DaysWithoutWater} | canHarvest={stage.CanHarvest}");
                }
            }
        });

        // ── refill — Lấy nước đầy cho WateringCan ──
        AddCommand("refill", "refill — Lấy nước đầy cho WateringCan đang cầm", _ =>
        {
            var player = FindAnyObjectByType<PlayerControler>();
            var playerEntity = player?.GetComponent<EntityRoot>()?.GetEntity();
            if (playerEntity == null) { LogError("Player null!"); return; }

            var hotbar = playerEntity.GetModules<InventoryRuntime>().Find(i => i.Type == InventoryType.Hotbar);
            var item = hotbar?.SelectedEntity;
            if (item == null) { LogError("Không có item trên tay!"); return; }

            var wateringCan = item.GetModule<WateringCanRuntime>();
            if (wateringCan == null) { LogError("Item đang cầm không phải WateringCan!"); return; }

            wateringCan.Refill();
            LogSuccess($"Đã lấy nước đầy! {wateringCan.CurrentCharges}/{wateringCan.MaxCharges}");
        });

        AddCommand("SetTime", "SetTime <hour> [minute] — Đặt giờ game", args =>
        {
            var tm = FindAnyObjectByType<TimeManager>();
            if (tm == null) { LogError("TimeManager not found."); return; }
            if (args.Length < 1 || !int.TryParse(args[0], out int h)) { LogError("Usage: SetTime <hour> [minute]"); return; }
            int m = args.Length > 1 && int.TryParse(args[1], out int mm) ? mm : 0;
            tm.SetTime(h, m);
            Log($"Thời gian: {tm.CurrentState}");
        });

        AddCommand("Time", "Hiển thị thời gian hiện tại", _ =>
        {
            var tm = FindAnyObjectByType<TimeManager>();
            if (tm == null) { LogError("TimeManager not found."); return; }
            Log($"{tm.CurrentState}");
        });

        AddCommand("PauseTime", "Dừng thời gian", _ =>
        {
            var tm = FindAnyObjectByType<TimeManager>();
            if (tm == null) { LogError("TimeManager not found."); return; }
            tm.Pause();
            Log("Thời gian đã dừng.");
        });

        AddCommand("ResumeTime", "Tiếp tục thời gian", _ =>
        {
            var tm = FindAnyObjectByType<TimeManager>();
            if (tm == null) { LogError("TimeManager not found."); return; }
            tm.Play();
            Log("Thời gian tiếp tục.");
        });

        // ── containers ──
        AddCommand("containers", "Liệt kê containers có inventory", _ =>
        {
            var roots = FindObjectsOfType<EntityRoot>();
            int c = 0;
            foreach (var r in roots)
            {
                var e = r.GetEntity(); if (e == null) continue;
                var invs = e.GetModules<InventoryRuntime>(); if (invs.Count == 0) continue;
                Log($"  {r.gameObject.name} — [{string.Join(", ", invs.Select(i => i.Type))}]");
                c++;
            }
            if (c == 0) Log("Không có container nào.");
        });

        // ── list [filter] ──
        AddCommand("list", "list [filter] — Liệt kê EntityData", args =>
        {
            var gm = GM(); if (gm == null) return;
            var results = gm.EntityDataRegistry.Search(args.Length > 0 ? args[0] : "", 30);
            if (results.Count == 0) { Log("Không tìm thấy."); return; }
            Log($"── EntityData ({results.Count}) ──");
            foreach (var d in results) Log($"  {d.keyName} (id:{d.id}) [{d.category}] stack:{d.maxStack}");
        });

        // ── spawn <prefab> <x> <z> [entityDataId] ──
        AddCommand("spawn", "spawn <prefab> <x> <z> [entityDataId]", args =>
        {
            if (args.Length < 3) { LogError("spawn <prefab> <x> <z> [entityDataId]"); return; }
            var gm = GM(); if (gm == null) return;

            if (!float.TryParse(args[1], out float x) || !float.TryParse(args[2], out float z))
            { LogError("Tọa độ không hợp lệ."); return; }

            if (!System.Enum.TryParse<ObjectType>(args[0], out var objectType))
            { LogError($"Prefab '{args[0]}' không hợp lệ. Dùng 'objects'."); return; }
            if (!gm.WorldObjects.Has(objectType))
            { LogError($"Prefab '{args[0]}' không tồn tại."); return; }

            string dataId = args.Length >= 4 ? args[3] : args[0];
            var data = FindEntityData(dataId); if (data == null) return;

            gm.EventBus.Publish(new SpawnRequestPublish(new Vector2(x, z), objectType, data));
            LogSuccess($"Spawn '{objectType}' ({data.keyName}) tại ({x},{z})");
        });

        // ── objects ──
        AddCommand("objects", "objects — Liệt kê WorldObject IDs", args =>
        {
            var gm = GM(); if (gm == null) return;
            var results = gm.WorldObjects.GetAllIds();
            if (results.Count() == 0) { Log("Không có."); return; }
            Log($"── WorldObjects ({results.Count()}) ──");
            foreach (var id in results) Log($"  {id}");
        });

        // ── Weather — Hiển thị thời tiết hiện tại ──
        AddCommand("Weather", "Weather — Hiển thị thời tiết hiện tại", _ =>
        {
            var gm = GM(); if (gm == null) return;
            var ws = gm.WeatherSystem;
            if (ws == null) { LogError("WeatherSystem null!"); return; }
            Log($"Thời tiết hiện tại: {ws.CurrentWeather}");
        });

        // ── SetWeather <Sunny|Rainy> ──
        AddCommand("SetWeather", "SetWeather <Sunny|Rainy> — Đặt thời tiết", args =>
        {
            if (args.Length < 1) { LogError("SetWeather <Sunny|Rainy>"); return; }
            var gm = GM(); if (gm == null) return;
            var ws = gm.WeatherSystem;
            if (ws == null) { LogError("WeatherSystem null!"); return; }
            if (!System.Enum.TryParse(args[0], true, out WeatherType wt))
            { LogError($"Thời tiết '{args[0]}' không hợp lệ. Dùng: Sunny, Rainy"); return; }
            ws.SetWeather(wt);
            LogSuccess($"Thời tiết đã đặt: {wt}");
        });

        // ── waterall — Tưới toàn bộ ô đất đã cày ──
        AddCommand("waterall", "waterall — Tưới toàn bộ ô đất đã cày (plowed) trong scene hiện tại", _ =>
        {
            var gm = GM(); if (gm == null) return;

            var tracker = gm.WateredTileTracker;
            if (tracker == null) { LogError("WateredTileTracker null!"); return; }

            int before = tracker.GetWateredCount();
            tracker.WaterAllPlowedCells();
            int after = tracker.GetWateredCount();

            LogSuccess($"Đã tưới toàn bộ ô đất đã cày! ({before} → {after} ô được tưới)");
        });

        // ── watercount — Đếm số ô đang được tưới ──
        AddCommand("watercount", "watercount — Hiển thị số ô đang được tưới hôm nay", _ =>
        {
            var gm = GM(); if (gm == null) return;

            var tracker = gm.WateredTileTracker;
            if (tracker == null) { LogError("WateredTileTracker null!"); return; }

            Log($"Số ô đang được tưới: {tracker.GetWateredCount()}");
        });
    }

    #endregion

    #region Input / Submit

    private void Submit()
    {
        var text = inputField.text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        Log($"> {text}");
        history.Add(text);
        historyIndex = history.Count;

        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var cmd = parts[0].ToLowerInvariant();
        var args = parts.Length > 1 ? parts.Skip(1).ToArray() : Array.Empty<string>();

        if (commands.TryGetValue(cmd, out var action))
            try { action(args); } catch (Exception ex) { LogError(ex.Message); }
        else
            LogError($"'{cmd}' không tồn tại. Gõ 'help'.");

        inputField.text = "";
        inputField.ActivateInputField();
        HideSuggestions();
    }

    private void OnInputChanged(string text)
    {
        if (string.IsNullOrEmpty(text)) { HideSuggestions(); return; }
        UpdateSuggestions(text);
    }

    #endregion

    #region Autocomplete

    private void UpdateSuggestions(string input)
    {
        currentSuggestions.Clear();
        selectedSuggestion = -1;
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var gm = GameManager.Instance;
        int max = suggestionTexts.Count;

        if (parts.Length <= 1)
        {
            // Gợi ý command
            var q = parts.Length > 0 ? parts[0].ToLowerInvariant() : "";
            currentSuggestions.AddRange(commands.Keys.Where(k => k.StartsWith(q)).Take(max));
        }
        else
        {
            var cmd = parts[0].ToLowerInvariant();
            if (cmd == "give" && parts.Length == 2)
                AddContainerSuggestions(parts[1], "give", max);
            else if (cmd == "give" && parts.Length >= 3)
                AddEntityDataSuggestions(parts[2], $"give {parts[1]}", max);
            else if (cmd == "set" && parts.Length == 2)
                AddEntityRootSuggestions(parts[1], "set", max);
            else if (cmd == "set" && parts.Length == 3)
                AddStatSuggestions(parts[2], $"set {parts[1]}", max);
            else if (cmd == "exp" && parts.Length == 3)
                AddEntityRootSuggestions(parts[2], $"exp {parts[1]}", max);
            else if (cmd == "list")
                AddEntityDataSuggestions(parts[1], "list", max);
            else if (cmd == "spawn" || cmd == "objects")
                AddWorldObjectSuggestions(parts[1], cmd, max);
        }

        ShowSuggestions();
    }

    private void AddContainerSuggestions(string query, string prefix, int max)
    {
        var q = query.ToLowerInvariant();
        foreach (var r in FindObjectsOfType<EntityRoot>())
        {
            if (currentSuggestions.Count >= max) break;
            var e = r.GetEntity(); if (e == null) continue;
            if (e.GetModules<InventoryRuntime>().Count == 0) continue;
            if (r.gameObject.name.ToLowerInvariant().Contains(q))
                currentSuggestions.Add($"{prefix} {r.gameObject.name}");
        }
    }

    private void AddEntityRootSuggestions(string query, string prefix, int max)
    {
        var q = query.ToLowerInvariant();
        foreach (var r in FindObjectsOfType<EntityRoot>())
        {
            if (currentSuggestions.Count >= max) break;
            var e = r.GetEntity();
            if (e == null) continue;

            if (r.gameObject.name.ToLowerInvariant().Contains(q)
                || (!string.IsNullOrWhiteSpace(e.id) && e.id.ToLowerInvariant().Contains(q))
                || (!string.IsNullOrWhiteSpace(e.entityData?.id) && e.entityData.id.ToLowerInvariant().Contains(q))
                || (!string.IsNullOrWhiteSpace(e.entityData?.keyName) && e.entityData.keyName.ToLowerInvariant().Contains(q)))
            {
                currentSuggestions.Add($"{prefix} {r.gameObject.name}");
            }
        }
    }

    private void AddStatSuggestions(string query, string prefix, int max)
    {
        var q = query.ToLowerInvariant();
        foreach (var name in System.Enum.GetNames(typeof(StatType)))
        {
            if (currentSuggestions.Count >= max) break;
            if (name.ToLowerInvariant().StartsWith(q))
                currentSuggestions.Add($"{prefix} {name}");
        }
    }

    private void AddEntityDataSuggestions(string query, string prefix, int max)
    {
        var gm = GameManager.Instance; if (gm == null) return;
        foreach (var d in gm.EntityDataRegistry.Search(query, max))
            currentSuggestions.Add($"{prefix} {d.keyName}");
    }

    private void AddWorldObjectSuggestions(string query, string prefix, int max)
    {
        var gm = GameManager.Instance; if (gm == null) return;
        foreach (var id in gm.WorldObjects.GetAllIds().Take(max))
            currentSuggestions.Add($"{prefix} {id}");
    }

    private void ShowSuggestions()
    {
        suggestionsPanel.SetActive(currentSuggestions.Count > 0);
        for (int i = 0; i < suggestionTexts.Count; i++)
        {
            if (i < currentSuggestions.Count)
            {
                suggestionTexts[i].gameObject.SetActive(true);
                suggestionTexts[i].text = currentSuggestions[i];
                suggestionTexts[i].color = Color.white;
            }
            else suggestionTexts[i].gameObject.SetActive(false);
        }
    }

    private void HideSuggestions()
    {
        currentSuggestions.Clear();
        selectedSuggestion = -1;
        suggestionsPanel.SetActive(false);
    }

    private void NavSuggestion(int dir)
    {
        if (currentSuggestions.Count == 0) return;
        if (selectedSuggestion >= 0 && selectedSuggestion < suggestionTexts.Count)
            suggestionTexts[selectedSuggestion].color = Color.white;

        selectedSuggestion += dir;
        if (selectedSuggestion < 0) selectedSuggestion = currentSuggestions.Count - 1;
        if (selectedSuggestion >= currentSuggestions.Count) selectedSuggestion = 0;
        suggestionTexts[selectedSuggestion].color = colHighlight;
    }

    private void ApplySuggestion()
    {
        if (selectedSuggestion < 0) selectedSuggestion = 0;
        if (selectedSuggestion >= currentSuggestions.Count) return;
        inputField.text = currentSuggestions[selectedSuggestion];
        inputField.caretPosition = inputField.text.Length;
        inputField.ActivateInputField();
        HideSuggestions();
    }

    private void NavHistory(int dir)
    {
        if (history.Count == 0) return;
        historyIndex = Mathf.Clamp(historyIndex + dir, 0, history.Count);
        inputField.text = historyIndex < history.Count ? history[historyIndex] : "";
        inputField.caretPosition = inputField.text.Length;
    }

    #endregion

    #region Helpers

    private void CacheSuggestionTexts()
    {
        suggestionTexts.Clear();
        if (suggestionsPanel == null) return;
        foreach (Transform child in suggestionsPanel.transform)
        {
            var tmp = child.GetComponent<TextMeshProUGUI>();
            if (tmp != null) suggestionTexts.Add(tmp);
        }
    }

    private void Append(string msg, string hex)
    {
        logLines.Add($"<color=#{hex}>{msg}</color>");
        while (logLines.Count > maxLogLines) logLines.RemoveAt(0);
        logText.text = string.Join("\n", logLines);
        if (scrollRect != null) { Canvas.ForceUpdateCanvases(); scrollRect.verticalNormalizedPosition = 0f; }
    }

    private GameManager GM()
    {
        var gm = GameManager.Instance;
        if (gm == null) LogError("GameManager chưa sẵn sàng.");
        return gm;
    }

    private EntityData FindEntityData(string idOrKey)
    {
        var gm = GameManager.Instance; if (gm == null) return null;
        var data = gm.EntityDataRegistry.Find(idOrKey);
        if (data != null) return data;

        var sug = gm.EntityDataRegistry.Search(idOrKey, 3);
        if (sug.Count > 0)
            LogError($"'{idOrKey}' không tìm thấy. Bạn có ý: {string.Join(", ", sug.Select(s => s.keyName))}?");
        else
            LogError($"EntityData '{idOrKey}' không tồn tại.");
        return null;
    }

    private EntityRoot FindContainer(string name)
    {
        var q = name.ToLowerInvariant();
        var roots = FindObjectsOfType<EntityRoot>();

        // Exact match
        foreach (var r in roots)
            if (r.gameObject.name.ToLowerInvariant() == q && HasInventory(r)) return r;
        // Substring
        foreach (var r in roots)
            if (r.gameObject.name.ToLowerInvariant().Contains(q) && HasInventory(r)) return r;
        return null;
    }

    private EntityRoot FindEntityRoot(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return null;

        var q = query.ToLowerInvariant();
        var roots = FindObjectsOfType<EntityRoot>();

        foreach (var root in roots)
        {
            var entity = root.GetEntity();
            if (entity == null) continue;

            if (root.gameObject.name.ToLowerInvariant() == q
                || (!string.IsNullOrWhiteSpace(entity.id) && entity.id.ToLowerInvariant() == q)
                || (!string.IsNullOrWhiteSpace(entity.entityData?.id) && entity.entityData.id.ToLowerInvariant() == q)
                || (!string.IsNullOrWhiteSpace(entity.entityData?.keyName) && entity.entityData.keyName.ToLowerInvariant() == q))
            {
                return root;
            }
        }

        foreach (var root in roots)
        {
            var entity = root.GetEntity();
            if (entity == null) continue;

            if (root.gameObject.name.ToLowerInvariant().Contains(q)
                || (!string.IsNullOrWhiteSpace(entity.id) && entity.id.ToLowerInvariant().Contains(q))
                || (!string.IsNullOrWhiteSpace(entity.entityData?.id) && entity.entityData.id.ToLowerInvariant().Contains(q))
                || (!string.IsNullOrWhiteSpace(entity.entityData?.keyName) && entity.entityData.keyName.ToLowerInvariant().Contains(q)))
            {
                return root;
            }
        }

        return null;
    }

    private static bool HasInventory(EntityRoot r)
    {
        var e = r.GetEntity();
        return e != null && e.GetModules<InventoryRuntime>().Count > 0;
    }

    private static int ParseInt(string s, int fallback)
    {
        return int.TryParse(s, out int v) ? v : fallback;
    }

    private static bool TryParseFloat(string text, out float value)
    {
        if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            return true;

        return float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
    }

    /// <summary>Tự tìm refs nếu chưa gán từ Inspector.</summary>
    private void AutoFindRefs()
    {
        var canvas = GetComponentInChildren<Canvas>(true);
        if (canvas == null)
        {
            var debugCanvas = GameObject.Find("Canvas_Debug");
            if (debugCanvas != null)
                canvas = debugCanvas.GetComponent<Canvas>();
        }

        if (canvas == null) return;

        if (panel == null)
            panel = canvas.transform.Find("Panel")?.gameObject;
        if (panel == null) return;

        if (inputField == null)
            inputField = panel.GetComponentInChildren<TMP_InputField>(true);
        if (scrollRect == null)
            scrollRect = panel.GetComponentInChildren<ScrollRect>(true);
        if (logText == null)
        {
            var content = panel.transform.Find("ScrollArea/Viewport/Content");
            if (content != null) logText = content.GetComponentInChildren<TextMeshProUGUI>(true);
        }
        if (suggestionsPanel == null)
            suggestionsPanel = panel.transform.Find("SuggestionsPanel")?.gameObject;
    }

    #endregion
}
