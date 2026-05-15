using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class UIHierarchyMigrationTool
{
    private const string OldUiRootName = "_______UI_______________";
    private const string UiRootName = "UIRoot";
    private const string HudCanvasName = "Canvas_HUD";
    private const string OldHudCanvasName = "HUD_Canvas";
    private const string WindowsCanvasName = "Canvas_Windows";
    private const string OverlayCanvasName = "Canvas_Overlay";
    private const string DebugCanvasName = "Canvas_Debug";

    [MenuItem("Tools/DATN/UI/Migrate Current UI Hierarchy")]
    public static void Execute()
    {
        var uiRoot = GetOrCreateUiRoot();
        var rootController = GetOrAdd<UIRootController>(uiRoot);

        var hudCanvas = GetOrCreateCanvas(uiRoot.transform, HudCanvasName, OldHudCanvasName, 0);
        var windowsCanvas = GetOrCreateCanvas(uiRoot.transform, WindowsCanvasName, null, 20);
        var overlayCanvas = GetOrCreateCanvas(uiRoot.transform, OverlayCanvasName, null, 40);

        var hudRoot = GetOrCreateRect("HUDRoot", hudCanvas.transform);
        var windowsRoot = GetOrCreateRect("WindowsRoot", windowsCanvas.transform);
        var systemsRoot = GetOrCreatePlain("Systems", uiRoot.transform);

        MoveIfExists("Panel", hudCanvas.transform, hudRoot.transform);
        MoveIfExists("FloatingCombatTextSpawner", hudCanvas.transform, hudRoot.transform);
        MoveIfExists("Menu", hudCanvas.transform, hudRoot.transform);

        var menu = FindDeepChild(hudRoot.transform, "Menu");
        var inventoryWindow = FindDeepChild(menu, "Window");
        if (inventoryWindow != null)
        {
            Undo.RecordObject(inventoryWindow.gameObject, "Rename Inventory Window");
            inventoryWindow.name = "InventoryWindow";
            MoveKeepWorld(inventoryWindow, windowsRoot.transform);
        }

        var npcInteraction = FindDeepChild(hudCanvas.transform, "NPCInteractionUI")
                          ?? FindDeepChild(uiRoot.transform, "NPCInteractionUI");
        if (npcInteraction != null)
            MoveKeepWorld(npcInteraction, windowsRoot.transform);

        var uiManager = FindDeepChild(hudCanvas.transform, "UIManager")
                     ?? FindDeepChild(uiRoot.transform, "UIManager");
        if (uiManager != null)
            MoveKeepWorld(uiManager, systemsRoot.transform);

        ConfigureWindows(windowsRoot.transform, hudRoot.transform);
        ConfigureRootController(rootController, uiRoot.transform, windowsRoot.transform, hudRoot.transform);
        MoveDebugCanvas(uiRoot.transform);
        EnsureEventSystem(uiRoot.transform);

        EditorUtility.SetDirty(uiRoot);
        EditorSceneManager.MarkSceneDirty(uiRoot.scene);
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[UIHierarchyMigrationTool] UI hierarchy migrated to UIRoot/Canvas_HUD/Canvas_Windows/Canvas_Overlay/Canvas_Debug.");
    }

    private static void ConfigureWindows(Transform windowsRoot, Transform hudRoot)
    {
        var menu = FindDeepChild(hudRoot, "Menu");
        if (menu != null)
        {
            var inventoryUi = menu.GetComponent<InventoryWindowUI>();
            var legacyUi = menu.GetComponent("UISystem");
            if (inventoryUi == null && legacyUi != null)
            {
                inventoryUi = Undo.AddComponent<InventoryWindowUI>(menu.gameObject);
                CopyInventoryWindowBindings(legacyUi, inventoryUi);
                Undo.DestroyObjectImmediate(legacyUi);
            }
            else if (inventoryUi == null)
            {
                Undo.AddComponent<InventoryWindowUI>(menu.gameObject);
            }
        }

        var npcRoot = windowsRoot.Find("NPCInteractionUI");
        if (npcRoot != null)
        {
            EnsureWindowComponent(npcRoot, "DialoguePanel", "dialogue", true, true);
            EnsureWindowComponent(npcRoot, "QuestPanel", "quest", true, true);
            EnsureWindowComponent(npcRoot, "ShopPanel", "shop", true, true);
        }
    }

    private static void EnsureWindowComponent(Transform root, string childName, string id, bool lockInput, bool modal)
    {
        var panel = root.Find(childName);
        if (panel == null) return;

        var closeButton = FindDeepChild(panel, "CloseButton")?.GetComponent<Button>();
        ConfigureWindow(panel.gameObject, id, panel.gameObject, closeButton, lockInput, modal, true);
    }

    private static void ConfigureWindow(GameObject owner, string id, GameObject panel, Button closeButton, bool lockInput, bool modal, bool hideInitially)
    {
        var window = owner.GetComponent<UIWindow>();
        if (window == null)
            window = Undo.AddComponent<UIWindow>(owner);

        window.Configure(id, panel, closeButton, lockInput, modal, hideInitially);
        EditorUtility.SetDirty(window);
    }

    private static void ConfigureRootController(UIRootController controller, Transform uiRoot, Transform windowsRoot, Transform hudRoot)
    {
        if (controller == null) return;

        var serialized = new SerializedObject(controller);

        ConfigureCanvasRefs(serialized, uiRoot);
        ConfigureEntries(serialized, windowsRoot, hudRoot);

        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(controller);
    }

    private static void ConfigureCanvasRefs(SerializedObject serialized, Transform uiRoot)
    {
        var refs = serialized.FindProperty("canvasRefs");
        refs.ClearArray();

        AddCanvasRef(refs, "hud", FindDeepChild(uiRoot, HudCanvasName)?.GetComponent<Canvas>());
        AddCanvasRef(refs, "windows", FindDeepChild(uiRoot, WindowsCanvasName)?.GetComponent<Canvas>());
        AddCanvasRef(refs, "overlay", FindDeepChild(uiRoot, OverlayCanvasName)?.GetComponent<Canvas>());
        AddCanvasRef(refs, "debug", FindDeepChild(uiRoot, DebugCanvasName)?.GetComponent<Canvas>());
    }

    private static void ConfigureEntries(SerializedObject serialized, Transform windowsRoot, Transform hudRoot)
    {
        var entries = serialized.FindProperty("entries");
        entries.ClearArray();

        var inventoryWindow = windowsRoot.Find("InventoryWindow")?.gameObject;
        var menu = FindDeepChild(hudRoot, "Menu");
        var openButton = FindDeepChild(menu, "Open")?.GetComponent<Button>();
        var closeButton = FindDeepChild(menu, "Exit")?.GetComponent<Button>();
        AddEntry(
            entries,
            "inventory",
            inventoryWindow,
            hideOnAwake: true,
            toggleKey: KeyCode.I,
            openButton: openButton,
            closeButton: closeButton,
            modal: true,
            lockPlayerInput: true,
            showWhenOpen: new[] { inventoryWindow, closeButton != null ? closeButton.gameObject : null },
            hideWhenOpen: new[] { openButton != null ? openButton.gameObject : null },
            showWhenClosed: new[] { openButton != null ? openButton.gameObject : null },
            hideWhenClosed: new[] { closeButton != null ? closeButton.gameObject : null });

        var npcRoot = windowsRoot.Find("NPCInteractionUI");
        AddPanelEntry(entries, npcRoot, "dialogue", "DialoguePanel");
        AddPanelEntry(entries, npcRoot, "quest", "QuestPanel");
        AddPanelEntry(entries, npcRoot, "shop", "ShopPanel");
    }

    private static void AddPanelEntry(SerializedProperty entries, Transform root, string id, string panelName)
    {
        var panel = root != null ? root.Find(panelName)?.gameObject : null;
        var closeButton = panel != null ? FindDeepChild(panel.transform, "CloseButton")?.GetComponent<Button>() : null;
        AddEntry(
            entries,
            id,
            panel,
            hideOnAwake: true,
            toggleKey: KeyCode.None,
            openButton: null,
            closeButton: closeButton,
            modal: true,
            lockPlayerInput: true,
            showWhenOpen: new[] { panel },
            hideWhenOpen: null,
            showWhenClosed: null,
            hideWhenClosed: new[] { panel });
    }

    private static void AddCanvasRef(SerializedProperty refs, string id, Canvas canvas)
    {
        if (canvas == null) return;

        int index = refs.arraySize;
        refs.InsertArrayElementAtIndex(index);
        var item = refs.GetArrayElementAtIndex(index);
        item.FindPropertyRelative("id").stringValue = id;
        item.FindPropertyRelative("canvas").objectReferenceValue = canvas;
    }

    private static void AddEntry(
        SerializedProperty entries,
        string id,
        GameObject target,
        bool hideOnAwake,
        KeyCode toggleKey,
        Button openButton,
        Button closeButton,
        bool modal,
        bool lockPlayerInput,
        GameObject[] showWhenOpen,
        GameObject[] hideWhenOpen,
        GameObject[] showWhenClosed,
        GameObject[] hideWhenClosed)
    {
        if (target == null) return;

        int index = entries.arraySize;
        entries.InsertArrayElementAtIndex(index);
        var item = entries.GetArrayElementAtIndex(index);

        item.FindPropertyRelative("id").stringValue = id;
        item.FindPropertyRelative("target").objectReferenceValue = target;
        item.FindPropertyRelative("hideOnAwake").boolValue = hideOnAwake;
        item.FindPropertyRelative("toggleKey").intValue = (int)toggleKey;
        item.FindPropertyRelative("openButton").objectReferenceValue = openButton;
        item.FindPropertyRelative("closeButton").objectReferenceValue = closeButton;
        item.FindPropertyRelative("modal").boolValue = modal;
        item.FindPropertyRelative("lockPlayerInput").boolValue = lockPlayerInput;
        SetObjectList(item.FindPropertyRelative("showWhenOpen"), showWhenOpen);
        SetObjectList(item.FindPropertyRelative("hideWhenOpen"), hideWhenOpen);
        SetObjectList(item.FindPropertyRelative("showWhenClosed"), showWhenClosed);
        SetObjectList(item.FindPropertyRelative("hideWhenClosed"), hideWhenClosed);
    }

    private static void SetObjectList(SerializedProperty property, GameObject[] objects)
    {
        property.ClearArray();
        if (objects == null) return;

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] == null) continue;

            int index = property.arraySize;
            property.InsertArrayElementAtIndex(index);
            property.GetArrayElementAtIndex(index).objectReferenceValue = objects[i];
        }
    }

    private static void CopyInventoryWindowBindings(Component source, InventoryWindowUI target)
    {
        if (source == null || target == null) return;

        var src = new SerializedObject(source);
        var dst = new SerializedObject(target);

        CopyProperty(src, dst, "controllerPanel");
        CopyProperty(src, dst, "menuPanel");
        CopyProperty(src, dst, "panelShownWhenMenuOpen");
        CopyProperty(src, dst, "openButton");
        CopyProperty(src, dst, "closeButton");
        CopyProperty(src, dst, "tabToggles");
        CopyProperty(src, dst, "tabPanels");
        CopyProperty(src, dst, "toggleKey");

        dst.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    private static void CopyProperty(SerializedObject src, SerializedObject dst, string propertyName)
    {
        var srcProp = src.FindProperty(propertyName);
        var dstProp = dst.FindProperty(propertyName);
        if (srcProp == null || dstProp == null) return;

        dstProp.serializedObject.CopyFromSerializedProperty(srcProp);
    }

    private static GameObject GetOrCreateUiRoot()
    {
        var root = GameObject.Find(UiRootName);
        if (root != null) return root;

        root = GameObject.Find(OldUiRootName);
        if (root != null)
        {
            Undo.RecordObject(root, "Rename UI Root");
            root.name = UiRootName;
            return root;
        }

        root = new GameObject(UiRootName);
        Undo.RegisterCreatedObjectUndo(root, "Create UI Root");
        return root;
    }

    private static Canvas GetOrCreateCanvas(Transform root, string name, string oldName, int sortingOrder)
    {
        var canvasTransform = root.Find(name);
        if (canvasTransform == null && !string.IsNullOrEmpty(oldName))
            canvasTransform = root.Find(oldName) ?? GameObject.Find(oldName)?.transform;

        GameObject canvasObject;
        if (canvasTransform != null)
        {
            canvasObject = canvasTransform.gameObject;
            Undo.RecordObject(canvasObject, "Rename UI Canvas");
            canvasObject.name = name;
            if (canvasTransform.parent != root)
                MoveKeepWorld(canvasTransform, root);
        }
        else
        {
            canvasObject = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(canvasObject, "Create UI Canvas");
            canvasObject.transform.SetParent(root, false);
        }

        var rect = canvasObject.GetComponent<RectTransform>();
        Stretch(rect);

        var canvas = GetOrAdd<Canvas>(canvasObject);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = GetOrAdd<CanvasScaler>(canvasObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GetOrAdd<GraphicRaycaster>(canvasObject);
        return canvas;
    }

    private static RectTransform GetOrCreateRect(string name, Transform parent)
    {
        var child = parent.Find(name);
        GameObject go;
        if (child != null)
        {
            go = child.gameObject;
        }
        else
        {
            go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            go.transform.SetParent(parent, false);
        }

        var rect = go.GetComponent<RectTransform>();
        Stretch(rect);
        return rect;
    }

    private static GameObject GetOrCreatePlain(string name, Transform parent)
    {
        var child = parent.Find(name);
        if (child != null) return child.gameObject;

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void MoveIfExists(string name, Transform from, Transform to)
    {
        var child = from.Find(name) ?? FindDeepChild(from, name) ?? GameObject.Find(name)?.transform;
        if (child != null)
            MoveKeepWorld(child, to);
    }

    private static void MoveDebugCanvas(Transform uiRoot)
    {
        var existing = uiRoot.Find(DebugCanvasName);
        var debugCanvas = existing != null
            ? existing
            : GameObject.Find("DebugConsole_Canvas")?.transform;

        if (debugCanvas == null) return;

        Undo.RecordObject(debugCanvas.gameObject, "Rename Debug Canvas");
        debugCanvas.name = DebugCanvasName;
        MoveKeepWorld(debugCanvas, uiRoot);

        var canvas = GetOrAdd<Canvas>(debugCanvas.gameObject);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = GetOrAdd<CanvasScaler>(debugCanvas.gameObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GetOrAdd<GraphicRaycaster>(debugCanvas.gameObject);
    }

    private static void EnsureEventSystem(Transform uiRoot)
    {
        var eventSystem = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null) return;

        var systems = uiRoot.Find("Systems") ?? GetOrCreatePlain("Systems", uiRoot).transform;
        if (eventSystem.transform.parent != systems)
            MoveKeepWorld(eventSystem.transform, systems);
    }

    private static void MoveKeepWorld(Transform child, Transform newParent)
    {
        if (child == null || newParent == null || child.parent == newParent) return;

        Undo.SetTransformParent(child, newParent, $"Move {child.name}");
        child.SetParent(newParent, true);
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

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        if (component != null) return component;

        Undo.AddComponent<T>(go);
        return go.GetComponent<T>();
    }

    private static void Stretch(RectTransform rect)
    {
        if (rect == null) return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.localScale = Vector3.one;
    }
}
