using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Applies the project's existing UI art to rough UIRoot panels without rebuilding gameplay wiring.
/// </summary>
public static class PolishUIRootSprites
{
    private const string UIRootPrefabPath = "Assets/Project/Prefabs/Systems/UIRoot.prefab";

    [MenuItem("Tools/DATN/One-off Setup/UI/Polish UIRoot Panel Sprites")]
    public static void Execute()
    {
        var root = PrefabUtility.LoadPrefabContents(UIRootPrefabPath);
        try
        {
            ApplyPanel(root.transform, "ShopPanel", "Assets/Project/Art/UI/Panels/ShopPanel.png", Color.white);
            ApplyPanel(root.transform, "DialoguePanel", "Assets/Project/Art/UI/Panels/DialoguePanel.png", Color.white);
            ApplyPanel(root.transform, "CraftingPanel", "Assets/Project/Art/UI/Panels/Panel_Menu.png", new Color(1f, 1f, 1f, 0.96f));
            ApplyPanel(root.transform, "QuestWindow", "Assets/Project/Art/UI/Panels/Panel_Menu.png", new Color(1f, 1f, 1f, 0.96f));
            ApplyPanel(root.transform, "SettingsWindow", "Assets/Project/Art/UI/Panels/Panel_Menu.png", new Color(1f, 1f, 1f, 0.96f));
            ApplyPanel(root.transform, "HudStatusMapPanel", "Assets/Project/Art/UI/Panels/Panel_Info_Player.png", new Color(1f, 1f, 1f, 0.96f));
            ApplyPanel(root.transform, "MapWindow", "Assets/Project/Art/UI/Panels/Panel_Menu.png", new Color(1f, 1f, 1f, 0.96f));
            ApplyPanel(root.transform, "EquipmentWindow", "Assets/Project/Art/UI/Panels/Panel_Menu.png", new Color(1f, 1f, 1f, 0.96f));
            ApplyPanel(root.transform, "BackpackWindow", "Assets/Project/Art/UI/Panels/Panel_Menu.png", new Color(1f, 1f, 1f, 0.96f));

            ApplyChildPanel(root.transform, "CraftingPanel", "RecipePanel", "Assets/Project/Art/UI/Panels/Panel_listMenu.png", new Color(1f, 1f, 1f, 0.82f));
            ApplyChildPanel(root.transform, "CraftingPanel", "DetailPanel", "Assets/Project/Art/UI/Panels/Panel_InfoItem.png", new Color(1f, 1f, 1f, 0.86f));
            ApplyChildPanel(root.transform, "QuestWindow", "Body", "Assets/Project/Art/UI/Panels/Panel_listMenu.png", new Color(1f, 1f, 1f, 0.82f));
            ApplyChildPanel(root.transform, "SettingsWindow", "Body", "Assets/Project/Art/UI/Panels/Panel_listMenu.png", new Color(1f, 1f, 1f, 0.82f));

            PrefabUtility.SaveAsPrefabAsset(root, UIRootPrefabPath);
            Debug.Log("[PolishUIRootSprites] Applied report-ready sprites to UIRoot panels.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ApplyPanel(Transform root, string objectName, string spritePath, Color color)
    {
        var target = FindDeepChild(root, objectName);
        if (target == null)
        {
            Debug.LogWarning($"[PolishUIRootSprites] Missing panel '{objectName}'.");
            return;
        }

        var image = target.GetComponent<Image>() ?? target.gameObject.AddComponent<Image>();
        ApplyImage(image, spritePath, color);
    }

    private static void ApplyChildPanel(Transform root, string parentName, string childName, string spritePath, Color color)
    {
        var parent = FindDeepChild(root, parentName);
        var child = FindDeepChild(parent, childName);
        if (child == null)
            return;

        var image = child.GetComponent<Image>() ?? child.gameObject.AddComponent<Image>();
        ApplyImage(image, spritePath, color);
    }

    private static void ApplyImage(Image image, string spritePath, Color color)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
        }

        image.color = color;
        image.raycastTarget = true;
    }

    private static Transform FindDeepChild(Transform root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name))
            return null;

        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindDeepChild(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }
}
