using UnityEngine;

public class ResetButton : MonoBehaviour
{
    public RobotController robot;
    public CommandManager commandManager;
    public SequenceUIManager uiManager;

    private Vector3 startPos;
    private Quaternion startRot;

    private void Start()
    {
        if (robot == null)
            robot = FindFirstObjectByType<RobotController>();

        if (commandManager == null)
            commandManager = FindFirstObjectByType<CommandManager>();

        if (uiManager == null)
            uiManager = FindFirstObjectByType<SequenceUIManager>();

        if (robot != null)
        {
            startPos = robot.transform.position;
            startRot = robot.transform.rotation;
        }
    }

    public void ResetGame()
    {
        // CommandButtonManager owns the full reset (buttons, mission, goal)
        CommandButtonManager buttonManager = FindFirstObjectByType<CommandButtonManager>();

        if (buttonManager != null)
        {
            buttonManager.ResetState();
            return;
        }

        if (robot != null)
        {
            robot.StopProgram();
            robot.ResetPose(startPos, startRot);
        }

        if (commandManager != null)
            commandManager.ClearCommands();

        if (uiManager != null)
            uiManager.ClearUI();
    }
}
