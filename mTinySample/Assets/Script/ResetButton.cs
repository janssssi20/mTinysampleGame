using System.Collections;
using System.Collections.Generic;
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
        robot = FindObjectOfType<RobotController>();
        
        startPos = robot.transform.position;
        startRot = robot.transform.rotation;

    }

    public void ResetGame()
    {
        robot.StopProgram();

        commandManager.ClearCommands();
        uiManager.ClearUI();

        robot.transform.position = startPos;
        robot.transform.rotation = startRot;
    }
}