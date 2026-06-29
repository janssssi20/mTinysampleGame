using UnityEngine;

public class GoalZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log(
            $"Goal Triggered By: {other.name} | Tag: {other.tag}"
        );

        if (other.CompareTag("Robot"))
        {
            Debug.Log("YOU WIN!");
        }
    }
}