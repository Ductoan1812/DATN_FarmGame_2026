using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.HeroEditor4D.Common.Scripts.Collections;
using Assets.HeroEditor4D.Common.Scripts.Data;
using Assets.HeroEditor4D.Common.Scripts.Enums;
using UnityEditor;
using UnityEngine;

public class EntityDataWorkbenchWindow : EditorWindow
{
    private enum TemplateKind
    {
        Custom,
        SeedItem,
        CropPlant,
        WoodTreePlant,
        FruitTreePlant,
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
    private Vector2 inspectorScroll;
    private Vector2 rightPaneScroll;
    private Editor entityEditor;
    private string searchText = string.Empty;
    private bool useCategoryFilter = false;
    private ItemCategory filterCategory;
    private EntityData selectedEntity;
    private TemplateKind selectedTemplate = TemplateKind.Custom;
    private TemplateKind createTemplate = TemplateKind.SeedItem;
    private string createName = string.Empty;
    private string createFolderOverride = string.Empty;
    private SpriteCollection appearanceSpriteCollection;
    private string appearanceSpriteFilter = string.Empty;

    [MenuItem("Tools/DATN/Workbench/Entity Data Workbench")]
    public static void OpenWindow()
    {
        var window = GetWindow<EntityDataWorkbenchWindow>("Entity Data Workbench");
        window.minSize = new Vector2(1080f, 720f);
        window.RefreshRecords();
    }

    private void OnEnable()
    {
        EnsureAppearanceSpriteCollection();
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

            GUILayout.Space(8f);
            useCategoryFilter = GUILayout.Toggle(useCategoryFilter, "Filter Category", EditorStyles.toolbarButton, GUILayout.Width(100f));
            if (useCategoryFilter)
            {
                filterCategory = (ItemCategory)EditorGUILayout.EnumPopup(filterCategory, GUILayout.Width(120f));
            }

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
            rightPaneScroll = EditorGUILayout.BeginScrollView(rightPaneScroll);
            try
            {
                DrawInspectorSection();
                GUILayout.Space(8f);
                DrawCreateSection();
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }
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
            
            if (selectedEntity != null)
            {
                EditorGUILayout.LabelField("Live Editor (Inspector)", EditorStyles.boldLabel);
                
                if (entityEditor == null || entityEditor.target != selectedEntity)
                {
                    Editor.CreateCachedEditor(selectedEntity, null, ref entityEditor);
                }

                if (entityEditor != null)
                {
                    inspectorScroll = EditorGUILayout.BeginScrollView(inspectorScroll, GUILayout.Height(350f));
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        entityEditor.OnInspectorGUI();
                    }
                    EditorGUILayout.EndScrollView();
                }

                GUILayout.Space(8f);
                DrawAppearanceSpritePicker();
            }

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
            EditorGUILayout.LabelField("View / Snapshot", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawSpritePreviewBox("Icon", selectedEntity.icon, selectedEntity.icon != null ? selectedEntity.icon.name : "(empty)");

                var appearance = GetModule<AppearanceModule>(selectedEntity);
                Sprite appearanceSprite = ResolveAppearanceSprite(appearance);
                string appearanceLabel = appearance != null && !string.IsNullOrWhiteSpace(appearance.spriteId)
                    ? appearance.spriteId
                    : "(empty)";
                DrawSpritePreviewBox("Appearance", appearanceSprite, appearanceLabel);

                using (new EditorGUILayout.VerticalScope())
                {
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
        }
    }

