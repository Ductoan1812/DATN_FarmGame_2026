using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class InspectInventoryDragDropSetup
{
    [MenuItem("Tools/DATN/Utilities/Inspect Inventory Drag Drop Setup")]
    public static void Execute()
    {
        foreach (var backpack in Object.FindObjectsByType<BackpackUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            InvokePrivate(backpack, "AutoAssignSlotsContainer");
            InvokePrivate(backpack, "EnsureViews");
            var slotsContainer = GetField<Transform>(backpack, "slotsContainer");
            Debug.Log($"[InspectInventoryDragDrop] BackpackUI='{GetPath(backpack.transform)}' active={backpack.gameObject.activeInHierarchy} slotsContainer='{GetPath(slotsContainer)}' childCount={(slotsContainer != null ? slotsContainer.childCount : -1)}");
            InspectSlots(slotsContainer, "Backpack");
        }

        foreach (var hotbar in Object.FindObjectsByType<HotbarUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Debug.Log($"[InspectInventoryDragDrop] HotbarUI='{GetPath(hotbar.transform)}' active={hotbar.gameObject.activeInHierarchy} childCount={hotbar.transform.childCount}");
            InspectSlots(hotbar.transform, "Hotbar");
        }

        foreach (var candidate in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!LooksLikeSlotContainer(candidate)) continue;
            Debug.Log($"[InspectInventoryDragDrop] CandidateSlotContainer path='{GetPath(candidate)}' active={candidate.gameObject.activeInHierarchy} childCount={candidate.childCount}");
        }
    }

    private static void InspectSlots(Transform container, string label)
    {
        if (container == null) return;

        int max = Mathf.Min(container.childCount, 12);
        for (int i = 0; i < max; i++)
        {
            var child = container.GetChild(i);
            var drag = child.GetComponent<DraggableSlot>();
            var graphic = child.GetComponent<Graphic>();
            var button = child.GetComponent<Button>();
            var toggle = child.GetComponent<Toggle>();
            var icon = child.Find("Icon")?.GetComponent<Image>() ?? child.Find("icon")?.GetComponent<Image>();

            Debug.Log(
                $"[InspectInventoryDragDrop] {label}[{i}] path='{GetPath(child)}' active={child.gameObject.activeInHierarchy} " +
                $"drag={(drag != null ? $"{drag.InventoryType}:{drag.SlotIndex}" : "null")} " +
                $"rootGraphic={(graphic != null ? $"{graphic.GetType().Name},raycast={graphic.raycastTarget}" : "null")} " +
                $"button={(button != null ? $"interactable={button.interactable}" : "null")} " +
                $"toggle={(toggle != null ? $"interactable={toggle.interactable}" : "null")} " +
                $"icon={(icon != null ? $"sprite={(icon.sprite != null ? icon.sprite.name : "null")},raycast={icon.raycastTarget},enabled={icon.enabled}" : "null")}");
        }
    }

    private static bool LooksLikeSlotContainer(Transform transform)
    {
        if (transform == null || transform.childCount < 4) return false;

        int slotLike = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            if (child.name.ToLowerInvariant().Contains("slot") ||
                child.Find("Icon") != null ||
                child.Find("icon") != null ||
                child.Find("Amount") != null)
            {
                slotLike++;
            }
        }

        return slotLike >= 4;
    }

    private static T GetField<T>(object target, string fieldName) where T : class
    {
        if (target == null) return null;
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        return field?.GetValue(target) as T;
    }

    private static void InvokePrivate(object target, string methodName)
    {
        if (target == null) return;
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        method?.Invoke(target, null);
    }

    private static string GetPath(Transform transform)
    {
        if (transform == null) return "null";
        var path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }
}
