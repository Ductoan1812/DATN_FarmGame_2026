using Assets.HeroEditor4D.Common.Scripts.CharacterScripts;
using UnityEngine;
public class ToolActionBridge : MonoBehaviour
{
    private AnimationManager _anim;
    private AnimationEvents _animEvents;
    private EntityRuntime _pendingActor;
    private EntityRuntime _pendingItem;
    private bool _waitingForStrike;

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
    /// Trả về false nếu đang bận hoặc không có animator.
    /// </summary>
    public bool Request(EntityRuntime actor, EntityRuntime item, string animTrigger)
    {
        if (_anim == null || _anim.IsAction)
            return false;

        _pendingActor = actor;
        _pendingItem = item;
        _waitingForStrike = true;

        _anim.Animator.SetTrigger(animTrigger);
        _anim.IsAction = true;
        return true;
    }

    // ── AnimationEvents callback ──────────────────────────────────────────────

    private void OnAnimEvent(string eventName)
    {
        if (!_waitingForStrike) return;
        if (eventName != "Strike") return;

        _waitingForStrike = false;

        _pendingItem?.TriggerEvent(new AnimStrikeEvent(_pendingActor, _pendingItem));

        _pendingActor = null;
        _pendingItem = null;
    }
}
