using System.Collections;
using TMPro;
using UnityEngine;

public class LevelUpEffect : MonoBehaviour
{
    [SerializeField] private float showSeconds = 1.25f;
    [SerializeField] private Color color = new(1f, 0.9f, 0.25f);

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
            subscribedBus.Unsubscribe<LevelUpPublish>(OnLevelUp);
            subscribedBus = null;
        }
    }

    private void Subscribe()
    {
        if (subscribedBus != null) return;
        var bus = GameManager.Instance?.EventBus;
        if (bus == null) return;

        bus.Subscribe<LevelUpPublish>(OnLevelUp);
        subscribedBus = bus;
    }

    private void OnLevelUp(LevelUpPublish evt)
    {
        var go = evt.target?.Owner?.GameObject;
        if (go == null || go.GetComponent<PlayerControler>() == null)
            return;

        if (routine != null)
            StopCoroutine(routine);
        routine = StartCoroutine(ShowRoutine(evt.level));
    }

    private IEnumerator ShowRoutine(int level)
    {
        label.text = $"LEVEL UP! Lv {level}";
        label.color = color;
        label.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < showSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, showSeconds));
            var c = color;
            c.a = 1f - Mathf.SmoothStep(0f, 1f, t);
            label.color = c;
            label.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.2f, 1f, t);
            yield return null;
        }

        label.gameObject.SetActive(false);
        routine = null;
    }

    private void EnsureView()
    {
        if (label != null) return;

        var canvas = RuntimeCanvasUtility.CreateOverlayCanvas("LevelUpCanvas", transform, 120);
        label = RuntimeCanvasUtility.CreateText(canvas.transform, "LevelUpText", 54, TextAlignmentOptions.Center);
        label.rectTransform.anchoredPosition = new Vector2(0f, 120f);
        label.gameObject.SetActive(false);
    }
}
