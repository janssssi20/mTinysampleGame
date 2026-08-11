using System.Collections.Generic;
using UnityEngine;

public enum CommandType
{
    Forward,
    TurnLeft,
    TurnRight,
    StartFlag,
    RunFlag,
    Emotion
}

public enum EmotionType
{
    Happy,
    Sad,
    Angry,
    Surprise,
    Dizzy,
    Sleepy,
    Playful
}

[System.Serializable]
public class Command
{
    public CommandType commandType;
    public EmotionType emotion;
    public int repeatCount;

    // Constructor for normal commands
    public Command(CommandType type, int repeat = 1)
    {
        commandType = type;
        emotion = EmotionType.Happy;
        repeatCount = Mathf.Max(1, repeat);
    }

    // Constructor for emotion commands
    public Command(
        CommandType type,
        EmotionType emotionType,
        int repeat = 1)
    {
        commandType = type;
        emotion = emotionType;
        repeatCount = Mathf.Max(1, repeat);
    }
}

public class CommandManager : MonoBehaviour
{
    public static CommandManager Instance;

    [Header("Command List")]
    public List<Command> commandList = new List<Command>();

    private SequenceUIManager uiManager;

    // =========================================================
    // COMMAND ICONS
    // =========================================================

    [Header("Command Icons")]
    public Sprite forwardIcon;
    public Sprite leftIcon;
    public Sprite rightIcon;
    public Sprite StartFlagIcon;
    public Sprite RunFlagIcon;

    // =========================================================
    // MULTIPLIER ICONS
    // =========================================================

    [Header("Multiplier Icons")]
    public Sprite multiplier2xIcon;
    public Sprite multiplier3xIcon;
    public Sprite multiplier4xIcon;
    public Sprite multiplier5xIcon;

    // =========================================================
    // EMOTION ICONS
    // =========================================================

    [Header("Emotion Icons")]
    public Sprite happyIcon;
    public Sprite sadIcon;
    public Sprite angryIcon;
    public Sprite surpriseIcon;
    public Sprite dizzyIcon;
    public Sprite sleepyIcon;
    public Sprite playfulIcon;

    // =========================================================
    // EMOTION PANEL
    // =========================================================

    [Header("Emotion Panel")]
    public GameObject emotionPanel;
    public Animator emotionAnimator;

    // =========================================================
    // PROGRAM UI
    // =========================================================

    [Header("Program UI")]
    public GameObject finishPanel;

    // =========================================================
    // CURRENT MULTIPLIER
    // =========================================================

    [Header("Current Multiplier")]
    [Range(1, 5)]
    public int currentMultiplier = 1;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        // Singleton protection
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        uiManager = FindFirstObjectByType<SequenceUIManager>();

        if (uiManager == null)
        {
            Debug.LogError(
                "CommandManager: SequenceUIManager not found in scene!"
            );
        }

        if (finishPanel != null)
        {
            finishPanel.SetActive(false);
        }

