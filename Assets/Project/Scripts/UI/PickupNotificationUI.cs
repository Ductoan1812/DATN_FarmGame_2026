using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hiển thị danh sách thông báo "Nhặt Nx Tên" ở góc dưới-trái màn hình.
/// Mỗi lần ItemPickedUpPublish bắn ra → 1 dòng mới trượt lên từ dưới, tự biến mất sau vài giây.
/// </summary>
public class PickupNotificationUI : MonoBehaviour
{
    private const string FontResourcePath = "Fonts & Materials/Roboto-Bold SDF";
    private const float DisplaySeconds = 5f;
    private const float FadeSeconds = 0.35f;
    private const float SlideSeconds = 0.2f;
    private const int MaxVisibleEntries = 6;

    private RectTransform listRoot;
    private EventBus subscribedBus;
    private readonly List<GameObject> activeEntries = new();
    private TMP_FontAsset fontAsset;

    private void OnEnable()
    {
        EnsureView();
        Subscribe();
    }

    private void Start()
    {
        EnsureView();
        Subscribe();
    }

    private void OnDisable()
    {
        if (subscribedBus != null)
        {
            subscribedBus.Unsubscribe<ItemPickedUpPublish>(OnItemPickedUp);
            subscribedBus = null;
        }
    }

    private void Subscribe()
    {
        if (subscribedBus != null) return;
        var bus = GameManager.Instance?.EventBus;
        if (bus == null) return;
        bus.Subscribe<ItemPickedUpPublish>(OnItemPickedUp);
        subscribedBus = bus;
    }

    private void OnItemPickedUp(ItemPickedUpPublish evt)
    {
        string localizedName = LocalizationManager.Instance != null
            ? LocalizationManager.Instance.GetText(evt.itemKeyName)
            : evt.itemKeyName;

        string message = $"Nhặt {evt.amount} {localizedName}";
        SpawnEntry(message, evt.icon);
    }

    private void EnsureView()
    {
        if (listRoot != null) return;

        fontAsset = Resources.Load<TMP_FontAsset>(FontResourcePath);

        var root = OverlayUIHelper.GetOrCreateOverlayRoot(gameObject, 100);

        var listGo = new GameObject("PickupNotificationList");
        listGo.transform.SetParent(root, false);

        var listRect = listGo.AddComponent<RectTransform>();
        listRect.anchorMin = new Vector2(0f, 0f);
        listRect.anchorMax = new Vector2(0f, 0f);
        listRect.pivot = new Vector2(0f, 0f);
        listRect.anchoredPosition = new Vector2(16f, 16f);
        listRect.sizeDelta = new Vector2(320f, 280f);

        var layout = listGo.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.LowerLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 6f;
        layout.reverseArrangement = false;

        var fitter = listGo.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        listRoot = listRect;
    }

    private void SpawnEntry(string message, Sprite icon)
    {
        var wrapper = new GameObject("PickupEntryWrapper");
        wrapper.transform.SetParent(listRoot, false);
        wrapper.transform.SetAsFirstSibling();

        var wrapperRect = wrapper.AddComponent<RectTransform>();
        wrapperRect.sizeDelta = new Vector2(0f, 40f);
        var wrapperLayoutElement = wrapper.AddComponent<LayoutElement>();
        wrapperLayoutElement.preferredHeight = 40f;

        var entry = new GameObject("PickupEntry");
        entry.transform.SetParent(wrapper.transform, false);

        var entryRect = entry.AddComponent<RectTransform>();
        entryRect.anchorMin = Vector2.zero;
        entryRect.anchorMax = Vector2.one;
        entryRect.offsetMin = Vector2.zero;
        entryRect.offsetMax = Vector2.zero;

        var bg = entry.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.7f);

        var canvasGroup = entry.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        var hLayout = entry.AddComponent<HorizontalLayoutGroup>();
        hLayout.padding = new RectOffset(8, 8, 6, 6);
        hLayout.spacing = 8f;
        hLayout.childAlignment = TextAnchor.MiddleLeft;
        hLayout.childControlWidth = false;
        hLayout.childControlHeight = true;
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = true;

        if (icon != null)
        {
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(entry.transform, false);
            var iconRect = iconGo.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(28f, 28f);
            var iconImage = iconGo.AddComponent<Image>();
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            var iconLayoutElement = iconGo.AddComponent<LayoutElement>();
            iconLayoutElement.preferredWidth = 28f;
            iconLayoutElement.preferredHeight = 28f;
        }

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(entry.transform, false);
        var label = textGo.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null)
            label.font = fontAsset;
        label.fontSize = 18;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.text = message;
        label.raycastTarget = false;

        var textLayoutElement = textGo.AddComponent<LayoutElement>();
        textLayoutElement.flexibleWidth = 1f;

        activeEntries.Add(entry);
        TrimExcessEntries();

        StartCoroutine(EntryLifecycle(entry, canvasGroup, entryRect));
    }

    private void TrimExcessEntries()
    {
        while (activeEntries.Count > MaxVisibleEntries)
        {
            var oldest = activeEntries[0];
            activeEntries.RemoveAt(0);
            if (oldest != null)
                Destroy(oldest);
        }
    }

    private IEnumerator EntryLifecycle(GameObject entry, CanvasGroup canvasGroup, RectTransform rect)
    {
        float startY = -20f;
        float elapsed = 0f;
        while (elapsed < SlideSeconds)
        {
            // Entry có thể bị Destroy giữa chừng (đóng panel, đổi scene, dồn quá nhiều entry...).
            if (entry == null || canvasGroup == null || rect == null)
                yield break;

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / SlideSeconds);
            canvasGroup.alpha = t;
            rect.anchoredPosition = new Vector2(0f, Mathf.Lerp(startY, 0f, t));
            yield return null;
        }

        if (entry == null || canvasGroup == null || rect == null)
            yield break;

        canvasGroup.alpha = 1f;
        rect.anchoredPosition = Vector2.zero;

        yield return new WaitForSecondsRealtime(DisplaySeconds);

        elapsed = 0f;
        while (elapsed < FadeSeconds)
        {
            if (entry == null || canvasGroup == null)
            {
                activeEntries.Remove(entry);
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / FadeSeconds);
            yield return null;
        }

        activeEntries.Remove(entry);
        if (entry != null)
            Destroy(entry);
    }

}
