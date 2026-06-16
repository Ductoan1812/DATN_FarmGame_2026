using System.Collections;
using TMPro;
using UnityEngine;

public class ToastUI : MonoBehaviour
{
    private EventBus subscribedBus;
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
            subscribedBus.Unsubscribe<ToastPublish>(OnToast);
            subscribedBus = null;
        }
    }

    private void Subscribe()
    {
        if (subscribedBus != null) return;
        var bus = GameManager.Instance?.EventBus;
        if (bus == null) return;
        bus.Subscribe<ToastPublish>(OnToast);
        subscribedBus = bus;
    }

    private void OnToast(ToastPublish evt)
    {
        if (string.IsNullOrWhiteSpace(evt.message))
            return;

        if (routine != null)
            StopCoroutine(routine);
        routine = StartCoroutine(ShowRoutine(evt.message, evt.duration));
    }

    private IEnumerator ShowRoutine(string message, float duration)
    {
        label.text = message;
        label.gameObject.SetActive(true);
        label.color = new Color(1f, 1f, 1f, 1f);

        float safeDuration = Mathf.Max(0.5f, duration);
        float elapsed = 0f;
        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float fade = Mathf.InverseLerp(safeDuration, safeDuration * 0.7f, elapsed);
            var color = label.color;
            color.a = Mathf.Clamp01(fade);
            label.color = color;
            yield return null;
        }

        label.gameObject.SetActive(false);
        routine = null;
    }

    private void EnsureView()
    {
        if (label != null) return;

        var canvas = RuntimeCanvasUtility.CreateOverlayCanvas("ToastCanvas", transform, 130);
        label = RuntimeCanvasUtility.CreateText(canvas.transform, "ToastText", 30, TextAlignmentOptions.Center);
        label.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        label.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        label.rectTransform.pivot = new Vector2(0.5f, 0f);
        label.rectTransform.anchoredPosition = new Vector2(0f, 160f);
        label.gameObject.SetActive(false);
    }
}
