using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    public static event Action LocalizationReady;
    public static event Action LanguageChanged;

    [Header("Language")]
    [SerializeField] private Language currentLanguage = Language.Vi;
    [SerializeField] private bool detectSystemLanguageOnStart = true;

    [Header("Resources")]
    [SerializeField] private string resourcesFolder = "Localization";

    public bool IsReady { get; private set; }
    public Language CurrentLanguage => currentLanguage;

    private readonly Dictionary<string, string> localizedTexts = new Dictionary<string, string>();
    private readonly HashSet<string> missingKeys = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (detectSystemLanguageOnStart)
            currentLanguage = MapSystemLanguage(Application.systemLanguage);

        LoadCurrentLanguage();
    }

    public void SetLanguage(Language language)
    {
        if (currentLanguage == language && IsReady)
            return;

        currentLanguage = language;
        LoadCurrentLanguage();
        LanguageChanged?.Invoke();
    }

    public string GetText(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        if (!IsReady)
            return key;

        if (localizedTexts.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
            return value;

        RegisterMissingKey(key);
        return key;
    }

    public string GetText(string key, params object[] args)
    {
        var template = GetText(key);
        return args == null || args.Length == 0 ? template : string.Format(template, args);
    }

    private void LoadCurrentLanguage()
    {
        localizedTexts.Clear();
        missingKeys.Clear();
        IsReady = false;

        var assetPath = $"{resourcesFolder}/{currentLanguage.ToString().ToLowerInvariant()}";
        var textAsset = Resources.Load<TextAsset>(assetPath);

        if (textAsset == null)
        {
            Debug.LogWarning($"[Localization] Missing language file at Resources/{assetPath}.json");
            EnsureLocalizationFoldersExist();
            IsReady = true;
            LocalizationReady?.Invoke();
            return;
        }

        var file = JsonUtility.FromJson<LocalizationFile>(textAsset.text);
        if (file?.entries != null)
        {
            foreach (var entry in file.entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                    continue;

                localizedTexts[entry.key] = entry.value ?? string.Empty;
            }
        }

        IsReady = true;
        LocalizationReady?.Invoke();
    }

    private void RegisterMissingKey(string key)
    {
        if (!missingKeys.Add(key))
            return;

        Debug.LogWarning($"[Localization] Missing key '{key}' for language '{currentLanguage}'.");
        EnsureLocalizationFoldersExist();
        WriteMissingKeyFile();
    }

    private void EnsureLocalizationFoldersExist()
    {
        var resourcesPath = Path.Combine(Application.dataPath, "Resources");
        var localizationPath = Path.Combine(resourcesPath, "Localization");
        var missingPath = Path.Combine(localizationPath, "MissingKeys");

        if (!Directory.Exists(resourcesPath))
            Directory.CreateDirectory(resourcesPath);
        if (!Directory.Exists(localizationPath))
            Directory.CreateDirectory(localizationPath);
        if (!Directory.Exists(missingPath))
            Directory.CreateDirectory(missingPath);
    }

    private void WriteMissingKeyFile()
    {
        var filePath = Path.Combine(Application.dataPath, "Resources", "Localization", "MissingKeys",
            $"{currentLanguage.ToString().ToLowerInvariant()}_missing_keys.txt");

        try
        {
            File.WriteAllLines(filePath, missingKeys);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Localization] Could not write missing keys file: {ex.Message}");
        }
    }

    private static Language MapSystemLanguage(SystemLanguage language)
    {
        switch (language)
        {
            case SystemLanguage.Vietnamese:
                return Language.Vi;
            default:
                return Language.En;
        }
    }
}
