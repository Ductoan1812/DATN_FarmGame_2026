using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CalendarUI : MonoBehaviour
{
    private Canvas _canvas;
    private GameObject _panel;
    private TextMeshProUGUI _dateText;
    private TextMeshProUGUI _researchText;
    private bool _visible;

    private void Start()
    {
        BuildUI();
        _panel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            _visible = !_visible;
            _panel.SetActive(_visible);
            if (_visible) Refresh();

            var uiCtrl = FindAnyObjectByType<UIController>();
            if (uiCtrl != null)
            {
                if (_visible) uiCtrl.OpenExternalExclusiveWindow("calendar");
                else uiCtrl.CloseExternalExclusiveWindow("calendar");
            }
        }
    }

    private void BuildUI()
    {
        var root = OverlayUIHelper.GetOrCreateOverlayRoot(gameObject, 95);

        _panel = new GameObject("CalendarPanel");
        _panel.transform.SetParent(root, false);
        var rect = _panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(400, 300);
        var img = _panel.AddComponent<Image>();
        img.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        var dateObj = new GameObject("DateText");
        dateObj.transform.SetParent(_panel.transform, false);
        var dateRect = dateObj.AddComponent<RectTransform>();
        dateRect.anchorMin = new Vector2(0, 1);
        dateRect.anchorMax = new Vector2(1, 1);
        dateRect.pivot = new Vector2(0.5f, 1);
        dateRect.anchoredPosition = new Vector2(0, -10);
        dateRect.sizeDelta = new Vector2(-20, 40);
        _dateText = dateObj.AddComponent<TextMeshProUGUI>();
        _dateText.fontSize = 18;
        _dateText.alignment = TextAlignmentOptions.Center;

        var researchObj = new GameObject("ResearchText");
        researchObj.transform.SetParent(_panel.transform, false);
        var researchRect = researchObj.AddComponent<RectTransform>();
        researchRect.anchorMin = new Vector2(0, 0);
        researchRect.anchorMax = new Vector2(1, 1);
        researchRect.offsetMin = new Vector2(10, 10);
        researchRect.offsetMax = new Vector2(-10, -60);
        _researchText = researchObj.AddComponent<TextMeshProUGUI>();
        _researchText.fontSize = 14;
        _researchText.alignment = TextAlignmentOptions.TopLeft;
    }

    private void Refresh()
    {
        var tm = GameManager.Instance?.TimeManager;
        if (tm == null)
            return;

        _dateText.text = $"Day {tm.Day} - Season {tm.Season} - Year {tm.Year}";

        var rs = GameManager.Instance?.ResearchService;
        var unlocked = rs != null ? rs.GetUnlockedResearch() : new List<ResearchData>();
        if (unlocked.Count == 0)
        {
            _researchText.text = "No research unlocked yet.";
        }
        else
        {
            var lines = new List<string> { "Unlocked Research:" };
            foreach (var r in unlocked)
            {
                var title = LocalizationManager.Instance?.GetText(r.titleKey) ?? r.titleKey;
                lines.Add($"- {title}");
            }

            _researchText.text = string.Join("\n", lines);
        }
    }
}
