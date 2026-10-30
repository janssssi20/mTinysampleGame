using UnityEngine;
using UnityEngine.SceneManagement;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance;

    [Header("Scene Settings")]
    [Tooltip("Name of the home or main menu scene.")]
    public string homeSceneName = "HomeScene";

    [Header("Mission Requirements")]
    public bool requireMovement = true;
    public bool requireMultiplier = true;
    public bool requireEmotion = true;
    public bool requireFinish = true;

    [Header("Mission UI")]
    public GameObject missionCompletePanel;
    public GameObject missionFailedPanel;

    private bool movementUsed;
    private bool multiplierUsed;
    private bool emotionUsed;
    private bool finishReached;

    private bool missionCompleted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        StartMission();
    }

    /// <summary>
    /// Resets mission progress. Called on scene start and by Reset.
    /// </summary>
    public void StartMission()
    {
        movementUsed = false;
        multiplierUsed = false;
        emotionUsed = false;
        finishReached = false;
        missionCompleted = false;

        if (missionCompletePanel != null)
        {
            missionCompletePanel.SetActive(false);
        }

        if (missionFailedPanel != null)
        {
            missionFailedPanel.SetActive(false);
        }
    }

    public void CheckProgram()
    {
        if (CommandManager.Instance == null)
        {
            return;
        }

        movementUsed = false;
        multiplierUsed = false;
        emotionUsed = false;

        foreach (Command command in CommandManager.Instance.commandList)
        {
            if (command == null)
            {
                continue;
            }

            // Movement requirement
            if (command.commandType == CommandType.Forward ||
                command.commandType == CommandType.TurnLeft ||
                command.commandType == CommandType.TurnRight)
            {
                movementUsed = true;
            }

            // Multiplier requirement
            if (command.repeatCount > 1)
            {
                multiplierUsed = true;
            }

            // Emotion requirement
            if (command.commandType == CommandType.Emotion)
            {
                emotionUsed = true;
            }
        }

        Debug.Log(
            $"Mission Check: " +
            $"Movement={movementUsed}, " +
            $"Multiplier={multiplierUsed}, " +
            $"Emotion={emotionUsed}, " +
            $"Finish={finishReached}"
        );
    }

    /// <summary>
    /// Called by GoalZone. Returns true when every mission requirement is met.
    /// </summary>
    public bool ReachFinish()
    {
        finishReached = true;

        Debug.Log("Finish line reached!");

        if (missionCompleted)
        {
            return true;
        }

        // Re-check the program so the result never depends on stale flags
        CheckProgram();

        string missingRequirement = GetMissingRequirement();

        if (missingRequirement != null)
        {
            FailMission(missingRequirement);
            return false;
        }

        missionCompleted = true;

        CompleteMission();

        return true;
    }

    private string GetMissingRequirement()
    {
        if (requireMovement && !movementUsed)
        {
            return "Use a movement block.";
        }

        if (requireMultiplier && !multiplierUsed)
        {
            return "Use a multiplier block.";
        }

        if (requireEmotion && !emotionUsed)
        {
            return "Use an emotion block.";
        }

        if (requireFinish && !finishReached)
        {
            return "Reach the finish.";
        }

        return null;
    }

    private void FailMission(string reason)
    {
        Debug.Log($"MISSION FAILED: {reason}");

        if (missionFailedPanel != null)
        {
            missionFailedPanel.SetActive(true);
            return;
        }

        // No failed panel: robot looks sad and the program can be edited again
        RobotController robot = FindFirstObjectByType<RobotController>();

        if (robot != null)
        {
            robot.StartCoroutine(robot.ShowEmotion(EmotionType.Sad));
        }

        if (CommandManager.Instance != null)
        {
            CommandManager.Instance.ShowSequenceUI();
        }
    }

    private void CompleteMission()
    {
        Debug.Log("MISSION COMPLETE!");

        if (missionCompletePanel != null)
        {
            missionCompletePanel.SetActive(true);
        }
    }

    public bool IsMissionComplete()
    {
        return missionCompleted;
    }

    /// <summary>
    /// Call this method from your Home UI button to load the scene with the loading screen.
    /// </summary>
    public void ReturnToHome()
    {
        if (string.IsNullOrEmpty(homeSceneName))
        {
            Debug.LogError("Home Scene Name is not set in the MissionManager inspector!");
            return;
        }

        // Use LoadingManager if it exists, otherwise fallback to direct load
        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.LoadScene(homeSceneName);
        }
        else
        {
            Debug.LogWarning("LoadingManager instance not found. Loading scene directly.");
            SceneManager.LoadScene(homeSceneName);
        }
    }
}