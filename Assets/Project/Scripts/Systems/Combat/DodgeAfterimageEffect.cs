using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerControler))]
public class DodgeAfterimageEffect : MonoBehaviour
{
    [SerializeField] private float spawnInterval = 0.035f;
    [SerializeField] private float fadeSeconds = 0.25f;
    [SerializeField] private Color color = new(0.45f, 0.85f, 1f, 0.35f);
    [SerializeField, Min(1)] private int prewarmCount = 8;

    private PlayerControler player;
    private SpriteRenderer[] sourceRenderers;
    private float nextSpawnRealtime;
    private readonly List<AfterimageSnapshot> snapshots = new();
    private Transform poolRoot;
    private int nextSnapshotIndex;

    private void Awake()
    {
        player = GetComponent<PlayerControler>();
        RefreshRenderers();
        EnsurePool();
    }

    private void Update()
    {
        TickSnapshots();

        if (player == null || !player.IsDodging)
            return;

        if (Time.realtimeSinceStartup < nextSpawnRealtime)
            return;

        nextSpawnRealtime = Time.realtimeSinceStartup + spawnInterval;
        SpawnAfterimage();
    }

    private void SpawnAfterimage()
    {
        RefreshRenderers();
        EnsurePool();
        var snapshot = GetSnapshot();
        snapshot.Capture(sourceRenderers, color, fadeSeconds);
    }

    private void TickSnapshots()
    {
        float delta = Time.unscaledDeltaTime;
        foreach (var snapshot in snapshots)
            snapshot.Tick(delta);
    }

    private AfterimageSnapshot GetSnapshot()
    {
        EnsurePool();

        for (int i = 0; i < snapshots.Count; i++)
        {
            int index = (nextSnapshotIndex + i) % snapshots.Count;
            if (!snapshots[index].IsActive)
            {
                nextSnapshotIndex = (index + 1) % snapshots.Count;
                return snapshots[index];
            }
        }

        var fallback = snapshots[nextSnapshotIndex];
        nextSnapshotIndex = (nextSnapshotIndex + 1) % snapshots.Count;
        return fallback;
    }

    private void RefreshRenderers()
    {
        sourceRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void EnsurePool()
    {
        if (poolRoot == null)
        {
            var root = new GameObject($"{name}_DodgeAfterimagePool");
            poolRoot = root.transform;
        }

        int target = Mathf.Max(1, prewarmCount);
        while (snapshots.Count < target)
            snapshots.Add(new AfterimageSnapshot(poolRoot, snapshots.Count));
    }

    private void OnDestroy()
    {
        if (poolRoot != null)
            Destroy(poolRoot.gameObject);
    }

    private sealed class AfterimageSnapshot
    {
        private readonly GameObject root;
        private readonly List<SpriteRenderer> renderers = new();
        private float elapsed;
        private float lifetime = 0.25f;
        private Color baseColor;

        public bool IsActive { get; private set; }

        public AfterimageSnapshot(Transform parent, int index)
        {
            root = new GameObject($"DodgeAfterimage_{index:00}");
            root.transform.SetParent(parent, false);
            root.SetActive(false);
        }

        public void Capture(SpriteRenderer[] sources, Color color, float fadeSeconds)
        {
            lifetime = Mathf.Max(0.01f, fadeSeconds);
            baseColor = color;
            elapsed = 0f;
            EnsureRendererCount(sources != null ? sources.Length : 0);

            int visibleCount = 0;
            if (sources != null)
            {
                for (int i = 0; i < sources.Length; i++)
                {
                    var source = sources[i];
                    var clone = renderers[i];
                    if (source == null || source.sprite == null || !source.enabled)
                    {
                        clone.gameObject.SetActive(false);
                        continue;
                    }

                    clone.gameObject.SetActive(true);
                    clone.transform.position = source.transform.position;
                    clone.transform.rotation = source.transform.rotation;
                    clone.transform.localScale = source.transform.lossyScale;
                    clone.sprite = source.sprite;
                    clone.flipX = source.flipX;
                    clone.flipY = source.flipY;
                    clone.sortingLayerID = source.sortingLayerID;
                    clone.sortingOrder = source.sortingOrder - 1;
                    clone.color = baseColor;
                    visibleCount++;
                }
            }

            for (int i = sources != null ? sources.Length : 0; i < renderers.Count; i++)
                renderers[i].gameObject.SetActive(false);

            IsActive = visibleCount > 0;
            root.SetActive(IsActive);
        }

        public void Tick(float delta)
        {
            if (!IsActive)
                return;

            elapsed += delta;
            float t = Mathf.Clamp01(elapsed / lifetime);
            float alpha = baseColor.a * (1f - t);
            foreach (var renderer in renderers)
            {
                if (renderer == null || !renderer.gameObject.activeSelf) continue;
                var c = renderer.color;
                c.a = alpha;
                renderer.color = c;
            }

            if (elapsed >= lifetime)
                Deactivate();
        }

        private void Deactivate()
        {
            IsActive = false;
            root.SetActive(false);
        }

        private void EnsureRendererCount(int count)
        {
            while (renderers.Count < count)
            {
                var go = new GameObject($"Sprite_{renderers.Count:00}");
                go.transform.SetParent(root.transform, true);
                renderers.Add(go.AddComponent<SpriteRenderer>());
            }
        }
    }
}
