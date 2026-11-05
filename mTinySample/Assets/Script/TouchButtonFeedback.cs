using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Touch feedback for buttons: touchscreens have no hover state, so a
// button visibly "squishes" while a finger is on it.
public class TouchButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Tooltip("Scale while pressed, relative to the resting scale.")]
    public float pressedScale = 0.9f;

    [Tooltip("How fast the button eases toward its target scale.")]
    public float speed = 18f;

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
        transform.localScale = restScale;
    }

    private void Update()
    {
        if (Mathf.Approximately(current, target))
        {
            return;
        }

        current = Mathf.MoveTowards(current, target, speed * Time.unscaledDeltaTime);
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
