using UnityEngine;
using UnityEngine.UI;

// Put this on the "Unlock all Stages" Toggle in the Settings panel.
//
// Every time the Settings panel opens, the Toggle shows the saved setting, and
// any change is saved straight away. The mission map reads the same setting
// whenever it opens, so nothing needs wiring in the Inspector.
[RequireComponent(typeof(Toggle))]
public class UnlockAllToggle : MonoBehaviour
{
    private Toggle toggle;

    private void Awake()
    {
        toggle = GetComponent<Toggle>();
    }

    private void OnEnable()
    {
        toggle.SetIsOnWithoutNotify(MissionMapUI.UnlockAll);
        toggle.onValueChanged.AddListener(OnToggled);
    }

    private void OnDisable()
    {
        toggle.onValueChanged.RemoveListener(OnToggled);
    }

    private void OnToggled(bool isOn)
    {
        MissionMapUI.UnlockAll = isOn;
    }
}
