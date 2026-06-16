using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyAlertIndicator : MonoBehaviour
{
    [SerializeField] private Vector3 worldOffset = new(0f, 1.55f, 0f);
    [SerializeField] private float showSeconds = 0.65f;
    [SerializeField] private Color alertColor = new(1f, 0.85f, 0.1f);

    private EnemyObject enemy;
    private TextMeshPro label;
    private Coroutine showRoutine;

    private void Awake()
    {
        enemy = GetComponentInParent<EnemyObject>();
        EnsureLabel();
    }

    private void OnEnable()
    {
        if (enemy == null)
            enemy = GetComponentInParent<EnemyObject>();
        if (enemy != null)
            enemy.StateChanged += OnStateChanged;
    }

    private void OnDisable()
    {
        if (enemy != null)
            enemy.StateChanged -= OnStateChanged;
    }

    private void LateUpdate()
    {
        if (label != null)
            label.transform.position = transform.position + worldOffset;
    }

    private void OnStateChanged(EnemyObject.EnemyState state)
    {
        if (state == EnemyObject.EnemyState.Dead)
        {
            Hide();
            return;
        }

        if (state == EnemyObject.EnemyState.ReturnHome)
        {
            Show("?", new Color(0.75f, 0.9f, 1f), showSeconds * 0.75f);
            return;
        }

        if (state != EnemyObject.EnemyState.Chase && state != EnemyObject.EnemyState.Attack)
            return;

        Show("!", alertColor, showSeconds);
    }

    private void Show(string value, Color color, float seconds)
    {
        if (showRoutine != null)
            StopCoroutine(showRoutine);
        label.text = value;
        label.color = color;
        showRoutine = StartCoroutine(ShowRoutine(seconds));
    }

    private IEnumerator ShowRoutine(float seconds)
    {
        label.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, seconds));
        label.gameObject.SetActive(false);
        showRoutine = null;
    }

    private void Hide()
    {
        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }
        if (label != null)
            label.gameObject.SetActive(false);
    }

    private void EnsureLabel()
    {
        if (label != null) return;

        var go = new GameObject("EnemyAlert");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = worldOffset;
        label = go.AddComponent<TextMeshPro>();
        label.text = "!";
        label.fontSize = 4f;
        label.alignment = TextAlignmentOptions.Center;
        label.fontStyle = FontStyles.Bold;
        label.color = alertColor;

        var renderer = label.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sortingLayerName = "Effect";
            renderer.sortingOrder = 125;
        }

        label.gameObject.SetActive(false);
    }
}
