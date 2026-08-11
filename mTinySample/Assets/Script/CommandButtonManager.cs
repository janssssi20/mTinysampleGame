using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CommandButtonManager : MonoBehaviour
{
    public Button startButton;

    public List<Button> commandButtons;

    private RobotController robot;
    public CommandManager commandManager;
    public SequenceUIManager uiManager;

    public Vector3 startPos;
    public Quaternion startRot;

    private bool started;



    private void Start()
    {
        robot = FindObjectOfType<RobotController>();
        commandManager = FindObjectOfType<CommandManager>();

        startPos = robot.transform.position;
        startRot = robot.transform.rotation;
        ResetState();
    }

    public void OnStartPressed()
    {
        if (started)
            return;

        started = true;

        CommandManager.Instance.AddCommand(CommandType.StartFlag);

        foreach (Button btn in commandButtons)
        {
            btn.interactable = true;
        }

        startButton.interactable = false;
    }

    public void ResetState()
    {
        started = false;

        foreach (Button btn in commandButtons)
        {
            btn.interactable = false;
        }

        startButton.interactable = true;

        robot.StopProgram();

        commandManager.ClearCommands();
        uiManager.ClearUI();

        robot.transform.position = startPos;

        Vector3 p = robot.transform.position;
        p.z = 0;
        robot.transform.position = p;

        robot.transform.rotation = startRot;
    }
}