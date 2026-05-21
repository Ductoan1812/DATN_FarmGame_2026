using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Diary UI hiển thị danh sách unlocked story events. Toggle với KeyCode.J.
/// </summary>
public class DiaryUI : MonoBehaviour
{
    private Canvas canvas;
    private GameObject panel;
    private TextMeshProUGUI contentText;
    private bool isVisible;

    private void Awake()
    {
        CreateUI();
        Hide();
    }

    private void OnEnable()
    {
        GameManager.Instance?.EventBus?.Subscribe<StoryEventUnlockedPublish>(OnStoryEventUnlocked);
    }

    private void OnDisable()
    {
        GameManager.Instance?.EventBus?.Unsubscribe<StoryEventUnlockedPublish>(OnStoryEventUnlocked);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
            Toggle();
    }

    private void CreateUI()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        gameObject.AddComponent<GraphicRaycaster>();

        panel = new GameObject("DiaryPanel");
        panel.transform.SetParent(transform, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(700, 500);

        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.05f, 0.05f, 0.05f, 0.95f);

        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -10);
        titleRect.sizeDelta = new Vector2(-20, 40);

        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "DIARY (Press J to close)";
        titleText.fontSize = 24;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;

        var scrollObj = new GameObject("ScrollView");
        scrollObj.transform.SetParent(panel.transform, false);
        var scrollRect = scrollObj.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0, 0);
        scrollRect.anchorMax = new Vector2(1, 1);
        scrollRect.pivot = new Vector2(0.5f, 1);
        scrollRect.anchoredPosition = new Vector2(0, -60);
        scrollRect.sizeDelta = new Vector2(-20, -70);

        var scrollView = scrollObj.AddComponent<ScrollRect>();
        scrollView.horizontal = false;
        scrollView.vertical = true;

        var viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(scrollObj.transform, false);
        var viewportRect = viewportObj.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        viewportRect.pivot = new Vector2(0, 1);

        var mask = viewportObj.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        var maskImage = viewportObj.AddComponent<Image>();
        maskImage.color = Color.clear;

        var contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);
        var contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 1000);

        contentText = contentObj.AddComponent<TextMeshProUGUI>();
        contentText.fontSize = 16;
        contentText.alignment = TextAlignmentOptions.TopLeft;
        contentText.color = Color.white;
        contentText.enableWordWrapping = true;

        scrollView.viewport = viewportRect;
        scrollView.content = contentRect;
    }

    private void OnStoryEventUnlocked(StoryEventUnlockedPublish e)
    {
        if (isVisible)
            RefreshContent();
    }

    private void Toggle()
    {
        if (isVisible)
            Hide();
        else
            Show();
    }

    private void Show()
    {
        isVisible = true;
        panel.SetActive(true);
        RefreshContent();
    }

    private void Hide()
    {
        isVisible = false;
        panel.SetActive(false);
    }

    private void RefreshContent()
    {
        var gm = GameManager.Instance;
        if (gm?.NarrativeService == null)
        {
            contentText.text = "No narrative service available.";
            return;
        }

        var unlocked = gm.NarrativeService.GetUnlockedEvents().ToList();
        if (unlocked.Count == 0)
        {
            contentText.text = "No entries yet.";
            return;
        }

        var lines = new List<string>();
        foreach (var evt in unlocked)
        {
            if (evt == null) continue;
            var title = GetLocalizedText(evt.titleKey);
            var body = GetLocalizedText(evt.bodyKey);
            lines.Add($"<b>{title}</b>");
            lines.Add(body);
            lines.Add("");
        }

        contentText.text = string.Join("\n", lines);

        var rect = contentText.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, contentText.preferredHeight + 20);
    }

    private string GetLocalizedText(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        if (LocalizationManager.Instance != null)
            return LocalizationManager.Instance.GetText(key);
        return key;
    }
}
