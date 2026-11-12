using UnityEngine;
using UnityEngine.EventSystems;

// The remote's joystick: drag the knob and it points Up, Left, Right or
// Down; let go and it springs back to the middle.
public class RemoteJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public RectTransform knob;

    [Tooltip("How far the knob can travel, in UI units.")]
    public float radius = 90f;

    [Tooltip("Share of the radius the knob must travel before it counts.")]
    [Range(0.1f, 0.9f)] public float deadZone = 0.45f;

    public float springBack = 12f;

    public StickDirection Direction { get; private set; }

    private bool held;


    public void OnPointerDown(PointerEventData eventData)
    {
        held = true;
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransform area = (RectTransform)transform;
        Vector2 local;

        // The camera that draws this screen UI: from the touch when it
        // carries one, else the canvas's own camera
        Camera eventCamera = eventData.pressEventCamera;
        if (eventCamera == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>().rootCanvas;
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) eventCamera = canvas.worldCamera;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, eventData.position, eventCamera, out local))
        {
            return;
        }

        Vector2 offset = Vector2.ClampMagnitude(local, radius);
        knob.anchoredPosition = offset;
        Direction = StickMath.DirectionOf(offset / radius, deadZone);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        held = false;
        Direction = StickDirection.None;
    }

    private void OnDisable()
    {
        held = false;
        Direction = StickDirection.None;

        if (knob != null)
        {
            knob.anchoredPosition = Vector2.zero;
        }
    }

    private void Update()
    {
        if (!held && knob != null)
        {
            knob.anchoredPosition = Vector2.Lerp(knob.anchoredPosition, Vector2.zero, springBack * Time.unscaledDeltaTime);
        }
    }
}
