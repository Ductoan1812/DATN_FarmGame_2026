using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class CreateSharedLoadingScreenPrefab
{
    private const string PrefabPath = "Assets/Project/Prefabs/UI/LoadingScreenView.prefab";
    private const string ConfigPath = "Assets/Project/Resources/UI/LoadingScreenPrefabConfig.asset";
    private const string RunnerSpritePath = "Assets/Project/Art/Characters/AnimationChibiLoadbar.png";
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    [MenuItem("Tools/DATN/One-off Setup/UI/Create Shared Loading Screen Prefab")]
    public static void Execute()
    {
        BuildPrefab();
    }

    public static void ExecuteSilently()
    {
        BuildPrefab();
    }

    private static void BuildPrefab()
    {
        EnsureFolder("Assets/Project/Prefabs");
        EnsureFolder("Assets/Project/Prefabs/UI");
        EnsureFolder("Assets/Project/Resources");
        EnsureFolder("Assets/Project/Resources/UI");

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        Sprite[] runnerFrames = AssetDatabase.LoadAllAssetsAtPath(RunnerSpritePath)
            .OfType<Sprite>()
            .OrderBy(sprite => sprite.name)
            .ToArray();

        var root = new GameObject("LoadingScreenView", typeof(RectTransform), typeof(CanvasGroup), typeof(LoadingScreenView));
        Stretch(root.GetComponent<RectTransform>());
        var canvasGroup = root.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        var background = CreateImage("Background", root.transform, new Color(0.73f, 0.93f, 1f, 1f));
        Stretch(background.rectTransform);

        var content = CreateObject("Content", root.transform);
        Anchor(content, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1160f, 620f));

        var titleRow = CreateObject("TitleRow", content);
        Anchor(titleRow, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -118f), new Vector2(620f, 64f));

        var leftIcon = CreateImage("LeftIcon", titleRow, new Color(1f, 1f, 1f, 0f));
        Anchor(leftIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(52f, 0f), new Vector2(48f, 48f));

        var title = CreateText("TitleText", titleRow, "ĐANG TẢI...", 36f, FontStyles.Bold, TextAlignmentOptions.Center, font, new Color(0.20f, 0.13f, 0.08f, 1f));
        Anchor(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 56f));

        var rightIcon = CreateImage("RightIcon", titleRow, new Color(1f, 1f, 1f, 0f));
        Anchor(rightIcon.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-52f, 0f), new Vector2(48f, 48f));

        var loadBarRoot = CreateObject("LoadBarRoot", content);
        Anchor(loadBarRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 194f), new Vector2(900f, 120f));

        var runnerRoot = CreateObject("RunnerRoot", loadBarRoot);
        Anchor(runnerRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-420f, 78f), new Vector2(142f, 156f));
        var runnerImage = CreateImage("RunnerImage", runnerRoot, Color.white);
        Stretch(runnerImage.rectTransform);
        runnerImage.preserveAspect = true;
        if (runnerFrames.Length > 0)
            runnerImage.sprite = runnerFrames[0];

        var trackFrame = CreateImage("ProgressFrame", loadBarRoot, new Color(0.48f, 0.23f, 0.08f, 1f));
        Anchor(trackFrame.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(900f, 54f));

        var track = CreateImage("ProgressTrack", trackFrame.transform, new Color(0.20f, 0.09f, 0.03f, 1f));
        Anchor(track.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(850f, 32f));

        var fill = CreateImage("ProgressFill", track.transform, new Color(0.47f, 0.86f, 0.12f, 1f));
        Stretch(fill.rectTransform);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 0f;

        var sceneText = CreateText("SceneText", content, "Đang tải cảnh", 28f, FontStyles.Bold, TextAlignmentOptions.Center, font, new Color(0.23f, 0.12f, 0.05f, 1f));
        Anchor(sceneText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 118f), new Vector2(820f, 42f));

        var percentText = CreateText("PercentText", content, "0%", 42f, FontStyles.Bold, TextAlignmentOptions.Center, font, new Color(0.36f, 0.55f, 0.08f, 1f));
        Anchor(percentText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 66f), new Vector2(360f, 56f));

        var tipText = CreateText("TipText", content, "Mẹo:", 22f, FontStyles.Bold, TextAlignmentOptions.Center, font, new Color(0.20f, 0.12f, 0.05f, 1f));
        tipText.enableWordWrapping = true;
        Anchor(tipText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(980f, 46f));

        ConfigureView(root.GetComponent<LoadingScreenView>(), title, sceneText, percentText, tipText, fill, runnerRoot, runnerImage, runnerFrames, canvasGroup);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        SaveConfig(prefab.GetComponent<LoadingScreenView>());
        EditorUtility.SetDirty(prefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CreateSharedLoadingScreenPrefab] Created {PrefabPath} and {ConfigPath}");
    }

    private static void SaveConfig(LoadingScreenView prefab)
    {
        var config = AssetDatabase.LoadAssetAtPath<LoadingScreenPrefabConfig>(ConfigPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<LoadingScreenPrefabConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
        }

        var so = new SerializedObject(config);
        so.FindProperty("prefab").objectReferenceValue = prefab;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(config);
    }

    private static void ConfigureView(
        LoadingScreenView view,
        TMP_Text titleText,
        TMP_Text sceneText,
        TMP_Text percentText,
        TMP_Text tipText,
        Image progressFill,
        RectTransform runnerRoot,
        Image runnerImage,
        Sprite[] runnerFrames,
        CanvasGroup canvasGroup)
    {
        var so = new SerializedObject(view);
        SetObject(so, "titleText", titleText);
        SetObject(so, "sceneText", sceneText);
        SetObject(so, "percentText", percentText);
        SetObject(so, "tipText", tipText);
        SetObject(so, "progressFill", progressFill);
        SetObject(so, "runnerRoot", runnerRoot);
        SetObject(so, "runnerImage", runnerImage);
        SetObject(so, "canvasGroup", canvasGroup);

        var framesProperty = so.FindProperty("runnerFrames");
        framesProperty.arraySize = runnerFrames.Length;
        for (int i = 0; i < runnerFrames.Length; i++)
            framesProperty.GetArrayElementAtIndex(i).objectReferenceValue = runnerFrames[i];

        so.FindProperty("runnerFrameRate").floatValue = 12f;
        so.FindProperty("runnerStartX").floatValue = -420f;
        so.FindProperty("runnerEndX").floatValue = 420f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObject(SerializedObject so, string propertyName, Object value)
    {
        so.FindProperty(propertyName).objectReferenceValue = value;
    }

    private static RectTransform CreateObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, FontStyles style, TextAlignmentOptions alignment, TMP_FontAsset font, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.font = font;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void Anchor(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private static void EnsureFolder(string folderPath)
    {
        string normalized = folderPath.Replace('\\', '/');
        if (AssetDatabase.IsValidFolder(normalized))
            return;

        string parent = Path.GetDirectoryName(normalized)?.Replace('\\', '/');
        string folderName = Path.GetFileName(normalized);
        if (!string.IsNullOrWhiteSpace(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, folderName);
    }
}
