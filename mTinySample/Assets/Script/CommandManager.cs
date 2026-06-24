using System.Collections.Generic;
using UnityEngine;

public enum CommandType
{
    Forward,
    TurnLeft,
    TurnRight
}

[System.Serializable]
public class Command
{
    public CommandType commandType;

    public Command(CommandType type)
    {
        commandType = type;
    }
}

public class CommandManager : MonoBehaviour
{
    public static CommandManager Instance;

    public List<Command> commandList = new List<Command>();

    private SequenceUIManager uiManager;

    [Header("Icons")]
    public Sprite forwardIcon;
    public Sprite leftIcon;
    public Sprite rightIcon;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        uiManager = FindFirstObjectByType<SequenceUIManager>();

        if (uiManager == null)
        {
            Debug.LogError("SequenceUIManager not found in scene!");
        }
    }

    public void AddCommand(CommandType type)
    {
        // 1. Add to logic list
        commandList.Add(new Command(type));

        // 2. Add UI icon
        if (uiManager != null)
        {
            uiManager.AddVisualBlock(GetIcon(type));
        }

        Debug.Log("Added: " + type);
    }

    public void ClearCommands()
    {
        commandList.Clear();
    }

    private Sprite GetIcon(CommandType type)
    {
        switch (type)
        {
            case CommandType.Forward:
                return forwardIcon;

            case CommandType.TurnLeft:
                return leftIcon;

            case CommandType.TurnRight:
                return rightIcon;

            default:
                return null;
        }
    }
}