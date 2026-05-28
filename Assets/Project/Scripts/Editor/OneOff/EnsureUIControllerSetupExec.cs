using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EnsureUIControllerSetupExec
{
    public static string Execute()
    {
        var windowsRoot = FindPath("UIRoot/Canvas_Windows/WindowsRoot");
        var uiRoot = FindPath("UIRoot");
        var menuWindow = FindPath("UIRoot/Canvas_Windows/WindowsRoot/MenuWindow");
        var hudRoot = FindPath("UIRoot/Canvas_HUD/HUDRoot");
        var hotbar = FindPath("UIRoot/Canvas_HUD/HUDRoot/Menu/ConTroller/Hotbar");
        var menuToggleContent = FindPath("UIRoot/Canvas_Windows/WindowsRoot/MenuWindow/Viewport/Content");
        var menuHud = FindPath("UIRoot/Canvas_HUD/HUDRoot/Menu");

        if (windowsRoot == null || menuWindow == null || hudRoot == null || hotbar == null || menuHud == null)
            return "[EnsureUIControllerSetupExec] Missing one or more required UI roots in scene.";

        if (uiRoot != null)
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(uiRoot);

        var controller = windowsRoot.GetComponent<UIController>();
        if (controller == null)
            controller = windowsRoot.AddComponent<UIController>();

        var so = new SerializedObject(controller);
        so.FindProperty("hudRoot").objectReferenceValue = hudRoot;
        so.FindProperty("windowsRoot").objectReferenceValue = windowsRoot;
        so.FindProperty("menuWindow").objectReferenceValue = menuWindow;
        so.FindProperty("hotbarRoot").objectReferenceValue = hotbar;
        so.FindProperty("menuToggleContainer").objectReferenceValue = menuToggleContent != null ? menuToggleContent.transform : null;
        so.FindProperty("menuToggleKey").enumValueIndex = (int)KeyCode.Tab;
        so.FindProperty("closeByEscape").boolValue = true;
        so.FindProperty("defaultWindowId").stringValue = "backpack";
        so.FindProperty("resetToClosedOnEnable").boolValue = true;
        so.FindProperty("hideHotbarWhenWindowOpen").boolValue = true;
        so.FindProperty("autoBindMenuToggles").boolValue = true;
        so.FindProperty("autoCreateTemplateWindows").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();

        var setupMethod = typeof(UIController).GetMethod("SetupMissingMenuWindows", BindingFlags.Instance | BindingFlags.NonPublic);
        setupMethod?.Invoke(controller, null);

        var inventoryWindowUi = menuHud.GetComponent<InventoryWindowUI>();
        if (inventoryWindowUi != null)
        {
            var soInventory = new SerializedObject(inventoryWindowUi);
            soInventory.FindProperty("uiController").objectReferenceValue = controller;
            soInventory.FindProperty("menuWindowRef").objectReferenceValue = menuWindow;
            soInventory.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorUtility.SetDirty(controller);
        if (inventoryWindowUi != null)
            EditorUtility.SetDirty(inventoryWindowUi);

        EditorSceneManager.MarkSceneDirty(windowsRoot.scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.Refresh();

        return "[EnsureUIControllerSetupExec] UIController setup completed.";
    }

    private static GameObject FindPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var segments = path.Split('/');
        var root = GameObject.Find(segments[0]);
        if (root == null) return null;

        Transform current = root.transform;
        for (int i = 1; i < segments.Length; i++)
        {
            current = current.Find(segments[i]);
            if (current == null) return null;
        }

        return current.gameObject;
    }
}
