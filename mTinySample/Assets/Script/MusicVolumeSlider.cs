using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Settings slider for the music volume. The music changes while the
// finger drags; the choice is saved when the finger lifts.
[RequireComponent(typeof(Slider))]
public class MusicVolumeSlider : MonoBehaviour, IPointerUpHandler
{
    private Slider slider;


    private void Awake()
    {
        slider = GetComponent<Slider>();
        slider.onValueChanged.AddListener(OnChanged);
    }

    private void OnEnable()
    {
        if (AudioManager.Instance != null)
        {
            slider.SetValueWithoutNotify(AudioManager.Instance.MusicVolume);
        }
    }

    private void OnChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(value);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SaveSettings();
        }
    }

    private void OnDisable()
    {
        // Covers keyboard or gamepad changes that never lift a finger
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SaveSettings();
        }
    }
}
