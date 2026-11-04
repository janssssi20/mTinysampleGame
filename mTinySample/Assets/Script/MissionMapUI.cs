using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Main Menu mission map: stage cards down the left for quick jumps, and a
// left-to-right winding path of all missions with a trophy after each
// stage. Tapping an open mission shows a card with a big Play button.
public class MissionMapUI : MonoBehaviour
{
    [System.Serializable]
    public class StageCard
    {
        public Button button;
        public TMP_Text starsText;
        public GameObject lockIcon;
        public Image trophyIcon;
        public GameObject highlight;
    }

    [System.Serializable]
    public class PathNode
    {
        public Button button;
        public Image face;
        public TMP_Text number;
        public Image[] stars;
        public GameObject lockIcon;
    }

    [System.Serializable]
    public class StageTrophy
    {
        public RectTransform rect;
        public Image icon;
    }

    private const string MissionSceneName = "Mission";

    // =========================================================
    // DATA
    // =========================================================

    [Header("Data")]
    public MissionCatalog catalog;

    // =========================================================
    // LAYOUT
    // =========================================================

    [Header("Path")]
    public ScrollRect pathScroll;
    public RectTransform pathContent;
    public PathNode[] nodes = new PathNode[15];
    public StageTrophy[] trophies = new StageTrophy[3];
    public RectTransform startBubble;
    public RectTransform mtinyMarker;

    [Header("Side")]
    public StageCard[] stageCards = new StageCard[3];
    public TMP_Text totalStarsText;

    // =========================================================
    // MISSION CARD (POP-UP)
    // =========================================================

    [Header("Mission Card")]
    public GameObject popup;
    public TMP_Text popupStageText;
    public TMP_Text popupTitleText;
    public GameObject popupMoveHint;
    public GameObject popupMultiplierHint;
    public GameObject popupEmotionHint;

    [Header("Effects")]
    public ConfettiBurst confetti;

    // =========================================================
    // COLORS
    // =========================================================

    [Header("Colors")]
    public Color doneColor = new Color(0.95f, 0.71f, 0.23f);
    public Color nextColor = new Color(0.94f, 0.54f, 0.14f);
    public Color lockedColor = new Color(0.85f, 0.84f, 0.80f);
    public Color starOnColor = Color.white;
    public Color starOffColor = new Color(1f, 1f, 1f, 0.3f);
    public Color lockedCardTint = new Color(0.72f, 0.72f, 0.72f);
    public Color noTrophyColor = new Color(0.80f, 0.78f, 0.74f);
    public Color bronzeColor = new Color(0.80f, 0.50f, 0.25f);
    public Color silverColor = new Color(0.76f, 0.78f, 0.82f);
    public Color goldColor = new Color(0.98f, 0.76f, 0.18f);

    private static readonly string[] StageNames = { "Easy", "Medium", "Hard" };

    private MissionProgress progress;
    private int selectedIndex = -1;
    private float scrollTarget = -1f;

    private MissionProgress Progress =>
        progress ?? (progress = new MissionProgress(catalog, new PlayerPrefsProgressStore()));


    private void OnEnable()
    {
        if (popup != null)
        {
            popup.SetActive(false);
        }

        Refresh();

        // Open on the next mission to play
        SnapTo(NodeX(StageMapModel.NextMissionIndex(Progress, catalog)));

        StartCoroutine(CelebrateNewTrophies());
    }

    private void Update()
    {
        if (scrollTarget >= 0f && pathScroll != null)
        {
            pathScroll.horizontalNormalizedPosition = Mathf.Lerp(
                pathScroll.horizontalNormalizedPosition, scrollTarget, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));

            if (Mathf.Abs(pathScroll.horizontalNormalizedPosition - scrollTarget) < 0.001f)
            {
                scrollTarget = -1f;
            }
        }

