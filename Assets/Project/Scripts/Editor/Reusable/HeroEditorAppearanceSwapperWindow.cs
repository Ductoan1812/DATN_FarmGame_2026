using System;
using System.Collections.Generic;
using System.Linq;
using Assets.HeroEditor4D.Common.Scripts.CharacterScripts;
using Assets.HeroEditor4D.Common.Scripts.Collections;
using Assets.HeroEditor4D.Common.Scripts.Data;
using Assets.HeroEditor4D.Common.Scripts.Enums;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
using HeroBodyPart = Assets.HeroEditor4D.Common.Scripts.Enums.BodyPart;
using HeroEquipmentPart = Assets.HeroEditor4D.Common.Scripts.Enums.EquipmentPart;

public class HeroEditorAppearanceSwapperWindow : EditorWindow
{
    private enum PartMode
    {
        Body,
        Equipment
    }

    private Character4D targetCharacter;
    private SpriteCollection spriteCollection;
    private PartMode partMode = PartMode.Equipment;
    private HeroBodyPart bodyPart = HeroBodyPart.Hair;
    private HeroEquipmentPart equipmentPart = HeroEquipmentPart.MeleeWeapon1H;
    private bool assignCollectionToCharacter = true;
    private bool useColorOverride;
    private Color colorOverride = Color.white;
    private string searchText = string.Empty;
    private string selectedItemId = string.Empty;
    private Vector2 windowScroll;
    private Vector2 itemScroll;
    private int lastPartKey = int.MinValue;

    [MenuItem("Tools/DATN/Appearance/Hero Appearance Swapper")]
    public static void OpenWindow()
    {
        var window = GetWindow<HeroEditorAppearanceSwapperWindow>("Appearance Swapper");
        window.minSize = new Vector2(460f, 520f);
        window.TryAdoptSelection(force: true);
        window.RefreshSelectionFromCurrentPart(force: true);
        window.Show();
    }

    private void OnEnable()
    {
        TryAdoptSelection(force: false);
        RefreshSelectionFromCurrentPart(force: true);
    }

    private void OnSelectionChange()
    {
        if (TryAdoptSelection(force: false))
        {
            RefreshSelectionFromCurrentPart(force: true);
            Repaint();
        }
    }

