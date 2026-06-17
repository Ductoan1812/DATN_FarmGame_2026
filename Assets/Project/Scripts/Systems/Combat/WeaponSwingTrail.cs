using UnityEngine;

[RequireComponent(typeof(PlayerControler))]
public class WeaponSwingTrail : MonoBehaviour
{
    [SerializeField] private float distanceFromPlayer = 0.65f;
    [SerializeField] private float width = 0.16f;
    [SerializeField] private float time = 0.18f;
    [SerializeField] private Color startColor = new(1f, 1f, 1f, 0.65f);
    [SerializeField] private Color endColor = new(1f, 1f, 1f, 0f);
    [SerializeField] private Material trailMaterial;

    private PlayerControler player;
    private ToolActionBridge bridge;
    private TrailRenderer trail;
    private static bool warnedMissingMaterial;

    private void Awake()
    {
        player = GetComponent<PlayerControler>();
        bridge = GetComponent<ToolActionBridge>();
        EnsureTrail();
    }

    private void LateUpdate()
    {
        if (player == null || trail == null)
            return;

        Vector3 direction = player.LastMoveDirection.sqrMagnitude > 0.001f ? player.LastMoveDirection.normalized : Vector3.down;
        trail.transform.position = transform.position + direction * distanceFromPlayer;

        bool active = bridge != null && bridge.IsBusy;
        if (trail.emitting != active)
        {
            trail.emitting = active;
            if (active)
                trail.Clear();
        }
    }

    private void EnsureTrail()
    {
        if (trail != null) return;

        var go = new GameObject("WeaponSwingTrail");
        go.transform.SetParent(transform, false);
        trail = go.AddComponent<TrailRenderer>();
        trail.time = time;
        trail.startWidth = width;
        trail.endWidth = 0f;
        trail.startColor = startColor;
        trail.endColor = endColor;
        trail.sortingOrder = 45;
        trail.emitting = false;
        var resolvedMaterial = ResolveMaterial();
        if (resolvedMaterial != null)
            trail.material = resolvedMaterial;
    }

    private Material ResolveMaterial()
    {
        if (trailMaterial != null)
            return trailMaterial;

        string[] shaderNames =
        {
            "Sprites/Default",
            "Universal Render Pipeline/2D/Sprite-Lit-Default",
            "Universal Render Pipeline/Unlit"
        };

        foreach (string shaderName in shaderNames)
        {
            var shader = Shader.Find(shaderName);
            if (shader != null)
                return new Material(shader);
        }

        if (!warnedMissingMaterial)
        {
            warnedMissingMaterial = true;
            Debug.LogWarning("[WeaponSwingTrail] No compatible trail shader found. TrailRenderer will use Unity fallback material.");
        }

        return null;
    }
}
