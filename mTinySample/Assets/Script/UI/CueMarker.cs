using UnityEngine;

// The badge on a marked tile (Stages 3-5): a smiley for "show a feeling
// here" or a note for "play music here". It breathes gently until mTiny
// does the right thing on its tile, then pops into a green check.
public class CueMarker : MonoBehaviour
{
    public SpriteRenderer badge;
    public SpriteRenderer glow;
    public Sprite waitingSprite;
    public Sprite doneSprite;
    public Color glowColor = new Color(1f, 0.85f, 0.35f, 0.75f);
    public Color doneGlowColor = new Color(0.24f, 0.75f, 0.42f, 0.6f);

    public CueKind kind;
    public Vector2Int cell;

    public bool Done { get; private set; }

    private Vector3 baseScale;
    private float popTime = -1f;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    public void ResetCue()
    {
        Done = false;
        popTime = -1f;
        if (badge != null) badge.sprite = waitingSprite;
        if (glow != null) glow.color = glowColor;
    }

    public void Complete()
    {
        if (Done) return;
        Done = true;
        popTime = 0f;
        if (badge != null) badge.sprite = doneSprite;
        if (glow != null) glow.color = doneGlowColor;
    }

    private void Update()
    {
        if (baseScale == Vector3.zero) baseScale = transform.localScale;

        float scale;
        if (popTime >= 0f && popTime < 0.45f)
        {
            // Pop: grow, overshoot, settle
            popTime += Time.deltaTime;
            float u = Mathf.Clamp01(popTime / 0.45f);
            scale = 1f + 0.45f * Mathf.Sin(u * Mathf.PI) * (1f - u * 0.5f);
        }
        else
        {
            // Gentle breathing so young players notice the tile
            scale = Done ? 1f : 1f + 0.08f * Mathf.Sin(Time.time * 4f);
        }

        transform.localScale = baseScale * scale;

        if (glow != null && !Done)
        {
            Color c = glowColor;
            c.a = glowColor.a * (0.65f + 0.35f * Mathf.Sin(Time.time * 4f));
            glow.color = c;
        }
    }
}