        if (emotionPanel != null)
        {
            emotionPanel.SetActive(false);
        }
    }


    // =========================================================
    // ADD NORMAL COMMAND
    // =========================================================

    public void AddCommand(CommandType type)
    {
        // Emotion commands must use AddEmotion()
        if (type == CommandType.Emotion)
        {
            Debug.LogWarning(
                "AddCommand(CommandType.Emotion) was called. " +
                "Use AddEmotion(EmotionType) instead."
            );

            return;
        }

        int multiplier = currentMultiplier;

        // Start and Run flags should not be repeated.
        if (type == CommandType.StartFlag ||
            type == CommandType.RunFlag)
        {
            multiplier = 1;
        }

        // Store the command
        Command newCommand = new Command(
            type,
            multiplier
        );

        commandList.Add(newCommand);

        // IMPORTANT:
        // The multiplier icon is NOT added here.
        //
        // SetMultiplier() already added the multiplier icon
        // when the multiplier button was pressed.

        // Add command icon
        AddCommandVisual(type);

        Debug.Log(
            $"Added command: {type} x{multiplier}"
        );

        // Reset multiplier after command is used
        currentMultiplier = 1;
    }


    // =========================================================
    // ADD EMOTION COMMAND
    // =========================================================

    public void AddEmotion(EmotionType emotionType)
    {
        int multiplier = currentMultiplier;

        // Store emotion command
        Command newCommand = new Command(
            CommandType.Emotion,
            emotionType,
            multiplier
        );

        commandList.Add(newCommand);

        // IMPORTANT:
        // Multiplier icon was already added by SetMultiplier().

        // Add emotion icon
        AddEmotionVisual(emotionType);

        Debug.Log(
            $"Added emotion: {emotionType} x{multiplier}"
        );

        // Reset multiplier
        currentMultiplier = 1;
    }


    // =========================================================
    // ORIGINAL MULTIPLIER FUNCTION
    // =========================================================

    public void SetMultiplier(int multiplier)
    {
        // Clamp multiplier between 1x and 5x
        currentMultiplier = Mathf.Clamp(
            multiplier,
            1,
            5
        );

        // ORIGINAL BEHAVIOR:
        // Immediately show the multiplier icon
        // when the multiplier button is pressed.

        if (uiManager != null)
        {
            Sprite icon = GetMultiplierIcon(
                currentMultiplier
            );

            if (icon != null)
            {
                uiManager.AddVisualBlock(icon);
            }
        }

        Debug.Log(
            $"Multiplier set to {currentMultiplier}x"
        );
    }


    // =========================================================
    // UI - COMMAND
    // =========================================================

    private void AddCommandVisual(CommandType type)
    {
        if (uiManager == null)
        {
            return;
        }

        Sprite icon = GetCommandIcon(type);

        if (icon != null)
        {
            uiManager.AddVisualBlock(icon);
        }
        else
        {
            Debug.LogWarning(
                $"No icon assigned for command type: {type}"
            );
        }
    }


    // =========================================================
    // UI - EMOTION
    // =========================================================

    private void AddEmotionVisual(EmotionType emotionType)
    {
        if (uiManager == null)
        {
            return;
        }

        Sprite icon = GetEmotionIcon(
            emotionType
        );

        if (icon != null)
        {
            uiManager.AddVisualBlock(icon);
        }
        else
        {
            Debug.LogWarning(
                $"No icon assigned for emotion: {emotionType}"
            );
        }
    }


    // =========================================================
    // GET COMMAND ICON
    // =========================================================

    private Sprite GetCommandIcon(CommandType type)
    {
        switch (type)
        {
            case CommandType.StartFlag:
                return StartFlagIcon;

            case CommandType.Forward:
                return forwardIcon;

            case CommandType.TurnLeft:
                return leftIcon;

            case CommandType.TurnRight:
                return rightIcon;

            case CommandType.RunFlag:
                return RunFlagIcon;

            default:
                return null;
        }
    }


    // =========================================================
    // GET MULTIPLIER ICON
    // =========================================================

    private Sprite GetMultiplierIcon(int multiplier)
    {
        switch (multiplier)
        {
            case 2:
                return multiplier2xIcon;

            case 3:
                return multiplier3xIcon;

            case 4:
                return multiplier4xIcon;

            case 5:
                return multiplier5xIcon;

            default:
                // 1x has no icon
                return null;
        }
    }


    // =========================================================
    // GET EMOTION ICON
    // =========================================================

    private Sprite GetEmotionIcon(
        EmotionType emotionType)
    {
        switch (emotionType)
        {
            case EmotionType.Happy:
                return happyIcon;

            case EmotionType.Sad:
                return sadIcon;

            case EmotionType.Angry:
                return angryIcon;

            case EmotionType.Surprise:
                return surpriseIcon;

            case EmotionType.Dizzy:
                return dizzyIcon;

            case EmotionType.Sleepy:
                return sleepyIcon;

            case EmotionType.Playful:
                return playfulIcon;

            default:
                return null;
        }
    }


    // =========================================================
    // CLEAR COMMANDS
    // =========================================================

    public void ClearCommands()
    {
        // Clear stored commands
        commandList.Clear();

        // Reset multiplier
        currentMultiplier = 1;

        // Clear visual UI
        if (uiManager != null)
        {
            uiManager.ClearUI();
        }

        Debug.Log("All commands cleared.");
    }


    // =========================================================
    // GET COMMANDS
    // =========================================================

    public List<Command> GetCommands()
    {
        return commandList;
    }


    // =========================================================
    // GET COMMAND COUNT
    // =========================================================

    public int GetCommandCount()
    {
        return commandList.Count;
    }


    // =========================================================
    // EMOTION PANEL
    // =========================================================

    public void OpenEmotionPanel()
    {
        if (emotionPanel != null)
        {
            emotionPanel.SetActive(true);
        }
    }


    public void CloseEmotionPanel()
    {
        if (emotionPanel != null)
        {
            emotionPanel.SetActive(false);
        }
    }


    // =========================================================
    // FINISH PANEL
    // =========================================================

    public void ShowFinishPanel()
    {
        if (finishPanel != null)
        {
            finishPanel.SetActive(true);
        }
    }


    public void HideFinishPanel()
    {
        if (finishPanel != null)
        {
            finishPanel.SetActive(false);
        }
    }
}