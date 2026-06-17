using UnityEngine;

public class InteractablePrompt : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private Collider2D interactionCollider;
    [SerializeField] private bool autoAssignCollider = true;
    [SerializeField] private bool warnIfColliderIsNotTrigger = true;
    [SerializeField] private string playerTag = "Player";

    public Collider2D InteractionCollider => interactionCollider;

    private void Reset()
    {
        AutoAssignCollider();
    }

    private void Awake()
    {
        if (interactionCollider == null && autoAssignCollider)
            AutoAssignCollider();

        if (interactionCollider != null && warnIfColliderIsNotTrigger && !interactionCollider.isTrigger)
        {
            Debug.LogWarning(
                $"[InteractablePrompt] '{name}' interactionCollider is not trigger.",
                this);
        }
    }

    public bool AcceptsScanCollider(Collider2D candidate)
    {
        if (candidate == null) return false;
        if (interactionCollider == null) return true;
        return candidate == interactionCollider;
    }

    public void Interact(EntityRuntime interactor) { }

    public void SetPromptText(string text) { }

    private void AutoAssignCollider()
    {
        interactionCollider = GetComponent<Collider2D>();
        if (interactionCollider != null) return;
        interactionCollider = GetComponentInChildren<Collider2D>();
    }

    private void OnDrawGizmosSelected()
    {
        if (interactionCollider == null) return;

        Gizmos.color = new Color(0.2f, 0.9f, 0.45f, 0.35f);
        var bounds = interactionCollider.bounds;
        Gizmos.DrawCube(bounds.center, bounds.size);

        Gizmos.color = new Color(0.2f, 0.9f, 0.45f, 1f);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
