using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveSlotButtonUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text detailText;
    [SerializeField] private Button button;

    private int slotIndex;
    private System.Action<int> onClicked;

    private void Awake()
    {
        AutoFindRefs();
    }

    public void Bind(SaveLoadManager.SaveSlotSummary summary, bool interactable, System.Action<int> clickHandler)
    {
        AutoFindRefs();

        slotIndex = summary != null ? summary.slotIndex : 0;
        onClicked = clickHandler;

        if (titleText != null)
            titleText.text = summary != null ? summary.displayName : "Bản Save";

        if (detailText != null)
            detailText.text = summary != null ? summary.detailText : string.Empty;

        if (button != null)
        {
            button.onClick.RemoveListener(HandleClicked);
            button.onClick.AddListener(HandleClicked);
            button.interactable = interactable;
        }
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleClicked);
    }

    private void HandleClicked()
    {
        if (slotIndex > 0)
            onClicked?.Invoke(slotIndex);
    }

    private void AutoFindRefs()
    {
        button ??= GetComponent<Button>();

        if (titleText != null && detailText != null)
            return;

        var texts = GetComponentsInChildren<TMP_Text>(true);
        if (texts.Length > 0)
            titleText ??= texts[0];
        if (texts.Length > 1)
            detailText ??= texts[1];
    }
}
