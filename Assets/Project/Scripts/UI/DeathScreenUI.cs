using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeathScreenUI : MonoBehaviour
{
    [SerializeField] private float fadeSeconds = 0.35f;
    [SerializeField] private float holdSeconds = 1.4f;

    private EventBus subscribedBus;
    private Image overlay;
    private TextMeshProUGUI label;
    private Coroutine routine;

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
            subscribedBus.Unsubscribe<PlayerDeathPublish>(OnPlayerDeath);
            subscribedBus = null;
        }
    }

    private void Subscribe()
    {
        if (subscribedBus != null) return;
        var bus = GameManager.Instance?.EventBus;
        if (bus == null) return;
        bus.Subscribe<PlayerDeathPublish>(OnPlayerDeath);
        subscribedBus = bus;
    }

    private void OnPlayerDeath(PlayerDeathPublish _)
    {
        if (routine != null)
            StopCoroutine(routine);
        routine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        overlay.gameObject.SetActive(true);
        label.gameObject.SetActive(true);
        label.text = "You fainted...";

        float elapsed = 0f;
        while (elapsed < fadeSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Clamp01(elapsed / Mathf.Max(0.01f, fadeSeconds)));
            yield return null;
        }

        yield return new WaitForSecondsRealtime(holdSeconds);

        elapsed = 0f;
        while (elapsed < fadeSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(1f - Mathf.Clamp01(elapsed / Mathf.Max(0.01f, fadeSeconds)));
            yield return null;
        }

        overlay.gameObject.SetActive(false);
        label.gameObject.SetActive(false);
        routine = null;
    }

    private void SetAlpha(float alpha)
    {
        var overlayColor = Color.black;
        overlayColor.a = Mathf.Lerp(0f, 0.68f, alpha);
        overlay.color = overlayColor;

        var textColor = Color.white;
        textColor.a = alpha;
        label.color = textColor;
    }

    private void EnsureView()
    {
        if (overlay != null) return;

        var canvas = RuntimeCanvasUtility.CreateOverlayCanvas("DeathScreenCanvas", transform, 150);
        overlay = RuntimeCanvasUtility.CreateImage(canvas.transform, "DeathOverlay", Color.clear);
        overlay.rectTransform.anchorMin = Vector2.zero;
        overlay.rectTransform.anchorMax = Vector2.one;
        overlay.rectTransform.offsetMin = Vector2.zero;
        overlay.rectTransform.offsetMax = Vector2.zero;

        label = RuntimeCanvasUtility.CreateText(canvas.transform, "DeathText", 46, TextAlignmentOptions.Center);
        label.gameObject.SetActive(false);
        overlay.gameObject.SetActive(false);
    }
}
