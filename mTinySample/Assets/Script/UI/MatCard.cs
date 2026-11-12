using UnityEngine;

// A soft cream "table" under the puzzle mat, sized to whatever board is
// showing (a mission, the city, or a map the child built). It lies on
// the board's plane, so it tilts with the mat in the angled views.
[RequireComponent(typeof(SpriteRenderer))]
public class MatCard : MonoBehaviour
{
    public CameraViews views;

    [Tooltip("Space around the tiles, in world units.")]
    public float margin = 0.7f;

    [Tooltip("How far below the tiles it sits.")]
    public float depth = 0.6f;

    public float refreshSeconds = 0.3f;

    private SpriteRenderer sprite;
    private float nextRefresh;

    private void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        if (Time.unscaledTime < nextRefresh || views == null) return;
        nextRefresh = Time.unscaledTime + refreshSeconds;
        Fit();
    }

    // Public so editor previews can place it without play mode
    public void Fit()
    {
        if (sprite == null) sprite = GetComponent<SpriteRenderer>();

        Bounds board;
        if (!views.BoardBounds(out board))
        {
            sprite.enabled = false;
            return;
        }

        sprite.enabled = true;
        transform.position = new Vector3(board.center.x, board.center.y, board.center.z + depth);
        sprite.size = new Vector2(board.size.x + margin * 2f, board.size.y + margin * 2f);
    }
}
