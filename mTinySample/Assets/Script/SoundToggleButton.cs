using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A Settings switch for music or sound effects: green with "ON" when
// playing, grey with "OFF" when muted.
[RequireComponent(typeof(Button))]
public class SoundToggleButton : MonoBehaviour
{
    public SoundChannel channel;
    public string title = "Music";

    public Image face;
    public TMP_Text label;
    public Image offSlash;

    public Color onColor = new Color(0.36f, 0.69f, 0.25f);
    public Color offColor = new Color(0.62f, 0.62f, 0.62f);


    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnPressed);
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void OnPressed()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.Toggle(channel);
        }

        Refresh();

        // Let the player hear that sounds are back on
        if (channel == SoundChannel.Effects)
        {
            AudioManager.Play(Sfx.Tap);
        }
    }

    public void Refresh()
    {
        bool on = AudioManager.Instance == null || AudioManager.Instance.IsOn(channel);

        if (face != null) face.color = on ? onColor : offColor;
        if (label != null) label.text = title + ": " + (on ? "ON" : "OFF");
        if (offSlash != null) offSlash.gameObject.SetActive(!on);
    }
}
