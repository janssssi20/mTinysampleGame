using UnityEngine;

public class CommandButton : MonoBehaviour
{
    [Header("Command")]
    public CommandType commandType;

    [Header("Emotion")]
    public EmotionType emotionType;

    [Header("Note")]
    public NoteType noteType;

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
    // ADD NOTE
    // =========================================================

    public void AddNote()
    {
        if (CommandManager.Instance == null)
        {
            Debug.LogError(
                "CommandButton: CommandManager.Instance is null!"
            );

            return;
        }

        CommandManager.Instance.AddNote(noteType);
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

        CommandManager commandManager = CommandManager.Instance;

        // Ignore extra presses: no Run icon, no second run
        if (robot.IsExecuting() ||
            (MissionManager.Instance != null && MissionManager.Instance.IsShowingFeedback) ||
            commandManager == null ||
            !commandManager.IsGameStarted() ||
            commandManager.IsGameFinished() ||
            !commandManager.HasRunnableCommands())
        {
            Debug.LogWarning(
                "CommandButton: Nothing to run right now."
            );

            return;
        }

        // Every run starts from the start tile, so "fix one block and
        // run again" replays the program from where it should begin.
        // (The Run button no longer adds an icon to the sequence bar:
        // it isn't a block, and Undo must only ever remove blocks.)
        CommandButtonManager buttons = FindFirstObjectByType<CommandButtonManager>();

        if (buttons != null)
        {
            buttons.ReturnRobotToStart();
        }

        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.CheckProgram();
        }

        robot.RunProgram();
    }
}