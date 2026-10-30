using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Main Menu mission picker: three stage cards, then a five-node trail
// for the chosen stage. Locked stages and missions can't be opened.
public class StageMapUI : MonoBehaviour
{
    [System.Serializable]
    public class StageCard
    {
        public Button button;
        public TMP_Text starsText;
        public GameObject lockIcon;
    }

    [System.Serializable]
    public class TrailNode
    {
        public Button button;
        public Image background;
        public Image[] stars;
        public GameObject lockIcon;
    }

    private static readonly string[] StageNames = { "Easy", "Medium", "Hard" };

    private const string MissionSceneName = "Mission";

    // =========================================================
    // DATA
    // =========================================================

    [Header("Data")]
    public MissionCatalog catalog;

    // =========================================================
    // PANELS
    // =========================================================

    [Header("Panels")]
    public GameObject stagePanel;
    public GameObject trailPanel;
    public TMP_Text bannerText;

    // =========================================================
    // STAGE CARDS
    // =========================================================

    [Header("Stage Cards")]
    public StageCard[] stageCards = new StageCard[3];

    // =========================================================
    // TRAIL
    // =========================================================

    [Header("Trail")]
    public TrailNode[] nodes = new TrailNode[5];
    public RectTransform mtinyMarker;
    public Vector2 markerOffset = new Vector2(110f, 60f);

    // =========================================================
    // COLORS
    // =========================================================

    [Header("Colors")]
    public Color doneColor = new Color(0.62f, 0.85f, 0.45f);
    public Color nextColor = new Color(1f, 0.82f, 0.35f);
    public Color lockedColor = new Color(0.75f, 0.75f, 0.75f);
    public Color starOnColor = Color.white;
    public Color starOffColor = new Color(1f, 1f, 1f, 0.25f);
    public Color lockedCardTint = new Color(0.72f, 0.72f, 0.72f);

    private MissionProgress progress;

    // Created on first use so editor previews work outside play mode
    private MissionProgress Progress =>
        progress ?? (progress = new MissionProgress(catalog, new PlayerPrefsProgressStore()));
    private List<int> stageMissions = new List<int>();


    private void Awake()
    {
        progress = new MissionProgress(catalog, new PlayerPrefsProgressStore());
    }

    private void OnEnable()
    {
        BackToStages();
    }


    // =========================================================
    // REFRESH
    // =========================================================

    public void Refresh()
    {
        for (int i = 0; i < stageCards.Length && i < StageNames.Length; i++)
        {
            MissionStage stage = (MissionStage)i;
            int maxStars = catalog.IndicesInStage(stage).Count * 3;

            stageCards[i].starsText.text =
                StageMapModel.StarCountText(Progress.GetStageStars(stage), maxStars);

            bool unlocked = Progress.IsStageUnlocked(stage);

            stageCards[i].lockIcon.SetActive(!unlocked);

            // Locked stages look greyed out, not just padlocked
            stageCards[i].button.image.color = unlocked ? Color.white : lockedCardTint;
        }

        if (trailPanel != null && trailPanel.activeSelf)
        {
            RefreshTrail();
        }
    }


    // =========================================================
    // STAGE CARDS
    // =========================================================

    public void OnStagePressed(int stage)
    {
        if (!Progress.IsStageUnlocked((MissionStage)stage))
        {
            StartCoroutine(Wiggle(stageCards[stage].lockIcon.transform));
            return;
        }

        OpenStage(stage);
    }

    public void OpenStage(int stage)
    {
        stageMissions = catalog.IndicesInStage((MissionStage)stage);

        stagePanel.SetActive(false);
        trailPanel.SetActive(true);

        if (bannerText != null)
        {
            bannerText.text = StageNames[stage];
        }

        RefreshTrail();
    }

    public void BackToStages()
    {
        trailPanel.SetActive(false);
        stagePanel.SetActive(true);

        if (bannerText != null)
        {
            bannerText.text = "choose a stage";
        }

        Refresh();
    }


    // =========================================================
    // TRAIL
    // =========================================================

    public void OnNodePressed(int slot)
    {
        if (slot < 0 || slot >= stageMissions.Count)
        {
            return;
        }

        int index = stageMissions[slot];

        if (!Progress.IsMissionUnlocked(index))
        {
            return;
        }

        MissionSelection.SelectedIndex = index;

        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.LoadScene(MissionSceneName);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(MissionSceneName);
        }
    }

    private void RefreshTrail()
    {
        bool markerPlaced = false;

        for (int slot = 0; slot < nodes.Length; slot++)
        {
            TrailNode node = nodes[slot];
            bool exists = slot < stageMissions.Count;

            node.button.gameObject.SetActive(exists);

            if (!exists)
            {
                continue;
            }

            int index = stageMissions[slot];
            NodeState state = StageMapModel.GetNodeState(Progress, index);
            int best = Progress.GetBestStars(index);

            node.button.interactable = state != NodeState.Locked;

            // The mission to play next gently pulses
            PulseScale pulse = node.button.GetComponent<PulseScale>();
            if (pulse != null)
            {
                pulse.enabled = state == NodeState.Next;
            }
            node.lockIcon.SetActive(state == NodeState.Locked);

            // A locked node shows only its padlock, not the number behind it
            TMPro.TMP_Text number = node.button.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (number != null)
            {
                number.enabled = state != NodeState.Locked;
            }

            node.background.color =
                state == NodeState.Done ? doneColor :
                state == NodeState.Next ? nextColor : lockedColor;

            for (int s = 0; s < node.stars.Length; s++)
            {
                node.stars[s].gameObject.SetActive(state == NodeState.Done);
                node.stars[s].color = s < best ? starOnColor : starOffColor;
            }

            if (state == NodeState.Next && mtinyMarker != null)
            {
                RectTransform nodeRect = (RectTransform)node.button.transform;
                mtinyMarker.anchoredPosition = nodeRect.anchoredPosition + markerOffset;
                markerPlaced = true;
            }
        }

        if (mtinyMarker != null)
        {
            mtinyMarker.gameObject.SetActive(markerPlaced);
        }
    }


    // =========================================================
    // LOCK WIGGLE
    // =========================================================

    private IEnumerator Wiggle(Transform target)
    {
        const float duration = 0.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            // Three shakes of +/-15 degrees
            float angle = Mathf.Sin(elapsed / duration * Mathf.PI * 6f) * 15f;
            target.localRotation = Quaternion.Euler(0f, 0f, angle);

            yield return null;
        }

        target.localRotation = Quaternion.identity;
    }
}
