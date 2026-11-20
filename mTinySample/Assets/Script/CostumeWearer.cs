using UnityEngine;

// Dresses the 3D mTiny in its costume: the animal's colours on the body,
// ears and back (so it shows from above), and the face mask as a gently
// curved card over the face, with mTiny's own eyes looking through.
// mTiny's face points +Y (the way it drives); the top of its head -Z.
public class CostumeWearer : MonoBehaviour
{
    [Tooltip("Wear the saved costume when the scene starts.")]
    public bool wearSavedCostume = true;

    public Costume Current { get; private set; } = Costume.None;

    // Material slots on the mTiny model: body, ears and side pads, back
    private static readonly int[] TintSlots = { 0, 1, 3 };

    private MeshRenderer body;
    private Material[] plainMaterials;
    private GameObject mask;

    private void Start()
    {
        if (wearSavedCostume) Wear(CostumeRules.SelectedRaw(new PlayerPrefsProgressStore()));
    }

    public void Wear(Costume costume)
    {
        if (body == null)
        {
            body = GetComponentInChildren<MeshRenderer>(true);
            if (body == null) return;
            plainMaterials = body.sharedMaterials;
        }

        Current = costume;
        if (mask != null)
        {
            if (Application.isPlaying) Destroy(mask); else DestroyImmediate(mask);
            mask = null;
        }

        // Body colours
        Color[] tint = Tints(costume);
        var mats = (Material[])plainMaterials.Clone();
        if (tint != null)
        {
            for (int k = 0; k < TintSlots.Length && TintSlots[k] < mats.Length; k++)
            {
                mats[TintSlots[k]] = new Material(plainMaterials[TintSlots[k]]) { color = tint[k] };
            }
        }
        body.sharedMaterials = mats;

        // Face mask
        string maskName = MaskName(costume);
        if (maskName != null)
        {
            Material mat = Resources.Load<Material>("Costumes/mask_" + maskName);
            if (mat != null) mask = BuildMask(mat, maskName);
        }
    }

    public static Color[] Tints(Costume c)
    {
        switch (c)
        {
            case Costume.Chicken: return new[] { Hex("FFDB6E"), Hex("F2552C"), Hex("F1C24F") };
            case Costume.Dog: return new[] { Hex("EDC48E"), Hex("8E4E26"), Hex("D9AB72") };
            case Costume.Cat: return new[] { Hex("BFC1CB"), Hex("7E8090"), Hex("A9ABB7") };
            case Costume.Golden: return new[] { Hex("FFD447"), Hex("E0A21B"), Hex("F2BE2E") };
            default: return null;
        }
    }

    public static string MaskName(Costume c)
    {
        switch (c)
        {
            case Costume.Panda: return "panda";
            case Costume.Chicken: return "chicken";
            case Costume.Dog: return "dog";
            case Costume.Cat: return "cat";
            default: return null;
        }
    }

    private GameObject BuildMask(Material mat, string maskName)
    {
        // The model's size in this object's own space, so turning and
        // squashing mTiny moves the mask with it
        Bounds b = LocalBounds();
        // The card takes the shape of the flat mask picture (the 3D texture
        // is a square copy, so it compresses with mipmaps on every device)
        Texture tex = mat.mainTexture;
        Sprite flat = Resources.Load<Sprite>("Costumes/mask_" + maskName);
        float w = b.size.x * 0.94f;
        float h = flat != null ? w * flat.rect.height / flat.rect.width
                : tex != null ? w * tex.height / tex.width : w * 0.65f;
        float depth = b.size.z;

        const int seg = 24;
        var verts = new Vector3[(seg + 1) * 2];
        var uv = new Vector2[verts.Length];
        var tris = new int[seg * 6];
        for (int i = 0; i <= seg; i++)
        {
            float u = (float)i / seg, x = (u - 0.5f) * w;
            float z = -0.1f * depth * (1f - Mathf.Pow(2f * u - 1f, 2f));
            verts[i * 2] = new Vector3(x, -h / 2f, z);
            verts[i * 2 + 1] = new Vector3(x, h / 2f, z);
            uv[i * 2] = new Vector2(u, 0f);
            uv[i * 2 + 1] = new Vector2(u, 1f);
            if (i < seg)
            {
                int t = i * 6, v = i * 2;
                tris[t] = v; tris[t + 1] = v + 1; tris[t + 2] = v + 2;
                tris[t + 3] = v + 2; tris[t + 4] = v + 1; tris[t + 5] = v + 3;
            }
        }
        var mesh = new Mesh { name = "costume mask", vertices = verts, uv = uv, triangles = tris };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var go = new GameObject("costumeMask");
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;

        // The eye holes (just over half way down the mask) sit on
        // mTiny's eyes, a little in front of the face
        float eyeZ = b.min.z + 0.37f * depth;
        float centreZ = eyeZ - 0.05f * h;
        go.transform.localPosition = new Vector3(b.center.x, b.max.y + 0.016f * b.size.y, centreZ);
        go.transform.localRotation = Quaternion.LookRotation(Vector3.down, Vector3.back);
        return go;
    }

    private Bounds LocalBounds()
    {
        MeshFilter mf = body.GetComponent<MeshFilter>();
        Bounds mb = mf.sharedMesh.bounds;
        bool first = true;
        Bounds result = new Bounds();
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) != 0 ? 1 : -1, (i & 2) != 0 ? 1 : -1, (i & 4) != 0 ? 1 : -1));
            Vector3 p = transform.InverseTransformPoint(mf.transform.TransformPoint(corner));
            if (first) { result = new Bounds(p, Vector3.zero); first = false; }
            else result.Encapsulate(p);
        }
        return result;
    }

    private static Color Hex(string hex)
    {
        Color c;
        ColorUtility.TryParseHtmlString("#" + hex, out c);
        return c;
    }
}
