using System.Collections.Generic;
using UnityEngine;

public class FloatingTextPool : MonoBehaviour
{
    [SerializeField, Min(0)] private int prewarmCount = 32;
    [SerializeField, Min(1)] private int maxCount = 96;

    private readonly Queue<FloatingDamageNumber> inactive = new();
    private readonly HashSet<FloatingDamageNumber> all = new();
    private Transform poolRoot;

    private void Awake()
    {
        EnsureRoot();
        Prewarm();
    }

    public FloatingDamageNumber Spawn(Vector3 position, string text, Color color, float size, bool critical)
    {
        EnsureRoot();
        var item = Get();
        if (item == null)
            return null;

        item.transform.position = position;
        item.transform.SetParent(poolRoot, true);
        item.gameObject.SetActive(true);
        item.Play(text, color, size, critical, Release);
        return item;
    }

    private FloatingDamageNumber Get()
    {
        while (inactive.Count > 0)
        {
            var item = inactive.Dequeue();
            if (item != null)
                return item;
        }

        if (all.Count >= Mathf.Max(1, maxCount))
            return null;

        return CreateItem();
    }

    private void Release(FloatingDamageNumber item)
    {
        if (item == null || !all.Contains(item))
            return;

        item.gameObject.SetActive(false);
        item.transform.SetParent(poolRoot, false);
        inactive.Enqueue(item);
    }

    private void Prewarm()
    {
        int target = Mathf.Min(Mathf.Max(0, prewarmCount), Mathf.Max(1, maxCount));
        while (all.Count < target)
        {
            var item = CreateItem();
            item.gameObject.SetActive(false);
            inactive.Enqueue(item);
        }
    }

    private FloatingDamageNumber CreateItem()
    {
        var go = new GameObject("FloatingText");
        go.transform.SetParent(poolRoot, false);
        var text = go.AddComponent<TMPro.TextMeshPro>();
        text.text = string.Empty;
        var item = go.AddComponent<FloatingDamageNumber>();
        all.Add(item);
        return item;
    }

    private void EnsureRoot()
    {
        if (poolRoot != null)
            return;

        var go = new GameObject("FloatingTextPool");
        go.transform.SetParent(transform, false);
        poolRoot = go.transform;
    }
}
