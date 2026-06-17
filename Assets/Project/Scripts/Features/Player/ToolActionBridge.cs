using Assets.HeroEditor4D.Common.Scripts.CharacterScripts;
using System.Collections;
using UnityEngine;

public class ToolActionBridge : MonoBehaviour
{
    [SerializeField] private float safetyTimeout = 1.25f;
    [SerializeField] private string defaultFallbackTrigger = "Slash1H";
    [SerializeField] private bool executeImmediatelyWhenAnimationMissing = true;

    private AnimationManager _anim;
    private AnimationEvents _animEvents;
    private EntityRuntime _pendingActor;
    private EntityRuntime _pendingItem;
    private bool _waitingForStrike;
    private Coroutine _safetyCoroutine;

    private void Start()
    {
        var character4D = GetComponentInChildren<Character4D>();
        if (character4D != null)
        {
            _anim = character4D.AnimationManager;
            _animEvents = character4D.GetComponent<AnimationEvents>();
        }

        if (_animEvents != null)
            _animEvents.OnEvent += OnAnimEvent;
    }

    private void OnDestroy()
    {
        if (_animEvents != null)
            _animEvents.OnEvent -= OnAnimEvent;
    }

    public bool IsBusy => _anim != null && _anim.IsAction;

    /// <summary>
    /// Play animation rồi fire AnimStrikeEvent lên item khi đến frame "Strike".
    /// Nếu animation/trigger chưa có trong animator, vẫn thực thi tool ngay để không khóa input.
    /// </summary>
    public bool Request(EntityRuntime actor, EntityRuntime item, string animTrigger)
    {
        if (_anim != null && _anim.IsAction)
            return false;

        _pendingActor = actor;
        _pendingItem = item;
        _waitingForStrike = true;

        if (_anim == null || _anim.Animator == null)
        {
            Debug.LogWarning("[ToolActionBridge] Missing player animator. Executing action without animation.");
            ExecutePendingStrike();
            ResetState();
            return true;
        }

        string resolvedTrigger = ResolveTrigger(animTrigger);
        if (string.IsNullOrWhiteSpace(resolvedTrigger))
        {
            Debug.LogWarning($"[ToolActionBridge] Animator trigger '{animTrigger}' not found. Executing action without animation.");
            if (executeImmediatelyWhenAnimationMissing)
                ExecutePendingStrike();
            ResetState();
            return true;
        }

        _anim.IsAction = true;
        _anim.Animator.ResetTrigger(resolvedTrigger);
        _anim.Animator.SetTrigger(resolvedTrigger);

        if (!string.Equals(resolvedTrigger, animTrigger, System.StringComparison.Ordinal))
        {
            Debug.LogWarning($"[ToolActionBridge] Animator trigger '{animTrigger}' not found. Using fallback '{resolvedTrigger}'.");
        }

        if (_safetyCoroutine != null)
            StopCoroutine(_safetyCoroutine);

        _safetyCoroutine = StartCoroutine(SafetyResetCoroutine(safetyTimeout));

        return true;
    }

    // ── AnimationEvents callback ──────────────────────────────────────────────

    private void OnAnimEvent(string eventName)
    {
        // "Strike": thực thi logic tool (chỉ xử lý 1 lần)
        if (eventName == "Strike" && _waitingForStrike)
        {
            ExecutePendingStrike();
        }

        // "End" / "Finish": animation hoàn tất → reset để player thao tác được
        if (eventName == "End" || eventName == "Finish")
        {
            ResetState();
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ResetState()
    {
        if (_safetyCoroutine != null)
        {
            StopCoroutine(_safetyCoroutine);
            _safetyCoroutine = null;
        }

        _waitingForStrike = false;
        _pendingActor     = null;
        _pendingItem      = null;
        if (_anim != null)
            _anim.IsAction = false;
    }

    private IEnumerator SafetyResetCoroutine(float timeout)
    {
        yield return new WaitForSeconds(timeout);
        _safetyCoroutine = null;

        if (_anim != null && _anim.IsAction)
        {
            if (_waitingForStrike)
                ExecutePendingStrike();

            Debug.LogWarning($"[ToolActionBridge] Safety reset sau {timeout}s: animation không fire event 'End/Finish'.");
            ResetState();
        }
    }

    private void ExecutePendingStrike()
    {
        if (!_waitingForStrike)
            return;

        _waitingForStrike = false;
        _pendingItem?.TriggerEvent(new AnimStrikeEvent(_pendingActor, _pendingItem));
    }

    private string ResolveTrigger(string requestedTrigger)
    {
        if (HasTrigger(requestedTrigger))
            return requestedTrigger;

        foreach (string candidate in GetFallbackTriggers(requestedTrigger))
        {
            if (HasTrigger(candidate))
                return candidate;
        }

        return string.Empty;
    }

    private string[] GetFallbackTriggers(string requestedTrigger)
    {
        if (!string.IsNullOrWhiteSpace(requestedTrigger))
        {
            string normalized = requestedTrigger.Trim().ToLowerInvariant();
            if (normalized is "pickaxe" or "hoe" or "axe" or "scythe" or "harvert" or "harvest" or "putdown" or "sow")
                return new[] { defaultFallbackTrigger, "Slash1H", "Jab", "Slash2H", "HeavySlash1H", "FastStab" };
        }

        return new[] { defaultFallbackTrigger, "Slash1H", "Jab", "Slash2H" };
    }

    private bool HasTrigger(string triggerName)
    {
        if (string.IsNullOrWhiteSpace(triggerName) || _anim?.Animator == null)
            return false;

        foreach (var parameter in _anim.Animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == triggerName)
                return true;
        }

        return false;
    }
}
