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


    public void ClearUI()
    {
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }
    }
}