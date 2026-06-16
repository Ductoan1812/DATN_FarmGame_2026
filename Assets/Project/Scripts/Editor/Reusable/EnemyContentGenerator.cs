using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class EnemyContentGenerator
{
    private const string EnemyArtRoot = "Assets/Project/Art/Characters/Enemy";
    private const string EnemyAnimationRoot = "Assets/Project/Animations/Enemies";
    private const string BaseControllerPath = EnemyAnimationRoot + "/Enemy_Base.controller";
    private const string BasePlaceholderClipRoot = EnemyAnimationRoot + "/Base/Clips";
    private const string EntityDataRoot = "Assets/Project/Resources/Data/Entities/Characters/Enemies";
    private const string PrefabRoot = "Assets/Project/Prefabs/Characters/Enemies";
    private const string WorldObjectRoot = "Assets/Project/Resources/Data/WorldObjects/Enemies";
    private const string MarkerRoot = "Assets/Project/Resources/Data/SceneMarkers/Enemies/SpawnTiles";
    private const string LocalizationEnPath = "Assets/Project/Resources/Localization/en.json";
    private const string LocalizationViPath = "Assets/Project/Resources/Localization/vi.json";
    private const string EnemyBasePrefabPath = "Assets/Project/Prefabs/Characters/Enemy_Base.prefab";

    private static readonly string[] DirectionNames = { "Front", "Back", "Left", "Right" };
    private static readonly EnemyAction[] CoreActions =
    {
        EnemyAction.Idle,
        EnemyAction.Walk,
        EnemyAction.Run,
        EnemyAction.Attack,
        EnemyAction.Hurt,
        EnemyAction.Death
    };

    private static readonly EnemySpec[] Specs =
    {
        new EnemySpec("Slime1", "enemy_slime1", "EnemySlime1", 18f, 3f, 0f, 2.2f, 1.0f, 1.45f, 4f, 7f, 2.5f, 20, new Vector2(0.75f, 0.45f), new Vector2(0f, -0.2f)),
        new EnemySpec("Slime2", "enemy_slime2", "EnemySlime2", 30f, 5f, 1f, 2.4f, 1.05f, 1.30f, 4.5f, 8f, 3f, 35, new Vector2(0.8f, 0.48f), new Vector2(0f, -0.2f)),
        new EnemySpec("Slime3", "enemy_slime3", "EnemySlime3", 45f, 7f, 2f, 2.6f, 1.1f, 1.15f, 5f, 9f, 3.5f, 55, new Vector2(0.85f, 0.5f), new Vector2(0f, -0.2f)),
        new EnemySpec("Orc1", "enemy_orc1", "EnemyOrc1", 65f, 9f, 3f, 2.5f, 1.2f, 1.1f, 5.5f, 9.5f, 3.5f, 90, new Vector2(0.65f, 0.45f), new Vector2(0f, -0.4f)),
        new EnemySpec("Orc2", "enemy_orc2", "EnemyOrc2", 90f, 12f, 5f, 2.7f, 1.25f, 1.0f, 6f, 10.5f, 4f, 140, new Vector2(0.68f, 0.48f), new Vector2(0f, -0.4f)),
        new EnemySpec("Orc3", "enemy_orc3", "EnemyOrc3", 125f, 16f, 7f, 2.9f, 1.3f, 0.9f, 6.5f, 12f, 4.5f, 220, new Vector2(0.72f, 0.5f), new Vector2(0f, -0.4f))
    };

    [MenuItem("Tools/DATN/Content/Generate Enemy Content", priority = 210)]
    public static void GenerateMenu()
    {
        Run(validationOnly: false);
    }

    [MenuItem("Tools/DATN/Content/Validate Enemy Content", priority = 211)]
    public static void ValidateMenu()
    {
        Run(validationOnly: true);
    }

    private static void Run(bool validationOnly)
    {
        var context = new GenerationContext(validationOnly);
        var scanResults = ScanEnemySources(context);
        if (scanResults.Count == 0)
        {
            context.Error("No enemy sources were discovered.");
            context.Flush();
            return;
        }

        if (!validationOnly)
        {
            EnsureFolder(EnemyAnimationRoot);
            EnsureFolder(BasePlaceholderClipRoot);
            EnsureFolder(EntityDataRoot);
            EnsureFolder(PrefabRoot);
            EnsureFolder(WorldObjectRoot);
            EnsureFolder(MarkerRoot);
        }

        AnimatorController baseController = null;
        if (!validationOnly)
            baseController = BuildBaseController(context);

        foreach (var result in scanResults)
        {
            ProcessEnemy(result, baseController, context);
        }

        if (!validationOnly)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        context.Flush();
    }

    private static List<EnemyScanResult> ScanEnemySources(GenerationContext context)
    {
        var results = new List<EnemyScanResult>(Specs.Length);
        foreach (var spec in Specs)
        {
            results.Add(ScanEnemy(spec, context));
        }

        return results;
    }

    private static EnemyScanResult ScanEnemy(EnemySpec spec, GenerationContext context)
    {
        string folderPath = $"{EnemyArtRoot}/{spec.enemyName}";
        string absoluteFolder = ToAbsoluteProjectPath(folderPath);
        var result = new EnemyScanResult(spec, folderPath);

        if (!Directory.Exists(absoluteFolder))
        {
            context.Error($"[{spec.enemyName}] Missing source folder: {folderPath}");
            return result;
        }

        var files = Directory.GetFiles(absoluteFolder)
            .Where(path => path.EndsWith("_full.png", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (files.Length == 0)
        {
            context.Error($"[{spec.enemyName}] No *_full.png sheets found under {folderPath}.");
            return result;
        }

        foreach (string absoluteFile in files)
        {
            string assetPath = ToAssetPath(absoluteFile);
            var action = ResolveActionFromPath(spec, assetPath);
            if (!action.HasValue)
            {
                context.Warning($"[{spec.enemyName}] Skipping unrecognized sheet: {assetPath}");
                continue;
            }

            var sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name, NaturalStringComparer.Instance)
                .ToList();

            if (sprites.Count == 0)
            {
                context.Error($"[{spec.enemyName}] Sheet has no Sprite subassets: {assetPath}");
                continue;
            }

            if (sprites.Count % 4 != 0)
            {
                context.Error($"[{spec.enemyName}] Sprite count must be divisible by 4: {assetPath} ({sprites.Count})");
                continue;
            }

            int quarterSize = sprites.Count / 4;
            var clipSource = new DirectionalSpriteSet(assetPath, action.Value, quarterSize);
            for (int directionIndex = 0; directionIndex < 4; directionIndex++)
            {
                for (int frameIndex = 0; frameIndex < quarterSize; frameIndex++)
                {
                    clipSource.framesByDirection[directionIndex].Add(sprites[(directionIndex * quarterSize) + frameIndex]);
                }
            }

            result.sources[action.Value] = clipSource;
        }

        foreach (var action in CoreActions)
        {
            if (!result.sources.ContainsKey(action))
                context.Error($"[{spec.enemyName}] Missing required sheet for action {action}.");
        }

        return result;
    }

    private static void ProcessEnemy(EnemyScanResult result, AnimatorController baseController, GenerationContext context)
    {
        var generatedClips = new Dictionary<ClipKey, AnimationClip>();
        foreach (var source in result.sources.Values)
        {
            GenerateDirectionalClips(result.spec, source, generatedClips, context);
        }

        if (!HasCoreDirectionalClips(generatedClips))
        {
            context.Error($"[{result.spec.enemyName}] Core directional clips could not be fully generated.");
            return;
        }

        AnimatorOverrideController overrideController = null;
        if (!context.validationOnly && baseController != null)
        {
            overrideController = BuildOverrideController(baseController, result.spec, generatedClips, context);
        }

        EntityData entityData = null;
        if (!context.validationOnly)
        {
            entityData = UpsertEntityData(result.spec, generatedClips, context);
            TryUpdateLocalization(result.spec, context);
        }

        bool hasObjectType = Enum.TryParse(result.spec.objectTypeName, out ObjectType objectType);
        if (!hasObjectType)
        {
            context.Error($"[{result.spec.enemyName}] Missing ObjectType.{result.spec.objectTypeName}. Skipping prefab, world object, and marker generation.");
            return;
        }

        if (!context.validationOnly && overrideController != null && entityData != null)
        {
            GameObject prefab = UpsertPrefabVariant(result.spec, overrideController, generatedClips, context);
            if (prefab != null)
            {
                UpsertWorldObjectDefinition(result.spec, objectType, prefab, context);
                UpsertSceneSpawnMarker(result.spec, objectType, entityData, generatedClips, context);
            }
        }
    }

    private static void GenerateDirectionalClips(
        EnemySpec spec,
        DirectionalSpriteSet source,
        Dictionary<ClipKey, AnimationClip> generatedClips,
        GenerationContext context)
    {
        string clipRoot = $"{EnemyAnimationRoot}/{spec.enemyName}/Clips";
        if (!context.validationOnly)
            EnsureFolder(clipRoot);

        for (int directionIndex = 0; directionIndex < 4; directionIndex++)
        {
            var frames = source.framesByDirection[directionIndex];
            if (frames.Count == 0)
            {
                context.Error($"[{spec.enemyName}] No frames found for {source.action} {DirectionNames[directionIndex]}.");
                continue;
            }

            string clipPath = $"{clipRoot}/{spec.enemyName}_{ActionToFileToken(source.action)}_{DirectionNames[directionIndex]}.anim";
            var clip = context.validationOnly
                ? null
                : LoadOrCreateAsset<AnimationClip>(clipPath, () => new AnimationClip());

            if (!context.validationOnly)
            {
                clip.frameRate = ResolveFps(source.action);
                AnimationUtility.SetObjectReferenceCurve(
                    clip,
                    EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite"),
                    BuildSpriteKeys(frames, clip.frameRate));

                ConfigureLoop(clip, IsLoopingAction(source.action));
                ConfigureEvents(clip, source.action);
                EditorUtility.SetDirty(clip);
            }

            generatedClips[new ClipKey(source.action, directionIndex)] = clip;
        }
    }

    private static AnimatorController BuildBaseController(GenerationContext context)
    {
        EnsureFolder(EnemyAnimationRoot);
        EnsureFolder(BasePlaceholderClipRoot);

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(BaseControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(BaseControllerPath);

        while (controller.parameters.Length > 0)
            controller.RemoveParameter(0);

        controller.AddParameter("Facing", AnimatorControllerParameterType.Int);
        controller.AddParameter("MoveState", AnimatorControllerParameterType.Int);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);

        var layers = controller.layers;
        if (layers == null || layers.Length == 0)
        {
            layers = new[]
            {
                new AnimatorControllerLayer
                {
                    name = "Base Layer",
                    defaultWeight = 1f,
                    stateMachine = new AnimatorStateMachine()
                }
            };
        }

        var rootMachine = layers[0].stateMachine ?? new AnimatorStateMachine();
        rootMachine.name = "Enemy_Base_Root";
        RebuildStateMachine(rootMachine);
        layers[0].stateMachine = rootMachine;
        controller.layers = layers;

        var locomotionStates = new Dictionary<ClipKey, AnimatorState>();
        var idleStates = new Dictionary<int, AnimatorState>();

        for (int directionIndex = 0; directionIndex < 4; directionIndex++)
        {
            float x = directionIndex * 360f;
            locomotionStates[new ClipKey(EnemyAction.Idle, directionIndex)] = AddState(
                rootMachine,
                $"{EnemyAction.Idle}_{DirectionNames[directionIndex]}",
                LoadOrCreatePlaceholderClip(EnemyAction.Idle, directionIndex),
                new Vector3(x, 0f));
            locomotionStates[new ClipKey(EnemyAction.Walk, directionIndex)] = AddState(
                rootMachine,
                $"{EnemyAction.Walk}_{DirectionNames[directionIndex]}",
                LoadOrCreatePlaceholderClip(EnemyAction.Walk, directionIndex),
                new Vector3(x, 90f));
            locomotionStates[new ClipKey(EnemyAction.Run, directionIndex)] = AddState(
                rootMachine,
                $"{EnemyAction.Run}_{DirectionNames[directionIndex]}",
                LoadOrCreatePlaceholderClip(EnemyAction.Run, directionIndex),
                new Vector3(x, 180f));

            idleStates[directionIndex] = locomotionStates[new ClipKey(EnemyAction.Idle, directionIndex)];
        }

        for (int directionIndex = 0; directionIndex < 4; directionIndex++)
        {
            var attackState = AddState(
                rootMachine,
                $"{EnemyAction.Attack}_{DirectionNames[directionIndex]}",
                LoadOrCreatePlaceholderClip(EnemyAction.Attack, directionIndex),
                new Vector3(directionIndex * 360f, 320f));
            var hurtState = AddState(
                rootMachine,
                $"{EnemyAction.Hurt}_{DirectionNames[directionIndex]}",
                LoadOrCreatePlaceholderClip(EnemyAction.Hurt, directionIndex),
                new Vector3(directionIndex * 360f, 420f));
            var deathState = AddState(
                rootMachine,
                $"{EnemyAction.Death}_{DirectionNames[directionIndex]}",
                LoadOrCreatePlaceholderClip(EnemyAction.Death, directionIndex),
                new Vector3(directionIndex * 360f, 520f));

            AddAnyStateDirectionalTransition(rootMachine, attackState, "Attack", directionIndex, null);
            AddAnyStateDirectionalTransition(rootMachine, hurtState, "Hurt", directionIndex, null);
            AddAnyStateDirectionalTransition(rootMachine, deathState, null, directionIndex, true);

            AddExitToIdle(attackState, idleStates[directionIndex]);
            AddExitToIdle(hurtState, idleStates[directionIndex]);
        }

        foreach (var from in locomotionStates)
        {
            foreach (var to in locomotionStates)
            {
                if (from.Key.directionIndex == to.Key.directionIndex && from.Key.action == to.Key.action)
                    continue;

                AddLocomotionTransition(from.Value, to.Value, to.Key.directionIndex, ResolveMoveState(to.Key.action));
            }
        }

        rootMachine.defaultState = idleStates[0];
        EditorUtility.SetDirty(rootMachine);
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AnimatorOverrideController BuildOverrideController(
        AnimatorController baseController,
        EnemySpec spec,
        Dictionary<ClipKey, AnimationClip> generatedClips,
        GenerationContext context)
    {
        string path = $"{EnemyAnimationRoot}/{spec.enemyName}/{spec.enemyName}.overrideController";
        EnsureFolder($"{EnemyAnimationRoot}/{spec.enemyName}");

        var overrideController = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
        if (overrideController == null)
        {
            overrideController = new AnimatorOverrideController(baseController);
            AssetDatabase.CreateAsset(overrideController, path);
        }
        else
        {
            overrideController.runtimeAnimatorController = baseController;
        }

        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        foreach (AnimationClip original in baseController.animationClips.Distinct())
        {
            ClipKey key;
            if (!TryParsePlaceholderKey(original.name, out key))
            {
                overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(original, original));
                continue;
            }

            generatedClips.TryGetValue(new ClipKey(key.action, key.directionIndex), out var replacement);
            overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(original, replacement != null ? replacement : original));
        }

        overrideController.ApplyOverrides(overrides);
        EditorUtility.SetDirty(overrideController);
        return overrideController;
    }

    private static EntityData UpsertEntityData(
        EnemySpec spec,
        Dictionary<ClipKey, AnimationClip> generatedClips,
        GenerationContext context)
    {
        string assetPath = $"{EntityDataRoot}/{spec.enemyName}.asset";
        var entityData = LoadOrCreateAsset<EntityData>(assetPath, ScriptableObject.CreateInstance<EntityData>);

        entityData.id = spec.entityId;
        entityData.keyName = BuildNameKey(spec);
        entityData.descKey = BuildDescKey(spec);
        entityData.icon = ResolvePreviewSprite(generatedClips);
        entityData.category = ItemCategory.Misc;
        entityData.maxStack = 1;
        entityData.buyPrice = 0;
        entityData.sellPrice = 0;
        entityData.placementRule = new PlacementRule
        {
            occupyLayer = EntityLayer.Plant,
            requireTags = PlacementTag.Walkable,
            provideTags = PlacementTag.None,
            blockLayers = new[] { EntityLayer.Plant, EntityLayer.Furniture }
        };
        entityData.baseStats ??= new StatsData();
        entityData.baseStats.baseStats ??= new List<StatEntry>();
        UpsertStat(entityData.baseStats.baseStats, StatType.MaxHp, spec.hp);
        UpsertStat(entityData.baseStats.baseStats, StatType.Hp, spec.hp);
        UpsertStat(entityData.baseStats.baseStats, StatType.Attack, spec.attack);
        UpsertStat(entityData.baseStats.baseStats, StatType.Defense, spec.defense);
        UpsertStat(entityData.baseStats.baseStats, StatType.Speed, spec.speed);
        UpsertStat(entityData.baseStats.baseStats, StatType.Range, spec.attackRange);
        UpsertStat(entityData.baseStats.baseStats, StatType.CoolDown, spec.cooldown);
        RemoveStat(entityData.baseStats.baseStats, StatType.Exp);

        entityData.modules ??= new List<IModuleData>();
        var healthModule = GetOrCreateModule<HealthModule>(entityData.modules);
        healthModule.canTakeDamage = true;

        var attackModule = GetOrCreateModule<AttackModule>(entityData.modules);
        attackModule.attackRange = spec.attackRange;
        attackModule.attackCooldown = spec.cooldown;

        var dropModule = GetOrCreateModule<DropModule>(entityData.modules);
        dropModule.harvestDrops = Array.Empty<DropEntry>();
        dropModule.deathDrops = Array.Empty<DropEntry>();
        dropModule.includeHarvestDropsOnDestroyWhenHarvestable = false;

        var expModule = GetOrCreateModule<ExpRewardModule>(entityData.modules);
        expModule.rewardExp = spec.expReward;
        expModule.sourceType = ExpSourceType.Combat;
        expModule.requireKiller = true;

        var mortalModule = GetOrCreateModule<MortalModule>(entityData.modules);
        float deathDuration = ResolveDeathDuration(generatedClips);
        TrySetFloatField(mortalModule, "destroyDelay", deathDuration, context, spec.enemyName);

        entityData.modules = ReorderModules(entityData.modules, healthModule, attackModule, dropModule, expModule, mortalModule);

        EditorUtility.SetDirty(entityData);
        return entityData;
    }

    private static GameObject UpsertPrefabVariant(
        EnemySpec spec,
        AnimatorOverrideController overrideController,
        Dictionary<ClipKey, AnimationClip> generatedClips,
        GenerationContext context)
    {
        var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyBasePrefabPath);
        if (basePrefab == null)
        {
            context.Error($"[{spec.enemyName}] Missing base prefab: {EnemyBasePrefabPath}");
            return null;
        }

        string prefabPath = $"{PrefabRoot}/{spec.enemyName}.prefab";
        var prefabInstance = PrefabUtility.InstantiatePrefab(basePrefab) as GameObject;
        if (prefabInstance == null)
        {
            context.Error($"[{spec.enemyName}] Failed to instantiate {EnemyBasePrefabPath}.");
            return null;
        }

        try
        {
            prefabInstance.name = spec.enemyName;

            var animator = prefabInstance.GetComponent<Animator>();
            if (animator == null)
                animator = prefabInstance.AddComponent<Animator>();
            animator.runtimeAnimatorController = overrideController;
            animator.applyRootMotion = false;

            var navAgent = prefabInstance.GetComponent<NavAgent2D>();
            if (navAgent == null)
                navAgent = prefabInstance.AddComponent<NavAgent2D>();
            navAgent.SetMoveSpeed(spec.speed);

            var spriteRenderer = prefabInstance.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                context.Error($"[{spec.enemyName}] Enemy base prefab is missing a SpriteRenderer.");
                return null;
            }

            var idleFront = ResolvePreviewSprite(generatedClips);
            if (idleFront != null)
                spriteRenderer.sprite = idleFront;

            var collider = prefabInstance.GetComponent<BoxCollider2D>();
            if (collider != null)
            {
                collider.size = spec.colliderSize;
                collider.offset = spec.colliderOffset;
            }

            var enemyObject = prefabInstance.GetComponent<EnemyObject>();
            if (enemyObject != null)
            {
                bool configured = TryInvokeConfigure(enemyObject, spec);
                if (!configured)
                {
                    var serializedEnemy = new SerializedObject(enemyObject);
                    SetFloatProperty(serializedEnemy, "detectRange", spec.detectRange);
                    SetFloatProperty(serializedEnemy, "attackRange", spec.attackRange);
                    SetFloatProperty(serializedEnemy, "attackCooldown", spec.cooldown);
                    SetFloatProperty(serializedEnemy, "moveSpeed", spec.speed);
                    TrySetFloatProperty(serializedEnemy, "leashRange", spec.leashRange);
                    TrySetFloatProperty(serializedEnemy, "wanderRadius", spec.wanderRadius);
                    serializedEnemy.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
            if (savedPrefab != null)
                EditorUtility.SetDirty(savedPrefab);
            return savedPrefab;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(prefabInstance);
        }
    }

    private static void UpsertWorldObjectDefinition(
        EnemySpec spec,
        ObjectType objectType,
        GameObject prefab,
        GenerationContext context)
    {
        string path = $"{WorldObjectRoot}/{spec.objectTypeName}.asset";
        var definition = LoadOrCreateAsset<WorldObjectDefinition>(path, ScriptableObject.CreateInstance<WorldObjectDefinition>);
        definition.idObject = objectType;
        definition.prefab = prefab;
        EditorUtility.SetDirty(definition);
    }

    private static void UpsertSceneSpawnMarker(
        EnemySpec spec,
        ObjectType objectType,
        EntityData entityData,
        Dictionary<ClipKey, AnimationClip> generatedClips,
        GenerationContext context)
    {
        string path = $"{MarkerRoot}/Marker_{spec.enemyName}.asset";
        var marker = LoadOrCreateAsset<SceneSpawnTile>(path, ScriptableObject.CreateInstance<SceneSpawnTile>);
        marker.markerKind = SceneMarkerKind.Enemy;
        marker.objectType = objectType;
        marker.entityData = entityData;
        marker.savePolicy = SceneEntitySavePolicy.Regenerating;
        marker.spawnGroupId = spec.entityId;
        marker.spawnPointId = string.Empty;
        marker.respawnMinutes = 1440;
        marker.initialAmount = 1;
        marker.bypassPlacementValidation = true;
        marker.stageSpawnMode = MarkerStageSpawnMode.Default;
        marker.fixedStartStageIndex = 0;
        marker.randomStartStageMin = 0;
        marker.randomStartStageMax = 0;
        marker.editorSprite = ResolvePreviewSprite(generatedClips);
        marker.editorColor = Color.white;
        EditorUtility.SetDirty(marker);
    }

    private static void TryUpdateLocalization(EnemySpec spec, GenerationContext context)
    {
        TryUpsertLocalizationFile(
            LocalizationEnPath,
            BuildNameKey(spec),
            BuildEnglishDisplayName(spec),
            BuildDescKey(spec),
            BuildEnglishDescription(spec),
            context,
            spec.enemyName);

        TryUpsertLocalizationFile(
            LocalizationViPath,
            BuildNameKey(spec),
            BuildVietnameseDisplayName(spec),
            BuildDescKey(spec),
            BuildVietnameseDescription(spec),
            context,
            spec.enemyName);
    }

    private static void TryUpsertLocalizationFile(
        string assetPath,
        string nameKey,
        string nameValue,
        string descKey,
        string descValue,
        GenerationContext context,
        string enemyName)
    {
        try
        {
            string absolutePath = ToAbsoluteProjectPath(assetPath);
            if (!File.Exists(absolutePath))
            {
                context.Warning($"[{enemyName}] Localization file not found: {assetPath}");
                return;
            }

            string json = File.ReadAllText(absolutePath);
            if (JsonUtility.FromJson<LocalizationFile>(json) == null)
            {
                context.Warning($"[{enemyName}] Localization file is not valid JSON: {assetPath}");
                return;
            }

            string lineEnding = json.Contains("\r\n") ? "\r\n" : "\n";
            json = UpsertLocalizationJsonEntry(json, nameKey, nameValue, lineEnding);
            json = UpsertLocalizationJsonEntry(json, descKey, descValue, lineEnding);
            File.WriteAllText(absolutePath, json);
            AssetDatabase.ImportAsset(assetPath);
        }
        catch (Exception exception)
        {
            context.Warning($"[{enemyName}] Failed to update localization file {assetPath}: {exception.Message}");
        }
    }

    private static string UpsertLocalizationJsonEntry(string json, string key, string value, string lineEnding)
    {
        string serialized = JsonUtility.ToJson(new LocalizationEntry { key = key, value = value });
        serialized = serialized
            .Replace("{\"key\":", "{ \"key\": ")
            .Replace(",\"value\":", ", \"value\": ");
        serialized = serialized.Substring(0, serialized.Length - 1) + " }";
        string escapedKey = Regex.Escape(key);
        string entryPattern =
            $@"(?<indent>^[ \t]*)\{{\s*""key""\s*:\s*""{escapedKey}""\s*,\s*""value""\s*:\s*""(?:\\.|[^""\\])*""\s*\}}";
        var existing = Regex.Match(json, entryPattern, RegexOptions.Multiline);
        if (existing.Success)
        {
            string indent = existing.Groups["indent"].Value;
            return json.Substring(0, existing.Index)
                + indent
                + serialized
                + json.Substring(existing.Index + existing.Length);
        }

        int arrayEnd = json.LastIndexOf(']');
        if (arrayEnd < 0)
            return json;

        string before = json.Substring(0, arrayEnd).TrimEnd();
        bool hasEntries = before.LastIndexOf('{') > before.LastIndexOf('[');
        string separator = hasEntries ? "," : string.Empty;
        return before
            + separator
            + lineEnding
            + "    "
            + serialized
            + lineEnding
            + "  "
            + json.Substring(arrayEnd);
    }

    private static void RebuildStateMachine(AnimatorStateMachine stateMachine)
    {
        foreach (var childState in stateMachine.states.ToArray())
            stateMachine.RemoveState(childState.state);

        foreach (var childMachine in stateMachine.stateMachines.ToArray())
            stateMachine.RemoveStateMachine(childMachine.stateMachine);

        foreach (var transition in stateMachine.anyStateTransitions.ToArray())
            stateMachine.RemoveAnyStateTransition(transition);

        foreach (var transition in stateMachine.entryTransitions.ToArray())
            stateMachine.RemoveEntryTransition(transition);
    }

    private static AnimatorState AddState(AnimatorStateMachine machine, string name, Motion motion, Vector3 position)
    {
        var state = machine.AddState(name, position);
        state.motion = motion;
        state.speed = 1f;
        return state;
    }

    private static void AddLocomotionTransition(AnimatorState fromState, AnimatorState toState, int facing, int moveState)
    {
        var transition = fromState.AddTransition(toState);
        transition.hasExitTime = false;
        transition.hasFixedDuration = true;
        transition.duration = 0f;
        transition.exitTime = 0f;
        transition.interruptionSource = TransitionInterruptionSource.None;
        transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "Dead");
        transition.AddCondition(AnimatorConditionMode.Equals, facing, "Facing");
        transition.AddCondition(AnimatorConditionMode.Equals, moveState, "MoveState");
    }

    private static void AddAnyStateDirectionalTransition(
        AnimatorStateMachine machine,
        AnimatorState targetState,
        string triggerParameter,
        int facing,
        bool? deadValue)
    {
        var transition = machine.AddAnyStateTransition(targetState);
        transition.hasExitTime = false;
        transition.hasFixedDuration = true;
        transition.duration = 0f;
        transition.exitTime = 0f;
        transition.canTransitionToSelf = false;
        transition.interruptionSource = TransitionInterruptionSource.None;
        transition.AddCondition(AnimatorConditionMode.Equals, facing, "Facing");

        if (!string.IsNullOrEmpty(triggerParameter))
            transition.AddCondition(AnimatorConditionMode.If, 0f, triggerParameter);

        if (deadValue.HasValue)
            transition.AddCondition(deadValue.Value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, "Dead");
        else
            transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "Dead");
    }

    private static void AddExitToIdle(AnimatorState fromState, AnimatorState idleState)
    {
        var transition = fromState.AddTransition(idleState);
        transition.hasExitTime = true;
        transition.exitTime = 1f;
        transition.hasFixedDuration = true;
        transition.duration = 0f;
        transition.interruptionSource = TransitionInterruptionSource.None;
    }

    private static AnimationClip LoadOrCreatePlaceholderClip(EnemyAction action, int directionIndex)
    {
        string path = $"{BasePlaceholderClipRoot}/Enemy_Base_{ActionToFileToken(action)}_{DirectionNames[directionIndex]}.anim";
        var clip = LoadOrCreateAsset<AnimationClip>(path, () => new AnimationClip());
        clip.frameRate = ResolveFps(action);
        ConfigureLoop(clip, IsLoopingAction(action));
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static bool TryParsePlaceholderKey(string clipName, out ClipKey key)
    {
        key = default;
        const string prefix = "Enemy_Base_";
        if (!clipName.StartsWith(prefix, StringComparison.Ordinal))
            return false;

        string token = clipName.Substring(prefix.Length);
        int separatorIndex = token.LastIndexOf('_');
        if (separatorIndex <= 0)
            return false;

        string actionToken = token.Substring(0, separatorIndex);
        string directionToken = token.Substring(separatorIndex + 1);
        if (!TryParseActionToken(actionToken, out var action))
            return false;

        int directionIndex = Array.FindIndex(DirectionNames, name => string.Equals(name, directionToken, StringComparison.OrdinalIgnoreCase));
        if (directionIndex < 0)
            return false;

        key = new ClipKey(action, directionIndex);
        return true;
    }

    private static bool HasCoreDirectionalClips(Dictionary<ClipKey, AnimationClip> generatedClips)
    {
        foreach (var action in CoreActions)
        {
            for (int directionIndex = 0; directionIndex < 4; directionIndex++)
            {
                if (!generatedClips.ContainsKey(new ClipKey(action, directionIndex)))
                    return false;
            }
        }

        return true;
    }

    private static Sprite ResolvePreviewSprite(Dictionary<ClipKey, AnimationClip> generatedClips)
    {
        var clip = generatedClips.TryGetValue(new ClipKey(EnemyAction.Idle, 0), out var idleFront) ? idleFront : null;
        if (clip == null)
            return null;

        var binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
        var frames = AnimationUtility.GetObjectReferenceCurve(clip, binding);
        return frames != null && frames.Length > 0 ? frames[0].value as Sprite : null;
    }

    private static float ResolveDeathDuration(Dictionary<ClipKey, AnimationClip> generatedClips)
    {
        if (!generatedClips.TryGetValue(new ClipKey(EnemyAction.Death, 0), out var deathClip) || deathClip == null)
            return 0f;

        return Mathf.Max(0.01f, deathClip.length);
    }

    private static ObjectReferenceKeyframe[] BuildSpriteKeys(IReadOnlyList<Sprite> sprites, float fps)
    {
        var keys = new ObjectReferenceKeyframe[sprites.Count];
        for (int i = 0; i < sprites.Count; i++)
        {
            keys[i] = new ObjectReferenceKeyframe
            {
                time = i / fps,
                value = sprites[i]
            };
        }

        return keys;
    }

    private static float CalculateClipDuration(int frameCount, float fps)
    {
        if (frameCount <= 0 || fps <= 0f)
            return 0f;

        return frameCount / fps;
    }

    private static void ConfigureLoop(AnimationClip clip, bool loop)
    {
        var serializedClip = new SerializedObject(clip);
        var loopProperty = serializedClip.FindProperty("m_AnimationClipSettings.m_LoopTime");
        if (loopProperty != null)
            loopProperty.boolValue = loop;
        serializedClip.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureEvents(AnimationClip clip, EnemyAction action)
    {
        float length = ResolveClipLength(clip);
        if (length <= 0f)
        {
            AnimationUtility.SetAnimationEvents(clip, Array.Empty<AnimationEvent>());
            return;
        }

        if (action == EnemyAction.Attack)
        {
            AnimationUtility.SetAnimationEvents(clip, new[]
            {
                new AnimationEvent
                {
                    functionName = "AnimationAttackStrike",
                    time = Mathf.Clamp(length * 0.6f, 0f, Mathf.Max(0f, length - 0.0001f))
                },
                new AnimationEvent
                {
                    functionName = "AnimationAttackComplete",
                    time = Mathf.Max(0f, length - 0.0001f)
                }
            });
            return;
        }

        if (action == EnemyAction.Hurt)
        {
            AnimationUtility.SetAnimationEvents(clip, new[]
            {
                new AnimationEvent
                {
                    functionName = "AnimationHurtComplete",
                    time = Mathf.Max(0f, length - 0.0001f)
                }
            });
            return;
        }

        if (action == EnemyAction.Death)
        {
            AnimationUtility.SetAnimationEvents(clip, new[]
            {
                new AnimationEvent
                {
                    functionName = "AnimationDeathComplete",
                    time = Mathf.Max(0f, length - 0.0001f)
                }
            });
            return;
        }

        AnimationUtility.SetAnimationEvents(clip, Array.Empty<AnimationEvent>());
    }

    private static float ResolveClipLength(AnimationClip clip)
    {
        return clip != null ? clip.length : 0f;
    }

    private static float ResolveFps(EnemyAction action)
    {
        switch (action)
        {
            case EnemyAction.Idle:
                return 6f;
            case EnemyAction.Walk:
            case EnemyAction.WalkAttack:
                return 8f;
            case EnemyAction.Run:
            case EnemyAction.RunAttack:
            case EnemyAction.Death:
                return 10f;
            case EnemyAction.Attack:
            case EnemyAction.Hurt:
                return 12f;
            default:
                return 10f;
        }
    }

    private static bool IsLoopingAction(EnemyAction action)
    {
        return action == EnemyAction.Idle || action == EnemyAction.Walk || action == EnemyAction.Run;
    }

    private static int ResolveMoveState(EnemyAction action)
    {
        switch (action)
        {
            case EnemyAction.Idle:
                return 0;
            case EnemyAction.Walk:
                return 1;
            case EnemyAction.Run:
                return 2;
            default:
                return 0;
        }
    }

    private static EnemyAction? ResolveActionFromPath(EnemySpec spec, string assetPath)
    {
        string fileName = Path.GetFileNameWithoutExtension(assetPath) ?? string.Empty;
        string normalized = NormalizeToken(fileName);
        string enemyToken = NormalizeToken(spec.enemyName);
        if (normalized.StartsWith(enemyToken, StringComparison.Ordinal))
            normalized = normalized.Substring(enemyToken.Length).Trim('_');
        if (normalized.EndsWith("_full", StringComparison.Ordinal))
            normalized = normalized.Substring(0, normalized.Length - "_full".Length);

        if (normalized.Contains("walk_attack"))
            return EnemyAction.WalkAttack;
        if (normalized.Contains("run_attack"))
            return EnemyAction.RunAttack;
        if (normalized.Contains("attack"))
            return EnemyAction.Attack;
        if (normalized.Contains("idle"))
            return EnemyAction.Idle;
        if (normalized.Contains("walk"))
            return EnemyAction.Walk;
        if (normalized.Contains("run"))
            return EnemyAction.Run;
        if (normalized.Contains("hurt"))
            return EnemyAction.Hurt;
        if (normalized.Contains("death"))
            return EnemyAction.Death;

        return null;
    }

    private static bool TryParseActionToken(string token, out EnemyAction action)
    {
        switch (NormalizeToken(token))
        {
            case "idle":
                action = EnemyAction.Idle;
                return true;
            case "walk":
                action = EnemyAction.Walk;
                return true;
            case "run":
                action = EnemyAction.Run;
                return true;
            case "attack":
                action = EnemyAction.Attack;
                return true;
            case "hurt":
                action = EnemyAction.Hurt;
                return true;
            case "death":
                action = EnemyAction.Death;
                return true;
            case "walk_attack":
                action = EnemyAction.WalkAttack;
                return true;
            case "run_attack":
                action = EnemyAction.RunAttack;
                return true;
            default:
                action = default;
                return false;
        }
    }

    private static string ActionToFileToken(EnemyAction action)
    {
        switch (action)
        {
            case EnemyAction.WalkAttack:
                return "WalkAttack";
            case EnemyAction.RunAttack:
                return "RunAttack";
            default:
                return action.ToString();
        }
    }

    private static string NormalizeToken(string value)
    {
        string normalized = value.Replace(' ', '_').Replace('-', '_');
        normalized = Regex.Replace(normalized, "_+", "_");
        return normalized.Trim('_').ToLowerInvariant();
    }

    private static string BuildNameKey(EnemySpec spec)
    {
        return $"enemy.{NormalizeToken(spec.enemyName)}.name";
    }

    private static string BuildDescKey(EnemySpec spec)
    {
        return $"enemy.{NormalizeToken(spec.enemyName)}.desc";
    }

    private static string BuildEnglishDisplayName(EnemySpec spec)
    {
        return spec.enemyName.StartsWith("Slime", StringComparison.OrdinalIgnoreCase)
            ? $"Slime {spec.enemyName.Substring("Slime".Length)}"
            : $"Orc {spec.enemyName.Substring("Orc".Length)}";
    }

    private static string BuildEnglishDescription(EnemySpec spec)
    {
        return spec.enemyName.StartsWith("Slime", StringComparison.OrdinalIgnoreCase)
            ? "A roaming slime that attacks players on sight."
            : "A roaming orc warrior that attacks players on sight.";
    }

    private static string BuildVietnameseDisplayName(EnemySpec spec)
    {
        return spec.enemyName.StartsWith("Slime", StringComparison.OrdinalIgnoreCase)
            ? $"Slime {spec.enemyName.Substring("Slime".Length)}"
            : $"Orc {spec.enemyName.Substring("Orc".Length)}";
    }

    private static string BuildVietnameseDescription(EnemySpec spec)
    {
        return spec.enemyName.StartsWith("Slime", StringComparison.OrdinalIgnoreCase)
            ? "Một con slime lang thang sẽ tấn công người chơi khi lại gần."
            : "Một chiến binh orc lang thang sẽ tấn công người chơi khi lại gần.";
    }

    private static bool TryInvokeConfigure(EnemyObject enemyObject, EnemySpec spec)
    {
        var method = enemyObject.GetType().GetMethod(
            "Configure",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
            null,
            new[] { typeof(float), typeof(float), typeof(float), typeof(float), typeof(float) },
            null);

        if (method == null)
            return false;

        method.Invoke(enemyObject, new object[]
        {
            spec.detectRange,
            spec.attackRange,
            spec.cooldown,
            spec.leashRange,
            spec.wanderRadius
        });
        return true;
    }

    private static void SetFloatProperty(SerializedObject serializedObject, string propertyName, float value)
    {
        var property = serializedObject.FindProperty(propertyName);
        if (property != null)
            property.floatValue = value;
    }

    private static void TrySetFloatProperty(SerializedObject serializedObject, string propertyName, float value)
    {
        var property = serializedObject.FindProperty(propertyName);
        if (property != null)
            property.floatValue = value;
    }

    private static void TrySetFloatField(object target, string fieldName, float value, GenerationContext context, string enemyName)
    {
        var field = target.GetType().GetField(fieldName);
        if (field == null || field.FieldType != typeof(float))
            return;

        field.SetValue(target, value);
    }

    private static void UpsertStat(List<StatEntry> stats, StatType statType, float value)
    {
        var entry = stats.FirstOrDefault(item => item != null && item.statType == statType);
        if (entry == null)
        {
            stats.Add(new StatEntry
            {
                statType = statType,
                value = value
            });
            return;
        }

        entry.value = value;
    }

    private static void RemoveStat(List<StatEntry> stats, StatType statType)
    {
        stats.RemoveAll(entry => entry != null && entry.statType == statType);
    }

    private static T GetOrCreateModule<T>(List<IModuleData> modules) where T : IModuleData, new()
    {
        var existing = modules.OfType<T>().FirstOrDefault();
        if (existing != null)
            return existing;

        var created = new T();
        modules.Add(created);
        return created;
    }

    private static List<IModuleData> ReorderModules(List<IModuleData> modules, params IModuleData[] preferredOrder)
    {
        var ordered = new List<IModuleData>();
        foreach (var module in preferredOrder)
        {
            if (module != null && modules.Contains(module))
                ordered.Add(module);
        }

        foreach (var module in modules)
        {
            if (module != null && !ordered.Contains(module))
                ordered.Add(module);
        }

        return ordered;
    }

    private static T LoadOrCreateAsset<T>(string assetPath, Func<T> factory) where T : UnityEngine.Object
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (existing != null)
            return existing;

        var created = factory();
        if (created is ScriptableObject)
            AssetDatabase.CreateAsset(created, assetPath);
        else
            AssetDatabase.CreateAsset(created, assetPath);
        return created;
    }

    private static void EnsureFolder(string folderPath)
    {
        string normalized = folderPath.Replace("\\", "/");
        if (AssetDatabase.IsValidFolder(normalized))
            return;

        string parent = Path.GetDirectoryName(normalized)?.Replace("\\", "/");
        string leaf = Path.GetFileName(normalized);
        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf))
            return;

        EnsureFolder(parent);
        if (!AssetDatabase.IsValidFolder(normalized))
            AssetDatabase.CreateFolder(parent, leaf);
    }

    private static string ToAbsoluteProjectPath(string assetPath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
        string relative = assetPath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(projectRoot, relative);
    }

    private static string ToAssetPath(string absolutePath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
        string normalizedRoot = projectRoot.Replace('\\', '/').TrimEnd('/');
        string normalizedPath = absolutePath.Replace('\\', '/');
        if (!normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            return absolutePath.Replace('\\', '/');

        string relative = normalizedPath.Substring(normalizedRoot.Length).TrimStart('/');
        return relative;
    }

    private readonly struct EnemySpec
    {
        public EnemySpec(
            string enemyName,
            string entityId,
            string objectTypeName,
            float hp,
            float attack,
            float defense,
            float speed,
            float attackRange,
            float cooldown,
            float detectRange,
            float leashRange,
            float wanderRadius,
            int expReward,
            Vector2 colliderSize,
            Vector2 colliderOffset)
        {
            this.enemyName = enemyName;
            this.entityId = entityId;
            this.objectTypeName = objectTypeName;
            this.hp = hp;
            this.attack = attack;
            this.defense = defense;
            this.speed = speed;
            this.attackRange = attackRange;
            this.cooldown = cooldown;
            this.detectRange = detectRange;
            this.leashRange = leashRange;
            this.wanderRadius = wanderRadius;
            this.expReward = expReward;
            this.colliderSize = colliderSize;
            this.colliderOffset = colliderOffset;
        }

        public readonly string enemyName;
        public readonly string entityId;
        public readonly string objectTypeName;
        public readonly float hp;
        public readonly float attack;
        public readonly float defense;
        public readonly float speed;
        public readonly float attackRange;
        public readonly float cooldown;
        public readonly float detectRange;
        public readonly float leashRange;
        public readonly float wanderRadius;
        public readonly int expReward;
        public readonly Vector2 colliderSize;
        public readonly Vector2 colliderOffset;
    }

    private sealed class EnemyScanResult
    {
        public EnemyScanResult(EnemySpec spec, string folderPath)
        {
            this.spec = spec;
            this.folderPath = folderPath;
        }

        public EnemySpec spec { get; }
        public string folderPath { get; }
        public Dictionary<EnemyAction, DirectionalSpriteSet> sources { get; } = new Dictionary<EnemyAction, DirectionalSpriteSet>();
    }

    private sealed class DirectionalSpriteSet
    {
        public DirectionalSpriteSet(string assetPath, EnemyAction action, int quarterSize)
        {
            this.assetPath = assetPath;
            this.action = action;
            framesByDirection = new[]
            {
                new List<Sprite>(quarterSize),
                new List<Sprite>(quarterSize),
                new List<Sprite>(quarterSize),
                new List<Sprite>(quarterSize)
            };
        }

        public string assetPath { get; }
        public EnemyAction action { get; }
        public List<Sprite>[] framesByDirection { get; }
    }

    private readonly struct ClipKey : IEquatable<ClipKey>
    {
        public ClipKey(EnemyAction action, int directionIndex)
        {
            this.action = action;
            this.directionIndex = directionIndex;
        }

        public readonly EnemyAction action;
        public readonly int directionIndex;

        public bool Equals(ClipKey other)
        {
            return action == other.action && directionIndex == other.directionIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is ClipKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)action * 397) ^ directionIndex;
            }
        }
    }

    private sealed class GenerationContext
    {
        private readonly List<string> info = new List<string>();
        private readonly List<string> warnings = new List<string>();
        private readonly List<string> errors = new List<string>();

        public GenerationContext(bool validationOnly)
        {
            this.validationOnly = validationOnly;
        }

        public bool validationOnly { get; }

        public void Info(string message) => info.Add(message);
        public void Warning(string message) => warnings.Add(message);
        public void Error(string message) => errors.Add(message);

        public void Flush()
        {
            foreach (string message in info)
                Debug.Log($"[EnemyContentGenerator] {message}");
            foreach (string message in warnings)
                Debug.LogWarning($"[EnemyContentGenerator] {message}");
            foreach (string message in errors)
                Debug.LogError($"[EnemyContentGenerator] {message}");

            string mode = validationOnly ? "Validation" : "Generation";
            if (errors.Count == 0)
                Debug.Log($"[EnemyContentGenerator] {mode} finished with {warnings.Count} warnings.");
            else
                Debug.LogError($"[EnemyContentGenerator] {mode} finished with {errors.Count} errors and {warnings.Count} warnings.");
        }
    }

    private enum EnemyAction
    {
        Idle,
        Walk,
        Run,
        Attack,
        Hurt,
        Death,
        WalkAttack,
        RunAttack
    }

    private sealed class NaturalStringComparer : IComparer<string>
    {
        private static readonly Regex TokenRegex = new Regex(@"\d+|\D+", RegexOptions.Compiled);
        public static readonly NaturalStringComparer Instance = new NaturalStringComparer();

        public int Compare(string x, string y)
        {
            if (ReferenceEquals(x, y))
                return 0;
            if (x == null)
                return -1;
            if (y == null)
                return 1;

            var xTokens = TokenRegex.Matches(x);
            var yTokens = TokenRegex.Matches(y);
            int count = Mathf.Min(xTokens.Count, yTokens.Count);
            for (int i = 0; i < count; i++)
            {
                string left = xTokens[i].Value;
                string right = yTokens[i].Value;
                bool leftNumeric = char.IsDigit(left[0]);
                bool rightNumeric = char.IsDigit(right[0]);
                if (leftNumeric && rightNumeric)
                {
                    int compare = long.Parse(left).CompareTo(long.Parse(right));
                    if (compare != 0)
                        return compare;
                    continue;
                }

                int tokenCompare = string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
                if (tokenCompare != 0)
                    return tokenCompare;
            }

            return xTokens.Count.CompareTo(yTokens.Count);
        }
    }
}
