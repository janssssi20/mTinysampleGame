using UnityEngine;

// Gentle idle bob for mascots and hints: up and down, never still.
public class Bob : MonoBehaviour
{
    [Tooltip("How far it moves, in UI units.")]
    public float height = 10f;
    public float speed = 2.6f;

    private RectTransform rect;
    private Vector2 rest;
    private float phase;

    private void Awake()
    {
        rect = transform as RectTransform;
        if (rect != null) rest = rect.anchoredPosition;
        phase = Random.value * 6.28f;
    }

    private void Update()
    {
        if (rect == null) return;
        rect.anchoredPosition = rest + Vector2.up * Mathf.Sin(Time.unscaledTime * speed + phase) * height;
    }
}
