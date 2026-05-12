using UnityEngine;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public class DayNightLightController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Light2D globalLight;
    [SerializeField] private TimeManager timeManager;
    [SerializeField] private bool autoFindReferences = true;

    [Header("Lighting")]
    [SerializeField] private Gradient colorOverDay = CreateDefaultColorGradient();
    [SerializeField] private AnimationCurve intensityOverDay = CreateDefaultIntensityCurve();

    [Header("Editor Preview")]
    [Range(0f, 1f)] [SerializeField] private float previewTime = 0.25f;
    [SerializeField] private bool usePreviewTimeInEditMode;

    private void Reset()
    {
        globalLight = GetComponent<Light2D>();
        timeManager = FindAnyObjectByType<TimeManager>();
        ApplyAt(previewTime);
    }

    private void Awake()
    {
        ResolveReferences();
        ApplyAt(timeManager != null ? timeManager.NormalizedTime : previewTime);
    }

    private void Update()
    {
        ResolveReferences();

        if (timeManager != null)
        {
            ApplyAt(timeManager.NormalizedTime);
            return;
        }

        if (!Application.isPlaying && usePreviewTimeInEditMode)
        {
            ApplyAt(previewTime);
        }
    }

    private void OnValidate()
    {
        if (colorOverDay == null)
            colorOverDay = CreateDefaultColorGradient();

        if (intensityOverDay == null || intensityOverDay.length == 0)
            intensityOverDay = CreateDefaultIntensityCurve();

        if (!Application.isPlaying && usePreviewTimeInEditMode)
            ApplyAt(previewTime);
    }

    private void ApplyAt(float normalizedTime)
    {
        if (globalLight == null) return;

        float t = Mathf.Repeat(normalizedTime, 1f);
        globalLight.color = colorOverDay.Evaluate(t);
        globalLight.intensity = Mathf.Max(0f, intensityOverDay.Evaluate(t));
    }

    private void ResolveReferences()
    {
        if (!autoFindReferences) return;

        if (timeManager == null)
            timeManager = FindAnyObjectByType<TimeManager>();

        if (globalLight != null) return;

        Light2D[] lights = FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Light2D light in lights)
        {
            if (light.lightType == Light2D.LightType.Global)
            {
                globalLight = light;
                return;
            }
        }
    }

    private static Gradient CreateDefaultColorGradient()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.08f, 0.11f, 0.24f), 0f),
                new GradientColorKey(new Color(0.95f, 0.48f, 0.28f), 0.22f),
                new GradientColorKey(new Color(1f, 0.95f, 0.82f), 0.34f),
                new GradientColorKey(new Color(1f, 0.95f, 0.82f), 0.68f),
                new GradientColorKey(new Color(0.98f, 0.44f, 0.24f), 0.78f),
                new GradientColorKey(new Color(0.08f, 0.11f, 0.24f), 0.92f),
                new GradientColorKey(new Color(0.08f, 0.11f, 0.24f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            });
        return gradient;
    }

    private static AnimationCurve CreateDefaultIntensityCurve()
    {
        return new AnimationCurve(
            new Keyframe(0f, 0.25f),
            new Keyframe(0.22f, 0.55f),
            new Keyframe(0.34f, 1f),
            new Keyframe(0.68f, 1f),
            new Keyframe(0.78f, 0.55f),
            new Keyframe(0.92f, 0.25f),
            new Keyframe(1f, 0.25f));
    }
}
