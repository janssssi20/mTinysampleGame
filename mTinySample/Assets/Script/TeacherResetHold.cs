using UnityEngine;
using UnityEngine.EventSystems;

// Hidden teacher control: hold the mTiny title for a few seconds to be
// asked whether to reset all mission progress.
public class TeacherResetHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Hold")]
    public float holdSeconds = 3f;

    [Header("References")]
    public GameObject confirmPanel;
    public MissionCatalog catalog;
    public MissionMapUI stageMap;

    private HoldTimer timer;


    private void Awake()
    {
        timer = new HoldTimer(holdSeconds);

        if (confirmPanel != null)
        {
            confirmPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (timer.Tick(Time.unscaledDeltaTime) && confirmPanel != null)
        {
            confirmPanel.SetActive(true);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        timer.Begin();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        timer.Cancel();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        timer.Cancel();
    }


    // =========================================================
    // CONFIRM PANEL BUTTONS
    // =========================================================

    public void ConfirmYes()
    {
        new MissionProgress(catalog, new PlayerPrefsProgressStore()).ResetAll();

        if (stageMap != null && stageMap.isActiveAndEnabled)
        {
            stageMap.Refresh();
        }

        Debug.Log("All mission progress reset.");

        confirmPanel.SetActive(false);
    }

    public void ConfirmNo()
    {
        confirmPanel.SetActive(false);
    }
}
