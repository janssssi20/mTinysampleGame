using UnityEngine;
using UnityEngine.UI;

public class SequenceUIManager : MonoBehaviour
{
    public GameObject iconPrefab;
    public Transform content;

    [Tooltip("Size of each block icon in the sequence bar.")]
    public Vector2 iconSize = new Vector2(150f, 150f);

    private ScrollRect scroll;

    public void AddVisualBlock(Sprite icon)
    {
        GameObject obj = Instantiate(iconPrefab, content);

        obj.GetComponent<Image>().sprite = icon;
        ((RectTransform)obj.transform).sizeDelta = iconSize;

        ScrollToNewest();
    }


    public void RemoveLastBlock()
    {
        if (content == null || content.childCount == 0)
        {
            return;
        }

        Transform last = content.GetChild(content.childCount - 1);

        // Detach first so childCount is correct this frame
        last.SetParent(null);

        Destroy(last.gameObject);

        ScrollToNewest();
    }


    public void ClearUI()
    {
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }
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
