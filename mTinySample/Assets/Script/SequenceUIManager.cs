using UnityEngine;
using UnityEngine.UI;

public class SequenceUIManager : MonoBehaviour
{
    public GameObject iconPrefab;
    public Transform content;

    [Tooltip("Size of each block icon in the sequence bar.")]
    public Vector2 iconSize = new Vector2(150f, 150f);

    [Tooltip("Dashed empty slot kept after the last block, inviting the next one (optional).")]
    public RectTransform nextSlot;

    private ScrollRect scroll;

    // Block icons in the bar (the empty slot is not one)
    public int IconCount
    {
        get
        {
            if (content == null) return 0;
            bool hasSlot = nextSlot != null && nextSlot.parent == content;
            return content.childCount - (hasSlot ? 1 : 0);
        }
    }

    public Transform Icon(int index)
    {
        return content.GetChild(index);
    }

    public void AddVisualBlock(Sprite icon)
    {
        GameObject obj = Instantiate(iconPrefab, content);

        obj.GetComponent<Image>().sprite = icon;
        ((RectTransform)obj.transform).sizeDelta = iconSize;

        // New blocks go in front of the empty slot
        if (nextSlot != null && nextSlot.parent == content)
        {
            nextSlot.SetAsLastSibling();
        }

        ScrollToNewest();
    }


    public void RemoveLastBlock()
    {
        if (content == null || IconCount == 0)
        {
            return;
        }

        Transform last = content.GetChild(IconCount - 1);

        // Detach first so childCount is correct this frame
        last.SetParent(null);

        Destroy(last.gameObject);

        ScrollToNewest();
    }


    public void ClearUI()
    {
        foreach (Transform child in content)
        {
            if (child != nextSlot) Destroy(child.gameObject);
        }
    }


    // =========================================================
    // RUN HIGHLIGHT
    // =========================================================
    // While mTiny runs, the block it is doing now grows and the rest
    // dim, so children can follow along. After a bump the block that
    // caused it stays lit in red until they try again.
    // =========================================================

    [Header("Run Highlight")]
    public float highlightScale = 1.18f;
    [Range(0f, 1f)] public float dimAlpha = 0.45f;
    [Tooltip("Rounded ring drawn over the lit block.")]
    public Sprite ringSprite;
    public Color runningGlow = new Color(0.36f, 0.69f, 0.25f);
    public Color problemGlow = new Color(0.9f, 0.2f, 0.15f);

    private const string RingName = "highlightRing";

    // Lights `count` icons starting at `first` (a block and its multiplier):
    // bigger, with a green ring (red after a bump); the rest dim.
    public void Highlight(int first, int count, bool problem = false)
    {
        if (content == null)
        {
            return;
        }

        for (int i = 0; i < IconCount; i++)
        {
            bool lit = i >= first && i < first + count;
            Transform icon = content.GetChild(i);

            icon.localScale = Vector3.one * (lit ? highlightScale : 1f);

            Image image = icon.GetComponent<Image>();
            if (image != null) image.color = new Color(1f, 1f, 1f, lit ? 1f : dimAlpha);

            Image ring = Ring(icon, lit);
            if (ring != null)
            {
                ring.gameObject.SetActive(lit);
                ring.color = problem ? problemGlow : runningGlow;
            }
        }

        ScrollTo(first);
    }

    public void ClearHighlight()
    {
        if (content == null)
        {
            return;
        }

        for (int i = 0; i < IconCount; i++)
        {
            Transform icon = content.GetChild(i);
            icon.localScale = Vector3.one;

            Image image = icon.GetComponent<Image>();
            if (image != null) image.color = Color.white;

            Image ring = Ring(icon, false);
            if (ring != null) ring.gameObject.SetActive(false);
        }
    }

    // The ring is made the first time an icon is lit
    private Image Ring(Transform icon, bool create)
    {
        Transform existing = icon.Find(RingName);

        if (existing != null || !create || ringSprite == null)
        {
            return existing != null ? existing.GetComponent<Image>() : null;
        }

        GameObject ring = new GameObject(RingName, typeof(RectTransform), typeof(Image));
        ring.transform.SetParent(icon, false);

        RectTransform rect = (RectTransform)ring.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(-6f, -6f);
        rect.offsetMax = new Vector2(6f, 6f);

        Image image = ring.GetComponent<Image>();
        image.sprite = ringSprite;
        image.type = Image.Type.Sliced;
        image.raycastTarget = false;

        return image;
    }

    // Keeps the lit block in view on a long program
    private void ScrollTo(int index)
    {
        if (scroll == null && content != null)
        {
            scroll = content.GetComponentInParent<ScrollRect>();
        }

        if (scroll == null || IconCount <= 1)
        {
            return;
        }

        scroll.horizontalNormalizedPosition = Mathf.Clamp01((float)index / (IconCount - 1));
    }


    // Keeps the most recently added block in view once the sequence is
    // longer than the bar, so children always see what they just placed.
    private void ScrollToNewest()
    {
        if (scroll == null && content != null)
        {
            scroll = content.GetComponentInParent<ScrollRect>();
        }

        if (scroll == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        scroll.horizontalNormalizedPosition = 1f;
    }
}
