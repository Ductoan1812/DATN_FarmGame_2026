using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftingRecipeRowUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text ingredientText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Button rowButton;
    [SerializeField] private Button craftButton;
    [SerializeField] private Image background;
    [SerializeField] private Color normalColor = new(0.36f, 0.22f, 0.10f, 0.92f);
    [SerializeField] private Color selectedColor = new(0.58f, 0.36f, 0.13f, 0.98f);
    [SerializeField] private Color lockedColor = new(0.18f, 0.15f, 0.12f, 0.88f);
    [SerializeField] private Color readyTextColor = new(0.45f, 0.95f, 0.25f, 1f);
    [SerializeField] private Color missingTextColor = new(1f, 0.55f, 0.18f, 1f);
    [SerializeField] private Color lockedTextColor = new(1f, 0.78f, 0.25f, 1f);

    private CraftingRecipeViewData currentView;
    private Action<CraftingRecipeViewData> onClick;

    public void Init(CraftingRecipeViewData viewData, Action<CraftingRecipeViewData> onClickCallback)
    {
        Init(viewData, onClickCallback, false);
    }

    public void Init(CraftingRecipeViewData viewData, Action<CraftingRecipeViewData> onClickCallback, bool selected)
    {
        AutoFindRefs();
        currentView = viewData;
        onClick = onClickCallback;

        var primaryOutput = viewData?.Recipe?.outputs != null && viewData.Recipe.outputs.Count > 0
            ? viewData.Recipe.outputs[0]?.item
            : null;

        SetIcon(primaryOutput);
        SetLocalizedText(nameText, !string.IsNullOrWhiteSpace(viewData?.Recipe?.titleKey)
            ? viewData.Recipe.titleKey
            : primaryOutput?.keyName);

        SetPlainText(levelText, viewData?.Recipe != null ? $"Lv {viewData.Recipe.requiredLevel}" : string.Empty);
        SetPlainText(ingredientText, BuildIngredientText(viewData));
        ApplyStateColors(viewData, selected);

        if (rowButton != null)
        {
            rowButton.onClick.RemoveAllListeners();
            rowButton.onClick.AddListener(OnClicked);
            rowButton.interactable = viewData != null;
        }

        if (craftButton != null)
        {
            craftButton.onClick.RemoveAllListeners();
            craftButton.onClick.AddListener(OnClicked);
            craftButton.interactable = viewData != null;
            SetButtonLabel(craftButton, GetStatusText(viewData));
        }
    }

    private void OnClicked()
    {
        if (currentView != null)
            onClick?.Invoke(currentView);
    }

    private void AutoFindRefs()
    {
        icon ??= FindImage(transform, "Icon");
        nameText ??= FindText(transform, "NameText");
        ingredientText ??= FindText(transform, "IngredientText");
        levelText ??= FindText(transform, "LevelText");
        rowButton ??= GetComponent<Button>();
        craftButton ??= FindButton(transform, "CraftButton");
        background ??= GetComponent<Image>();
    }

    private void SetIcon(EntityData itemData)
    {
        if (icon == null) return;
        icon.sprite = itemData != null ? itemData.icon : null;
        icon.enabled = icon.sprite != null;
        icon.preserveAspect = true;
    }

    private static string BuildIngredientText(CraftingRecipeViewData viewData)
    {
        if (viewData?.Ingredients == null || viewData.Ingredients.Count == 0)
            return string.Empty;

        var builder = new StringBuilder();
        foreach (var ingredient in viewData.Ingredients)
        {
            if (ingredient?.Item == null) continue;
            if (builder.Length > 0) builder.Append("  ");
            builder.Append(Localize(ingredient.Item.keyName));
            builder.Append(": ");
            builder.Append(ingredient.CurrentAmount);
            builder.Append("/");
            builder.Append(ingredient.RequiredAmount);
        }
        return builder.ToString();
    }

    private void ApplyStateColors(CraftingRecipeViewData viewData, bool selected)
    {
        if (background == null)
            background = GetComponent<Image>();

        if (background != null)
            background.color = selected ? selectedColor : (viewData != null && !viewData.LevelOk ? lockedColor : normalColor);

        if (ingredientText != null)
            ingredientText.color = viewData != null && viewData.HasIngredients ? readyTextColor : missingTextColor;

        if (levelText != null)
            levelText.color = viewData != null && viewData.LevelOk ? readyTextColor : lockedTextColor;
    }

    private static string GetStatusText(CraftingRecipeViewData viewData)
    {
        if (viewData == null) return string.Empty;
        if (!viewData.LevelOk) return $"Lv.{viewData.Recipe.requiredLevel}";
        if (!viewData.HasIngredients) return "Thiếu";
        return "Chọn";
    }

    private static void SetButtonLabel(Button button, string value)
    {
        var text = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
        if (text != null)
            text.text = value ?? string.Empty;
    }

    private static string Localize(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;
        return LocalizationManager.Instance != null ? LocalizationManager.Instance.GetText(key) : key;
    }

    private static void SetLocalizedText(TMP_Text text, string key)
    {
        if (text == null) return;

        if (string.IsNullOrWhiteSpace(key))
        {
            text.text = string.Empty;
            return;
        }

        var localized = text.GetComponent<LocalizedText>();
        if (localized == null)
            localized = text.gameObject.AddComponent<LocalizedText>();

        localized.SetKey(key);
    }

    private static void SetPlainText(TMP_Text text, string value)
    {
        if (text == null) return;
        text.text = value ?? string.Empty;
    }

    private static Image FindImage(Transform root, string name)
    {
        var child = FindDeepChild(root, name);
        return child != null ? child.GetComponent<Image>() : null;
    }

    private static TMP_Text FindText(Transform root, string name)
    {
        var child = FindDeepChild(root, name);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private static Button FindButton(Transform root, string name)
    {
        var child = FindDeepChild(root, name);
        return child != null ? child.GetComponent<Button>() : null;
    }

    private static Transform FindDeepChild(Transform root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name)) return null;
        if (root.name == name) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindDeepChild(root.GetChild(i), name);
            if (found != null) return found;
        }

        return null;
    }
}
