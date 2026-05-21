using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Hiển thị news banner rộng ở top-center cho News events.
/// </summary>
public class NewsBroadcastUI : MonoBehaviour
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
        var root = OverlayUIHelper.GetOrCreateOverlayRoot(gameObject, 100);

        var panel = new GameObject("NewsPanel");
        panel.transform.SetParent(root, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1);
        panelRect.anchorMax = new Vector2(0.5f, 1);
        panelRect.pivot = new Vector2(0.5f, 1);
        panelRect.anchoredPosition = new Vector2(0, -20);
        panelRect.sizeDelta = new Vector2(600, 100);

        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.2f, 0.1f, 0.05f, 0.95f);

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
        titleText.fontSize = 20;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.yellow;

        var bodyObj = new GameObject("Body");
        bodyObj.transform.SetParent(panel.transform, false);
        var bodyRect = bodyObj.AddComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0, 0);
        bodyRect.anchorMax = new Vector2(1, 1);
        bodyRect.pivot = new Vector2(0.5f, 1);
        bodyRect.anchoredPosition = new Vector2(0, -45);
        bodyRect.sizeDelta = new Vector2(-20, -55);

        bodyText = bodyObj.AddComponent<TextMeshProUGUI>();
        bodyText.fontSize = 16;
        bodyText.alignment = TextAlignmentOptions.Center;
        bodyText.color = Color.white;
        bodyText.enableWordWrapping = true;
    }

    private void OnStoryEventUnlocked(StoryEventUnlockedPublish e)
    {
        if (e.data == null) return;
        if (e.data.channel != StoryEventChannel.News) return;

        Show(e.data);
    }

    private void Show(StoryEventData data)
    {
        titleText.text = GetLocalizedText(data.titleKey);
        bodyText.text = GetLocalizedText(data.bodyKey);

        canvasGroup.alpha = 1f;
        canvas.enabled = true;

        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(HideAfterDelay(5f));
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