        HighlightStageInView();
    }


    // =========================================================
    // REFRESH
    // =========================================================

    public void Refresh()
    {
        if (totalStarsText != null)
        {
            totalStarsText.text = StageMapModel.StarCountText(Progress.GetTotalStars(), catalog.Count * 3);
        }

        for (int s = 0; s < stageCards.Length; s++)
        {
            MissionStage stage = (MissionStage)s;
            StageCard card = stageCards[s];
            bool unlocked = Progress.IsStageUnlocked(stage);
            TrophyTier tier = StageMapModel.GetTrophy(Progress, catalog, stage);

            card.starsText.text = StageMapModel.StarCountText(
                Progress.GetStageStars(stage), catalog.IndicesInStage(stage).Count * 3);
            card.lockIcon.SetActive(!unlocked);
            card.button.image.color = unlocked ? Color.white : lockedCardTint;

            if (card.trophyIcon != null)
            {
                card.trophyIcon.gameObject.SetActive(tier != TrophyTier.None);
                card.trophyIcon.color = TrophyColor(tier);
            }

            if (s < trophies.Length && trophies[s].icon != null)
            {
                trophies[s].icon.color = TrophyColor(tier);
            }
        }

        int next = StageMapModel.NextMissionIndex(Progress, catalog);

        for (int i = 0; i < nodes.Length && i < catalog.Count; i++)
        {
            PathNode node = nodes[i];
            NodeState state = StageMapModel.GetNodeState(Progress, i);
            int best = Progress.GetBestStars(i);

            node.face.color =
                state == NodeState.Done ? doneColor :
                state == NodeState.Next ? nextColor : lockedColor;

            node.lockIcon.SetActive(state == NodeState.Locked);
            node.number.enabled = state != NodeState.Locked;

            for (int s = 0; s < node.stars.Length; s++)
            {
                node.stars[s].gameObject.SetActive(state == NodeState.Done);
                node.stars[s].color = s < best ? starOnColor : starOffColor;
            }

            PulseScale pulse = node.button.GetComponent<PulseScale>();
            if (pulse != null)
            {
                pulse.enabled = i == next && state == NodeState.Next;
            }
        }

        // START bubble and mTiny stand at the next mission
        bool hasNext = StageMapModel.GetNodeState(Progress, next) == NodeState.Next;
        RectTransform nextRect = (RectTransform)nodes[next].button.transform;

        if (startBubble != null)
        {
            startBubble.gameObject.SetActive(hasNext);
            startBubble.anchoredPosition = nextRect.anchoredPosition + new Vector2(0f, 135f);
        }

        if (mtinyMarker != null)
        {
            mtinyMarker.gameObject.SetActive(hasNext);
            mtinyMarker.anchoredPosition = nextRect.anchoredPosition + new Vector2(140f, -10f);
        }
    }

    private Color TrophyColor(TrophyTier tier)
    {
        switch (tier)
        {
            case TrophyTier.Gold: return goldColor;
            case TrophyTier.Silver: return silverColor;
            case TrophyTier.Bronze: return bronzeColor;
            default: return noTrophyColor;
        }
    }


    // =========================================================
    // STAGE CARDS (QUICK JUMP)
    // =========================================================

    public void OnStagePressed(int stage)
    {
        if (!Progress.IsStageUnlocked((MissionStage)stage))
        {
            StartCoroutine(Wiggle(stageCards[stage].lockIcon.transform));
            return;
        }

        ScrollTo(NodeX(catalog.IndicesInStage((MissionStage)stage)[0]));
    }

    private void HighlightStageInView()
    {
        if (pathScroll == null || pathContent == null)
        {
            return;
        }

        float centre = -pathContent.anchoredPosition.x + pathScroll.viewport.rect.width * 0.5f;
        int inView = stageCards.Length - 1;

        for (int s = 0; s < trophies.Length; s++)
        {
            if (centre <= trophies[s].rect.anchoredPosition.x)
            {
                inView = s;
                break;
            }
        }

        for (int s = 0; s < stageCards.Length; s++)
        {
            if (stageCards[s].highlight != null)
            {
                stageCards[s].highlight.SetActive(s == inView);
            }
        }
    }


    // =========================================================
    // MISSIONS
    // =========================================================

    public void OnNodePressed(int index)
    {
        if (StageMapModel.GetNodeState(Progress, index) == NodeState.Locked)
        {
            StartCoroutine(Wiggle(nodes[index].lockIcon.transform));
            return;
        }

        selectedIndex = index;
        MissionDefinition mission = catalog.Get(index);
        int number = catalog.IndicesInStage(mission.stage).IndexOf(index) + 1;

        popupStageText.text = StageNames[(int)mission.stage] + " · Mission " + number;
        popupTitleText.text = mission.title;
        popupMoveHint.SetActive(mission.requireMovement);
        popupMultiplierHint.SetActive(mission.requireMultiplier);
        popupEmotionHint.SetActive(mission.requireEmotion);
        popup.SetActive(true);
    }

    public void OnPlayPressed()
    {
        if (selectedIndex < 0)
        {
            return;
        }

        MissionSelection.SelectedIndex = selectedIndex;

        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.LoadScene(MissionSceneName);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(MissionSceneName);
        }
    }

    public void ClosePopup()
    {
        popup.SetActive(false);
        selectedIndex = -1;
    }


    // =========================================================
    // TROPHY CONFETTI
    // =========================================================

    private IEnumerator CelebrateNewTrophies()
    {
        // Let the layout settle so the trophy positions are final
        yield return null;

        for (int s = 0; s < trophies.Length; s++)
        {
            MissionStage stage = (MissionStage)s;
            TrophyTier tier = StageMapModel.GetTrophy(Progress, catalog, stage);

            if (!Progress.TakeTrophyCelebration(stage, tier))
            {
                continue;
            }

            if (confetti != null)
            {
                confetti.Play(trophies[s].rect.position);
            }

            PulseScale pulse = trophies[s].rect.GetComponent<PulseScale>();
            if (pulse != null)
            {
                pulse.enabled = true;
                yield return new WaitForSecondsRealtime(1.4f);
                pulse.enabled = false;
            }
        }
    }


    // =========================================================
    // SCROLLING
    // =========================================================

    private float NodeX(int index)
    {
        return ((RectTransform)nodes[index].button.transform).anchoredPosition.x;
    }

    private float NormalizedFor(float contentX)
    {
        Canvas.ForceUpdateCanvases();

        float viewport = pathScroll.viewport.rect.width;
        float range = pathContent.rect.width - viewport;

        return range <= 0f ? 0f : Mathf.Clamp01((contentX - viewport * 0.5f) / range);
    }

    private void SnapTo(float contentX)
    {
        if (pathScroll == null) return;

        pathScroll.horizontalNormalizedPosition = NormalizedFor(contentX);
        scrollTarget = -1f;
    }

    private void ScrollTo(float contentX)
    {
        if (pathScroll == null) return;

        pathScroll.velocity = Vector2.zero;
        scrollTarget = NormalizedFor(contentX);
    }

    private IEnumerator Wiggle(Transform target)
    {
        const float duration = 0.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            target.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(elapsed / duration * Mathf.PI * 6f) * 15f);
            yield return null;
        }

        target.localRotation = Quaternion.identity;
    }
}
