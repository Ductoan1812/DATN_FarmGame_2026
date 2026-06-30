using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    public static event Action LocalizationReady;
    public static event Action LanguageChanged;

    [Header("Language")]
    [SerializeField] private Language currentLanguage = Language.Vi;

    [Header("Resources")]
    [SerializeField] private string resourcesFolder = "Localization";

    public bool IsReady { get; private set; }
    public Language CurrentLanguage => currentLanguage;

    private readonly Dictionary<string, string> localizedTexts = new Dictionary<string, string>();
    private readonly HashSet<string> missingKeys = new HashSet<string>();

    private const string PlayerPrefsLangKey = "settings_language";
    private const string MissingKeysFolderName = "MissingKeys";
    private const string MissingVietnameseKeysFileName = "vi_missing_keys.txt";
    private const string ProjectResourcesFolderName = "Project";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        if (transform.parent != null)
            transform.SetParent(null, false);

        DontDestroyOnLoad(gameObject);

        // Ưu tiên: 1) PlayerPrefs đã lưu, 2) default Vi (từ serialized field)
        if (PlayerPrefs.HasKey(PlayerPrefsLangKey))
            currentLanguage = PlayerPrefs.GetInt(PlayerPrefsLangKey, 0) == 1 ? Language.En : Language.Vi;

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
        var resourcesPath = GetPreferredResourcesRootPath();
        var localizationPath = Path.Combine(resourcesPath, resourcesFolder);
        var missingPath = Path.Combine(localizationPath, MissingKeysFolderName);

        if (!Directory.Exists(resourcesPath))
            Directory.CreateDirectory(resourcesPath);
        if (!Directory.Exists(localizationPath))
            Directory.CreateDirectory(localizationPath);
        if (!Directory.Exists(missingPath))
            Directory.CreateDirectory(missingPath);
    }

    private void WriteMissingKeyFile()
    {
        var missingKeysDirectory = Path.Combine(
            GetPreferredResourcesRootPath(),
            resourcesFolder,
            MissingKeysFolderName);
        var currentLanguageFilePath = Path.Combine(
            missingKeysDirectory,
            $"{currentLanguage.ToString().ToLowerInvariant()}_missing_keys.txt");
        var vietnameseFilePath = Path.Combine(missingKeysDirectory, MissingVietnameseKeysFileName);
        var orderedKeys = new List<string>(missingKeys);
        orderedKeys.Sort(StringComparer.Ordinal);

        try
        {
            File.WriteAllLines(currentLanguageFilePath, orderedKeys);

            if (!string.Equals(currentLanguageFilePath, vietnameseFilePath, StringComparison.OrdinalIgnoreCase))
                File.WriteAllLines(vietnameseFilePath, orderedKeys);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Localization] Could not write missing keys file: {ex.Message}");
        }
    }

    private static string GetPreferredResourcesRootPath()
    {
        var projectResourcesPath = Path.Combine(Application.dataPath, ProjectResourcesFolderName, "Resources");
        if (Directory.Exists(projectResourcesPath))
            return projectResourcesPath;

        return Path.Combine(Application.dataPath, "Resources");
    }

}
