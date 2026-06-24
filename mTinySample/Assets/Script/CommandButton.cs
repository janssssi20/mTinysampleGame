using UnityEngine;

public class CommandButton : MonoBehaviour
{
    public CommandType commandType;

    public RobotController robot;

    public Sprite icon;

    public SequenceUIManager uiManager;

    public void AddCommand()
    {
        CommandManager.Instance.AddCommand(commandType);
        //uiManager.AddVisualBlock(icon);
    }

    public void RunProgram()
    {
        robot.RunProgram();
    }
}