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

    public void ReachFinish()
    {
        finishReached = true;

        Debug.Log("Finish line reached!");

        CheckMissionComplete();
    }

    private void CheckMissionComplete()
    {
        if (missionCompleted)
        {
            return;
        }

        if (requireMovement && !movementUsed)
        {
            return;
        }

        if (requireMultiplier && !multiplierUsed)
        {
            return;
        }

        if (requireEmotion && !emotionUsed)
        {
            return;
        }

        if (requireFinish && !finishReached)
        {
            return;
        }

        missionCompleted = true;

        CompleteMission();
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