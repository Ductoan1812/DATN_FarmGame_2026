using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class EndOfDaySummaryUI : MonoBehaviour
{
    private Canvas _canvas;
    private GameObject _panel;
    private TextMeshProUGUI _text;
    private float _timer;
    private bool _visible;

    private void Awake()
    {
        var root = OverlayUIHelper.GetOrCreateOverlayRoot(gameObject, 1000);

        _panel = new GameObject("Panel");
        _panel.transform.SetParent(root, false);
        var rect = _panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(400, 200);
        var img = _panel.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(_panel.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 10);
        textRect.offsetMax = new Vector2(-10, -10);
        _text = textObj.AddComponent<TextMeshProUGUI>();
        _text.fontSize = 18;
        _text.color = Color.white;
        _text.alignment = TextAlignmentOptions.Center;

        _panel.SetActive(false);
        GameManager.Instance?.EventBus?.Subscribe<DayChangedPublish>(OnDayChanged);
    }

    private void OnDestroy()
    {
        GameManager.Instance?.EventBus?.Unsubscribe<DayChangedPublish>(OnDayChanged);
    }

    private void OnDayChanged(DayChangedPublish e)
    {
        var gm = GameManager.Instance;
        if (gm?.DailyTracker == null) return;
        var s = gm.DailyTracker.GetLastSummary();
        _text.text = $"Day {s.day}, {s.season}, Year {s.year}\n" +
                     $"Income: {s.income}g\nEXP: {s.expGained}\nLevel Ups: {s.levelUps}\n" +
                     $"Weather: {gm.WeatherSystem?.CurrentWeather.ToString() ?? "N/A"}\n" +
                     $"[Space/Esc to close]";
        _panel.SetActive(true);
        _visible = true;
        _timer = 5f;
    }

    private void Update()
    {
        if (!_visible) return;
        _timer -= Time.deltaTime;
        if (_timer <= 0 || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
        {
            _panel.SetActive(false);
            _visible = false;
        }
    }
}
