using System.Collections;
using UnityEngine;

public class GoalZone : MonoBehaviour
{
    private CommandManager commandManager;

    private bool goalTriggered = false;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        commandManager =
            FindObjectOfType<CommandManager>();

        if (commandManager == null)
        {
            Debug.LogError(
                "GoalZone: CommandManager not found!"
            );
        }
    }


    // =========================================================
    // GOAL TRIGGER
    // =========================================================

    private void OnTriggerEnter2D(
        Collider2D other)
    {
        Debug.Log(
            $"Goal Triggered By: {other.name} | " +
            $"Tag: {other.tag}"
        );

        // Make sure the goal can only be triggered once
        if (goalTriggered)
        {
            return;
        }

        // Check if the object is the Robot
        if (other.CompareTag("Robot"))
        {
            goalTriggered = true;

            Debug.Log(
                "YOU WIN!"
            );

            // -------------------------------------------------
            // MISSION MANAGER
            // -------------------------------------------------

            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.ReachFinish();
            }

            // -------------------------------------------------
            // COMMAND MANAGER
            // -------------------------------------------------

            if (commandManager != null)
            {
                // This will:
                // 1. Stop the timer
                // 2. Calculate the stars
                // 3. Generate star prefabs
                // 4. Display final time
                // 5. Show the finish panel

                commandManager.ShowFinishPanel();
            }
            else
            {
                Debug.LogError(
                    "GoalZone: CommandManager is null!"
                );
            }
        }
    }


    // =========================================================
    // OPTIONAL DELAYED FINISH PANEL
    // =========================================================

    public IEnumerator finishPanelShow()
    {
        yield return new WaitForSeconds(1f);

        if (commandManager != null)
        {
            commandManager.ShowFinishPanel();
        }
    }
}