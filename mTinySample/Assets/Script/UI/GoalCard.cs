using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Mission goal at a glance: a small chip with mTiny and the stars earned
// so far. Tap it to open the card: the mission name, pictures of the
// blocks it needs, the goal tile, the instruction and the best answer.
public class GoalCard : MonoBehaviour
{
    public MissionBoardBuilder board;

    [Header("Chip")]
    public Button chip;
    public Image[] chipStars;

    [Header("Card")]
    public GameObject card;
    public TMP_Text title;
    public TMP_Text instruction;
    public TMP_Text best;
    public GameObject moveHint;
    public GameObject multiplierHint;
    public GameObject emotionHint;
    public GameObject soundHint;
    public Image goalTile;
    public Sprite parkGoal;
    public Sprite cityGoal;

    public Color starOn = new Color(1f, 0.72f, 0f);
    public Color starOff = new Color(0.85f, 0.87f, 0.92f);

    private void Start()
    {
        if (chip != null) chip.onClick.AddListener(Toggle);
        if (card != null) card.SetActive(false);
        Refresh();
    }

    public void Toggle()
    {
        if (card != null) card.SetActive(!card.activeSelf);
    }

    public void Refresh()
    {
        if (board == null || board.Current == null) return;

        MissionDefinition mission = board.Current;
        int stars = new MissionProgress(board.catalog, new PlayerPrefsProgressStore()).GetBestStars(board.CurrentIndex);

        if (chipStars != null)
        {
            for (int i = 0; i < chipStars.Length; i++)
            {
                if (chipStars[i] != null) chipStars[i].color = i < stars ? starOn : starOff;
            }
        }

        if (title != null) title.text = mission.title;
        if (instruction != null) instruction.text = mission.instruction;
        if (best != null) best.text = "Best: " + board.BestBlocks + " blocks";

        if (moveHint != null) moveHint.SetActive(mission.requireMovement);
        if (multiplierHint != null) multiplierHint.SetActive(mission.requireMultiplier);
        if (emotionHint != null) emotionHint.SetActive(mission.requireEmotion);
        if (soundHint != null) soundHint.SetActive(mission.requireSound);
        if (goalTile != null) goalTile.sprite = board.IsCityBoard ? cityGoal : parkGoal;
    }
}
