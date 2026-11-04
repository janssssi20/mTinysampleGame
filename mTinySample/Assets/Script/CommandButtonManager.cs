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
        robot = FindFirstObjectByType<RobotController>();

        if (commandManager == null)
        {
            commandManager = FindFirstObjectByType<CommandManager>();
        }

        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<SequenceUIManager>();
        }

        if (robot == null)
        {
            Debug.LogError("CommandButtonManager: RobotController not found!");
        }
        else
        {
            startPos = robot.transform.position;
            startRot = robot.transform.rotation;
        }

        ResetState();
    }

    public void OnStartPressed()
    {
        if (started)
            return;

        if (CommandManager.Instance == null)
        {
            Debug.LogError("CommandButtonManager: CommandManager.Instance is null!");
            return;
        }

        started = true;

        AudioManager.Play(Sfx.Start);

        // Make sure the timer is running before any block is added
        CommandManager.Instance.StartGame();

        CommandManager.Instance.AddCommand(CommandType.StartFlag);

        SetCommandButtonsInteractable(true);

        if (startButton != null)
            startButton.interactable = false;

        SetStartFlagPulse(false);
    }

    public void ResetState()
    {
        started = false;

        SetCommandButtonsInteractable(false);

        if (startButton != null)
            startButton.interactable = true;

        if (robot != null)
        {
            robot.StopProgram();
            robot.ResetPose(startPos, startRot);
        }

        if (commandManager != null)
        {
            if (commandManager.emotionPanel != null)
                commandManager.emotionPanel.SetActive(false);

            commandManager.ResetAfterFinish();
            commandManager.ShowSequenceUI();
            commandManager.ClearCommands();
        }

        if (uiManager != null)
            uiManager.ClearUI();

        if (MissionManager.Instance != null)
            MissionManager.Instance.StartMission();

        foreach (GoalZone goal in FindObjectsByType<GoalZone>(FindObjectsSortMode.None))
        {
            goal.ResetGoal();
        }

        SetStartFlagPulse(true);
    }

    // The start flag pulses while it is the block the player must press
    // first; it stops once the sequence has started.
    private void SetStartFlagPulse(bool pulsing)
    {
        if (startButton == null)
            return;

        PulseScale pulse = startButton.GetComponent<PulseScale>();

        if (pulse != null)
            pulse.enabled = pulsing;
    }

    // Puts the robot back on the start tile and re-arms the goal, but
    // keeps the program so the child can fix it and run again.
    public void ReturnRobotToStart()
    {
        if (robot != null)
        {
            robot.StopProgram();
            robot.ResetPose(startPos, startRot);
        }

        if (MissionManager.Instance != null)
            MissionManager.Instance.StartMission();

        foreach (GoalZone goal in FindObjectsByType<GoalZone>(FindObjectsSortMode.None))
        {
            goal.ResetGoal();
        }
    }

    private void SetCommandButtonsInteractable(bool interactable)
    {
        if (commandButtons == null)
            return;

        foreach (Button btn in commandButtons)
        {
            if (btn != null)
                btn.interactable = interactable;
        }
    }
}
