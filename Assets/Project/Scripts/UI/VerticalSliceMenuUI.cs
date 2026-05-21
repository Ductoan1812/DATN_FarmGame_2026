using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Vertical slice presentation menu: main title overlay + pause overlay.
/// Builds UI hierarchy in Awake if missing. Intended parent: UIRoot/Canvas_Overlay/VerticalSliceMenuUI.
/// </summary>
public class VerticalSliceMenuUI : MonoBehaviour
{
    private Transform _uiRoot;
    private GameObject _mainOverlay;
    private GameObject _pauseOverlay;
    private Button _continueButton;
    private TextMeshProUGUI _statusText;
    private static bool sessionStarted;

    private void Awake()
    {
        BuildUI();
        UpdateContinueButton();

        if (sessionStarted)
            HideAll();
        else
            ShowMainOverlay();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_mainOverlay.activeSelf) return;
            if (IsAnyOtherOverlayActive()) return;
            TogglePause();
        }
    }

    private void BuildUI()
    {
        _uiRoot = OverlayUIHelper.GetOrCreateOverlayRoot(gameObject, 120);

        if (transform.childCount > 0) return;

        // Main overlay
        _mainOverlay = CreatePanel("MainOverlay", new Color(0.1f, 0.1f, 0.1f, 0.95f));
        CreateTitle(_mainOverlay.transform, "DATN Farm Game");
        CreateButton(_mainOverlay.transform, "New Game", OnNewGame, new Vector2(0, 40));
        _continueButton = CreateButton(_mainOverlay.transform, "Continue", OnContinue, new Vector2(0, -20));
        CreateButton(_mainOverlay.transform, "Settings", OnSettings, new Vector2(0, -80));
        CreateButton(_mainOverlay.transform, "Quit", OnQuit, new Vector2(0, -140));

        // Pause overlay
        _pauseOverlay = CreatePanel("PauseOverlay", new Color(0.1f, 0.1f, 0.1f, 0.9f));
        CreateTitle(_pauseOverlay.transform, "Paused");
        CreateButton(_pauseOverlay.transform, "Resume", OnResume, new Vector2(0, 80));
        CreateButton(_pauseOverlay.transform, "Save", OnSave, new Vector2(0, 20));
        CreateButton(_pauseOverlay.transform, "Load", OnLoad, new Vector2(0, -40));
        CreateButton(_pauseOverlay.transform, "Settings", OnSettings, new Vector2(0, -100));
        CreateButton(_pauseOverlay.transform, "Return to Title", OnReturnToTitle, new Vector2(0, -160));
        _pauseOverlay.SetActive(false);

        // Status text
        var statusObj = new GameObject("StatusText");
        statusObj.transform.SetParent(_uiRoot != null ? _uiRoot : transform, false);
        var statusRect = statusObj.AddComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.5f, 0);
        statusRect.anchorMax = new Vector2(0.5f, 0);
        statusRect.pivot = new Vector2(0.5f, 0);
        statusRect.anchoredPosition = new Vector2(0, 20);
        statusRect.sizeDelta = new Vector2(800, 40);
        _statusText = statusObj.AddComponent<TextMeshProUGUI>();
        _statusText.alignment = TextAlignmentOptions.Center;
        _statusText.fontSize = 18;
        _statusText.color = Color.yellow;
        _statusText.text = "";
    }

    private GameObject CreatePanel(string name, Color color)
    {
        var panel = new GameObject(name);
        panel.transform.SetParent(_uiRoot != null ? _uiRoot : transform, false);
        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        var img = panel.AddComponent<Image>();
        img.color = color;
        return panel;
    }

    private void CreateTitle(Transform parent, string text)
    {
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(parent, false);
        var rect = titleObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, 200);
        rect.sizeDelta = new Vector2(800, 80);
        var tmp = titleObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 48;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
    }

    private Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction action, Vector2 pos)
    {
        var btnObj = new GameObject(label);
        btnObj.transform.SetParent(parent, false);
        var rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(300, 50);
        var img = btnObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        var btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(action);

        var txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        var txtRect = txtObj.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;
        var tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return btn;
    }

    private void ShowMainOverlay()
    {
        _mainOverlay.SetActive(true);
        _pauseOverlay.SetActive(false);
        PauseTime();
    }

    private void ShowPauseOverlay()
    {
        _mainOverlay.SetActive(false);
        _pauseOverlay.SetActive(true);
        PauseTime();
    }

    private void HideAll()
    {
        _mainOverlay.SetActive(false);
        _pauseOverlay.SetActive(false);
        ResumeTime();
    }

    private void TogglePause()
    {
        if (_pauseOverlay.activeSelf) OnResume();
        else ShowPauseOverlay();
    }

    private void OnNewGame()
    {
        sessionStarted = true;
        SaveLoadManager.DeleteAllSaveData();
        SceneManager.LoadScene("FarmScene");
    }

    private void OnContinue()
    {
        sessionStarted = true;
        UpdateContinueButton();
        HideAll();
    }

    private void OnSave()
    {
        var eventBus = GameManager.Instance?.EventBus;
        if (eventBus != null)
        {
            eventBus.Publish(new SaveGameRequestPublish());
            ShowStatus("Game saved.");
        }
        else ShowStatus("EventBus not found.");
    }

    private void OnLoad()
    {
        if (!SaveLoadManager.HasAnySaveData())
        {
            ShowStatus("No save data found.");
            return;
        }
        var eventBus = GameManager.Instance?.EventBus;
        if (eventBus != null)
        {
            eventBus.Publish(new LoadGameRequestPublish());
            ShowStatus("Game loaded.");
        }
        else ShowStatus("EventBus not found.");
    }

    private void OnSettings()
    {
        var uiController = FindAnyObjectByType<UIController>();
        if (uiController == null)
        {
            ShowStatus("Settings window not found.");
            return;
        }

        _mainOverlay.SetActive(false);
        _pauseOverlay.SetActive(false);
        PauseTime();
        uiController.Open("settings");
        ShowStatus("Settings opened.");
    }

    private void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnResume()
    {
        HideAll();
    }

    private void OnReturnToTitle()
    {
        sessionStarted = false;
        UpdateContinueButton();
        ShowMainOverlay();
    }

    private void UpdateContinueButton()
    {
        if (_continueButton != null)
            _continueButton.interactable = SaveLoadManager.HasAnySaveData();
    }

    private void ShowStatus(string msg)
    {
        if (_statusText != null)
        {
            _statusText.text = msg;
            CancelInvoke(nameof(ClearStatus));
            Invoke(nameof(ClearStatus), 3f);
        }
    }

    private void ClearStatus() { if (_statusText != null) _statusText.text = ""; }

    private void PauseTime()
    {
        var tm = GameManager.Instance?.TimeManager;
        if (tm != null) tm.Pause();
    }

    private void ResumeTime()
    {
        var tm = GameManager.Instance?.TimeManager;
        if (tm != null) tm.Play();
    }

    private bool IsAnyOtherOverlayActive()
    {
        var uiController = FindAnyObjectByType<UIController>();
        if (uiController == null) return false;

        return uiController.IsOpen("backpack")
            || uiController.IsOpen("equipment")
            || uiController.IsOpen("quest")
            || uiController.IsOpen("map")
            || uiController.IsOpen("skills")
            || uiController.IsOpen("settings")
            || uiController.IsOpen("shop");
    }
}
