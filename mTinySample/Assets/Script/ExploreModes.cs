using UnityEngine;
using UnityEngine.UI;

public enum ExploreMode
{
    Code,
    Remote,
    Build
}

// Explore's three ways to play, picked from big buttons on the left:
// Code (blocks and Run), Remote (drive mTiny now) and Build (lay your
// own road map). Each mode shows only the controls it needs.
public class ExploreModes : MonoBehaviour
{
    [Header("Mode Buttons")]
    public Image codeButton;
    public Image remoteButton;
    public Image buildButton;
    public Color activeColor = new Color(0.85f, 0.49f, 0.16f);
    public Color idleColor = new Color(0.98f, 0.82f, 0.48f);

    [Header("Code Controls")]
    public GameObject palette;
    public GameObject sequenceBar;
    public GameObject runButton;
    public GameObject undoButton;

    [Header("Other Modes")]
    public GameObject remotePanel;
    public GameObject buildPanel;
    public MapBuilder mapBuilder;
    public CommandButtonManager buttonManager;

    [Tooltip("Fold-away chip that picks the mode (redesign); optional.")]
    public CollapsibleChooser chooser;

    public ExploreMode Mode { get; private set; }

    private bool started;


    private void Start()
    {
        SetMode(ExploreMode.Code);
        started = true;
    }

    public void ShowCode() { SetMode(ExploreMode.Code); }
    public void ShowRemote() { SetMode(ExploreMode.Remote); }
    public void ShowBuild() { SetMode(ExploreMode.Build); }

    public void SetMode(ExploreMode mode)
    {
        Mode = mode;

        bool code = mode == ExploreMode.Code;

        Set(palette, code);
        Set(sequenceBar, code);
        Set(runButton, code);
        Set(undoButton, code);
        Set(remotePanel, mode == ExploreMode.Remote);
        Set(buildPanel, mode == ExploreMode.Build);

        if (mapBuilder != null)
        {
            if (mode == ExploreMode.Build) mapBuilder.BeginEditing();
            else if (mapBuilder.IsEditing) mapBuilder.EndEditing();
        }

        // Every mode change starts mTiny fresh on the start tile
        // (not on the first frame: the start tile isn't known yet)
        if (started && buttonManager != null)
        {
            buttonManager.ReturnRobotToStart();
        }

        Paint(codeButton, code);
        Paint(remoteButton, mode == ExploreMode.Remote);
        Paint(buildButton, mode == ExploreMode.Build);

        if (chooser != null) chooser.SetCurrent((int)mode);
    }

    // Build panel: drive on the big city again
    public void BackToCity()
    {
        if (mapBuilder != null) mapBuilder.UseCity();
        SetMode(ExploreMode.Code);
    }

    public void ClearMyMap()
    {
        if (mapBuilder != null) mapBuilder.ClearMap();
    }

    private void Paint(Image button, bool active)
    {
        if (button != null) button.color = active ? activeColor : idleColor;
    }

    private static void Set(GameObject target, bool visible)
    {
        if (target != null) target.SetActive(visible);
    }
}
