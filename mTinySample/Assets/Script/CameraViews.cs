using UnityEngine;
using UnityEngine.UI;

public enum CameraView
{
    Top,
    Tilt,
    Follow
}

// Three ways to look at the mat, picked from the view buttons:
//  Top    - straight down, the whole map (best for planning blocks)
//  Tilt   - the mat at an angle, like a puzzle mat on a table
//  Follow - behind mTiny, turning with it as it drives
// Game rules don't change: only the camera moves. The choice is
// remembered per scene.
[RequireComponent(typeof(Camera))]
public class CameraViews : MonoBehaviour
{
    [Tooltip("Saved choice, e.g. mtiny.view.explore")]
    public string saveKey = "mtiny.view";

    [Header("Board")]
    [Tooltip("Parents of the tiles; the tilted view fits what's showing.")]
    public Transform[] boardRoots;

    [Tooltip("World panels (face, finish, instruction) kept on screen in every view.")]
    public Transform[] screenPanels;

    [Header("View Buttons")]
    public Image topButton;
    public Image tiltButton;
    public Image followButton;
    public Color activeColor = new Color(0.85f, 0.49f, 0.16f);
    public Color idleColor = new Color(0.98f, 0.82f, 0.48f);

    [Header("Tilt")]
    public float tiltFov = 38f;
    [Tooltip("Direction from the board to the camera.")]
    public Vector3 tiltDirection = new Vector3(0f, -12.5f, -17f);
    public float tiltMargin = 1.15f;

    [Header("Follow")]
    public float followFov = 58f;
    public float followBack = 5.2f;
    public float followHeight = 3.3f;
    public float followLookAhead = 3.4f;
    public float followSmoothing = 6f;

    [Tooltip("Fold-away chip that picks the view (redesign); optional.")]
    public CollapsibleChooser chooser;

    public CameraView View { get; private set; }

    private Camera cam;
    private Vector3 topPosition;
    private Quaternion topRotation;
    private float topSize;
    private float topFar;

    private RobotController robot;
    private Transform facingArrow;

    // Each panel's place in front of the top camera, so it can be kept in
    // the same spot on screen whatever the camera does
    private Vector3[] panelLocalPos;
    private Quaternion[] panelLocalRot;
    private Vector3[] panelWorldScale;

    // Screen UI drawn through this camera. In the angled views it moves
    // right up to the lens, so the mat and mTiny can never cover it.
    private Canvas[] canvases;
    private float[] canvasDistance;

    private const float PanelDepth = 1.5f;
    private const float CanvasDepth = 0.6f;


    private void Awake()
    {
        cam = GetComponent<Camera>();
        topPosition = transform.position;
        topRotation = transform.rotation;
        topSize = cam.orthographicSize;
        topFar = cam.farClipPlane;

        int count = screenPanels != null ? screenPanels.Length : 0;
        panelLocalPos = new Vector3[count];
        panelLocalRot = new Quaternion[count];
        panelWorldScale = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            if (screenPanels[i] == null) continue;
            panelLocalPos[i] = transform.InverseTransformPoint(screenPanels[i].position);
            panelLocalRot[i] = Quaternion.Inverse(topRotation) * screenPanels[i].rotation;
            panelWorldScale[i] = screenPanels[i].lossyScale;
        }

