using UnityEngine;

public class CommandButton : MonoBehaviour
{
    [Header("Command")]
    public CommandType commandType;

    [Header("Emotion")]
    public EmotionType emotionType;

    [Header("Button Icon")]
    public Sprite icon;

    private RobotController robot;
    private SequenceUIManager uiManager;

    private void Start()
    {
        robot = FindFirstObjectByType<RobotController>();
        uiManager = FindFirstObjectByType<SequenceUIManager>();

        if (robot == null)
        {
            Debug.LogWarning(
                $"CommandButton on {gameObject.name}: " +
                "RobotController not found."
            );
        }

        if (uiManager == null)
        {
            Debug.LogWarning(
                $"CommandButton on {gameObject.name}: " +
                "SequenceUIManager not found."
            );
        }
    }

    // =========================================================
    // ADD NORMAL COMMAND
    // =========================================================

    public void AddCommand()
    {
        if (CommandManager.Instance == null)
        {
            Debug.LogError(
                "CommandButton: CommandManager.Instance is null!"
            );

            return;
        }

        if (commandType == CommandType.Emotion)
        {
            Debug.LogWarning(
                "This button is configured as Emotion. " +
                "Use AddEmotion() instead."
            );

            return;
        }

        CommandManager.Instance.AddCommand(commandType);
    }

    // =========================================================
    // ADD EMOTION
    // =========================================================

    public void AddEmotion()
    {
        if (CommandManager.Instance == null)
        {
            Debug.LogError(
                "CommandButton: CommandManager.Instance is null!"
            );

            return;
        }

        CommandManager.Instance.AddEmotion(emotionType);
    }

    // =========================================================
    // RUN PROGRAM
    // =========================================================

    public void RunProgram()
    {
        if (robot == null)
        {
            robot = FindFirstObjectByType<RobotController>();
        }

        if (robot == null)
        {
            Debug.LogError(
                "CommandButton: RobotController not found!"
            );

            return;
        }

        if (uiManager != null && icon != null)
        {
            uiManager.AddVisualBlock(icon);
        }

        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.CheckProgram();
        }

        robot.RunProgram();
    }
}