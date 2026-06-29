using UnityEngine;

public class CommandButton : MonoBehaviour
{
    public CommandType commandType;

    private RobotController robot;

    public Sprite icon;

    public SequenceUIManager uiManager;

    public void Start()
    {
        robot = FindObjectOfType<RobotController>();
    }

    public void AddCommand()
    {
        CommandManager.Instance.AddCommand(commandType);
        //uiManager.AddVisualBlock(icon);
    }

    public void RunProgram()
    {
        robot.RunProgram();
        uiManager.AddVisualBlock(icon);
    }
}