        canvases = System.Array.FindAll(FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None),
            c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceCamera);
        canvasDistance = System.Array.ConvertAll(canvases, c => c.planeDistance);
    }

    private void Start()
    {
        SetView((CameraView)Mathf.Clamp(PlayerPrefs.GetInt(saveKey, 0), 0, 2), snap: true);
    }

    // Editor previews: lay out a view without entering play mode
    public void Preview(CameraView view)
    {
        if (cam == null) Awake();
        SetView(view, true);
        if (FindRobot() && facingArrow != null) facingArrow.gameObject.SetActive(view == CameraView.Top);
        KeepPanelsOnScreen();
    }

    public void ShowTop() { SetView(CameraView.Top, true); }
    public void ShowTilt() { SetView(CameraView.Tilt, true); }
    public void ShowFollow() { SetView(CameraView.Follow, true); }

    public void SetView(CameraView view, bool snap)
    {
        View = view;
        PlayerPrefs.SetInt(saveKey, (int)view);

        cam.orthographic = view == CameraView.Top;

        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] != null)
            {
                canvases[i].planeDistance = view == CameraView.Top ? canvasDistance[i] : CanvasDepth;
            }
        }

        if (view == CameraView.Top)
        {
            transform.SetPositionAndRotation(topPosition, topRotation);
            cam.orthographicSize = topSize;
            cam.farClipPlane = topFar;
        }
        else
        {
            cam.fieldOfView = view == CameraView.Tilt ? tiltFov : followFov;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 500f;

            if (snap) PlaceCamera(1f);
        }

        Paint(topButton, view == CameraView.Top);
        Paint(tiltButton, view == CameraView.Tilt);
        Paint(followButton, view == CameraView.Follow);

        if (chooser != null) chooser.SetCurrent((int)view);
    }

    private void LateUpdate()
    {
        if (View != CameraView.Top)
        {
            PlaceCamera(1f - Mathf.Exp(-followSmoothing * Time.deltaTime));
        }

        // From straight above, mTiny's front is hard to see: show an arrow
        if (FindRobot() && facingArrow != null)
        {
            facingArrow.gameObject.SetActive(View == CameraView.Top);
        }

        KeepPanelsOnScreen();
    }

    // Moves the camera toward where this view wants it (t = 1 jumps there)
    private void PlaceCamera(float t)
    {
        Vector3 position;
        Quaternion rotation;

        if (View == CameraView.Tilt)
        {
            Bounds board;
            if (!BoardBounds(out board)) return;

            Vector3 direction = tiltDirection.normalized;
            float aspect = Mathf.Max(0.1f, cam.aspect);
            float halfHeight = Mathf.Max(board.extents.y, board.extents.x / aspect) * tiltMargin;
            float distance = halfHeight / Mathf.Tan(tiltFov * 0.5f * Mathf.Deg2Rad) + board.extents.y * 0.5f;

            position = board.center + direction * distance;
            rotation = Quaternion.LookRotation(board.center - position, Vector3.up);
        }
        else
        {
            if (!FindRobot()) return;

            Vector3 r = robot.transform.position;
            Vector3 forward = robot.transform.up;

            // Behind mTiny and above the mat (off the mat is -z)
            position = r - forward * followBack + Vector3.back * followHeight;
            rotation = Quaternion.LookRotation(r + forward * followLookAhead - position, Vector3.back);
        }

        transform.position = Vector3.Lerp(transform.position, position, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, t);
    }

    private readonly System.Collections.Generic.List<SpriteRenderer> tiles = new System.Collections.Generic.List<SpriteRenderer>();
    private readonly System.Collections.Generic.List<float> xs = new System.Collections.Generic.List<float>();
    private readonly System.Collections.Generic.List<float> ys = new System.Collections.Generic.List<float>();

    // Bounds of the tiles showing. Pieces parked far from the rest (an
    // unused goal tile off to the side) are left out, so they can't
    // shrink the board into a corner of the screen.
    public bool BoardBounds(out Bounds bounds)
    {
        bounds = new Bounds();
        tiles.Clear();
        xs.Clear();
        ys.Clear();

        if (boardRoots == null) return false;

        foreach (Transform root in boardRoots)
        {
            if (root == null || !root.gameObject.activeInHierarchy) continue;

            foreach (SpriteRenderer sprite in root.GetComponentsInChildren<SpriteRenderer>())
            {
                if (!sprite.enabled || sprite.bounds.size.x > 50f) continue;
                tiles.Add(sprite);
                xs.Add(sprite.bounds.center.x);
                ys.Add(sprite.bounds.center.y);
            }
        }

        if (tiles.Count == 0) return false;

        xs.Sort();
        ys.Sort();
        Vector2 middle = new Vector2(xs[xs.Count / 2], ys[ys.Count / 2]);
        bool found = false;

        foreach (SpriteRenderer sprite in tiles)
        {
            Vector2 offset = (Vector2)sprite.bounds.center - middle;
            if (Mathf.Abs(offset.x) > MaxBoardReach || Mathf.Abs(offset.y) > MaxBoardReach) continue;

            if (found) bounds.Encapsulate(sprite.bounds);
            else { bounds = sprite.bounds; found = true; }
        }

        return found;
    }

    // Farthest a tile can sit from the middle of the board (in tiles of 1.8)
    private const float MaxBoardReach = 1.8f * 6f;

    // Puts each world panel where it sat in front of the top camera, scaled
    // so it covers the same part of the screen in a perspective view
    private void KeepPanelsOnScreen()
    {
        for (int i = 0; i < panelLocalPos.Length; i++)
        {
            Transform panel = screenPanels[i];
            if (panel == null) continue;

            float scale = 1f;
            Vector3 local = panelLocalPos[i];

            if (!cam.orthographic)
            {
                // Pull it close to the lens (same spot on screen), sized so
                // it covers as much of the screen as in the top view
                local = new Vector3(local.x, local.y, 0f) * (PanelDepth * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) / topSize)
                        + Vector3.forward * PanelDepth;
                scale = PanelDepth * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) / topSize;
            }

            panel.SetPositionAndRotation(transform.TransformPoint(local), transform.rotation * panelLocalRot[i]);
            SetWorldScale(panel, panelWorldScale[i] * scale);
        }
    }

    private static void SetWorldScale(Transform t, Vector3 world)
    {
        Vector3 parent = t.parent != null ? t.parent.lossyScale : Vector3.one;
        t.localScale = new Vector3(world.x / parent.x, world.y / parent.y, world.z / parent.z);
    }

    private bool FindRobot()
    {
        if (robot == null)
        {
            robot = FindFirstObjectByType<RobotController>();
            if (robot != null) facingArrow = robot.transform.Find("facingArrow");
        }

        return robot != null;
    }

    private void Paint(Image button, bool active)
    {
        if (button != null) button.color = active ? activeColor : idleColor;
    }
}
