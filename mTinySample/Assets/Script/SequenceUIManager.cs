using UnityEngine;
using UnityEngine.UI;

public class SequenceUIManager : MonoBehaviour
{
    public GameObject iconPrefab;
    public Transform content;

    public void AddVisualBlock(Sprite icon)
    {
        GameObject obj = Instantiate(iconPrefab, content);

        obj.GetComponent<Image>().sprite = icon;
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
    }


    public void ClearUI()
    {
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }
    }
}