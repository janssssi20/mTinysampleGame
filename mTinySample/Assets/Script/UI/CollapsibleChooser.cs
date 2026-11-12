using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A small chip that shows the current choice (mode or camera view) and
// opens into the full row of choices when tapped. Picking one does its
// action and folds the row away again, keeping the screen clean.
public class CollapsibleChooser : MonoBehaviour
{
    [Header("Chip")]
    public Button toggle;
    public Image toggleIcon;
    public TMP_Text toggleLabel;
    public RectTransform chevron;

    [Header("Choices")]
    public GameObject optionsPanel;
    public Image[] optionFaces;
    public Sprite[] icons;
    public string[] labels;

    public Color selectedColor = new Color(0.29f, 0.62f, 1f);
    public Color idleColor = Color.white;
    public Color selectedInk = Color.white;
    public Color idleInk = new Color(0.18f, 0.16f, 0.31f);

    [Tooltip("Colour for drawn white icons on the chip; pictures keep their own colours.")]
    public Color drawnColor = new Color(0.18f, 0.16f, 0.31f);

    public int Current { get; private set; }

    private void Awake()
    {
        if (toggle != null) toggle.onClick.AddListener(ToggleOpen);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        Paint();
    }

    public bool IsOpen
    {
        get { return optionsPanel != null && optionsPanel.activeSelf; }
    }

    public void ToggleOpen()
    {
        if (optionsPanel == null) return;
        optionsPanel.SetActive(!optionsPanel.activeSelf);
        if (chevron != null) chevron.localEulerAngles = new Vector3(0f, 0f, IsOpen ? 180f : 0f);
    }

    public void Close()
    {
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (chevron != null) chevron.localEulerAngles = Vector3.zero;
    }

    // Wired after each choice's own action on its button
    public void Choose(int index)
    {
        SetCurrent(index);
        Close();
    }

    // Shows a choice made elsewhere (a saved view, Back to city...)
    public void SetCurrent(int index)
    {
        Current = index;
        Paint();
    }

    // The game's own drawn icons are plain white shapes made to be tinted
    public static bool IsDrawn(Sprite icon)
    {
        if (icon == null) return false;
        string n = icon.name;
        return n.StartsWith("view_") || n.StartsWith("icon_") || n.StartsWith("joy_");
    }

    private void Paint()
    {
        if (icons != null && Current < icons.Length && toggleIcon != null)
        {
            toggleIcon.sprite = icons[Current];
            toggleIcon.color = IsDrawn(icons[Current]) ? drawnColor : Color.white;
        }
        if (labels != null && Current < labels.Length && toggleLabel != null) toggleLabel.text = labels[Current];

        if (optionFaces == null) return;

        for (int i = 0; i < optionFaces.Length; i++)
        {
            if (optionFaces[i] == null) continue;

            bool on = i == Current;
            optionFaces[i].color = on ? selectedColor : idleColor;

            // Only the words change colour; icons keep theirs
            foreach (TMP_Text t in optionFaces[i].GetComponentsInChildren<TMP_Text>(true))
            {
                t.color = on ? selectedInk : idleInk;
            }
        }
    }
}
