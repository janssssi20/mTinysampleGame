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
            FindFirstObjectByType<CommandManager>();

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

        // Make sure the goal can only be triggered once per run
        if (goalTriggered)
        {
            return;
        }

        // Check if the object is the Robot
        if (!other.CompareTag("Robot"))
        {
            return;
        }

        goalTriggered = true;

        // -------------------------------------------------
        // EXPLORE (FREE PLAY)
        // -------------------------------------------------
        // Reaching the goal is just a happy moment: mTiny keeps
        // going, and nothing locks or shows a finish panel.

        if (CommandManager.Instance != null && CommandManager.Instance.freePlay)
        {
            AudioManager.Play(Sfx.Win);
            return;
        }

        // -------------------------------------------------
        // STOP THE ROBOT
        // -------------------------------------------------
        // Finish the current step, skip remaining commands.

        RobotController robot =
            other.GetComponentInParent<RobotController>();

        if (robot != null)
        {
            robot.StopAtGoal();
        }

        // -------------------------------------------------
        // MISSION MANAGER
        // -------------------------------------------------
        // A failed mission shows its own feedback and
        // must not award stars.

        if (MissionManager.Instance != null &&
            !MissionManager.Instance.ReachFinish())
        {
            AudioManager.Play(Sfx.Oops);
            return;
        }

        Debug.Log(
            "YOU WIN!"
        );

        AudioManager.Play(Sfx.Win);

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

            int stars = commandManager.ShowFinishPanel();

            // Saves progress, shows Next, or opens the celebration
            if (MissionRunner.Instance != null)
            {
                MissionRunner.Instance.OnMissionWon(stars);
            }
        }
        else
        {
            Debug.LogError(
                "GoalZone: CommandManager is null!"
            );
        }
    }


    // =========================================================
    // RESET GOAL
    // =========================================================
    // Called by Reset so the goal can trigger again.
    // =========================================================

    public void ResetGoal()
    {
        goalTriggered = false;
    }
}
