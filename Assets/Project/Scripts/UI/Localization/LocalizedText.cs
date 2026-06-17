using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private TMP_Text targetText;
    [SerializeField] private string localizationKey;

    private void Reset()
    {
        targetText = GetComponent<TMP_Text>();
    }

    private void Awake()
    {
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        LocalizationManager.LocalizationReady += RefreshText;
        LocalizationManager.LanguageChanged += RefreshText;
        RefreshText();
    }

    private void OnDisable()
    {
        LocalizationManager.LocalizationReady -= RefreshText;
        LocalizationManager.LanguageChanged -= RefreshText;
    }

    public void RefreshText()
    {
        if (targetText == null)
            return;

        if (LocalizationManager.Instance == null || !LocalizationManager.Instance.IsReady)
            return;

        targetText.text = LocalizationManager.Instance.GetText(localizationKey);
    }

    public void SetKey(string key)
    {
        localizationKey = key;

        // Đảm bảo đã subscribe events trước khi refresh
        if (isActiveAndEnabled)
        {
            RefreshText();
        }
        // Nếu chưa enabled, OnEnable sẽ gọi RefreshText() khi component active
    }
}
