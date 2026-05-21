using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Hiển thị notification nhỏ ở góc trên-phải cho Message/Diary events.
/// </summary>
public class MessageNotificationUI : MonoBehaviour
{
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI bodyText;
    private Coroutine hideCoroutine;

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

    private void CreateUI()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        gameObject.AddComponent<GraphicRaycaster>();

        var panel = new GameObject("NotificationPanel");
        panel.transform.SetParent(transform, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 1);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.pivot = new Vector2(1, 1);
        panelRect.anchoredPosition = new Vector2(-20, -20);
        panelRect.sizeDelta = new Vector2(300, 120);

        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        canvasGroup = panel.AddComponent<CanvasGroup>();

        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -10);
        titleRect.sizeDelta = new Vector2(-20, 30);

        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.fontSize = 18;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.TopLeft;
        titleText.color = Color.white;

        var bodyObj = new GameObject("Body");
        bodyObj.transform.SetParent(panel.transform, false);
        var bodyRect = bodyObj.AddComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0, 0);
        bodyRect.anchorMax = new Vector2(1, 1);
        bodyRect.pivot = new Vector2(0.5f, 1);
        bodyRect.anchoredPosition = new Vector2(0, -45);
        bodyRect.sizeDelta = new Vector2(-20, -55);

        bodyText = bodyObj.AddComponent<TextMeshProUGUI>();
        bodyText.fontSize = 14;
        bodyText.alignment = TextAlignmentOptions.TopLeft;
        bodyText.color = new Color(0.9f, 0.9f, 0.9f);
        bodyText.enableWordWrapping = true;
    }

    private void OnStoryEventUnlocked(StoryEventUnlockedPublish e)
    {
        if (e.data == null || !e.data.showNotification) return;
        if (e.data.channel != StoryEventChannel.Message && e.data.channel != StoryEventChannel.Diary) return;

        Show(e.data);
    }

    private void Show(StoryEventData data)
    {
        titleText.text = GetLocalizedText(data.titleKey);
        bodyText.text = GetLocalizedText(data.bodyKey);

        canvasGroup.alpha = 1f;
        canvas.enabled = true;

        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(HideAfterDelay(4f));
    }

    private void Hide()
    {
        canvasGroup.alpha = 0f;
        canvas.enabled = false;
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Hide();
    }

    private string GetLocalizedText(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        if (LocalizationManager.Instance != null)
            return LocalizationManager.Instance.GetText(key);
        return key;
    }
}