    private void OnGUI()
    {
        windowScroll = EditorGUILayout.BeginScrollView(windowScroll);
        DrawHeader();
        DrawTargetSection();

        using (new EditorGUI.DisabledScope(targetCharacter == null))
        {
            DrawCollectionSection();
            DrawPartSection();
            DrawColorSection();
            DrawItemSection();
            DrawActionSection();
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("HeroEditor4D Prefab Swapper", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Đổi sprite trực tiếp trên prefab/instance Human bằng SpriteCollection hiện có. Phù hợp để đổi tool, tóc, mũ và chuẩn bị pose animation mà không cần Play Mode.",
            MessageType.Info);
    }

    private void DrawTargetSection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        targetCharacter = (Character4D)EditorGUILayout.ObjectField(
            new GUIContent("Human / Character4D"),
            targetCharacter,
            typeof(Character4D),
            true);
        if (EditorGUI.EndChangeCheck())
        {
            if (targetCharacter != null && spriteCollection == null)
                spriteCollection = targetCharacter.SpriteCollection;

            RefreshSelectionFromCurrentPart(force: true);
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Use Selection"))
        {
            TryAdoptSelection(force: true);
            RefreshSelectionFromCurrentPart(force: true);
        }

        if (GUILayout.Button("Ping Target"))
        {
            EditorGUIUtility.PingObject(targetCharacter);
        }
        EditorGUILayout.EndHorizontal();

        if (targetCharacter == null)
        {
            EditorGUILayout.HelpBox("Hãy chọn prefab/instance có component Character4D, ví dụ Human hoặc Player/Human child.", MessageType.Warning);
        }
        else
        {
            var targetPath = AssetDatabase.GetAssetPath(targetCharacter.gameObject);
            if (!string.IsNullOrEmpty(targetPath))
                EditorGUILayout.LabelField("Asset Path", targetPath);
            else
                EditorGUILayout.LabelField("Scene Object", targetCharacter.gameObject.scene.path);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawCollectionSection()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Sprite Collection", EditorStyles.boldLabel);

        var detectedCollection = targetCharacter != null ? targetCharacter.SpriteCollection : null;
        if (spriteCollection == null && detectedCollection != null)
            spriteCollection = detectedCollection;

        EditorGUI.BeginChangeCheck();
        spriteCollection = (SpriteCollection)EditorGUILayout.ObjectField(
            new GUIContent("Collection"),
            spriteCollection,
            typeof(SpriteCollection),
            false);
        if (EditorGUI.EndChangeCheck())
        {
            RefreshSelectionFromCurrentPart(force: true);
        }

        assignCollectionToCharacter = EditorGUILayout.ToggleLeft(
            "Gán collection này lại cho toàn bộ 4 hướng của Human trước khi apply",
            assignCollectionToCharacter);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Use Target Collection"))
        {
            spriteCollection = detectedCollection;
            RefreshSelectionFromCurrentPart(force: true);
        }

        if (GUILayout.Button("Apply Collection Only"))
        {
            AssignCollectionToCharacter();
        }
        EditorGUILayout.EndHorizontal();

        if (spriteCollection == null)
        {
            EditorGUILayout.HelpBox("Chưa có SpriteCollection để duyệt sprite.", MessageType.Warning);
        }
        else if (detectedCollection != null && spriteCollection != detectedCollection)
        {
            EditorGUILayout.HelpBox("Collection đang chọn khác collection hiện gắn trên target. Nếu muốn serializer/runtime nhận đúng nguồn, nên bật tùy chọn gán collection trước khi apply.", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawPartSection()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Part", EditorStyles.boldLabel);

        var newMode = (PartMode)GUILayout.Toolbar((int)partMode, new[] { "Body Parts", "Equipment" });
        if (newMode != partMode)
        {
            partMode = newMode;
            RefreshSelectionFromCurrentPart(force: true);
        }

        EditorGUI.BeginChangeCheck();
        if (partMode == PartMode.Body)
        {
            bodyPart = (HeroBodyPart)EditorGUILayout.EnumPopup("Body Part", bodyPart);
        }
        else
        {
            equipmentPart = (HeroEquipmentPart)EditorGUILayout.EnumPopup("Equipment Part", equipmentPart);
        }
        if (EditorGUI.EndChangeCheck())
        {
            RefreshSelectionFromCurrentPart(force: true);
        }

        string currentId = TryDetectCurrentItemId(spriteCollection);
        EditorGUILayout.LabelField("Current", string.IsNullOrEmpty(currentId) ? "<none>" : currentId);

        if (CurrentPartIsUnsupported())
        {
            EditorGUILayout.HelpBox("Part này không có list tương ứng trong SpriteCollection hiện tại, nên tool chưa hỗ trợ trực tiếp ở đây.", MessageType.Warning);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawColorSection()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Color Override", EditorStyles.boldLabel);

        bool supportsColor = CurrentPartSupportsColor();
        using (new EditorGUI.DisabledScope(!supportsColor))
        {
            useColorOverride = EditorGUILayout.ToggleLeft("Apply màu ngay khi đổi part", useColorOverride);
            using (new EditorGUI.DisabledScope(!useColorOverride))
            {
                colorOverride = EditorGUILayout.ColorField("Color", colorOverride);
            }
        }

        if (!supportsColor)
        {
            EditorGUILayout.HelpBox("Part hiện tại chủ yếu chỉ đổi sprite, không có lợi ích rõ khi override màu.", MessageType.None);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawItemSection()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Sprites", EditorStyles.boldLabel);

        var availableItems = GetActiveItems(spriteCollection);
        if (availableItems.Count == 0)
        {
            EditorGUILayout.HelpBox("Không có item nào cho part này trong SpriteCollection.", MessageType.Warning);
            EditorGUILayout.EndVertical();
            return;
        }

        int currentPartKey = GetCurrentPartKey();
        if (currentPartKey != lastPartKey)
        {
            lastPartKey = currentPartKey;
            RefreshSelectionFromCurrentPart(force: true);
        }

        searchText = EditorGUILayout.TextField("Search", searchText);

        var filteredItems = availableItems
            .Where(item => MatchesSearch(item, searchText))
            .ToList();

        if (filteredItems.Count == 0)
        {
            EditorGUILayout.HelpBox("Không có item nào khớp với từ khóa tìm kiếm.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        if (string.IsNullOrEmpty(selectedItemId) || filteredItems.All(item => item.Id != selectedItemId))
        {
            selectedItemId = filteredItems[0].Id;
        }

        int selectedIndex = Mathf.Max(0, filteredItems.FindIndex(item => item.Id == selectedItemId));
        var displayOptions = filteredItems.Select(FormatItemLabel).ToArray();

        EditorGUI.BeginChangeCheck();
        selectedIndex = EditorGUILayout.Popup("Sprite", selectedIndex, displayOptions);
        if (EditorGUI.EndChangeCheck() && selectedIndex >= 0 && selectedIndex < filteredItems.Count)
        {
            selectedItemId = filteredItems[selectedIndex].Id;
        }

        var selectedItem = filteredItems.FirstOrDefault(item => item.Id == selectedItemId) ?? filteredItems[0];
        selectedItemId = selectedItem.Id;

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("Preview", GetPreviewSprite(selectedItem), typeof(Sprite), false);
        }

        EditorGUILayout.LabelField("Count", filteredItems.Count.ToString());
        EditorGUILayout.LabelField("Item Id", selectedItem.Id);

        if (GUILayout.Button("Apply Selected Sprite", GUILayout.Height(24f)))
            ApplySelectedItem(selectedItem);

        itemScroll = EditorGUILayout.BeginScrollView(itemScroll, GUILayout.MinHeight(160f));
        foreach (var item in filteredItems)
        {
            bool isSelected = item.Id == selectedItemId;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Toggle(isSelected, GUIContent.none, GUILayout.Width(18f)) != isSelected)
                    selectedItemId = item.Id;

                if (GUILayout.Button(FormatItemLabel(item), isSelected ? EditorStyles.miniButtonMid : EditorStyles.miniButton, GUILayout.Height(22f)))
                    selectedItemId = item.Id;
            }
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();
    }

    private void DrawActionSection()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

        var selectedItem = GetActiveItems(spriteCollection).FirstOrDefault(item => item.Id == selectedItemId);

        using (new EditorGUI.DisabledScope(selectedItem == null))
        {
            if (GUILayout.Button("Apply Selected Sprite", GUILayout.Height(28f)))
                ApplySelectedItem(selectedItem);
        }

        using (new EditorGUI.DisabledScope(!CanClearCurrentPart()))
        {
            if (GUILayout.Button("Clear Current Part"))
                ClearCurrentPart();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh From Target"))
            RefreshSelectionFromCurrentPart(force: true);

        if (GUILayout.Button("Save Assets"))
            AssetDatabase.SaveAssets();
        EditorGUILayout.EndHorizontal();

        if (targetCharacter != null && PrefabUtility.IsPartOfPrefabInstance(targetCharacter.gameObject))
        {
            if (GUILayout.Button("Apply Prefab Overrides"))
                PrefabUtility.ApplyPrefabInstance(targetCharacter.gameObject, InteractionMode.UserAction);
        }

        EditorGUILayout.EndVertical();
    }

    private bool TryAdoptSelection(bool force)
    {
        var resolved = ResolveCharacter(Selection.activeObject);
        if (resolved == null)
            return false;

        if (!force && resolved == targetCharacter)
            return false;

        targetCharacter = resolved;
        spriteCollection = targetCharacter.SpriteCollection;
        return true;
    }

    private static Character4D ResolveCharacter(Object source)
    {
        switch (source)
        {
            case Character4D direct:
                return direct;
            case GameObject gameObject:
                return gameObject.GetComponent<Character4D>() ?? gameObject.GetComponentInParent<Character4D>() ?? gameObject.GetComponentInChildren<Character4D>(true);
            case Component component:
                return component.GetComponent<Character4D>() ?? component.GetComponentInParent<Character4D>() ?? component.GetComponentInChildren<Character4D>(true);
            default:
                return null;
        }
    }

    private void AssignCollectionToCharacter()
    {
        if (targetCharacter == null || spriteCollection == null)
            return;

        Undo.RegisterFullObjectHierarchyUndo(targetCharacter.gameObject, "Assign HeroEditor SpriteCollection");
        foreach (var part in GetCharacterParts(targetCharacter))
        {
            if (part == null)
                continue;

            part.SpriteCollection = spriteCollection;
            EditorUtility.SetDirty(part);
        }

        EditorUtility.SetDirty(targetCharacter);
        SaveCharacterChanges(targetCharacter);
    }

    private void ApplySelectedItem(ItemSprite item)
    {
        if (targetCharacter == null || spriteCollection == null || item == null)
            return;

        Undo.RegisterFullObjectHierarchyUndo(targetCharacter.gameObject, $"Apply {item.Id}");

        if (assignCollectionToCharacter)
            AssignCollectionWithoutUndo(targetCharacter, spriteCollection);

        if (partMode == PartMode.Body)
        {
            if (useColorOverride && CurrentPartSupportsColor())
                targetCharacter.SetBody(item, bodyPart, colorOverride);
            else
                targetCharacter.SetBody(item, bodyPart);
        }
        else
        {
            if (useColorOverride && CurrentPartSupportsColor())
                targetCharacter.Equip(item, equipmentPart, colorOverride);
            else
                targetCharacter.Equip(item, equipmentPart);
        }

        selectedItemId = item.Id;
        SaveCharacterChanges(targetCharacter);
    }

    private void ClearCurrentPart()
    {
        if (targetCharacter == null)
            return;

        Undo.RegisterFullObjectHierarchyUndo(targetCharacter.gameObject, $"Clear {GetCurrentPartLabel()}");

        if (partMode == PartMode.Body)
        {
            targetCharacter.SetBody(null, bodyPart);
        }
        else
        {
            targetCharacter.UnEquip(equipmentPart);
        }

        SaveCharacterChanges(targetCharacter);
        RefreshSelectionFromCurrentPart(force: true);
    }

    private void SaveCharacterChanges(Character4D character)
    {
        EditorUtility.SetDirty(character);
        foreach (var part in GetCharacterParts(character))
        {
            if (part == null)
                continue;

            EditorUtility.SetDirty(part);
            PrefabUtility.RecordPrefabInstancePropertyModifications(part);

            foreach (var renderer in part.GetComponentsInChildren<SpriteRenderer>(true))
            {
                EditorUtility.SetDirty(renderer);
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
        }

        PrefabUtility.RecordPrefabInstancePropertyModifications(character);

        if (EditorUtility.IsPersistent(character.gameObject))
        {
            AssetDatabase.SaveAssets();
            return;
        }

        if (character.gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(character.gameObject.scene);
    }

    private void AssignCollectionWithoutUndo(Character4D character, SpriteCollection collection)
    {
        foreach (var part in GetCharacterParts(character))
        {
            if (part == null)
                continue;

            part.SpriteCollection = collection;
            EditorUtility.SetDirty(part);
        }
    }

    private List<ItemSprite> GetActiveItems(SpriteCollection collection)
    {
        if (collection == null || CurrentPartIsUnsupported())
            return new List<ItemSprite>();

        if (partMode == PartMode.Body)
        {
            return bodyPart switch
            {
                HeroBodyPart.Body => collection.Body ?? new List<ItemSprite>(),
                HeroBodyPart.Head => collection.Body ?? new List<ItemSprite>(),
                HeroBodyPart.Hair => collection.Hair ?? new List<ItemSprite>(),
                HeroBodyPart.Ears => collection.Ears ?? new List<ItemSprite>(),
                HeroBodyPart.Eyebrows => collection.Eyebrows ?? new List<ItemSprite>(),
                HeroBodyPart.Eyes => collection.Eyes ?? new List<ItemSprite>(),
                HeroBodyPart.Mouth => collection.Mouth ?? new List<ItemSprite>(),
                HeroBodyPart.Beard => collection.Beard ?? new List<ItemSprite>(),
                HeroBodyPart.Makeup => collection.Makeup ?? new List<ItemSprite>(),
                _ => new List<ItemSprite>()
            };
        }

        return equipmentPart switch
        {
            EquipmentPart.Armor => collection.Armor ?? new List<ItemSprite>(),
            EquipmentPart.Helmet => collection.Armor ?? new List<ItemSprite>(),
            EquipmentPart.Vest => collection.Armor ?? new List<ItemSprite>(),
            EquipmentPart.Bracers => collection.Armor ?? new List<ItemSprite>(),
            EquipmentPart.Leggings => collection.Armor ?? new List<ItemSprite>(),
            EquipmentPart.MeleeWeapon1H => collection.MeleeWeapon1H ?? new List<ItemSprite>(),
            EquipmentPart.MeleeWeapon2H => collection.MeleeWeapon2H ?? new List<ItemSprite>(),
            EquipmentPart.SecondaryMelee1H => collection.MeleeWeapon1H ?? new List<ItemSprite>(),
            EquipmentPart.Bow => collection.Bow ?? new List<ItemSprite>(),
            EquipmentPart.Crossbow => collection.Crossbow ?? new List<ItemSprite>(),
            EquipmentPart.Firearm1H => collection.Firearm1H ?? new List<ItemSprite>(),
            EquipmentPart.Firearm2H => collection.Firearm2H ?? new List<ItemSprite>(),
            EquipmentPart.SecondaryFirearm1H => collection.Firearm1H ?? new List<ItemSprite>(),
            EquipmentPart.Shield => collection.Shield ?? new List<ItemSprite>(),
            EquipmentPart.Back => collection.Back ?? new List<ItemSprite>(),
            EquipmentPart.Mask => collection.Mask ?? new List<ItemSprite>(),
            EquipmentPart.Earrings => collection.Earrings ?? new List<ItemSprite>(),
            EquipmentPart.Wings => collection.Wings ?? new List<ItemSprite>(),
            _ => new List<ItemSprite>()
        };
    }

    private void RefreshSelectionFromCurrentPart(bool force)
    {
        if (!force && !string.IsNullOrEmpty(selectedItemId))
            return;

        var currentId = TryDetectCurrentItemId(spriteCollection);
        if (!string.IsNullOrEmpty(currentId))
            selectedItemId = currentId;
        else
            selectedItemId = GetActiveItems(spriteCollection).FirstOrDefault()?.Id ?? string.Empty;
    }

    private string TryDetectCurrentItemId(SpriteCollection collection)
    {
        if (targetCharacter == null || collection == null)
            return string.Empty;

        var front = targetCharacter.Front;
        if (front == null)
            return string.Empty;

        foreach (var item in GetActiveItems(collection))
        {
            if (item == null)
                continue;

            if (partMode == PartMode.Body && MatchesBodyPart(front, item, bodyPart))
                return item.Id;

            if (partMode == PartMode.Equipment && MatchesEquipmentPart(front, item, equipmentPart))
                return item.Id;
        }

        return string.Empty;
    }

    private static bool MatchesBodyPart(Character front, ItemSprite item, HeroBodyPart part)
    {
        switch (part)
        {
            case HeroBodyPart.Body:
                return SequenceEquals(front.Body, item.Sprites);
            case HeroBodyPart.Head:
                return ContainsSprite(item.Sprites, front.Head);
            case HeroBodyPart.Hair:
                return ContainsSprite(item.Sprites, front.Hair);
            case HeroBodyPart.Ears:
                return SequenceEquals(front.Ears, item.Sprites);
            case HeroBodyPart.Eyebrows:
                return front.Expressions.Count > 0 && ContainsSprite(item.Sprites, front.Expressions[0].Eyebrows);
            case HeroBodyPart.Eyes:
                return front.Expressions.Count > 0 && ContainsSprite(item.Sprites, front.Expressions[0].Eyes);
            case HeroBodyPart.Mouth:
                return front.Expressions.Count > 0 && ContainsSprite(item.Sprites, front.Expressions[0].Mouth);
            case HeroBodyPart.Beard:
                return ContainsSprite(item.Sprites, front.Beard);
            case HeroBodyPart.Makeup:
                return ContainsSprite(item.Sprites, front.Makeup);
            default:
                return false;
        }
    }

    private static bool MatchesEquipmentPart(Character front, ItemSprite item, HeroEquipmentPart part)
    {
        switch (part)
        {
            case HeroEquipmentPart.Armor:
            case HeroEquipmentPart.Vest:
                return ContainsNamedSprite(front.Armor, item.Sprites, "FrontBody");
            case HeroEquipmentPart.Bracers:
                return ContainsNamedSprite(front.Armor, item.Sprites, "FrontArmL");
            case HeroEquipmentPart.Leggings:
                return ContainsNamedSprite(front.Armor, item.Sprites, "FrontLegL");
            case HeroEquipmentPart.Helmet:
                return ContainsSprite(item.Sprites, front.Helmet);
            case HeroEquipmentPart.MeleeWeapon1H:
            case HeroEquipmentPart.MeleeWeapon2H:
            case HeroEquipmentPart.Firearm1H:
            case HeroEquipmentPart.Firearm2H:
                return front.PrimaryWeapon == item.Sprite || ContainsSprite(item.Sprites, front.PrimaryWeapon);
            case HeroEquipmentPart.SecondaryMelee1H:
            case HeroEquipmentPart.SecondaryFirearm1H:
                return front.SecondaryWeapon == item.Sprite || ContainsSprite(item.Sprites, front.SecondaryWeapon);
            case HeroEquipmentPart.Bow:
            case HeroEquipmentPart.Crossbow:
                return SequenceEquals(front.CompositeWeapon, item.Sprites);
            case HeroEquipmentPart.Shield:
                return SequenceEquals(front.Shield, item.Sprites);
            case HeroEquipmentPart.Back:
                return ContainsSprite(item.Sprites, front.Back);
            case HeroEquipmentPart.Mask:
                return ContainsSprite(item.Sprites, front.Mask);
            case HeroEquipmentPart.Earrings:
                return SequenceEquals(front.Earrings, item.Sprites);
            case HeroEquipmentPart.Wings:
                return ContainsSprite(item.Sprites, front.Wings);
            default:
                return false;
        }
    }

    private static bool SequenceEquals(IReadOnlyCollection<Sprite> a, IReadOnlyCollection<Sprite> b)
    {
        if (a == null || b == null)
            return false;

        if (a.Count != b.Count)
            return false;

        return a.SequenceEqual(b);
    }

    private static bool ContainsNamedSprite(IEnumerable<Sprite> equipped, IEnumerable<Sprite> candidate, string spriteName)
    {
        if (equipped == null || candidate == null)
            return false;

        var equippedSprite = equipped.FirstOrDefault(sprite => sprite != null && sprite.name == spriteName);
        var candidateSprite = candidate.FirstOrDefault(sprite => sprite != null && sprite.name == spriteName);
        return equippedSprite != null && equippedSprite == candidateSprite;
    }

    private static bool ContainsSprite(IEnumerable<Sprite> sprites, Sprite target)
    {
        return target != null && sprites != null && sprites.Contains(target);
    }

    private bool MatchesSearch(ItemSprite item, string filter)
    {
        if (item == null)
            return false;

        if (string.IsNullOrWhiteSpace(filter))
            return true;

        return (!string.IsNullOrEmpty(item.Id) && item.Id.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) ||
               (!string.IsNullOrEmpty(item.Name) && item.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private string FormatItemLabel(ItemSprite item)
    {
        if (item == null)
            return "<null>";

        return string.IsNullOrEmpty(item.Name) || string.Equals(item.Name, item.Id, StringComparison.Ordinal)
            ? item.Id
            : $"{item.Name}  —  {item.Id}";
    }

    private Sprite GetPreviewSprite(ItemSprite item)
    {
        if (item == null)
            return null;

        if (item.Sprite != null)
            return item.Sprite;

        return item.Sprites?.FirstOrDefault(sprite => sprite != null);
    }

    private bool CanClearCurrentPart()
    {
        if (partMode == PartMode.Body)
        {
            return bodyPart == HeroBodyPart.Hair ||
                   bodyPart == HeroBodyPart.Beard ||
                   bodyPart == HeroBodyPart.Makeup;
        }

        return !CurrentPartIsUnsupported();
    }

    private bool CurrentPartSupportsColor()
    {
        if (partMode == PartMode.Body)
        {
            return bodyPart == HeroBodyPart.Body ||
                   bodyPart == HeroBodyPart.Head ||
                   bodyPart == HeroBodyPart.Hair ||
                   bodyPart == HeroBodyPart.Ears ||
                   bodyPart == HeroBodyPart.Eyes ||
                   bodyPart == HeroBodyPart.Beard ||
                   bodyPart == HeroBodyPart.Makeup;
        }

        return equipmentPart == HeroEquipmentPart.Armor ||
               equipmentPart == HeroEquipmentPart.Helmet ||
               equipmentPart == HeroEquipmentPart.Vest ||
               equipmentPart == HeroEquipmentPart.Bracers ||
               equipmentPart == HeroEquipmentPart.Leggings ||
               equipmentPart == HeroEquipmentPart.MeleeWeapon1H ||
               equipmentPart == HeroEquipmentPart.MeleeWeapon2H ||
               equipmentPart == HeroEquipmentPart.SecondaryMelee1H ||
               equipmentPart == HeroEquipmentPart.Back ||
               equipmentPart == HeroEquipmentPart.Quiver ||
               equipmentPart == HeroEquipmentPart.Earrings ||
               equipmentPart == HeroEquipmentPart.Mask ||
               equipmentPart == HeroEquipmentPart.Wings;
    }

    private bool CurrentPartIsUnsupported()
    {
        if (partMode == PartMode.Body)
            return false;

        return equipmentPart == HeroEquipmentPart.Cape || equipmentPart == HeroEquipmentPart.Quiver;
    }

    private int GetCurrentPartKey()
    {
        return (int)partMode * 1000 + (partMode == PartMode.Body ? (int)bodyPart : (int)equipmentPart);
    }

    private string GetCurrentPartLabel()
    {
        return partMode == PartMode.Body ? bodyPart.ToString() : equipmentPart.ToString();
    }

    private static IEnumerable<Character> GetCharacterParts(Character4D character)
    {
        if (character == null)
            return Array.Empty<Character>();

        if (character.Parts != null && character.Parts.Count > 0)
            return character.Parts.Where(part => part != null);

        return new[] { character.Front, character.Back, character.Left, character.Right }.Where(part => part != null);
    }
}
