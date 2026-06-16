using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EnemyObject))]
public class EnemyDeathEffect : MonoBehaviour
{
    [SerializeField] private float fadeSeconds = 0.35f;
    [SerializeField] private float scalePunch = 1.08f;

    private EnemyObject enemy;
    private SpriteRenderer[] renderers;
    private Color[] originalColors;
    private Vector3 originalScale;
    private Coroutine routine;
    public float RequiredLifetime => Mathf.Max(0.01f, fadeSeconds);

    private void Awake()
    {
        enemy = GetComponent<EnemyObject>();
        Cache();
    }

    private void OnEnable()
    {
        if (enemy == null)
            enemy = GetComponent<EnemyObject>();
        if (enemy != null)
            enemy.DeathStarted += OnDeathStarted;
    }

    private void OnDisable()
    {
        if (enemy != null)
            enemy.DeathStarted -= OnDeathStarted;
    }

    private void OnDeathStarted()
    {
        if (routine != null)
            StopCoroutine(routine);
        routine = StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        Cache();
        float elapsed = 0f;
        while (elapsed < fadeSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, fadeSeconds));
            transform.localScale = originalScale * Mathf.Lerp(scalePunch, 0.9f, t);

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                var color = i < originalColors.Length ? originalColors[i] : Color.white;
                color.a *= 1f - t;
                renderers[i].color = color;
            }

            yield return null;
        }

        routine = null;
    }

    private void Cache()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            originalColors[i] = renderers[i] != null ? renderers[i].color : Color.white;
        originalScale = transform.localScale;
    }
}
