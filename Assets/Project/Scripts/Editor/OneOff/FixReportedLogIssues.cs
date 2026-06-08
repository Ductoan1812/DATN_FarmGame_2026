using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class FixReportedLogIssues
{
    private const string FarmScenePath = "Assets/Project/Scenes/Coreplay/FarmScene.unity";
    private const string PlayerControllerPath = "Assets/Project/Animations/Player/Controller.controller";
    private const string PlayerDataPath = "Assets/Project/Resources/Data/Entities/Characters/PlayerEntityData.asset";
    private const string OakSeedPath = "Assets/Project/Resources/Data/Entities/Items/Seeds/seed_oak_woodtree.asset";
    private const string BrechSeedPath = "Assets/Project/Resources/Data/Entities/Items/Seeds/seed_brech_woodtree.asset";
    private const string OakTreeDataPath = "Assets/Project/Resources/Data/Entities/World/WoodTrees/world_tree_oak.asset";
    private const string RockNodeDataPath = "Assets/Project/Resources/Data/Entities/World/Resources/world_node_rock.asset";

    [MenuItem("Tools/DATN/Fix Reported Log Issues")]
    public static void Execute()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            Debug.Log("[FixReportedLogIssues] Stopping Play Mode. Run again after Unity exits Play Mode.");
            return;
        }

        int changed = 0;
        changed += FixDuplicateSeedIds();
        changed += RemoveEmptyPlayerAppearanceModule();
        changed += FixFarmSceneMarkerTiles();
        changed += FixPlayerScytheTrigger();
        changed += FixPortalLocalization();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[FixReportedLogIssues] Completed. Changed {changed} asset/scene item(s).");
    }

    private static int FixDuplicateSeedIds()
    {
        int changed = 0;
        changed += ConfigureSeed(OakSeedPath, "seed_oak_woodtree", "item.seed.oak_woodtree.name", "item.seed.oak_woodtree.desc");
        changed += ConfigureSeed(BrechSeedPath, "seed_brech_woodtree", "item.seed.brech_woodtree.name", "item.seed.brech_woodtree.desc");
        return changed;
    }

    private static int ConfigureSeed(string assetPath, string id, string nameKey, string descKey)
    {
        var data = AssetDatabase.LoadAssetAtPath<EntityData>(assetPath);
        if (data == null)
        {
            Debug.LogWarning($"[FixReportedLogIssues] Missing seed asset: {assetPath}");
            return 0;
        }

        bool changed = false;
        if (data.id != id)
        {
            data.id = id;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(data.keyName))
        {
            data.keyName = nameKey;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(data.descKey))
        {
            data.descKey = descKey;
            changed = true;
        }

        if (changed)
            EditorUtility.SetDirty(data);

        return changed ? 1 : 0;
    }

    private static int RemoveEmptyPlayerAppearanceModule()
    {
        var data = AssetDatabase.LoadAssetAtPath<EntityData>(PlayerDataPath);
        if (data?.modules == null)
            return 0;

        int before = data.modules.Count;
        data.modules.RemoveAll(module =>
            module is AppearanceModule appearance && string.IsNullOrWhiteSpace(appearance.spriteId));

        if (data.modules.Count == before)
            return 0;

        EditorUtility.SetDirty(data);
        return before - data.modules.Count;
    }

    private static int FixFarmSceneMarkerTiles()
    {
        var currentScene = EditorSceneManager.GetActiveScene();
        if (!string.Equals(currentScene.path, FarmScenePath, StringComparison.OrdinalIgnoreCase))
            currentScene = EditorSceneManager.OpenScene(FarmScenePath);

        var treeData = AssetDatabase.LoadAssetAtPath<EntityData>(OakTreeDataPath);
        var rockData = AssetDatabase.LoadAssetAtPath<EntityData>(RockNodeDataPath);
        if (treeData == null || rockData == null)
        {
            Debug.LogWarning("[FixReportedLogIssues] Missing tree or rock EntityData for FarmScene markers.");
            return 0;
        }

        int changed = 0;
        foreach (var tilemap in UnityEngine.Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!string.Equals(tilemap.gameObject.scene.path, FarmScenePath, StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (var cell in tilemap.cellBounds.allPositionsWithin)
            {
                var tile = tilemap.GetTile<SceneSpawnTile>(cell);
                if (tile == null || tile.markerKind != SceneMarkerKind.ResourceNode)
                    continue;

                EntityData data = tile.objectType switch
                {
                    ObjectType.TreeNode01 => treeData,
                    ObjectType.RockNode01 => rockData,
                    _ => null
                };

                if (data == null)
                    continue;

                bool tileChanged = false;
                if (tile.entityData != data)
                {
                    tile.entityData = data;
                    tileChanged = true;
                }

                if (tile.savePolicy != SceneEntitySavePolicy.Regenerating)
                {
                    tile.savePolicy = SceneEntitySavePolicy.Regenerating;
                    tileChanged = true;
                }

                if (tile.respawnMinutes <= 0)
                {
                    tile.respawnMinutes = 720;
                    tileChanged = true;
                }

                if (tile.initialAmount < 1)
                {
                    tile.initialAmount = 1;
                    tileChanged = true;
                }

                if (string.IsNullOrWhiteSpace(tile.spawnGroupId))
                {
                    tile.spawnGroupId = tile.objectType == ObjectType.TreeNode01 ? "farm_tree" : "farm_rock";
                    tileChanged = true;
                }

                if (!tile.bypassPlacementValidation)
                {
                    tile.bypassPlacementValidation = true;
                    tileChanged = true;
                }

                if (!tileChanged)
                    continue;

                EditorUtility.SetDirty(tile);
                changed++;
            }
        }

        if (changed > 0)
            EditorSceneManager.MarkSceneDirty(currentScene);

        return changed;
    }

    private static int FixPlayerScytheTrigger()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerControllerPath);
        if (controller == null)
        {
            Debug.LogWarning($"[FixReportedLogIssues] Missing animator controller: {PlayerControllerPath}");
            return 0;
        }

        int changed = 0;
        if (!controller.parameters.Any(parameter => parameter.name == "Scythe"))
        {
            controller.AddParameter("Scythe", AnimatorControllerParameterType.Trigger);
            changed++;
        }

        foreach (var layer in controller.layers)
        {
            var state = FindState(layer.stateMachine, "Harvert")
                     ?? FindState(layer.stateMachine, "Harvest")
                     ?? FindState(layer.stateMachine, "Axe");

            if (state == null)
                continue;

            bool hasScytheTransition = layer.stateMachine.anyStateTransitions
                .Any(transition => transition.conditions.Any(condition => condition.parameter == "Scythe"));

            if (hasScytheTransition)
                continue;

            var transition = layer.stateMachine.AddAnyStateTransition(state);
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.canTransitionToSelf = true;
            transition.AddCondition(AnimatorConditionMode.If, 0f, "Scythe");
            changed++;
        }

        if (changed > 0)
            EditorUtility.SetDirty(controller);

        return changed;
    }

    private static AnimatorState FindState(AnimatorStateMachine stateMachine, string stateName)
    {
        foreach (var child in stateMachine.states)
        {
            if (child.state != null && child.state.name == stateName)
                return child.state;
        }

        foreach (var childMachine in stateMachine.stateMachines)
        {
            var found = FindState(childMachine.stateMachine, stateName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static int FixPortalLocalization()
    {
        int changed = 0;
        changed += UpsertLocalization("Assets/Resources/Localization/vi.json", "m1.portal.farm_to_town.name", "Cổng đến Thị trấn");
        changed += UpsertLocalization("Assets/Resources/Localization/en.json", "m1.portal.farm_to_town.name", "Portal to Town");
        changed += UpsertLocalization("Assets/Project/Resources/Localization/vi.json", "m1.portal.farm_to_town.name", "Cổng đến Thị trấn");
        changed += UpsertLocalization("Assets/Project/Resources/Localization/en.json", "m1.portal.farm_to_town.name", "Portal to Town");
        changed += UpsertLocalization("Assets/Resources/Localization/vi.json", "item.seed.oak_woodtree.name", "Hạt giống sồi");
        changed += UpsertLocalization("Assets/Resources/Localization/vi.json", "item.seed.oak_woodtree.desc", "Hạt giống trồng cây sồi lấy gỗ.");
        changed += UpsertLocalization("Assets/Resources/Localization/vi.json", "item.seed.brech_woodtree.name", "Hạt giống bạch dương");
        changed += UpsertLocalization("Assets/Resources/Localization/vi.json", "item.seed.brech_woodtree.desc", "Hạt giống trồng cây bạch dương lấy gỗ.");
        changed += UpsertLocalization("Assets/Resources/Localization/en.json", "item.seed.oak_woodtree.name", "Oak Tree Seed");
        changed += UpsertLocalization("Assets/Resources/Localization/en.json", "item.seed.oak_woodtree.desc", "A seed for growing an oak wood tree.");
        changed += UpsertLocalization("Assets/Resources/Localization/en.json", "item.seed.brech_woodtree.name", "Birch Tree Seed");
        changed += UpsertLocalization("Assets/Resources/Localization/en.json", "item.seed.brech_woodtree.desc", "A seed for growing a birch wood tree.");
        return changed;
    }

    private static int UpsertLocalization(string assetPath, string key, string value)
    {
        string fullPath = Path.GetFullPath(assetPath);
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning($"[FixReportedLogIssues] Missing localization file: {assetPath}");
            return 0;
        }

        var file = JsonUtility.FromJson<LocalizationFile>(File.ReadAllText(fullPath)) ?? new LocalizationFile();
        var entries = file.entries != null
            ? new List<LocalizationEntry>(file.entries)
            : new List<LocalizationEntry>();

        var entry = entries.FirstOrDefault(item => item.key == key);
        if (entry != null)
        {
            if (entry.value == value)
                return 0;

            entry.value = value;
        }
        else
        {
            entries.Add(new LocalizationEntry { key = key, value = value });
        }

        file.entries = entries.OrderBy(item => item.key, StringComparer.Ordinal).ToArray();
        File.WriteAllText(fullPath, JsonUtility.ToJson(file, true));
        AssetDatabase.ImportAsset(assetPath);
        return 1;
    }
}
