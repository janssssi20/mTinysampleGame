using UnityEngine;

// A coloured heading ("Move", "Repeat"...) over a row of blocks. Missions
// hide blocks they don't use; when a whole row is hidden its heading
// hides too, so no empty groups are left on screen.
public class PaletteGroup : MonoBehaviour
{
    public GameObject heading;
    public Transform row;

    private void LateUpdate()
    {
        if (heading == null || row == null) return;

        bool any = false;
        foreach (Transform block in row)
        {
            if (block.gameObject.activeSelf) { any = true; break; }
        }

        if (heading.activeSelf != any) heading.SetActive(any);
        if (row.gameObject.activeSelf != any) row.gameObject.SetActive(any);
    }
}
