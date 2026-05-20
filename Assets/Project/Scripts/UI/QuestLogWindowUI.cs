using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hiển thị danh sách tất cả quest trong QuestLogRuntime của player.
/// InProgress trước, Completed sau.
/// Click vào row → publish QuestViewPublish để QuestPanelUI hiển thị chi tiết.
/// </summary>
public class QuestLogWindowUI : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private Transform questListRoot;
    [SerializeField] private GameObject questRowTemplate;

    [Header("Empty State")]
    [SerializeField] private GameObject emptyLabel;

    private EventBus subscribedBus;
    private EntityRuntime playerEntity;
    private readonly List<GameObject> spawnedRows = new();
    private bool playerReady;

    // ── Lifecycle ────────────────────────────────────────────────

    private void OnEnable()
    {
        TrySubscribe();
        if (playerReady) Rebuild();
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void Update()
    {
        if (subscribedBus == null) TrySubscribe();
    }

    private void OnDisable()
    {
        if (subscribedBus != null)
        {
            subscribedBus.Unsubscribe<GameReadyPublish>(OnGameReady);
            subscribedBus.Unsubscribe<QuestStateChangedPublish>(OnQuestStateChanged);
            subscribedBus = null;
        }
    }

    // ── Event Handlers ────────────────────────────────────────────

    private void OnGameReady(GameReadyPublish _)
    {
        ResolvePlayer();
        playerReady = true;
        if (gameObject.activeInHierarchy) Rebuild();
    }

    private void OnQuestStateChanged(QuestStateChangedPublish e)
    {
        if (playerEntity == null || e.playerEntityId != playerEntity.id) return;
        if (gameObject.activeInHierarchy) Rebuild();
    }

    // ── Core ──────────────────────────────────────────────────────

    private void TrySubscribe()
    {
        if (subscribedBus != null) return;

        var bus = GameManager.Instance?.EventBus;
        if (bus == null) return;

        bus.Subscribe<GameReadyPublish>(OnGameReady);
        bus.Subscribe<QuestStateChangedPublish>(OnQuestStateChanged);
        subscribedBus = bus;
    }

    private void ResolvePlayer()
    {
        var playerInventory = Object.FindAnyObjectByType<PlayerInventory>();
        var entityRoot = playerInventory?.GetComponent<EntityRoot>();
        playerEntity = entityRoot?.GetEntity();
    }

    private void Rebuild()
    {
        ClearRows();

        if (playerEntity == null) ResolvePlayer();
        if (playerEntity == null)
        {
            SetEmptyLabel(true);
            return;
        }

        var log = playerEntity.GetModule<QuestLogRuntime>();
        if (log == null)
        {
            SetEmptyLabel(true);
            return;
        }

        // Thu thập tất cả QuestGraphData từ NPC entities có QuestModule
        var allQuests = CollectAllQuestGraphs();

        // Lọc: InProgress trước, Completed sau, bỏ NotStarted
        var inProgress = new List<QuestGraphData>();
        var completed  = new List<QuestGraphData>();

        foreach (var graph in allQuests)
        {
            var state = log.GetState(graph.id);
            if (state == QuestState.InProgress)  inProgress.Add(graph);
            else if (state == QuestState.Completed) completed.Add(graph);
        }

        if (inProgress.Count == 0 && completed.Count == 0)
        {
            SetEmptyLabel(true);
            return;
        }

        SetEmptyLabel(false);
        EnsureTemplate();

        foreach (var graph in inProgress)
            SpawnRow(graph, QuestState.InProgress, log);

        foreach (var graph in completed)
            SpawnRow(graph, QuestState.Completed, log);
    }

    private void SpawnRow(QuestGraphData graph, QuestState state, QuestLogRuntime log)
    {
        if (questListRoot == null || questRowTemplate == null || graph == null) return;

        var row = Instantiate(questRowTemplate, questListRoot);
        row.SetActive(true);
        spawnedRows.Add(row);

        // Title
        var titleText = FindDeepChild(row.transform, "TitleText")?.GetComponent<TMP_Text>();
        if (titleText != null)
        {
            string title = LocalizationManager.Instance != null
                ? LocalizationManager.Instance.GetText(graph.titleKey)
                : graph.titleKey;
            titleText.text = string.IsNullOrWhiteSpace(title) ? graph.id : title;
        }

        // State badge
        var stateText = FindDeepChild(row.transform, "StateText")?.GetComponent<TMP_Text>();
        if (stateText != null)
        {
            stateText.text  = GetStateLabel(state);
            stateText.color = state == QuestState.Completed
                ? new Color(0.40f, 0.85f, 0.40f)
                : new Color(1.00f, 0.80f, 0.20f);
        }

        // Click → show chi tiết qua QuestPanelUI
        var btn = row.GetComponent<Button>();
        if (btn != null)
        {
            var capturedGraph = graph;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnRowClicked(capturedGraph));
        }
    }

    private void OnRowClicked(QuestGraphData graph)
    {
        // Tìm questOwner (NPC entity sở hữu quest này)
        EntityRuntime questOwner = null;
        if (GameManager.Instance?.EntityRegistry != null)
        {
            foreach (var entity in GameManager.Instance.EntityRegistry.GetAll())
            {
                if (entity?.entityData?.modules == null) continue;
                foreach (var mod in entity.entityData.modules.OfType<QuestModule>())
                {
                    if (mod?.quests == null) continue;
                    foreach (var q in mod.quests)
                    {
                        if (q != null && q.id == graph.id)
                        {
                            questOwner = entity;
                            break;
                        }
                    }
                    if (questOwner != null) break;
                }
                if (questOwner != null) break;
            }
        }

        QuestService.ShowQuest(playerEntity, questOwner, graph);
    }

    private List<QuestGraphData> CollectAllQuestGraphs()
    {
        var result = new List<QuestGraphData>();
        if (GameManager.Instance?.EntityRegistry == null) return result;

        var seen = new HashSet<string>();
        foreach (var entity in GameManager.Instance.EntityRegistry.GetAll())
        {
            if (entity?.entityData?.modules == null) continue;
            foreach (var mod in entity.entityData.modules.OfType<QuestModule>())
            {
                if (mod?.quests == null) continue;
                foreach (var quest in mod.quests)
                {
                    if (quest != null && seen.Add(quest.id))
                        result.Add(quest);
                }
            }
        }

        return result;
    }

    private void ClearRows()
    {
        foreach (var row in spawnedRows)
            if (row != null) Destroy(row);
        spawnedRows.Clear();
    }

    private void SetEmptyLabel(bool show)
    {
        if (emptyLabel != null) emptyLabel.SetActive(show);
    }

    // ── Template auto-create ──────────────────────────────────────

    private void EnsureTemplate()
    {
        if (questRowTemplate != null) return;
        if (questListRoot == null) return;

        questRowTemplate = BuildRowTemplate(questListRoot);
        questRowTemplate.SetActive(false);
    }

    private static GameObject BuildRowTemplate(Transform parent)
    {
        // Root: Button + HorizontalLayoutGroup
        var root = new GameObject("QuestRowTemplate", typeof(RectTransform));
        root.transform.SetParent(parent, false);

        var le = root.AddComponent<LayoutElement>();
        le.minHeight = 52f;
        le.preferredHeight = 52f;
        le.flexibleWidth = 1f;

        var img = root.AddComponent<Image>();
        img.color = new Color(0.25f, 0.14f, 0.06f, 0.85f);

        var btn = root.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.40f, 0.24f, 0.10f, 0.95f);
        colors.pressedColor     = new Color(0.55f, 0.34f, 0.14f, 1.00f);
        btn.colors = colors;

        var layout = root.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 0, 0);
        layout.spacing = 10f;
        layout.childAlignment        = TextAnchor.MiddleLeft;
        layout.childControlWidth     = true;
        layout.childControlHeight    = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight= true;

        // Title Text
        var titleGo = new GameObject("TitleText", typeof(RectTransform));
        titleGo.transform.SetParent(root.transform, false);
        var titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.text      = string.Empty;
        titleText.fontSize  = 20f;
        titleText.color     = new Color(0.96f, 0.88f, 0.65f);
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        titleText.enableWordWrapping = false;
        var titleLE = titleGo.AddComponent<LayoutElement>();
        titleLE.flexibleWidth = 1f;

        // State Badge
        var stateGo = new GameObject("StateText", typeof(RectTransform));
        stateGo.transform.SetParent(root.transform, false);
        var stateText = stateGo.AddComponent<TextMeshProUGUI>();
        stateText.text      = string.Empty;
        stateText.fontSize  = 16f;
        stateText.color     = new Color(1f, 0.8f, 0.2f);
        stateText.alignment = TextAlignmentOptions.MidlineRight;
        stateText.enableWordWrapping = false;
        var stateLE = stateGo.AddComponent<LayoutElement>();
        stateLE.minWidth      = 110f;
        stateLE.preferredWidth= 110f;

        return root;
    }

    // ── Helpers ───────────────────────────────────────────────────

    private static string GetStateLabel(QuestState state) => state switch
    {
        QuestState.InProgress => "Đang làm",
        QuestState.Completed  => "Hoàn thành",
        _                     => "Chưa nhận"
    };

    private static Transform FindDeepChild(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindDeepChild(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
