using System.Text;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class UIControllerPlaytestExec
{
    public static string EnterPlayMode()
    {
        if (!EditorApplication.isPlaying)
            EditorApplication.isPlaying = true;

        return Snapshot();
    }

    public static string StopPlayMode()
    {
        if (EditorApplication.isPlaying)
            EditorApplication.isPlaying = false;

        return "Stopped PlayMode.";
    }

    public static string ToggleMenu()
    {
        var controller = Object.FindAnyObjectByType<UIController>(FindObjectsInactive.Include);
        if (controller == null) return "[UIControllerPlaytestExec] UIController not found.";

        controller.ToggleMenu();
        Canvas.ForceUpdateCanvases();
        return Snapshot();
    }

    public static string SelectBackpack() => SelectToggle("Toggle_backpack");
    public static string SelectEquipment() => SelectToggle("Toggle_equipment");
    public static string SelectSkills() => SelectToggle("Toggle_skill");
    public static string SelectMap() => SelectToggle("Toggle_map");
    public static string SelectQuest() => SelectToggle("Toggle_quest");
    public static string SelectSettings() => SelectToggle("Toggle_setting");
    public static string OpenBackpack() => OpenById("backpack");
    public static string OpenEquipment() => OpenById("equipment");
    public static string OpenSkills() => OpenById("skills");
    public static string OpenMap() => OpenById("map");
    public static string OpenQuest() => OpenById("quest");
    public static string OpenSettings() => OpenById("settings");
    public static string HoverFirstBackpackSlot() => SendBackpackPointerEvent(ExecuteEvents.pointerEnterHandler);
    public static string ExitFirstBackpackSlot() => SendBackpackPointerEvent(ExecuteEvents.pointerExitHandler);
    public static string InspectController() => Inspect();
    public static string InspectWindowChain() => InspectChain();

    public static string Snapshot()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"playMode={EditorApplication.isPlaying}");

        AppendActive(sb, "MenuWindow", "UIRoot/Canvas_Windows/WindowsRoot/MenuWindow");
        AppendActive(sb, "BackpackWindow", "UIRoot/Canvas_Windows/WindowsRoot/BackpackWindow");
        AppendActive(sb, "EquipmentWindow", "UIRoot/Canvas_Windows/WindowsRoot/EquipmentWindow");
        AppendActive(sb, "SkillsWindow", "UIRoot/Canvas_Windows/WindowsRoot/SkillsWindow");
        AppendActive(sb, "MapWindow", "UIRoot/Canvas_Windows/WindowsRoot/MapWindow");
        AppendActive(sb, "QuestWindow", "UIRoot/Canvas_Windows/WindowsRoot/QuestWindow");
        AppendActive(sb, "SettingsWindow", "UIRoot/Canvas_Windows/WindowsRoot/SettingsWindow");
        AppendActive(sb, "Hotbar", "UIRoot/Canvas_HUD/HUDRoot/Menu/ConTroller/Hotbar");

        var menuPath = "UIRoot/Canvas_Windows/WindowsRoot/MenuWindow/Viewport/Content";
        var menuRoot = FindPath(menuPath);
        if (menuRoot != null)
        {
            for (int i = 0; i < menuRoot.transform.childCount; i++)
            {
                var child = menuRoot.transform.GetChild(i);
                var toggle = child.GetComponent<Toggle>();
                if (toggle != null)
                    sb.AppendLine($"{child.name}.isOn={toggle.isOn}");
            }
        }

        return sb.ToString();
    }

    private static string SelectToggle(string toggleName)
    {
        var toggleGo = FindPath($"UIRoot/Canvas_Windows/WindowsRoot/MenuWindow/Viewport/Content/{toggleName}");
        if (toggleGo == null) return $"[UIControllerPlaytestExec] {toggleName} not found.";

        var toggle = toggleGo.GetComponent<Toggle>();
        if (toggle == null) return $"[UIControllerPlaytestExec] Toggle component missing on {toggleName}.";

        toggle.isOn = true;
        Canvas.ForceUpdateCanvases();
        return Snapshot();
    }

    private static string OpenById(string id)
    {
        var controller = Object.FindAnyObjectByType<UIController>(FindObjectsInactive.Include);
        if (controller == null) return "[UIControllerPlaytestExec] UIController not found.";

        controller.Open(id);
        Canvas.ForceUpdateCanvases();
        return Snapshot();
    }

    private static string SendBackpackPointerEvent<T>(ExecuteEvents.EventFunction<T> eventFunction)
        where T : IEventSystemHandler
    {
        var slot = FindPath(
            "UIRoot/Canvas_Windows/WindowsRoot/BackpackWindow/Body/GridPanel/SlotScrollView/Viewport/Content/Slot_toggle");
        if (slot == null) return "[UIControllerPlaytestExec] First backpack slot not found.";

        var eventSystem = Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include);
        if (eventSystem == null) return "[UIControllerPlaytestExec] EventSystem not found.";

        ExecuteEvents.Execute(slot, new PointerEventData(eventSystem), eventFunction);
        Canvas.ForceUpdateCanvases();
        return Snapshot();
    }

    private static string Inspect()
    {
        var controller = Object.FindAnyObjectByType<UIController>(FindObjectsInactive.Include);
        if (controller == null) return "[UIControllerPlaytestExec] UIController not found.";

        var sb = new StringBuilder();
        sb.AppendLine($"controller={controller.name}");
        sb.AppendLine($"controllerPath={GetTransformPath(controller.transform)}");

        var entriesField = typeof(UIController).GetField("entries", BindingFlags.Instance | BindingFlags.NonPublic);
        var mapField = typeof(UIController).GetField("entryMap", BindingFlags.Instance | BindingFlags.NonPublic);
        var defaultField = typeof(UIController).GetField("defaultWindowId", BindingFlags.Instance | BindingFlags.NonPublic);
        var menuField = typeof(UIController).GetField("menuWindow", BindingFlags.Instance | BindingFlags.NonPublic);
        var hotbarField = typeof(UIController).GetField("hotbarRoot", BindingFlags.Instance | BindingFlags.NonPublic);

        if (defaultField != null)
            sb.AppendLine($"defaultWindowId={defaultField.GetValue(controller)}");

        if (menuField?.GetValue(controller) is GameObject menu)
            sb.AppendLine($"menuWindowRef={GetTransformPath(menu.transform)} active={menu.activeInHierarchy}");

        if (hotbarField?.GetValue(controller) is GameObject hotbar)
            sb.AppendLine($"hotbarRef={GetTransformPath(hotbar.transform)} active={hotbar.activeInHierarchy}");

        if (entriesField?.GetValue(controller) is System.Collections.IList list)
        {
            sb.AppendLine($"entriesCount={list.Count}");
            for (int i = 0; i < list.Count; i++)
            {
                var entry = list[i];
                if (entry == null) continue;

                var entryType = entry.GetType();
                var idField = entryType.GetField("id");
                var toggleField = entryType.GetField("toggle");
                var windowField = entryType.GetField("window");

                string id = idField?.GetValue(entry) as string ?? "<null>";
                var toggleObj = toggleField?.GetValue(entry) as Toggle;
                var windowObj = windowField?.GetValue(entry) as GameObject;

                var toggleName = toggleObj != null ? toggleObj.name : "<none>";
                var windowName = windowObj != null ? windowObj.name : "<none>";
                var togglePath = toggleObj != null ? GetTransformPath(toggleObj.transform) : "<none>";
                var windowPath = windowObj != null ? GetTransformPath(windowObj.transform) : "<none>";
                var activeInHierarchy = windowObj != null && windowObj.activeInHierarchy;
                var activeSelf = windowObj != null && windowObj.activeSelf;
                sb.AppendLine($"entry[{i}] id={id} toggle={toggleName} togglePath={togglePath} window={windowName} windowPath={windowPath} activeSelf={activeSelf} active={activeInHierarchy}");
            }
        }

        if (mapField?.GetValue(controller) is System.Collections.IDictionary dict)
            sb.AppendLine($"entryMapCount={dict.Count}");

        return sb.ToString();
    }

    private static string InspectChain()
    {
        var sb = new StringBuilder();
        string[] paths =
        {
            "UIRoot",
            "UIRoot/Canvas_HUD",
            "UIRoot/Canvas_HUD/HUDRoot",
            "UIRoot/Canvas_Windows",
            "UIRoot/Canvas_Windows/WindowsRoot",
            "UIRoot/Canvas_Windows/WindowsRoot/MenuWindow",
            "UIRoot/Canvas_Windows/WindowsRoot/SettingsWindow",
            "UIRoot/Canvas_Windows/WindowsRoot/SkillsWindow",
            "UIRoot/Canvas_Windows/WindowsRoot/QuestWindow"
        };

        for (int i = 0; i < paths.Length; i++)
        {
            var go = FindPathIncludingInactive(paths[i]);
            if (go == null)
            {
                sb.AppendLine($"{paths[i]} => <missing>");
                continue;
            }

            sb.AppendLine($"{paths[i]} => activeSelf={go.activeSelf}, activeInHierarchy={go.activeInHierarchy}");
        }

        return sb.ToString();
    }

    private static void AppendActive(StringBuilder sb, string label, string path)
    {
        var go = FindPath(path);
        sb.AppendLine($"{label}={(go != null && go.activeInHierarchy)}");
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

    private static GameObject FindPathIncludingInactive(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var segments = path.Split('/');
        Transform root = null;
        var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].parent == null && all[i].name == segments[0])
            {
                root = all[i];
                break;
            }
        }
        if (root == null) return null;

        Transform current = root;
        for (int i = 1; i < segments.Length; i++)
        {
            var next = current.Find(segments[i]);
            if (next == null) return null;
            current = next;
        }

        return current.gameObject;
    }

    private static string GetTransformPath(Transform t)
    {
        if (t == null) return "<null>";

        var sb = new StringBuilder(t.name);
        while (t.parent != null)
        {
            t = t.parent;
            sb.Insert(0, "/");
            sb.Insert(0, t.name);
        }

        return sb.ToString();
    }
}
