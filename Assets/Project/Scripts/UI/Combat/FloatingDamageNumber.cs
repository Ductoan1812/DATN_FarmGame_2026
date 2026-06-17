using TMPro;
using System;
using UnityEngine;

[RequireComponent(typeof(TextMeshPro))]
public class FloatingDamageNumber : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.75f;
    [SerializeField] private float riseSpeed = 1.4f;
    [SerializeField] private float horizontalJitter = 0.25f;

    private TextMeshPro label;
    private Color baseColor;
    private float elapsed;
    private Vector3 velocity;
    private Action<FloatingDamageNumber> onComplete;
    private bool playing;

    private void Awake()
    {
        label = GetComponent<TextMeshPro>();
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = false;

        var renderer = label.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sortingLayerName = "Effect";
            renderer.sortingOrder = 130;
        }
    }

    public void Play(string text, Color color, float size, bool critical, Action<FloatingDamageNumber> completed = null)
    {
        if (label == null)
            label = GetComponent<TextMeshPro>();

        label.text = text;
        label.color = color;
        label.fontSize = size;
        label.fontStyle = critical ? FontStyles.Bold : FontStyles.Normal;
        baseColor = color;
        elapsed = 0f;
        velocity = new Vector3(UnityEngine.Random.Range(-horizontalJitter, horizontalJitter), riseSpeed, 0f);
        onComplete = completed;
        playing = true;
    }

    private void Update()
    {
        if (!playing)
            return;

        float dt = Time.unscaledDeltaTime;
        elapsed += dt;
        transform.position += velocity * dt;

        float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, lifetime));
        var color = baseColor;
        color.a = 1f - t;
        label.color = color;
        transform.localScale = Vector3.one * Mathf.Lerp(1.15f, 0.9f, t);

        if (elapsed >= lifetime)
            Complete();
    }

    private void Complete()
    {
        playing = false;
        var callback = onComplete;
        onComplete = null;

        if (callback != null)
        {
            callback.Invoke(this);
            return;
        }

        gameObject.SetActive(false);
    }
}
