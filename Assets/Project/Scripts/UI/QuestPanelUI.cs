using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private Transform objectivesRoot;
    [SerializeField] private TMP_Text objectiveTextPrefab;
    [SerializeField] private Button closeButton;

    private readonly List<TMP_Text> spawnedObjectives = new();
    private EventBus subscribedBus;
    private bool closeListenerRegistered;
    private QuestViewData currentViewData;

    private void OnEnable()
    {
        TrySubscribe();

        if (closeButton != null && !closeListenerRegistered)
        {
            closeButton.onClick.AddListener(Hide);
            closeListenerRegistered = true;
        }

        Hide();
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void Update()
    {
        if (subscribedBus == null)
            TrySubscribe();
    }

    private void OnDisable()
    {
        if (subscribedBus != null)
        {
            subscribedBus.Unsubscribe<QuestViewPublish>(OnQuestView);
            subscribedBus.Unsubscribe<InventoryChangedPublish>(OnInventoryChanged);
            subscribedBus = null;
        }

        if (closeButton != null && closeListenerRegistered)
        {
            closeButton.onClick.RemoveListener(Hide);
            closeListenerRegistered = false;
        }
    }

    private void OnQuestView(QuestViewPublish e)
    {
        if (e.viewData == null) return;

        currentViewData = e.viewData;
        ClearObjectives();
        Show();

        SetLocalizedText(titleText, e.viewData.TitleKey);
        SetLocalizedText(descriptionText, e.viewData.DescriptionKey);
        SetLocalizedText(stateText, GetStateKey(e.viewData.State));

        if (e.viewData.Objectives == null) return;

        foreach (var objective in e.viewData.Objectives)
        {
            if (objective == null || objectiveTextPrefab == null || objectivesRoot == null) continue;

            var row = Instantiate(objectiveTextPrefab, objectivesRoot);
            row.gameObject.SetActive(true);
            spawnedObjectives.Add(row);

            string text = LocalizationManager.Instance != null
                ? LocalizationManager.Instance.GetText(objective.DescriptionKey)
                : objective.DescriptionKey;

            row.text = $"{text} {objective.CurrentAmount}/{objective.RequiredAmount}";
        }
    }

    private void Show()
    {
        if (TryOpenViaRoot("quest")) return;

        if (panel != null) panel.SetActive(true);
        else gameObject.SetActive(true);
        UIRootController.Instance?.NotifyWindowStateChanged();
    }

    private void Hide()
    {
        ClearObjectives();
        currentViewData = null;
        if (TryCloseViaRoot("quest")) return;

        if (panel != null) panel.SetActive(false);
        else gameObject.SetActive(false);
        UIRootController.Instance?.NotifyWindowStateChanged();
    }

    private bool TryOpenViaRoot(string id)
    {
        var root = UIRootController.Instance;
        if (root == null || !root.TryGetEntry(id, out _)) return false;

        root.Open(id);
        return true;
    }

    private bool TryCloseViaRoot(string id)
    {
        var root = UIRootController.Instance;
        if (root == null || !root.TryGetEntry(id, out _)) return false;

        root.Close(id);
        return true;
    }

    private void ClearObjectives()
    {
        foreach (var objective in spawnedObjectives)
        {
            if (objective != null) Destroy(objective.gameObject);
        }
        spawnedObjectives.Clear();
    }

    private static string GetStateKey(QuestState state)
    {
        return state switch
        {
            QuestState.InProgress => "ui.quest.state.in_progress",
            QuestState.Completed => "ui.quest.state.completed",
            _ => "ui.quest.state.not_started"
        };
    }

    private static void SetLocalizedText(TMP_Text text, string key)
    {
        if (text == null) return;

        var localized = text.GetComponent<LocalizedText>();
        if (localized == null)
            localized = text.gameObject.AddComponent<LocalizedText>();

        localized.SetKey(key);
    }

    private void TrySubscribe()
    {
        if (subscribedBus != null) return;

        var bus = GameManager.Instance?.EventBus;
        if (bus == null) return;

        bus.Subscribe<QuestViewPublish>(OnQuestView);
        bus.Subscribe<InventoryChangedPublish>(OnInventoryChanged);
        subscribedBus = bus;
        Debug.Log("[QuestPanelUI] Subscribed to QuestViewPublish.");
    }

    private void OnInventoryChanged(InventoryChangedPublish e)
    {
        if (currentViewData == null) return;
        if (currentViewData.Player == null || currentViewData.QuestOwner == null) return;
        if (string.IsNullOrWhiteSpace(currentViewData.QuestId)) return;
        if (currentViewData.State != QuestState.InProgress) return;
        if (!gameObject.activeInHierarchy && (panel == null || !panel.activeInHierarchy)) return;
        if (!string.Equals(currentViewData.Player.id, e.entityId)) return;

        var graph = ResolveQuestGraph(currentViewData.QuestOwner, currentViewData.QuestId);
        if (graph == null) return;

        QuestService.ShowQuest(currentViewData.Player, currentViewData.QuestOwner, graph);
    }

    private static QuestGraphData ResolveQuestGraph(EntityRuntime owner, string questId)
    {
        if (owner?.entityData?.modules == null || string.IsNullOrWhiteSpace(questId))
            return null;

        foreach (var module in owner.entityData.modules.OfType<QuestModule>())
        {
            if (module?.quests == null) continue;
            foreach (var quest in module.quests)
            {
                if (quest != null && string.Equals(quest.id, questId, System.StringComparison.OrdinalIgnoreCase))
                    return quest;
            }
        }

        return null;
    }
}
