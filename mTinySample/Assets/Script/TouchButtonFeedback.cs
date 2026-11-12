using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Touch feedback for buttons: touchscreens have no hover state, so a
// button visibly "squishes" while a finger is on it.
public class TouchButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Tooltip("Scale while pressed, relative to the resting scale.")]
    public float pressedScale = 0.9f;

    [Tooltip("Off for buttons that make their own sound, like note blocks.")]
    public bool tapSound = true;

    private Vector3 restScale;
    private float target = 1f;
    private float current = 1f;
    private Selectable selectable;


    private void Awake()
    {
        restScale = transform.localScale;
        selectable = GetComponent<Selectable>();
    }

    private void OnDisable()
    {
        target = 1f;
        current = 1f;
        velocity = 0f;
        transform.localScale = restScale;
    }

    // A soft spring: it squashes in fast and springs back with a little
    // wobble past full size, which feels squishy to small hands
    [Tooltip("Springiness of the bounce back; higher = wobblier.")]
    public float stiffness = 420f;
    public float damping = 18f;

    private float velocity;

    private void Update()
    {
        if (Mathf.Abs(current - target) < 0.0005f && Mathf.Abs(velocity) < 0.001f)
        {
            if (current != target)
            {
                current = target;
                transform.localScale = restScale * current;
            }
            return;
        }

        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.033f);
        velocity += (stiffness * (target - current) - damping * velocity) * dt;
        current += velocity * dt;
        transform.localScale = restScale * current;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (selectable != null && !selectable.IsInteractable())
        {
            return;
        }

        target = pressedScale;
        if (tapSound)
        {
            AudioManager.Play(Sfx.Tap);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        target = 1f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        target = 1f;
    }
}
