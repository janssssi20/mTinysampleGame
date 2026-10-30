using UnityEngine;

// Gently grows and shrinks a UI element to say "tap me" to children
// who can't read yet. Turn it on or off with the component's enabled flag.
public class PulseScale : MonoBehaviour
{
    [Tooltip("Extra size at the top of the pulse (0.08 = 8% bigger).")]
    public float amount = 0.08f;

    [Tooltip("Pulses per second.")]
    public float speed = 1.2f;

    private Vector3 restScale;
    private bool hasRestScale;


    private void OnEnable()
    {
        if (!hasRestScale)
        {
            restScale = transform.localScale;
            hasRestScale = true;
        }
    }

    private void OnDisable()
    {
        if (hasRestScale)
        {
            transform.localScale = restScale;
        }
    }

    private void Update()
    {
        float wave = (Mathf.Sin(Time.unscaledTime * speed * Mathf.PI * 2f) + 1f) * 0.5f;

        transform.localScale = restScale * (1f + wave * amount);
    }
}
