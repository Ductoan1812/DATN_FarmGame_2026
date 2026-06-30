using UnityEngine;

[DefaultExecutionOrder(-600)]
public sealed class MainMenuLocalizationBootstrap : MonoBehaviour
{
    private void Awake()
    {
        if (LocalizationManager.Instance != null)
            return;

        var go = new GameObject("LocalizationManager");
        go.AddComponent<LocalizationManager>();
    }
}
