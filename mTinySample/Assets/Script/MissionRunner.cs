using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Mission scene flow after a win: saves progress, shows Next,
// and opens the celebration after the final mission.
public class MissionRunner : MonoBehaviour
{
    public static MissionRunner Instance;

    private const string MissionSceneName = "Mission";

    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    public MissionBoardBuilder board;
    public MissionCatalog catalog;

    // =========================================================
    // FINISH PANEL
    // =========================================================

    [Header("Finish Panel")]
    public GameObject finishPanel;
    public GameObject nextButton;

    // =========================================================
    // CELEBRATION PANEL
    // =========================================================

    [Header("Celebration Panel")]
    public GameObject celebrationPanel;
    public TMP_Text celebrationStarsText;

    private MissionProgress progress;


    private void Awake()
    {
        // Scene-scoped: the newest scene's manager takes over. Never destroy
        // the GameObject here; it carries other managers too.

        Instance = this;

        progress = new MissionProgress(catalog, new PlayerPrefsProgressStore());

        if (celebrationPanel != null)
        {
            celebrationPanel.SetActive(false);
        }
    }


    // =========================================================
    // MISSION WON
    // =========================================================
    // Called by GoalZone after the finish panel is shown.
    // =========================================================

    public void OnMissionWon(int stars)
    {
        if (stars <= 0 || board == null)
        {
            return;
        }

        MissionWinOutcome outcome =
            MissionWinOutcome.Decide(progress, catalog, board.CurrentIndex, stars);

        if (nextButton != null)
        {
            nextButton.SetActive(outcome.showNext);
        }

        if (!outcome.celebrate)
        {
            return;
        }

        if (finishPanel != null)
        {
            finishPanel.SetActive(false);
        }

        if (celebrationStarsText != null)
        {
            celebrationStarsText.text = StageMapModel.StarCountText(outcome.totalStars, catalog.Count * 3);
        }

        if (celebrationPanel != null)
        {
            celebrationPanel.SetActive(true);
        }

        Debug.Log("All missions complete. Celebration!");
    }


    // =========================================================
    // REPLAY / NEXT
    // =========================================================

    public void Replay()
    {
        LoadMissionScene();
    }

    public void Next()
    {
        if (board == null)
        {
            return;
        }

        int next = catalog.NextIndex(board.CurrentIndex);

        if (next == -1)
        {
            return;
        }

        MissionSelection.SelectedIndex = next;

        LoadMissionScene();
    }

    private static void LoadMissionScene()
    {
        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.LoadScene(MissionSceneName);
        }
        else
        {
            SceneManager.LoadScene(MissionSceneName);
        }
    }
}
