using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TargetHighlightBridge : MonoBehaviour
{
    [SerializeField] private bool autoCollectRenderers = true;
    [SerializeField] private Color highlightTint = new(1f, 0.95f, 0.65f, 1f);
    [SerializeField] private Color normalTint = Color.white;

    [SerializeField] private List<SpriteRenderer> renderers = new();
    [SerializeField] private WorldEntityNameplate nameplate;

    private bool initialized;

    private void Awake()
    {
        EnsureRefs();
    }

    public void SetHighlighted(bool highlighted)
    {
        EnsureRefs();

        Color tint = highlighted ? highlightTint : normalTint;
        for (int i = 0; i < renderers.Count; i++)
        {
            var renderer = renderers[i];
            if (renderer == null) continue;
            renderer.color = tint;
        }

        if (nameplate != null)
            nameplate.SetHighlighted(highlighted);
    }

    private void EnsureRefs()
    {
        if (initialized) return;
        initialized = true;

        if (nameplate == null)
            nameplate = GetComponentInChildren<WorldEntityNameplate>();

        if (!autoCollectRenderers || renderers.Count > 0) return;

        var all = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < all.Length; i++)
        {
            var sr = all[i];
            if (sr == null) continue;
            renderers.Add(sr);
        }
    }
}
