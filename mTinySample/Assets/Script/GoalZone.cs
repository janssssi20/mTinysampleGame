using UnityEngine;

public class GoalZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Triggered by: {other.name}, tag: {other.tag}");

        if (other.CompareTag("Robot"))
        {
            Debug.Log("YOU WIN!");
        }
    }
}