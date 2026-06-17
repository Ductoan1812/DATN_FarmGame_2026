using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class LocalPortalTrigger2D : MonoBehaviour
{
    [SerializeField] private string targetSpawnPointId;
    [SerializeField] private bool hideRenderersOnPlay = true;
    [SerializeField, Min(0.05f)] private float cooldownSeconds = 0.5f;

    private float nextAllowedTriggerTime;
    private bool requirePlayerExitBeforeNextTrigger;

    private void Awake()
    {
        var trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;

        if (!hideRenderersOnPlay)
            return;

        var renderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryTeleport(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryTeleport(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerControler>() != null)
            requirePlayerExitBeforeNextTrigger = false;
    }

    private void TryTeleport(Collider2D other)
    {
        if (Time.time < nextAllowedTriggerTime)
            return;

        var player = other.GetComponentInParent<PlayerControler>();
        if (player == null)
            return;

        if (requirePlayerExitBeforeNextTrigger)
            return;

        if (string.IsNullOrWhiteSpace(targetSpawnPointId))
            return;

        var targetPosition = SceneSpawnResolver.Resolve(targetSpawnPointId, player.transform.position);
        if (Vector2.Distance((Vector2)player.transform.position, targetPosition) <= 0.01f)
        {
            Debug.LogWarning($"[LocalPortalTrigger2D] Spawn point '{targetSpawnPointId}' was not resolved or matches current position.");
            nextAllowedTriggerTime = Time.time + 0.1f;
            return;
        }

        nextAllowedTriggerTime = Time.time + cooldownSeconds;
        requirePlayerExitBeforeNextTrigger = true;

        var body = player.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.velocity = Vector2.zero;
            body.position = targetPosition;
        }

        player.transform.position = new Vector3(targetPosition.x, targetPosition.y, player.transform.position.z);

        var entity = player.GetComponent<EntityRoot>()?.GetEntity();
        var worldService = GameManager.Instance?.WorldService;
        if (entity != null && worldService != null)
        {
            var cell = new Vector2Int(
                Mathf.FloorToInt(targetPosition.x),
                Mathf.FloorToInt(targetPosition.y));
            worldService.MoveEntity(entity.id, targetPosition, new[] { cell });
        }
    }
}
