using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// A short burst of coloured paper pieces, drawn as UI so it works on any
// canvas. Pieces fly up, tumble, fall and fade in about a second.
public class ConfettiBurst : MonoBehaviour
{
    [Header("Pieces")]
    public Sprite pieceSprite;
    public int pieceCount = 36;
    public Vector2 pieceSize = new Vector2(22f, 14f);

    [Header("Motion (canvas units per second)")]
    public float lifetime = 1.3f;
    public float gravity = 1800f;
    public Vector2 horizontalSpeed = new Vector2(-520f, 520f);
    public Vector2 upwardSpeed = new Vector2(500f, 1050f);

    public Color[] colors =
    {
        new Color(0.89f, 0.29f, 0.29f),
        new Color(0.22f, 0.54f, 0.87f),
        new Color(0.39f, 0.60f, 0.13f),
        new Color(0.94f, 0.62f, 0.15f),
        new Color(0.83f, 0.33f, 0.49f),
        new Color(0.50f, 0.47f, 0.87f)
    };

    private class Piece
    {
        public RectTransform rect;
        public Image image;
        public Vector2 velocity;
        public float spin;
        public float age;
    }

    private readonly List<Piece> pieces = new List<Piece>();


    // Bursts from a point given in world space (e.g. a trophy's position)
    public void Play(Vector3 worldPosition)
    {
        RectTransform area = (RectTransform)transform;
        Vector2 origin = area.InverseTransformPoint(worldPosition);

        AudioManager.Play(Sfx.Cheer);

        for (int i = 0; i < pieceCount; i++)
        {
            Piece piece = i < pieces.Count ? pieces[i] : CreatePiece();

            piece.rect.anchoredPosition = origin;
            piece.rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            piece.velocity = new Vector2(
                Random.Range(horizontalSpeed.x, horizontalSpeed.y),
                Random.Range(upwardSpeed.x, upwardSpeed.y));
            piece.spin = Random.Range(-540f, 540f);
            piece.age = 0f;
            piece.image.color = colors[Random.Range(0, colors.Length)];
            piece.rect.gameObject.SetActive(true);
        }
    }

    private Piece CreatePiece()
    {
        GameObject go = new GameObject("confetti", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);

        Piece piece = new Piece
        {
            rect = (RectTransform)go.transform,
            image = go.GetComponent<Image>()
        };

        piece.rect.sizeDelta = pieceSize;
        piece.image.sprite = pieceSprite;
        piece.image.raycastTarget = false;

        pieces.Add(piece);

        return piece;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;

        foreach (Piece piece in pieces)
        {
            if (!piece.rect.gameObject.activeSelf)
            {
                continue;
            }

            piece.age += dt;

            if (piece.age >= lifetime)
            {
                piece.rect.gameObject.SetActive(false);
                continue;
            }

            piece.velocity.y -= gravity * dt;
            piece.rect.anchoredPosition += piece.velocity * dt;
            piece.rect.Rotate(0f, 0f, piece.spin * dt);

            // Fade out over the last 40% of the flight
            float fade = Mathf.Clamp01((lifetime - piece.age) / (lifetime * 0.4f));
            Color c = piece.image.color;
            c.a = fade;
            piece.image.color = c;
        }
    }
}
