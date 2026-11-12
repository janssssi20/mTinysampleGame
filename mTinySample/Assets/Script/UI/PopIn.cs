using UnityEngine;

// Panels and cards pop in: they grow from a little smaller, overshoot a
// touch and settle, so new things feel friendly rather than abrupt.
public class PopIn : MonoBehaviour
{
    public float duration = 0.28f;
    [Range(0.5f, 1f)] public float startScale = 0.82f;

    private Vector3 rest = Vector3.one;
    private float t = 1f;
    private bool captured;

    private void Awake()
    {
        rest = transform.localScale;
        captured = true;
    }

    private void OnEnable()
    {
        if (!captured) Awake();
        t = 0f;
        transform.localScale = rest * startScale;
    }

    private void Update()
    {
        if (t >= 1f) return;

        t = Mathf.Min(1f, t + Time.unscaledDeltaTime / duration);
        transform.localScale = rest * Mathf.LerpUnclamped(startScale, 1f, EaseOutBack(t));
    }

    // Goes a little past 1 before settling
    public static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}
