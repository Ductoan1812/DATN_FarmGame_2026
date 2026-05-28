using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class EntityDataWorkbenchWindow : EditorWindow
{
    private enum TemplateKind
    {
        Custom,
        PlantSeedFlow,
        TreeGrowthFlow,
        ResourceNode,
        Enemy,
        Npc,
        Animal,
        ToolItem,
        WeaponItem,
        BuildingItem
    }

    private enum IssueSeverity
    {
        Error,
        Warning,
        Info
    }

    private sealed class EntityAssetRecord
    {
        public EntityData data;
        public string path;
        public TemplateKind guessedTemplate;
    }

    private sealed class ValidationIssue
    {
        public IssueSeverity severity;
        public string message;
        public string actionLabel;
        public Action<EntityData> fix;
    }

    private sealed class TemplateConfig
    {
        public TemplateKind kind;
        public string label;
        public string description;
        public string defaultFolder;
        public string defaultPrefix;
        public ItemCategory category;
        public int maxStack;
        public EntityLayer occupyLayer;
        public Type[] requiredModules;
        public Type[] preferredModuleOrder;
        public KeyValuePair<StatType, float>[] requiredStats;
    }

    private static readonly Dictionary<TemplateKind, TemplateConfig> TemplateMap = BuildTemplateMap();
    private static readonly string[] ScanRoots =
    {
        "Assets/Project/ScriptableObjects",
        "Assets/Project/Resources/Data"
    };

    private readonly List<EntityAssetRecord> records = new();
    private readonly List<ValidationIssue> currentIssues = new();
    private Vector2 summaryScroll;
    private Vector2 listScroll;
    private Vector2 issueScroll;
    private string searchText = string.Empty;
    private EntityData selectedEntity;
    private TemplateKind selectedTemplate = TemplateKind.Custom;
    private TemplateKind createTemplate = TemplateKind.PlantSeedFlow;
    private string createName = string.Empty;
    private string createFolderOverride = string.Empty;

    [MenuItem("Tools/DATN/Workbench/Entity Data Workbench")]
    public static void OpenWindow()
    {
        var window = GetWindow<EntityDataWorkbenchWindow>("Entity Data Workbench");
        window.minSize = new Vector2(1080f, 720f);
        window.RefreshRecords();
    }

    private void OnEnable()
    {
        RefreshRecords();
    }

    private void OnGUI()
    {
        DrawToolbar();

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawLeftPane();
            DrawRightPane();
        }
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f)))
            {
                RefreshRecords();
            }

            GUILayout.Space(8f);
            searchText = GUILayout.TextField(searchText, EditorStyles.toolbarSearchField, GUILayout.MinWidth(220f));

            GUILayout.FlexibleSpace();
            GUILayout.Label($"Total EntityData: {records.Count}", EditorStyles.miniBoldLabel);
        }
    }

    private void DrawLeftPane()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(position.width * 0.42f)))
        {
            DrawSummarySection();
            GUILayout.Space(8f);
            DrawEntityListSection();
        }
    }

    private void DrawRightPane()
    {
        using (new EditorGUILayout.VerticalScope())
        {
            DrawInspectorSection();
            GUILayout.Space(8f);
            DrawCreateSection();
        }
    }

    private void DrawSummarySection()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Project Summary", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Dashboard này chỉ quét EntityData trong Assets/Project.", EditorStyles.wordWrappedMiniLabel);

            summaryScroll = EditorGUILayout.BeginScrollView(summaryScroll, GUILayout.Height(180f));

            foreach (var group in records
                         .GroupBy(r => GetGroupName(r.path))
                         .OrderByDescending(g => g.Count()))
            {
                EditorGUILayout.LabelField($"{group.Key}: {group.Count()}", EditorStyles.label);
            }

            GUILayout.Space(6f);
            EditorGUILayout.LabelField("Current seed_* assets", EditorStyles.boldLabel);
            foreach (var path in records
                         .Select(r => r.path)
                         .Where(p => p.IndexOf("seed_", StringComparison.OrdinalIgnoreCase) >= 0)
                         .OrderBy(p => p))
            {
                EditorGUILayout.LabelField(path.Replace('\\', '/'), EditorStyles.miniLabel);
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawEntityListSection()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("EntityData List", EditorStyles.boldLabel);

            listScroll = EditorGUILayout.BeginScrollView(listScroll);
            foreach (var record in GetFilteredRecords())
            {
                DrawEntityListRow(record);
            }
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawEntityListRow(EntityAssetRecord record)
    {
        string displayName = record.data != null ? record.data.name : "(Missing)";
        string moduleSummary = record.data?.modules == null
            ? string.Empty
            : string.Join(", ", record.data.modules.Where(m => m != null).Select(m => m.GetType().Name));

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(displayName, EditorStyles.miniButtonLeft))
                {
                    selectedEntity = record.data;
                    selectedTemplate = record.guessedTemplate;
                    RebuildIssues();
                    GUI.FocusControl(null);
                }

                GUILayout.Label(record.guessedTemplate.ToString(), EditorStyles.miniLabel, GUILayout.Width(120f));
                if (GUILayout.Button("Ping", EditorStyles.miniButtonRight, GUILayout.Width(48f)) && record.data != null)
                {
                    EditorGUIUtility.PingObject(record.data);
                }
            }

            EditorGUILayout.LabelField(record.path.Replace('\\', '/'), EditorStyles.wordWrappedMiniLabel);
            if (!string.IsNullOrEmpty(moduleSummary))
                EditorGUILayout.LabelField(moduleSummary, EditorStyles.wordWrappedMiniLabel);
        }
    }

    private void DrawInspectorSection()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Inspect / Validate", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            selectedEntity = (EntityData)EditorGUILayout.ObjectField("EntityData", selectedEntity, typeof(EntityData), false);
            if (EditorGUI.EndChangeCheck())
            {
                selectedTemplate = GuessTemplate(selectedEntity, GetAssetPathSafe(selectedEntity));
                RebuildIssues();
            }

            using (new EditorGUI.DisabledScope(selectedEntity == null))
            {
                selectedTemplate = (TemplateKind)EditorGUILayout.EnumPopup("Template", selectedTemplate);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Analyze"))
                        RebuildIssues();

                    if (GUILayout.Button("Guess Template"))
                    {
                        selectedTemplate = GuessTemplate(selectedEntity, GetAssetPathSafe(selectedEntity));
                        RebuildIssues();
                    }

                    if (GUILayout.Button("Apply Template Defaults"))
                    {
                        ApplyTemplateDefaults(selectedEntity, selectedTemplate, overwriteCoreFields: false);
                        RebuildIssues();
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Apply All Fixes"))
                    {
                        ApplyAllFixes();
                    }

                    if (GUILayout.Button("Ping Asset"))
                    {
                        EditorGUIUtility.PingObject(selectedEntity);
                    }
                }
            }

            GUILayout.Space(8f);
            DrawSelectedEntitySnapshot();
            GUILayout.Space(8f);
            DrawIssueList();
        }
    }

    private void DrawSelectedEntitySnapshot()
    {
        if (selectedEntity == null)
        {
            EditorGUILayout.HelpBox("Chọn một EntityData để xem module, stat và lỗi thiếu dữ liệu.", MessageType.Info);
            return;
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Snapshot", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Path: {GetAssetPathSafe(selectedEntity).Replace('\\', '/')}", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField($"Category: {selectedEntity.category} | MaxStack: {selectedEntity.maxStack}", EditorStyles.miniLabel);

            var statSummary = selectedEntity.baseStats?.baseStats == null
                ? "(none)"
                : string.Join(", ", selectedEntity.baseStats.baseStats.Select(s => $"{s.statType}={s.value}"));
            EditorGUILayout.LabelField($"Stats: {statSummary}", EditorStyles.wordWrappedMiniLabel);

            var moduleSummary = selectedEntity.modules == null || selectedEntity.modules.Count == 0
                ? "(none)"
                : string.Join(", ", selectedEntity.modules.Where(m => m != null).Select(m => m.GetType().Name));
            EditorGUILayout.LabelField($"Modules: {moduleSummary}", EditorStyles.wordWrappedMiniLabel);
        }
    }

    private void DrawIssueList()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Detected Issues", EditorStyles.boldLabel);

            if (selectedEntity == null)
            {
                EditorGUILayout.HelpBox("Chưa có EntityData được chọn.", MessageType.None);
                return;
            }

            if (currentIssues.Count == 0)
            {
                EditorGUILayout.HelpBox("Không phát hiện thiếu core field/module/stat theo template đang chọn.", MessageType.Info);
                return;
            }

            issueScroll = EditorGUILayout.BeginScrollView(issueScroll, GUILayout.Height(260f));
            foreach (var issue in currentIssues)
            {
                DrawIssueRow(issue);
            }
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawIssueRow(ValidationIssue issue)
    {
        MessageType messageType = issue.severity switch
        {
            IssueSeverity.Error => MessageType.Error,
            IssueSeverity.Warning => MessageType.Warning,
            _ => MessageType.Info
        };

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.HelpBox(issue.message, messageType);
            if (issue.fix != null && !string.IsNullOrWhiteSpace(issue.actionLabel))
            {
                if (GUILayout.Button(issue.actionLabel))
                {
                    issue.fix(selectedEntity);
                    RebuildIssues();
                }
            }
        }
    }

    private void DrawCreateSection()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Quick Create", EditorStyles.boldLabel);

            createTemplate = (TemplateKind)EditorGUILayout.EnumPopup("Template", createTemplate);
            createName = EditorGUILayout.TextField("Name", createName);

            TemplateConfig config = GetTemplateConfig(createTemplate);
            string defaultFolder = config.defaultFolder;
            createFolderOverride = EditorGUILayout.TextField("Folder", string.IsNullOrWhiteSpace(createFolderOverride) ? defaultFolder : createFolderOverride);

            if (config != null)
                EditorGUILayout.LabelField(config.description, EditorStyles.wordWrappedMiniLabel);

            if (GUILayout.Button("Create EntityData"))
            {
                CreateEntityDataAsset();
            }
        }
    }

    private void RefreshRecords()
    {
        records.Clear();

        foreach (string guid in AssetDatabase.FindAssets("t:EntityData", ScanRoots))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var data = AssetDatabase.LoadAssetAtPath<EntityData>(path);
            if (data == null)
                continue;

            records.Add(new EntityAssetRecord
            {
                data = data,
                path = path,
                guessedTemplate = GuessTemplate(data, path)
            });
        }

        records.Sort((a, b) => string.Compare(a.path, b.path, StringComparison.OrdinalIgnoreCase));

        if (selectedEntity == null && records.Count > 0)
        {
            selectedEntity = records[0].data;
            selectedTemplate = records[0].guessedTemplate;
        }

        RebuildIssues();
        Repaint();
    }

    private IEnumerable<EntityAssetRecord> GetFilteredRecords()
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return records;

        return records.Where(r =>
            r.path.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
            (r.data != null && r.data.name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
            (!string.IsNullOrWhiteSpace(r.data?.id) && r.data.id.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
            (!string.IsNullOrWhiteSpace(r.data?.keyName) && r.data.keyName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0));
    }

    private void RebuildIssues()
    {
        currentIssues.Clear();
        if (selectedEntity == null)
            return;

        currentIssues.AddRange(ValidateEntity(selectedEntity, selectedTemplate, GetAssetPathSafe(selectedEntity)));
    }

    private void ApplyAllFixes()
    {
        if (selectedEntity == null)
            return;

        foreach (var issue in currentIssues.Where(i => i.fix != null).ToList())
            issue.fix(selectedEntity);

        RebuildIssues();
    }

    private void CreateEntityDataAsset()
    {
        if (createTemplate == TemplateKind.Custom)
        {
            EditorUtility.DisplayDialog("Entity Data Workbench", "Template Custom không có đủ default để tạo nhanh. Hãy chọn một template cụ thể.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(createName))
        {
            EditorUtility.DisplayDialog("Entity Data Workbench", "Bạn cần nhập Name trước khi tạo asset.", "OK");
            return;
        }

        TemplateConfig config = GetTemplateConfig(createTemplate);
        string folder = string.IsNullOrWhiteSpace(createFolderOverride) ? config.defaultFolder : createFolderOverride;
        EnsureFolderExists(folder);

        string baseName = SanitizeFileToken(createName);
        string finalName = baseName.StartsWith(config.defaultPrefix, StringComparison.OrdinalIgnoreCase)
            ? baseName
            : config.defaultPrefix + baseName;
        string assetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folder, finalName + ".asset").Replace('\\', '/'));

        var data = CreateInstance<EntityData>();
        data.name = finalName;
        data.modules = new List<IModuleData>();
        data.baseStats = new StatsData { baseStats = new List<StatEntry>() };

        ApplyTemplateDefaults(data, createTemplate, overwriteCoreFields: true);
        SetIdFieldsFromName(data, finalName);

        AssetDatabase.CreateAsset(data, assetPath);
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        selectedEntity = data;
        selectedTemplate = createTemplate;
        Selection.activeObject = data;
        EditorGUIUtility.PingObject(data);
        RefreshRecords();
    }

    private static Dictionary<TemplateKind, TemplateConfig> BuildTemplateMap()
    {
        return new Dictionary<TemplateKind, TemplateConfig>
        {
            {
                TemplateKind.Custom,
                new TemplateConfig
                {
                    kind = TemplateKind.Custom,
                    label = "Custom",
                    description = "Chỉ kiểm tra field cơ bản, không ép module/template.",
                    defaultFolder = "Assets/Project/ScriptableObjects",
                    defaultPrefix = string.Empty,
                    category = ItemCategory.None,
                    maxStack = 1,
                    occupyLayer = EntityLayer.Ground,
                    requiredModules = Array.Empty<Type>(),
                    preferredModuleOrder = Array.Empty<Type>(),
                    requiredStats = Array.Empty<KeyValuePair<StatType, float>>()
                }
            },
            {
                TemplateKind.PlantSeedFlow,
                new TemplateConfig
                {
                    kind = TemplateKind.PlantSeedFlow,
                    label = "Plant Seed Flow",
                    description = "Flow seed = plant: một EntityData duy nhất có Placement, Stage, Harvest, Health, Drop, Mortal; đặt xuống rồi phát triển, nhận damage và thu hoạch.",
                    defaultFolder = "Assets/Project/ScriptableObjects/WorldObjects/Plants",
                    defaultPrefix = "seed_",
                    category = ItemCategory.Seed,
                    maxStack = 999,
                    occupyLayer = EntityLayer.Plant,
                    requiredModules = new[]
                    {
                        typeof(PlacementModule),
                        typeof(StageModule),
                        typeof(HarvestModule),
                        typeof(HealthModule),
                        typeof(DropModule),
                        typeof(MortalModule)
                    },
                    preferredModuleOrder = new[]
                    {
                        typeof(PlacementModule),
                        typeof(StageModule),
                        typeof(HarvestModule),
                        typeof(HealthModule),
                        typeof(DropModule),
                        typeof(ExpRewardModule),
                        typeof(MortalModule),
                        typeof(QualityModule)
                    },
                    requiredStats = new[]
                    {
                        new KeyValuePair<StatType, float>(StatType.MaxHp, 10f),
                        new KeyValuePair<StatType, float>(StatType.Hp, 10f)
                    }
                }
            },
            {
                TemplateKind.TreeGrowthFlow,
                new TemplateConfig
                {
                    kind = TemplateKind.TreeGrowthFlow,
                    label = "Tree Growth Flow",
                    description = "Flow cây gỗ kiểu repo hiện tại: asset seed_* lớn thành resource node bằng ResourceGrowth.",
                    defaultFolder = "Assets/Project/ScriptableObjects/WorldObjects/Resources",
                    defaultPrefix = "seed_",
                    category = ItemCategory.Placeable,
                    maxStack = 999,
                    occupyLayer = EntityLayer.Furniture,
                    requiredModules = new[] { typeof(HealthModule), typeof(HarvestModule), typeof(DropModule), typeof(ExpRewardModule), typeof(MortalModule), typeof(ResourceGrowthModule) },
                    preferredModuleOrder = new[]
                    {
                        typeof(HealthModule),
                        typeof(HarvestModule),
                        typeof(DropModule),
                        typeof(ExpRewardModule),
                        typeof(MortalModule),
                        typeof(ResourceGrowthModule)
                    },
                    requiredStats = new[]
                    {
                        new KeyValuePair<StatType, float>(StatType.MaxHp, 25f),
                        new KeyValuePair<StatType, float>(StatType.Hp, 25f)
                    }
                }
            },
            {
                TemplateKind.ResourceNode,
                new TemplateConfig
                {
                    kind = TemplateKind.ResourceNode,
                    label = "Resource Node",
                    description = "Đá, quặng, node khai thác có HP, Harvest, Drop, Mortal, ExpReward.",
                    defaultFolder = "Assets/Project/ScriptableObjects/WorldObjects/Resources",
                    defaultPrefix = string.Empty,
                    category = ItemCategory.Placeable,
                    maxStack = 1,
                    occupyLayer = EntityLayer.Furniture,
                    requiredModules = new[] { typeof(HealthModule), typeof(HarvestModule), typeof(DropModule), typeof(MortalModule), typeof(ExpRewardModule) },
                    preferredModuleOrder = new[]
                    {
                        typeof(HealthModule),
                        typeof(HarvestModule),
                        typeof(DropModule),
                        typeof(ExpRewardModule),
                        typeof(MortalModule)
                    },
                    requiredStats = new[]
                    {
                        new KeyValuePair<StatType, float>(StatType.MaxHp, 10f),
                        new KeyValuePair<StatType, float>(StatType.Hp, 10f)
                    }
                }
            },
            {
                TemplateKind.Enemy,
                new TemplateConfig
                {
                    kind = TemplateKind.Enemy,
                    label = "Enemy",
                    description = "Enemy runtime: HP, Attack, Speed, Drop, Respawn.",
                    defaultFolder = "Assets/Project/ScriptableObjects/WorldObjects/Enemies",
                    defaultPrefix = "Enemy_",
                    category = ItemCategory.None,
                    maxStack = 1,
                    occupyLayer = EntityLayer.Ground,
                    requiredModules = new[] { typeof(HealthModule), typeof(DropModule), typeof(RespawnModule) },
                    preferredModuleOrder = new[] { typeof(HealthModule), typeof(DropModule), typeof(RespawnModule) },
                    requiredStats = new[]
                    {
                        new KeyValuePair<StatType, float>(StatType.MaxHp, 20f),
                        new KeyValuePair<StatType, float>(StatType.Hp, 20f),
                        new KeyValuePair<StatType, float>(StatType.Attack, 3f),
                        new KeyValuePair<StatType, float>(StatType.Speed, 1.8f)
                    }
                }
            },
            {
                TemplateKind.Npc,
                new TemplateConfig
                {
                    kind = TemplateKind.Npc,
                    label = "NPC",
                    description = "NPC cơ bản với Dialogue + Inventory. Có thể thêm Shop hoặc Quest sau đó.",
                    defaultFolder = "Assets/Project/ScriptableObjects/Characters/NPCs",
                    defaultPrefix = "NPC_",
                    category = ItemCategory.None,
                    maxStack = 1,
                    occupyLayer = EntityLayer.Ground,
                    requiredModules = new[] { typeof(DialogueModule), typeof(InventoryModule) },
                    preferredModuleOrder = new[] { typeof(DialogueModule), typeof(InventoryModule) },
                    requiredStats = new[]
                    {
                        new KeyValuePair<StatType, float>(StatType.Money, 0f)
                    }
                }
            },
            {
                TemplateKind.Animal,
                new TemplateConfig
                {
                    kind = TemplateKind.Animal,
                    label = "Animal",
                    description = "Animal world entity với AnimalModule, Health và Mortal.",
                    defaultFolder = "Assets/Project/ScriptableObjects/WorldObjects/Animals",
                    defaultPrefix = "Animal_",
                    category = ItemCategory.None,
                    maxStack = 1,
                    occupyLayer = EntityLayer.Ground,
                    requiredModules = new[] { typeof(AnimalModule), typeof(HealthModule), typeof(MortalModule) },
                    preferredModuleOrder = new[] { typeof(AnimalModule), typeof(HealthModule), typeof(MortalModule) },
                    requiredStats = new[]
                    {
                        new KeyValuePair<StatType, float>(StatType.MaxHp, 10f),
                        new KeyValuePair<StatType, float>(StatType.Hp, 10f)
                    }
                }
            },
            {
                TemplateKind.ToolItem,
                new TemplateConfig
                {
                    kind = TemplateKind.ToolItem,
                    label = "Tool Item",
                    description = "Farm tool item dùng ToolModule, lấy stat từ baseStats.",
                    defaultFolder = "Assets/Project/ScriptableObjects/Items/Tools",
                    defaultPrefix = string.Empty,
                    category = ItemCategory.Tool,
                    maxStack = 1,
                    occupyLayer = EntityLayer.Ground,
                    requiredModules = new[] { typeof(ToolModule) },
                    preferredModuleOrder = new[] { typeof(ToolModule) },
                    requiredStats = new[]
                    {
                        new KeyValuePair<StatType, float>(StatType.Range, 1f),
                        new KeyValuePair<StatType, float>(StatType.CoolDown, 0.5f)
                    }
                }
            },
            {
                TemplateKind.WeaponItem,
                new TemplateConfig
                {
                    kind = TemplateKind.WeaponItem,
                    label = "Weapon Item",
                    description = "Weapon item dùng WeaponModule, có Attack/Range/CoolDown.",
                    defaultFolder = "Assets/Project/ScriptableObjects/Items/Weapons",
                    defaultPrefix = string.Empty,
                    category = ItemCategory.Weapon,
                    maxStack = 1,
                    occupyLayer = EntityLayer.Ground,
                    requiredModules = new[] { typeof(WeaponModule) },
                    preferredModuleOrder = new[] { typeof(WeaponModule) },
                    requiredStats = new[]
                    {
                        new KeyValuePair<StatType, float>(StatType.Attack, 4f),
                        new KeyValuePair<StatType, float>(StatType.Range, 1.35f),
                        new KeyValuePair<StatType, float>(StatType.CoolDown, 0.45f)
                    }
                }
            },
            {
                TemplateKind.BuildingItem,
                new TemplateConfig
                {
                    kind = TemplateKind.BuildingItem,
                    label = "Building Item",
                    description = "Item đặt công trình với BuildingModule.",
                    defaultFolder = "Assets/Project/ScriptableObjects/Items/Buildings",
                    defaultPrefix = "Item_",
                    category = ItemCategory.Placeable,
                    maxStack = 99,
                    occupyLayer = EntityLayer.Ground,
                    requiredModules = new[] { typeof(BuildingModule) },
                    preferredModuleOrder = new[] { typeof(BuildingModule) },
                    requiredStats = Array.Empty<KeyValuePair<StatType, float>>()
                }
            }
        };
    }

    private static TemplateConfig GetTemplateConfig(TemplateKind kind)
    {
        return TemplateMap.TryGetValue(kind, out var config) ? config : TemplateMap[TemplateKind.Custom];
    }

    private static TemplateKind GuessTemplate(EntityData data, string path)
    {
        if (data == null)
            return TemplateKind.Custom;

        if (HasModule<StageModule>(data) && HasModule<PlacementModule>(data))
            return TemplateKind.PlantSeedFlow;
        if (HasModule<ResourceGrowthModule>(data))
            return TemplateKind.TreeGrowthFlow;
        if (HasModule<AnimalModule>(data))
            return TemplateKind.Animal;
        if (HasModule<WeaponModule>(data))
            return TemplateKind.WeaponItem;
        if (HasModule<ToolModule>(data))
            return TemplateKind.ToolItem;
        if (HasModule<BuildingModule>(data))
            return TemplateKind.BuildingItem;
        if (HasModule<DialogueModule>(data) || HasModule<ShopModule>(data) || HasModule<QuestModule>(data))
            return TemplateKind.Npc;
        if (path.IndexOf("/Enemies/", StringComparison.OrdinalIgnoreCase) >= 0)
            return TemplateKind.Enemy;
        if (HasModule<RespawnModule>(data) && HasModule<HealthModule>(data))
            return TemplateKind.Enemy;
        if (HasModule<HarvestModule>(data) && HasModule<HealthModule>(data))
            return TemplateKind.ResourceNode;

        return TemplateKind.Custom;
    }

    private List<ValidationIssue> ValidateEntity(EntityData data, TemplateKind template, string path)
    {
        var issues = new List<ValidationIssue>();
        TemplateConfig config = GetTemplateConfig(template);

        if (string.IsNullOrWhiteSpace(data.id))
        {
            issues.Add(new ValidationIssue
            {
                severity = IssueSeverity.Error,
                message = "Thiếu `id`.",
                actionLabel = "Generate id",
                fix = entity => MutateEntity(entity, "Generate id", () => SetIdFieldsFromName(entity, entity.name))
            });
        }

        if (string.IsNullOrWhiteSpace(data.keyName))
        {
            issues.Add(new ValidationIssue
            {
                severity = IssueSeverity.Error,
                message = "Thiếu `keyName`.",
                actionLabel = "Generate keyName",
                fix = entity => MutateEntity(entity, "Generate keyName", () => SetKeyFieldsFromName(entity, entity.name))
            });
        }

        if (string.IsNullOrWhiteSpace(data.descKey))
        {
            issues.Add(new ValidationIssue
            {
                severity = IssueSeverity.Warning,
                message = "Thiếu `descKey`.",
                actionLabel = "Generate descKey",
                fix = entity => MutateEntity(entity, "Generate descKey", () => SetKeyFieldsFromName(entity, entity.name))
            });
        }

        if (data.baseStats == null)
        {
            issues.Add(new ValidationIssue
            {
                severity = IssueSeverity.Error,
                message = "Thiếu `baseStats`.",
                actionLabel = "Create baseStats",
                fix = entity => MutateEntity(entity, "Create baseStats", () => entity.baseStats = new StatsData { baseStats = new List<StatEntry>() })
            });
        }
        else if (data.baseStats.baseStats == null)
        {
            issues.Add(new ValidationIssue
            {
                severity = IssueSeverity.Error,
                message = "Thiếu danh sách `baseStats.baseStats`.",
                actionLabel = "Create stat list",
                fix = entity => MutateEntity(entity, "Create stat list", () => entity.baseStats.baseStats = new List<StatEntry>())
            });
        }

        if (data.modules == null)
        {
            issues.Add(new ValidationIssue
            {
                severity = IssueSeverity.Error,
                message = "Thiếu danh sách `modules`.",
                actionLabel = "Create module list",
                fix = entity => MutateEntity(entity, "Create module list", () => entity.modules = new List<IModuleData>())
            });
        }

        if (data.maxStack <= 0)
        {
            issues.Add(new ValidationIssue
            {
                severity = IssueSeverity.Warning,
                message = $"`maxStack` đang là {data.maxStack}.",
                actionLabel = $"Set maxStack = {Mathf.Max(1, config.maxStack)}",
                fix = entity => MutateEntity(entity, "Set maxStack", () => entity.maxStack = Mathf.Max(1, config.maxStack))
            });
        }

        if (data.icon == null)
        {
            issues.Add(new ValidationIssue
            {
                severity = IssueSeverity.Info,
                message = "Chưa gán `icon`.",
                actionLabel = null,
                fix = null
            });
        }

        if (template != TemplateKind.Custom && data.category != config.category)
        {
            issues.Add(new ValidationIssue
            {
                severity = IssueSeverity.Warning,
                message = $"Category hiện tại là `{data.category}`, template `{config.label}` khuyến nghị `{config.category}`.",
                actionLabel = $"Set category = {config.category}",
                fix = entity => MutateEntity(entity, "Set category", () => entity.category = config.category)
            });
        }

        foreach (var pair in config.requiredStats)
        {
            StatType statType = pair.Key;
            float defaultValue = pair.Value;
            if (HasStat(data, statType))
                continue;

            issues.Add(new ValidationIssue
            {
                severity = IssueSeverity.Error,
                message = $"Thiếu stat `{statType}`.",
                actionLabel = $"Add {statType}",
                fix = entity => MutateEntity(entity, $"Add stat {statType}", () => EnsureStat(entity, statType, defaultValue))
            });
        }

        foreach (Type requiredModule in config.requiredModules)
        {
            Type localType = requiredModule;
            if (HasModule(data, localType))
                continue;

            issues.Add(new ValidationIssue
            {
                severity = IssueSeverity.Error,
                message = $"Thiếu module `{localType.Name}`.",
                actionLabel = $"Add {localType.Name}",
                fix = entity => MutateEntity(entity, $"Add module {localType.Name}", () => EnsureModule(entity, localType))
            });
        }

        if (config.preferredModuleOrder != null &&
            config.preferredModuleOrder.Length > 0 &&
            HasModuleOrderMismatch(data, config.preferredModuleOrder))
        {
            issues.Add(new ValidationIssue
            {
                severity = IssueSeverity.Warning,
                message = $"Thứ tự module chưa đúng template `{config.label}`. Khuyến nghị: {FormatModuleOrder(config.preferredModuleOrder)}.",
                actionLabel = "Reorder modules",
                fix = entity => MutateEntity(entity, "Reorder modules", () => ReorderModules(entity, config.preferredModuleOrder))
            });
        }

        ValidateSpecialCases(data, template, path, issues);
        return issues;
    }

    private void ValidateSpecialCases(EntityData data, TemplateKind template, string path, List<ValidationIssue> issues)
    {
        if (HasModule<StageModule>(data))
        {
            var stage = GetModule<StageModule>(data);
            if (stage.stages == null || stage.stages.Length == 0)
            {
                issues.Add(new ValidationIssue
                {
                    severity = IssueSeverity.Error,
                    message = "StageModule chưa có `stages`.",
                    actionLabel = "Add placeholder stage",
                    fix = entity => MutateEntity(entity, "Create placeholder stage", () =>
                    {
                        var module = GetModule<StageModule>(entity);
                        if (module != null)
                            module.stages = new[] { new GrowthStage { daysToGrow = 1, canHarvest = true } };
                    })
                });
            }
        }

        if (HasModule<ResourceGrowthModule>(data))
        {
            var growth = GetModule<ResourceGrowthModule>(data);
            if (growth.stages == null || growth.stages.Length == 0)
            {
                issues.Add(new ValidationIssue
                {
                    severity = IssueSeverity.Error,
                    message = "ResourceGrowthModule chưa có `stages`.",
                    actionLabel = "Add placeholder stage",
                    fix = entity => MutateEntity(entity, "Create resource growth stage", () =>
                    {
                        var module = GetModule<ResourceGrowthModule>(entity);
                        if (module != null)
                            module.stages = new[] { new GrowthStage { daysToGrow = 1, canHarvest = true } };
                    })
                });
            }
        }

        if (HasModule<DropModule>(data))
        {
            int dropModuleCount = data.modules?.Count(module => module is DropModule) ?? 0;
            if (dropModuleCount > 1)
            {
                issues.Add(new ValidationIssue
                {
                    severity = IssueSeverity.Warning,
                    message = $"EntityData đang có {dropModuleCount} DropModule. Flow harvest hiện tại chỉ dùng module drop đầu tiên.",
                    actionLabel = null,
                    fix = null
                });
            }

            var drop = GetModule<DropModule>(data);
            if (drop.harvestDrops == null)
            {
                issues.Add(new ValidationIssue
                {
                    severity = IssueSeverity.Warning,
                    message = "DropModule có `harvestDrops = null`.",
                    actionLabel = "Initialize drop list",
                    fix = entity => MutateEntity(entity, "Initialize drop list", () =>
                    {
                        var module = GetModule<DropModule>(entity);
                        if (module != null)
                            module.harvestDrops = Array.Empty<DropEntry>();
                    })
                });
            }
            else if (template == TemplateKind.PlantSeedFlow && drop.harvestDrops.Length == 0)
            {
                issues.Add(new ValidationIssue
                {
                    severity = IssueSeverity.Info,
                    message = "DropModule đang có `harvestDrops` rỗng. Plant vẫn lớn được nhưng harvest sẽ không cho item nào.",
                    actionLabel = null,
                    fix = null
                });
            }
        }

        if (HasModule<PlacementModule>(data))
        {
            var placement = GetModule<PlacementModule>(data);
            if (placement.objectTypeToSpawn == ObjectType.EntityDrop && placement.placedEntityData == null)
            {
                issues.Add(new ValidationIssue
                {
                    severity = IssueSeverity.Warning,
                    message = "PlacementModule vẫn để mặc định `EntityDrop` và chưa gán `placedEntityData`.",
                    actionLabel = template == TemplateKind.PlantSeedFlow ? "Set objectTypeToSpawn = Plant01" : null,
                    fix = template == TemplateKind.PlantSeedFlow
                        ? entity => MutateEntity(entity, "Set placement object type", () =>
                        {
                            var module = GetModule<PlacementModule>(entity);
                            if (module != null)
                            {
                                module.objectTypeToSpawn = ObjectType.Plant01;
                                module.centerTile = true;
                                if (string.IsNullOrWhiteSpace(module.animTrigger) || string.Equals(module.animTrigger, "Sow", StringComparison.OrdinalIgnoreCase))
                                    module.animTrigger = "PutDown";
                            }
                        })
                        : null
                });
            }

            if (template == TemplateKind.PlantSeedFlow &&
                !string.Equals(placement.animTrigger, "PutDown", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new ValidationIssue
                {
                    severity = IssueSeverity.Warning,
                    message = $"PlacementModule đang dùng animTrigger `{placement.animTrigger}`. Flow plant hiện tại khuyến nghị `PutDown`.",
                    actionLabel = "Set animTrigger = PutDown",
                    fix = entity => MutateEntity(entity, "Set plant anim trigger", () =>
                    {
                        var module = GetModule<PlacementModule>(entity);
                        if (module != null)
                            module.animTrigger = "PutDown";
                    })
                });
            }
        }

        if (HasModule<HarvestModule>(data))
        {
            int harvestModuleCount = data.modules?.Count(module => module is HarvestModule) ?? 0;
            if (harvestModuleCount > 1)
            {
                issues.Add(new ValidationIssue
                {
                    severity = IssueSeverity.Warning,
                    message = $"EntityData đang có {harvestModuleCount} HarvestModule. Flow hiện tại chỉ dùng HarvestModule đầu tiên, hãy gộp nhiều cách harvest vào cùng một module.",
                    actionLabel = null,
                    fix = null
                });
            }

            var harvest = GetModule<HarvestModule>(data);
            if (!harvest.AllowsHandHarvest && !harvest.HasAnyToolHarvest)
            {
                string actionLabel = null;
                ToolType harvestTool = ToolType.None;
                if (template == TemplateKind.PlantSeedFlow)
                {
                    actionLabel = "Set harvestTool = Scythe";
                    harvestTool = ToolType.Scythe;
                }
                else if (template == TemplateKind.TreeGrowthFlow || template == TemplateKind.ResourceNode)
                {
                    actionLabel = template == TemplateKind.ResourceNode ? "Set harvestTool = Pickaxe" : "Set harvestTool = Axe";
                    harvestTool = template == TemplateKind.ResourceNode ? ToolType.Pickaxe : ToolType.Axe;
                }

                issues.Add(new ValidationIssue
                {
                    severity = IssueSeverity.Warning,
                    message = "HarvestModule chưa cấu hình cách thu hoạch nào.",
                    actionLabel = actionLabel,
                    fix = harvestTool != ToolType.None
                        ? entity => MutateEntity(entity, "Set harvest tool", () =>
                        {
                            var module = GetModule<HarvestModule>(entity);
                            if (module != null)
                                module.harvestTool = harvestTool;
                        })
                        : null
                });
            }
        }

        if (HasModule<ToolModule>(data))
        {
            var tool = GetModule<ToolModule>(data);
            if (tool.toolType == ToolType.None)
            {
                issues.Add(new ValidationIssue
                {
                    severity = IssueSeverity.Warning,
                    message = "ToolModule chưa chọn `toolType`.",
                    actionLabel = null,
                    fix = null
                });
            }
        }

        if (HasModule<DialogueModule>(data) && GetModule<DialogueModule>(data).graph == null)
        {
            issues.Add(new ValidationIssue
            {
                severity = IssueSeverity.Warning,
                message = "DialogueModule chưa gán `graph`.",
                actionLabel = null,
                fix = null
            });
        }

        if (HasModule<InventoryModule>(data) && GetModule<InventoryModule>(data).size <= 0)
        {
            issues.Add(new ValidationIssue
            {
                severity = IssueSeverity.Warning,
                message = "InventoryModule có `size <= 0`.",
                actionLabel = "Set size = 20",
                fix = entity => MutateEntity(entity, "Set inventory size", () =>
                {
                    var module = GetModule<InventoryModule>(entity);
                    if (module != null)
                        module.size = 20;
                })
            });
        }

        if (HasModule<AnimalModule>(data))
        {
            var animal = GetModule<AnimalModule>(data);
            if (animal.feedItem == null)
                issues.Add(new ValidationIssue { severity = IssueSeverity.Warning, message = "AnimalModule chưa gán `feedItem`." });
            if (animal.productItem == null)
                issues.Add(new ValidationIssue { severity = IssueSeverity.Warning, message = "AnimalModule chưa gán `productItem`." });
        }

        if (HasModule<BuildingModule>(data) && GetModule<BuildingModule>(data).buildingEntity == null)
        {
            issues.Add(new ValidationIssue
            {
                severity = IssueSeverity.Warning,
                message = "BuildingModule chưa gán `buildingEntity`.",
                actionLabel = null,
                fix = null
            });
        }

        if (HasModule<StageModule>(data) && HasModule<ResourceGrowthModule>(data))
        {
            issues.Add(new ValidationIssue
            {
                severity = IssueSeverity.Warning,
                message = "EntityData đang có cả StageModule và ResourceGrowthModule. Hãy chắc đây là chủ đích.",
                actionLabel = null,
                fix = null
            });
        }

        if (path.IndexOf("seed_", StringComparison.OrdinalIgnoreCase) >= 0 && !HasModule<PlacementModule>(data) && template != TemplateKind.TreeGrowthFlow)
        {
            issues.Add(new ValidationIssue
            {
                severity = IssueSeverity.Info,
                message = "Tên asset theo pattern `seed_*` nhưng không có PlacementModule.",
                actionLabel = null,
                fix = null
            });
        }
    }

    private void ApplyTemplateDefaults(EntityData data, TemplateKind template, bool overwriteCoreFields)
    {
        if (data == null)
            return;

        TemplateConfig config = GetTemplateConfig(template);
        MutateEntity(data, $"Apply template {config.label}", () =>
        {
            data.baseStats ??= new StatsData();
            data.baseStats.baseStats ??= new List<StatEntry>();
            data.modules ??= new List<IModuleData>();

            if (overwriteCoreFields || string.IsNullOrWhiteSpace(data.id))
                SetIdFieldsFromName(data, data.name);

            if (overwriteCoreFields || string.IsNullOrWhiteSpace(data.keyName) || string.IsNullOrWhiteSpace(data.descKey))
                SetKeyFieldsFromName(data, data.name);

            if (overwriteCoreFields || data.maxStack <= 0)
                data.maxStack = Mathf.Max(1, config.maxStack);

            if (overwriteCoreFields || data.category == ItemCategory.None)
                data.category = config.category;

            data.placementRule = new PlacementRule
            {
                occupyLayer = config.occupyLayer,
                requireTags = data.placementRule.requireTags,
                provideTags = data.placementRule.provideTags,
                blockLayers = data.placementRule.blockLayers ?? Array.Empty<EntityLayer>()
            };

            foreach (Type moduleType in config.requiredModules)
                EnsureModule(data, moduleType);

            foreach (var stat in config.requiredStats)
                EnsureStat(data, stat.Key, stat.Value);

            ApplyModuleDefaults(data, template);
            if (config.preferredModuleOrder != null && config.preferredModuleOrder.Length > 0)
                ReorderModules(data, config.preferredModuleOrder);
        });
    }

    private static void ApplyModuleDefaults(EntityData data, TemplateKind template)
    {
        if (template == TemplateKind.PlantSeedFlow)
        {
            var placement = GetModule<PlacementModule>(data);
            if (placement != null)
            {
                placement.objectTypeToSpawn = placement.objectTypeToSpawn == ObjectType.EntityDrop ? ObjectType.Plant01 : placement.objectTypeToSpawn;
                placement.centerTile = true;
                if (string.IsNullOrWhiteSpace(placement.animTrigger) || string.Equals(placement.animTrigger, "Sow", StringComparison.OrdinalIgnoreCase))
                    placement.animTrigger = "PutDown";
            }

            var stage = GetModule<StageModule>(data);
            if (stage != null && (stage.stages == null || stage.stages.Length == 0))
                stage.stages = new[] { new GrowthStage { daysToGrow = 1, canHarvest = true } };

            var harvest = GetModule<HarvestModule>(data);
            if (harvest != null && harvest.harvestTool == ToolType.None)
                harvest.harvestTool = ToolType.Scythe;

            var drop = GetModule<DropModule>(data);
            if (drop != null && drop.harvestDrops == null)
                drop.harvestDrops = Array.Empty<DropEntry>();
        }

        if (template == TemplateKind.TreeGrowthFlow)
        {
            var harvest = GetModule<HarvestModule>(data);
            if (harvest != null && harvest.harvestTool == ToolType.None)
                harvest.harvestTool = ToolType.Axe;

            var growth = GetModule<ResourceGrowthModule>(data);
            if (growth != null && (growth.stages == null || growth.stages.Length == 0))
                growth.stages = new[] { new GrowthStage { daysToGrow = 1, canHarvest = true } };

            var drop = GetModule<DropModule>(data);
            if (drop != null && drop.harvestDrops == null)
                drop.harvestDrops = Array.Empty<DropEntry>();
        }

        if (template == TemplateKind.ResourceNode)
        {
            var harvest = GetModule<HarvestModule>(data);
            if (harvest != null && harvest.harvestTool == ToolType.None)
                harvest.harvestTool = ToolType.Pickaxe;
        }

        if (template == TemplateKind.Npc)
        {
            var inventory = GetModule<InventoryModule>(data);
            if (inventory != null && inventory.size <= 0)
                inventory.size = 20;
        }
    }

    private static void SetIdFieldsFromName(EntityData data, string rawName)
    {
        string token = SanitizeKeyToken(rawName);
        data.id = token;
        if (string.IsNullOrWhiteSpace(data.keyName) || data.keyName.EndsWith("_name", StringComparison.OrdinalIgnoreCase))
            data.keyName = token.ToLowerInvariant() + "_name";
        if (string.IsNullOrWhiteSpace(data.descKey) || data.descKey.EndsWith("_desc", StringComparison.OrdinalIgnoreCase))
            data.descKey = token.ToLowerInvariant() + "_desc";
    }

    private static void SetKeyFieldsFromName(EntityData data, string rawName)
    {
        string token = SanitizeKeyToken(rawName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(data.keyName))
            data.keyName = token + "_name";
        if (string.IsNullOrWhiteSpace(data.descKey))
            data.descKey = token + "_desc";
    }

    private static void EnsureStat(EntityData data, StatType statType, float value)
    {
        data.baseStats ??= new StatsData();
        data.baseStats.baseStats ??= new List<StatEntry>();

        if (data.baseStats.baseStats.Any(s => s != null && s.statType == statType))
            return;

        data.baseStats.baseStats.Add(new StatEntry
        {
            statType = statType,
            value = value
        });
    }

    private static bool HasStat(EntityData data, StatType statType)
    {
        return data?.baseStats?.baseStats != null && data.baseStats.baseStats.Any(s => s != null && s.statType == statType);
    }

    private static void EnsureModule(EntityData data, Type moduleType)
    {
        if (data.modules == null)
            data.modules = new List<IModuleData>();

        if (HasModule(data, moduleType))
            return;

        if (Activator.CreateInstance(moduleType) is IModuleData module)
            data.modules.Add(module);
    }

    private static bool HasModuleOrderMismatch(EntityData data, IReadOnlyList<Type> preferredOrder)
    {
        if (data?.modules == null || preferredOrder == null || preferredOrder.Count == 0)
            return false;

        int previousIndex = -1;
        foreach (Type moduleType in preferredOrder)
        {
            int currentIndex = data.modules.FindIndex(module => module != null && moduleType.IsInstanceOfType(module));
            if (currentIndex < 0)
                continue;

            if (currentIndex < previousIndex)
                return true;

            previousIndex = currentIndex;
        }

        return false;
    }

    private static void ReorderModules(EntityData data, IReadOnlyList<Type> preferredOrder)
    {
        if (data?.modules == null || preferredOrder == null || preferredOrder.Count == 0)
            return;

        var remaining = data.modules.Where(module => module != null).ToList();
        var reordered = new List<IModuleData>(remaining.Count);

        foreach (Type moduleType in preferredOrder)
        {
            var matches = remaining.Where(module => moduleType.IsInstanceOfType(module)).ToList();
            if (matches.Count == 0)
                continue;

            reordered.AddRange(matches);
            remaining.RemoveAll(module => module != null && moduleType.IsInstanceOfType(module));
        }

        reordered.AddRange(remaining);
        data.modules = reordered;
    }

    private static string FormatModuleOrder(IEnumerable<Type> moduleOrder)
    {
        return string.Join(" -> ", moduleOrder.Select(type => type.Name));
    }

    private static bool HasModule<T>(EntityData data) where T : IModuleData
    {
        return GetModule<T>(data) != null;
    }

    private static bool HasModule(EntityData data, Type moduleType)
    {
        return data?.modules != null && data.modules.Any(m => m != null && moduleType.IsInstanceOfType(m));
    }

    private static T GetModule<T>(EntityData data) where T : IModuleData
    {
        return data?.modules?.FirstOrDefault(m => m is T) as T;
    }

    private static string GetGroupName(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "Unknown";

        string normalized = path.Replace('\\', '/');
        const string root = "Assets/Project/ScriptableObjects/";
        if (normalized.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            string rest = normalized.Substring(root.Length);
            string[] parts = rest.Split('/');
            if (parts.Length >= 2)
                return $"{parts[0]}/{parts[1]}";
            if (parts.Length == 1)
                return parts[0];
        }

        const string resourcesRoot = "Assets/Project/Resources/Data/";
        if (normalized.StartsWith(resourcesRoot, StringComparison.OrdinalIgnoreCase))
        {
            string rest = normalized.Substring(resourcesRoot.Length);
            string[] parts = rest.Split('/');
            return parts.Length > 0 ? $"Resources/{parts[0]}" : "Resources";
        }

        return "Other";
    }

    private static string GetAssetPathSafe(UnityEngine.Object obj)
    {
        return obj == null ? string.Empty : AssetDatabase.GetAssetPath(obj);
    }

    private static void EnsureFolderExists(string folderPath)
    {
        string normalized = folderPath.Replace('\\', '/');
        string[] parts = normalized.Split('/');
        if (parts.Length < 2 || parts[0] != "Assets")
            throw new InvalidOperationException($"Folder phải nằm dưới Assets. Nhận được: {folderPath}");

        string current = "Assets";
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static string SanitizeFileToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "NewEntity";

        string result = value.Trim().Replace(' ', '_');
        foreach (char invalidChar in Path.GetInvalidFileNameChars())
            result = result.Replace(invalidChar.ToString(), string.Empty);

        return result;
    }

    private static string SanitizeKeyToken(string value)
    {
        string token = SanitizeFileToken(value);
        token = token.Replace("-", "_");
        while (token.IndexOf("__", StringComparison.Ordinal) >= 0)
            token = token.Replace("__", "_");
        return token.Trim('_');
    }

    private static void MutateEntity(EntityData data, string undoLabel, Action mutation)
    {
        if (data == null || mutation == null)
            return;

        Undo.RecordObject(data, undoLabel);
        mutation();
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
    }
}
