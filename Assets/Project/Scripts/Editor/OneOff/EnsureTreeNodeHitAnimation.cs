using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class EnsureTreeNodeHitAnimation
{
    private const string AnimationFolder = "Assets/Project/Animations/WorldEntities/TreeNode";
    private const string IdleClipPath = AnimationFolder + "/TreeNode_Idle.anim";
    private const string HitClipPath = AnimationFolder + "/TreeNode_Hit.anim";
    private const string ControllerPath = AnimationFolder + "/TreeNode.controller";
    private const string PrefabPath = "Assets/Project/Prefabs/WorldEntities/TreeNode_Base.prefab";

    [MenuItem("Tools/DATN/One-off Setup/Animation/Ensure TreeNode Hit Animation")]
    public static void RunFromMenu()
    {
        Run();
    }

    public static void Run()
    {
        EnsureFolder(AnimationFolder);

        var idleClip = CreateOrUpdateIdleClip();
        var hitClip = CreateOrUpdateHitClip();
        var controller = CreateOrUpdateController(idleClip, hitClip);
        PatchPrefab(controller);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[EnsureTreeNodeHitAnimation] TreeNode hit animation assets and prefab wiring are ready.");
    }

    private static AnimationClip CreateOrUpdateIdleClip()
    {
        var clip = LoadOrCreateClip(IdleClipPath, "TreeNode_Idle");
        clip.ClearCurves();
        clip.frameRate = 60f;

        SetCurve(clip, typeof(Transform), "m_LocalPosition.x", Constant(0f, 0f));
        SetCurve(clip, typeof(Transform), "m_LocalPosition.y", Constant(0f, 0f));
        SetCurve(clip, typeof(Transform), "m_LocalScale.x", Constant(0f, 1f));
        SetCurve(clip, typeof(Transform), "m_LocalScale.y", Constant(0f, 1f));
        SetCurve(clip, typeof(Transform), "m_LocalScale.z", Constant(0f, 1f));
        SetCurve(clip, typeof(SpriteRenderer), "m_Color.r", Constant(0f, 0.4f));
        SetCurve(clip, typeof(SpriteRenderer), "m_Color.g", Constant(0f, 0.72f));
        SetCurve(clip, typeof(SpriteRenderer), "m_Color.b", Constant(0f, 0.42f));
        SetCurve(clip, typeof(SpriteRenderer), "m_Color.a", Constant(0f, 1f));
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimationClip CreateOrUpdateHitClip()
    {
        var clip = LoadOrCreateClip(HitClipPath, "TreeNode_Hit");
        clip.ClearCurves();
        clip.frameRate = 60f;

        SetCurve(clip, typeof(Transform), "m_LocalPosition.x", Ease(
            0f, 0f,
            0.04f, -0.05f,
            0.08f, 0.075f,
            0.13f, -0.03f,
            0.2f, 0f));

        SetCurve(clip, typeof(Transform), "m_LocalScale.x", Ease(
            0f, 1f,
            0.04f, 1.045f,
            0.08f, 0.985f,
            0.13f, 1.015f,
            0.2f, 1f));

        SetCurve(clip, typeof(Transform), "m_LocalScale.y", Ease(
            0f, 1f,
            0.04f, 0.97f,
            0.08f, 1.025f,
            0.13f, 0.99f,
            0.2f, 1f));

        SetCurve(clip, typeof(SpriteRenderer), "m_Color.r", Ease(
            0f, 0.4f,
            0.03f, 1f,
            0.08f, 0.82f,
            0.2f, 0.4f));

        SetCurve(clip, typeof(SpriteRenderer), "m_Color.g", Ease(
            0f, 0.72f,
            0.03f, 0.95f,
            0.08f, 0.8f,
            0.2f, 0.72f));

        SetCurve(clip, typeof(SpriteRenderer), "m_Color.b", Ease(
            0f, 0.42f,
            0.03f, 0.62f,
            0.08f, 0.5f,
            0.2f, 0.42f));

        SetCurve(clip, typeof(SpriteRenderer), "m_Color.a", Constant(0f, 1f));
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorController CreateOrUpdateController(AnimationClip idleClip, AnimationClip hitClip)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        while (controller.parameters.Length > 0)
            controller.RemoveParameter(controller.parameters[0]);

        if (controller.layers.Length == 0)
            controller.AddLayer("Base Layer");

        var stateMachine = controller.layers[0].stateMachine;
        while (stateMachine.states.Length > 0)
            stateMachine.RemoveState(stateMachine.states[0].state);
        while (stateMachine.anyStateTransitions.Length > 0)
            stateMachine.RemoveAnyStateTransition(stateMachine.anyStateTransitions[0]);

        controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);

        var idleState = stateMachine.AddState("Idle");
        idleState.motion = idleClip;
        idleState.writeDefaultValues = true;
        stateMachine.defaultState = idleState;

        var hitState = stateMachine.AddState("Hit");
        hitState.motion = hitClip;
        hitState.speed = 1f;
        hitState.writeDefaultValues = true;

        var anyToHit = stateMachine.AddAnyStateTransition(hitState);
        anyToHit.hasExitTime = false;
        anyToHit.hasFixedDuration = true;
        anyToHit.duration = 0.02f;
        anyToHit.canTransitionToSelf = false;
        anyToHit.AddCondition(AnimatorConditionMode.If, 0f, "Hit");

        var hitToIdle = hitState.AddTransition(idleState);
        hitToIdle.hasExitTime = true;
        hitToIdle.exitTime = 0.95f;
        hitToIdle.hasFixedDuration = true;
        hitToIdle.duration = 0.04f;

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void PatchPrefab(AnimatorController controller)
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root == null)
            throw new FileNotFoundException($"Missing prefab at {PrefabPath}");

        try
        {
            var animator = root.GetComponent<Animator>();
            if (animator == null)
                animator = root.AddComponent<Animator>();

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var reaction = root.GetComponent<ResourceHitReactionObject>();
            if (reaction == null)
                reaction = root.AddComponent<ResourceHitReactionObject>();

            var renderer = root.GetComponentInChildren<SpriteRenderer>();
            var serialized = new SerializedObject(reaction);
            serialized.FindProperty("targetRenderer").objectReferenceValue = renderer;
            serialized.FindProperty("targetAnimator").objectReferenceValue = animator;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static AnimationClip LoadOrCreateClip(string path, string clipName)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip != null)
            return clip;

        clip = new AnimationClip { name = clipName };
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static void SetCurve(AnimationClip clip, System.Type type, string propertyName, AnimationCurve curve)
    {
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, type, propertyName), curve);
    }

    private static AnimationCurve Constant(float time, float value)
    {
        return AnimationCurve.Constant(time, time + 1f / 60f, value);
    }

    private static AnimationCurve Ease(params float[] keyPairs)
    {
        var keyframes = new Keyframe[keyPairs.Length / 2];
        for (int index = 0; index < keyframes.Length; index++)
            keyframes[index] = new Keyframe(keyPairs[index * 2], keyPairs[index * 2 + 1]);

        return new AnimationCurve(keyframes);
    }

    private static void EnsureFolder(string assetFolder)
    {
        var normalized = assetFolder.Replace("\\", "/");
        var parts = normalized.Split('/');
        string current = parts[0];
        for (int index = 1; index < parts.Length; index++)
        {
            string next = current + "/" + parts[index];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[index]);
            current = next;
        }
    }
}