    private void DrawAppearanceSpritePicker()
    {
        var appearance = GetModule<AppearanceModule>(selectedEntity);
        if (appearance == null)
            return;

        EnsureAppearanceSpriteCollection();
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Appearance Sprite Picker", EditorStyles.boldLabel);
            appearanceSpriteCollection = (SpriteCollection)EditorGUILayout.ObjectField("Sprite Collection", appearanceSpriteCollection, typeof(SpriteCollection), false);

            EditorGUI.BeginChangeCheck();
            var selectedPart = (EquipmentPart)EditorGUILayout.EnumPopup("Equipment Part", appearance.equipmentPart);
            if (EditorGUI.EndChangeCheck())
            {
                MutateEntity(selectedEntity, "Set equipment part", () =>
                {
                    var module = GetModule<AppearanceModule>(selectedEntity);
                    if (module != null)
                        module.equipmentPart = selectedPart;
                });
                RebuildIssues();
                appearance = GetModule<AppearanceModule>(selectedEntity);
            }

            using (new EditorGUI.DisabledScope(appearanceSpriteCollection == null))
            {
                appearanceSpriteFilter = EditorGUILayout.TextField("Filter", appearanceSpriteFilter);

                var activeItems = GetAppearanceSpritesForPart(appearanceSpriteCollection, appearance.equipmentPart);
                if (!string.IsNullOrWhiteSpace(appearanceSpriteFilter))
                {
                    activeItems = activeItems
                        .Where(item => MatchesSpriteFilter(item, appearanceSpriteFilter))
                        .ToList();
                }

                var options = new List<string> { "<none>" };
                options.AddRange(activeItems.Select(FormatSpriteOption));

                int currentIndex = 0;
                int itemIndex = activeItems.FindIndex(item => string.Equals(item.Id, appearance.spriteId, StringComparison.Ordinal));
                if (itemIndex >= 0)
                    currentIndex = itemIndex + 1;

                EditorGUI.BeginChangeCheck();
                int selectedIndex = EditorGUILayout.Popup("Sprite Id", currentIndex, options.ToArray());
                if (EditorGUI.EndChangeCheck())
                {
                    string selectedSpriteId = selectedIndex <= 0 ? string.Empty : activeItems[selectedIndex - 1].Id;
                    MutateEntity(selectedEntity, "Set appearance sprite id", () =>
                    {
                        var module = GetModule<AppearanceModule>(selectedEntity);
                        if (module != null)
                            module.spriteId = selectedSpriteId;
                    });
                    RebuildIssues();
                    appearance = GetModule<AppearanceModule>(selectedEntity);
                }
            }

            EditorGUI.BeginChangeCheck();
            string manualSpriteId = EditorGUILayout.TextField("Manual Sprite Id", appearance.spriteId);
            if (EditorGUI.EndChangeCheck())
            {
                MutateEntity(selectedEntity, "Set manual sprite id", () =>
                {
                    var module = GetModule<AppearanceModule>(selectedEntity);
                    if (module != null)
                        module.spriteId = manualSpriteId;
                });
                RebuildIssues();
            }

            Sprite previewSprite = ResolveAppearanceSprite(GetModule<AppearanceModule>(selectedEntity));
            DrawSpritePreviewBox("Selected Appearance", previewSprite, previewSprite != null ? previewSprite.name : "(not resolved)");
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
        IEnumerable<EntityAssetRecord> result = records;

        if (useCategoryFilter)
        {
            result = result.Where(r => r.data != null && r.data.category == filterCategory);
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            result = result.Where(r =>
                r.path.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                (r.data != null && r.data.name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (!string.IsNullOrWhiteSpace(r.data?.id) && r.data.id.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (!string.IsNullOrWhiteSpace(r.data?.keyName) && r.data.keyName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        return result;
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
                TemplateKind.SeedItem,
                new TemplateConfig
                {
                    kind = TemplateKind.SeedItem,
                    label = "Seed Item",
                    description = "Vật phẩm Hạt giống nằm trong túi đồ. Cần PlacementModule để gieo trồng ra các loại cây (Crop/Tree) trên bản đồ.",
                    defaultFolder = "Assets/Project/ScriptableObjects/Items/Crops/Seeds",
                    defaultPrefix = "Seed_",
                    category = ItemCategory.Seed,
                    maxStack = 99,
                    occupyLayer = EntityLayer.Ground, // Item doesn't occupy world layer before placing
                    requiredModules = new[] { typeof(PlacementModule) },
                    preferredModuleOrder = new[] { typeof(PlacementModule) },
                    requiredStats = Array.Empty<KeyValuePair<StatType, float>>()
                }
            },
            {
                TemplateKind.CropPlant,
                new TemplateConfig
                {
                    kind = TemplateKind.CropPlant,
                    label = "Crop Plant",
                    description = "Thực thể cây nông nghiệp mọc trên đất (World Object). Không nằm trong túi đồ.",
                    defaultFolder = "Assets/Project/ScriptableObjects/WorldObjects/Plants",
                    defaultPrefix = "CropPlant_",
                    category = ItemCategory.Placeable,
                    maxStack = 1,
                    occupyLayer = EntityLayer.Plant,
                    requiredModules = new[]
                    {
                        typeof(StageModule),
                        typeof(HarvestModule),
                        typeof(HealthModule),
                        typeof(DropModule),
                        typeof(MortalModule),
                        typeof(ExpRewardModule),
                        typeof(ResourceHitReactionModule)
                    },
                    preferredModuleOrder = new[]
                    {
                        typeof(StageModule),
                        typeof(HarvestModule),
                        typeof(DropModule),
                        typeof(HealthModule),
                        typeof(ResourceHitReactionModule),
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
                TemplateKind.WoodTreePlant,
                new TemplateConfig
                {
                    kind = TemplateKind.WoodTreePlant,
                    label = "Wood Tree Plant",
                    description = "Thực thể cây lấy gỗ mọc trên đất. Không cần tưới nước, dùng ResourceGrowth, chặt bằng Rìu.",
                    defaultFolder = "Assets/Project/ScriptableObjects/WorldObjects/WoodTrees",
                    defaultPrefix = "TreeNode_",
                    category = ItemCategory.Placeable,
                    maxStack = 1,
                    occupyLayer = EntityLayer.Plant,
                    requiredModules = new[]
                    {
                        typeof(ResourceGrowthModule),
                        typeof(HarvestModule),
                        typeof(HealthModule),
                        typeof(DropModule),
                        typeof(MortalModule),
                        typeof(ResourceHitReactionModule)
                    },
                    preferredModuleOrder = new[]
                    {
                        typeof(ResourceGrowthModule),
                        typeof(HarvestModule),
                        typeof(ResourceHitReactionModule),
                        typeof(HealthModule),
                        typeof(DropModule),
                        typeof(MortalModule)
                    },
                    requiredStats = new[]
                    {
                        new KeyValuePair<StatType, float>(StatType.MaxHp, 12f),
                        new KeyValuePair<StatType, float>(StatType.Hp, 12f)
                    }
                }
            },
            {
                TemplateKind.FruitTreePlant,
                new TemplateConfig
                {
                    kind = TemplateKind.FruitTreePlant,
                    label = "Fruit Tree Plant",
                    description = "Thực thể cây ăn quả mọc trên đất. Không cần tưới nước, có giới hạn mùa vụ, thu hoạch quả bằng tay nhiều lần.",
                    defaultFolder = "Assets/Project/ScriptableObjects/WorldObjects/FruitTrees",
                    defaultPrefix = "FruitTree_",
                    category = ItemCategory.Placeable,
                    maxStack = 1,
                    occupyLayer = EntityLayer.Plant,
                    requiredModules = new[]
                    {
                        typeof(StageModule),
                        typeof(SeasonRuleModule),
                        typeof(HarvestModule),
                        typeof(HealthModule),
                        typeof(DropModule)
                    },
                    preferredModuleOrder = new[]
                    {
                        typeof(StageModule),
                        typeof(SeasonRuleModule),
                        typeof(HarvestModule),
                        typeof(HealthModule),
                        typeof(DropModule)
                    },
                    requiredStats = new[]
                    {
                        new KeyValuePair<StatType, float>(StatType.MaxHp, 15f),
                        new KeyValuePair<StatType, float>(StatType.Hp, 15f)
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
                    description = "Farm tool item dùng ToolModule + ToolRequirementModule + AppearanceModule. Hoe/WateringCan không cần Attack; Axe/Pickaxe/Scythe là damage tool nên cần Attack.",
                    defaultFolder = "Assets/Project/ScriptableObjects/Items/Tools",
                    defaultPrefix = string.Empty,
                    category = ItemCategory.Tool,
                    maxStack = 1,
                    occupyLayer = EntityLayer.Ground,
                    requiredModules = new[] { typeof(ToolModule), typeof(ToolRequirementModule), typeof(AppearanceModule) },
                    preferredModuleOrder = new[] { typeof(ToolModule), typeof(ToolRequirementModule), typeof(AppearanceModule) },
                    requiredStats = new[]
                    {
                        new KeyValuePair<StatType, float>(StatType.Stamina, 2f),
                        new KeyValuePair<StatType, float>(StatType.AreaX, 1f),
                        new KeyValuePair<StatType, float>(StatType.AreaY, 1f),
                        new KeyValuePair<StatType, float>(StatType.Range, 1.5f),
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

        if (HasModule<StageModule>(data) && HasModule<SeasonRuleModule>(data))
            return TemplateKind.FruitTreePlant;
        if (HasModule<ResourceGrowthModule>(data))
            return TemplateKind.WoodTreePlant;
        if (HasModule<StageModule>(data))
            return TemplateKind.CropPlant;
        if (HasModule<PlacementModule>(data) && data.category == ItemCategory.Seed)
            return TemplateKind.SeedItem;

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
            else if (template == TemplateKind.CropPlant && drop.harvestDrops.Length == 0)
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
                    actionLabel = template == TemplateKind.SeedItem ? "Set objectTypeToSpawn = Plant01" : null,
                    fix = template == TemplateKind.SeedItem
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

            if (template == TemplateKind.SeedItem &&
                !string.Equals(placement.animTrigger, "PutDown", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new ValidationIssue
                {
                    severity = IssueSeverity.Warning,
                    message = $"PlacementModule đang dùng animTrigger `{placement.animTrigger}`. Flow plant/tree hiện tại khuyến nghị `PutDown`.",
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
                if (template == TemplateKind.CropPlant)
                {
                    actionLabel = "Set harvestTool = Scythe";
                    harvestTool = ToolType.Scythe;
                }
                else if (template == TemplateKind.WoodTreePlant || template == TemplateKind.ResourceNode)
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

            ValidateToolItem(data, tool, issues);
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

        if (path.IndexOf("seed_", StringComparison.OrdinalIgnoreCase) >= 0 && !HasModule<PlacementModule>(data))
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

    private void ValidateToolItem(EntityData data, ToolModule tool, List<ValidationIssue> issues)
    {
        if (data == null || tool == null || issues == null)
            return;

        bool hasConcreteToolType = tool.toolType != ToolType.None;
        if (hasConcreteToolType && string.IsNullOrWhiteSpace(tool.animTrigger))
        {
            issues.Add(new ValidationIssue
            {
                severity = IssueSeverity.Warning,
                message = $"ToolModule `{tool.toolType}` chưa gán `animTrigger`. Runtime sẽ fallback về `{tool.toolType}`.",
                actionLabel = $"Set animTrigger = {tool.toolType}",
                fix = entity => MutateEntity(entity, "Set tool anim trigger", () =>
                {
                    var module = GetModule<ToolModule>(entity);
                    if (module != null)
                        module.animTrigger = module.toolType.ToString();
                })
            });
        }

        if (hasConcreteToolType && IsDamageTool(tool.toolType) && !HasStat(data, StatType.Attack))
        {
            issues.Add(new ValidationIssue
            {
                severity = IssueSeverity.Error,
                message = $"Tool `{tool.toolType}` gây sát thương lên node nên cần stat `Attack`.",
                actionLabel = "Add Attack",
                fix = entity => MutateEntity(entity, "Add tool Attack", () =>
                {
                    var module = GetModule<ToolModule>(entity);
                    EnsureStat(entity, StatType.Attack, DefaultAttackForTool(module));
                })
            });
        }

        var requirement = GetModule<ToolRequirementModule>(data);
        if (hasConcreteToolType && requirement != null &&
            (requirement.requiredToolType != tool.toolType || requirement.minimumToolTier != Mathf.Max(1, tool.toolTier)))
        {
            issues.Add(new ValidationIssue
            {
                severity = IssueSeverity.Warning,
                message = $"ToolRequirementModule đang lệch ToolModule. Hiện yêu cầu `{requirement.requiredToolType}` tier {requirement.minimumToolTier}, tool là `{tool.toolType}` tier {tool.toolTier}.",
                actionLabel = "Sync ToolRequirementModule",
                fix = entity => MutateEntity(entity, "Sync tool requirement", () => SyncToolRequirement(entity))
            });
        }

        var appearance = GetModule<AppearanceModule>(data);
        if (appearance == null)
            return;

        if (string.IsNullOrWhiteSpace(appearance.spriteId))
        {
            issues.Add(new ValidationIssue
            {
                severity = IssueSeverity.Warning,
                message = "AppearanceModule có `spriteId` rỗng. Dùng Appearance Sprite Picker để chọn từ SpriteCollection.",
                actionLabel = null,
                fix = null
            });
        }
        else
        {
            EnsureAppearanceSpriteCollection();
            if (appearanceSpriteCollection != null && ResolveAppearanceSprite(appearance) == null)
            {
                issues.Add(new ValidationIssue
                {
                    severity = IssueSeverity.Warning,
                    message = $"Không tìm thấy spriteId `{appearance.spriteId}` trong SpriteCollection theo EquipmentPart `{appearance.equipmentPart}`.",
                    actionLabel = null,
                    fix = null
                });
            }
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
        if (template == TemplateKind.SeedItem)
        {
            var placement = GetModule<PlacementModule>(data);
            if (placement != null)
            {
                placement.objectTypeToSpawn = placement.objectTypeToSpawn == ObjectType.EntityDrop ? ObjectType.Plant01 : placement.objectTypeToSpawn;
                placement.centerTile = true;
                if (string.IsNullOrWhiteSpace(placement.animTrigger) || string.Equals(placement.animTrigger, "Sow", StringComparison.OrdinalIgnoreCase))
                    placement.animTrigger = "Sow";
            }
        }
        else if (template == TemplateKind.CropPlant)
        {
            var stage = GetModule<StageModule>(data);
            if (stage != null && (stage.stages == null || stage.stages.Length == 0))
                stage.stages = new[] { new GrowthStage { daysToGrow = 1, canHarvest = true } };

            var harvest = GetModule<HarvestModule>(data);
            if (harvest != null && harvest.harvestTool == ToolType.None)
                harvest.harvestTool = ToolType.Scythe;

            var drop = GetModule<DropModule>(data);
            if (drop != null && drop.harvestDrops == null)
                drop.harvestDrops = Array.Empty<DropEntry>();

            var exp = GetModule<ExpRewardModule>(data);
            if (exp != null && exp.rewardExp == 0)
            {
                exp.rewardExp = 10;
                exp.sourceType = ExpSourceType.Harvest;
            }
        }
        else if (template == TemplateKind.WoodTreePlant)
        {
            var growth = GetModule<ResourceGrowthModule>(data);
            if (growth != null && (growth.stages == null || growth.stages.Length == 0))
            {
                growth.stages = new[] {
                    new GrowthStage { daysToGrow = 4, canHarvest = false },
                    new GrowthStage { daysToGrow = 4, canHarvest = false },
                    new GrowthStage { daysToGrow = 4, canHarvest = false },
                    new GrowthStage { daysToGrow = 0, canHarvest = true }
                };
            }

            var harvest = GetModule<HarvestModule>(data);
            if (harvest != null)
            {
                if (harvest.harvestTool == ToolType.None) harvest.harvestTool = ToolType.Axe;
                harvest.harvestCausesDamage = true;
                harvest.destroyOnHarvest = false;
            }
        }
        else if (template == TemplateKind.FruitTreePlant)
        {
            var stage = GetModule<StageModule>(data);
            if (stage != null)
            {
                if (stage.stages == null || stage.stages.Length == 0)
                {
                    stage.stages = new[] {
                        new GrowthStage { daysToGrow = 7, canHarvest = false },
                        new GrowthStage { daysToGrow = 7, canHarvest = false },
                        new GrowthStage { daysToGrow = 7, canHarvest = false },
                        new GrowthStage { daysToGrow = 7, canHarvest = false },
                        new GrowthStage { daysToGrow = 0, canHarvest = true }
                    };
                }
                stage.requiresWater = false;
                stage.harvestGoToStageIndex = stage.harvestGoToStageIndex < 0 ? 3 : stage.harvestGoToStageIndex;
                stage.lastStageLoopToIndex = stage.lastStageLoopToIndex < 0 ? 4 : stage.lastStageLoopToIndex;
                stage.daysToReturnAfterHarvest = stage.daysToReturnAfterHarvest <= 0 ? 1 : stage.daysToReturnAfterHarvest;
            }

            var harvest = GetModule<HarvestModule>(data);
            if (harvest != null)
            {
                harvest.harvestTool = ToolType.None;
                harvest.allowHandHarvest = true;
                harvest.dropMode = HarvestDropMode.World;
                harvest.harvestCausesDamage = false;
                harvest.destroyOnHarvest = false;

                if (!HasAdditionalHarvestTool(harvest, ToolType.Axe))
                {
                    harvest.additionalHarvestTools = AppendAdditionalHarvestTool(
                        harvest.additionalHarvestTools,
                        new HarvestToolOption
                        {
                            toolType = ToolType.Axe,
                            harvestCausesDamage = true,
                            destroyOnHarvest = false,
                            oneHitDestroy = false
                        });
                }
            }
        }
        else if (template == TemplateKind.ResourceNode)
        {
            var harvest = GetModule<HarvestModule>(data);
            if (harvest != null && harvest.harvestTool == ToolType.None)
                harvest.harvestTool = ToolType.Pickaxe;
        }
        else if (template == TemplateKind.ToolItem)
        {
            var tool = GetModule<ToolModule>(data);
            if (tool != null)
            {
                tool.toolTier = Mathf.Max(1, tool.toolTier);
                if (tool.toolType != ToolType.None && string.IsNullOrWhiteSpace(tool.animTrigger))
                    tool.animTrigger = tool.toolType.ToString();

                if (IsDamageTool(tool.toolType))
                    EnsureStat(data, StatType.Attack, DefaultAttackForTool(tool));
            }

            SyncToolRequirement(data);

            var appearance = GetModule<AppearanceModule>(data);
            if (appearance != null && appearance.equipmentPart == EquipmentPart.Armor)
                appearance.equipmentPart = EquipmentPart.MeleeWeapon1H;
        }
        else if (template == TemplateKind.Npc)
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

    private void EnsureAppearanceSpriteCollection()
    {
        if (appearanceSpriteCollection != null)
            return;

        string[] guids = AssetDatabase.FindAssets("t:SpriteCollection");
        string preferredPath = guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(path => path.Replace('\\', '/').Equals("Assets/Project/Resources/SpriteCollection.asset", StringComparison.OrdinalIgnoreCase));

        string selectedPath = !string.IsNullOrWhiteSpace(preferredPath)
            ? preferredPath
            : guids.Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(selectedPath))
            appearanceSpriteCollection = AssetDatabase.LoadAssetAtPath<SpriteCollection>(selectedPath);
    }

    private Sprite ResolveAppearanceSprite(AppearanceModule appearance)
    {
        if (appearance == null)
            return null;

        EnsureAppearanceSpriteCollection();
        if (appearanceSpriteCollection == null || string.IsNullOrWhiteSpace(appearance.spriteId))
            return null;

        ItemSprite item = GetAppearanceSpritesForPart(appearanceSpriteCollection, appearance.equipmentPart)
            .FirstOrDefault(sprite => string.Equals(sprite.Id, appearance.spriteId, StringComparison.Ordinal));

        item ??= appearanceSpriteCollection.GetAllSprites()
            .FirstOrDefault(sprite => string.Equals(sprite.Id, appearance.spriteId, StringComparison.Ordinal));

        return GetPreviewSprite(item);
    }

    private static List<ItemSprite> GetAppearanceSpritesForPart(SpriteCollection collection, EquipmentPart part)
    {
        if (collection == null)
            return new List<ItemSprite>();

        List<ItemSprite> source = part switch
        {
            EquipmentPart.Armor => collection.Armor,
            EquipmentPart.Helmet => collection.Armor,
            EquipmentPart.Vest => collection.Armor,
            EquipmentPart.Bracers => collection.Armor,
            EquipmentPart.Leggings => collection.Armor,
            EquipmentPart.MeleeWeapon1H => collection.MeleeWeapon1H,
            EquipmentPart.SecondaryMelee1H => collection.MeleeWeapon1H,
            EquipmentPart.MeleeWeapon2H => collection.MeleeWeapon2H,
            EquipmentPart.Bow => collection.Bow,
            EquipmentPart.Crossbow => collection.Crossbow,
            EquipmentPart.Firearm1H => collection.Firearm1H,
            EquipmentPart.SecondaryFirearm1H => collection.Firearm1H,
            EquipmentPart.Firearm2H => collection.Firearm2H,
            EquipmentPart.Shield => collection.Shield,
            EquipmentPart.Back => collection.Back,
            EquipmentPart.Cape => collection.Back,
            EquipmentPart.Quiver => collection.Back,
            EquipmentPart.Mask => collection.Mask,
            EquipmentPart.Earrings => collection.Earrings,
            EquipmentPart.Wings => collection.Wings,
            _ => collection.GetAllSprites()
        };

        return source?.Where(item => item != null && !string.IsNullOrWhiteSpace(item.Id)).OrderBy(item => item.Id).ToList()
            ?? new List<ItemSprite>();
    }

    private static bool MatchesSpriteFilter(ItemSprite item, string filter)
    {
        if (item == null || string.IsNullOrWhiteSpace(filter))
            return true;

        return ContainsIgnoreCase(item.Id, filter) ||
               ContainsIgnoreCase(item.Name, filter) ||
               ContainsIgnoreCase(item.Collection, filter) ||
               (item.Tags != null && item.Tags.Any(tag => ContainsIgnoreCase(tag, filter)));
    }

    private static bool ContainsIgnoreCase(string value, string search)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string FormatSpriteOption(ItemSprite item)
    {
        if (item == null)
            return "(missing)";

        return string.IsNullOrWhiteSpace(item.Name)
            ? item.Id
            : $"{item.Name} - {item.Id}";
    }

    private static Sprite GetPreviewSprite(ItemSprite item)
    {
        if (item == null)
            return null;

        if (item.Sprite != null)
            return item.Sprite;

        return item.Sprites?.FirstOrDefault(sprite => sprite != null);
    }

    private static void DrawSpritePreviewBox(string title, Sprite sprite, string detail)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(110f)))
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel, GUILayout.Width(100f));
            Rect rect = GUILayoutUtility.GetRect(72f, 72f, GUILayout.Width(72f), GUILayout.Height(72f));
            DrawSprite(rect, sprite);
            EditorGUILayout.LabelField(detail, EditorStyles.wordWrappedMiniLabel, GUILayout.Width(100f));
        }
    }

    private static void DrawSprite(Rect rect, Sprite sprite)
    {
        GUI.Box(rect, GUIContent.none);
        if (sprite == null || sprite.texture == null)
        {
            GUI.Label(rect, "No Sprite", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        Texture2D texture = sprite.texture;
        Rect textureRect = sprite.textureRect;
        var textureCoords = new Rect(
            textureRect.x / texture.width,
            textureRect.y / texture.height,
            textureRect.width / texture.width,
            textureRect.height / texture.height);

        GUI.DrawTextureWithTexCoords(rect, texture, textureCoords, true);
    }

    private static bool IsDamageTool(ToolType toolType)
    {
        return toolType == ToolType.Axe ||
               toolType == ToolType.Pickaxe ||
               toolType == ToolType.Scythe;
    }

    private static float DefaultAttackForTool(ToolModule tool)
    {
        if (tool == null)
            return 1f;

        return Mathf.Max(1f, tool.toolTier);
    }

    private static void SyncToolRequirement(EntityData data)
    {
        var tool = GetModule<ToolModule>(data);
        var requirement = GetModule<ToolRequirementModule>(data);
        if (tool == null || requirement == null)
            return;

        requirement.requiredToolType = tool.toolType;
        requirement.minimumToolTier = Mathf.Max(1, tool.toolTier);
        requirement.wrongToolPenalty = 0f;
        requirement.blockDamageIfWrongTool = true;
        requirement.blockDamageIfBelowTier = true;
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

    private static bool IsTreeGrowthAsset(EntityData data, string path)
    {
        if (path.IndexOf("_tree", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        var stage = GetModule<StageModule>(data);
        if (stage == null)
            return false;

        if (!stage.requiresWater && !stage.wiltOnSeasonChange)
            return true;

        var harvest = GetModule<HarvestModule>(data);
        return harvest != null && HasAdditionalHarvestTool(harvest, ToolType.Axe);
    }

    private static bool HasAdditionalHarvestTool(HarvestModule harvest, ToolType toolType)
    {
        if (harvest?.additionalHarvestTools == null)
            return false;

        for (int i = 0; i < harvest.additionalHarvestTools.Length; i++)
        {
            var option = harvest.additionalHarvestTools[i];
            if (option != null && option.toolType == toolType)
                return true;
        }

        return false;
    }

    private static HarvestToolOption[] AppendAdditionalHarvestTool(HarvestToolOption[] source, HarvestToolOption option)
    {
        if (option == null)
            return source ?? Array.Empty<HarvestToolOption>();

        var items = source?.Where(entry => entry != null).ToList() ?? new List<HarvestToolOption>();
        items.Add(option);
        return items.ToArray();
    }

    private static GrowthStage[] CreateDefaultTreeStages()
    {
        return new[]
        {
            new GrowthStage { daysToGrow = 1, canHarvest = false },
            new GrowthStage { daysToGrow = 1, canHarvest = false },
            new GrowthStage { daysToGrow = 1, canHarvest = false },
            new GrowthStage { daysToGrow = 1, canHarvest = false },
            new GrowthStage { daysToGrow = 1, canHarvest = false },
            new GrowthStage { daysToGrow = 1, canHarvest = false },
            new GrowthStage { daysToGrow = 1, canHarvest = false },
            new GrowthStage { daysToGrow = 19999, canHarvest = true },
            new GrowthStage { daysToGrow = 1, canHarvest = false }
        };
    }
}
