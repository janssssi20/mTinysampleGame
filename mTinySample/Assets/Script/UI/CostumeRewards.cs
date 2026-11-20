using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The finish card's costume row: the stage's mask and a bar that springs
// forward by one ("+1") for each new level won. When the bar fills, a
// "New mask!" card shows mTiny wearing its new costume and the new
// background (world) that comes with it.
public class CostumeRewards : MonoBehaviour
{
    [Header("Finish card row")]
    public GameObject row;
    public Image rowIcon;
    public TMP_Text rowTitle;
    public Image rowFill;
    public TMP_Text rowCount;
    public TMP_Text plusOne;

    [Header("New mask card")]
    public GameObject newCard;
    public Image newPicture;
    public Image newWorld;                 // the new background, beside mTiny
    public TMP_Text newMessage;
    public Button wearButton;
    public Button laterButton;
    public RectTransform rays;

    [Header("Art (index = Costume)")]
    public Sprite[] maskIcons = new Sprite[CostumeRules.Count];
    public Sprite[] costumePictures = new Sprite[CostumeRules.Count];
    public Sprite[] worldPictures = new Sprite[CostumeRules.Count];

    private Costume pending = Costume.None;

    private void Awake()
    {
        if (newCard != null) newCard.SetActive(false);
        if (wearButton != null) wearButton.onClick.AddListener(WearNew);
        if (laterButton != null) laterButton.onClick.AddListener(CloseNew);
    }

    // Called after a win: wonBefore/wonAfter are the stage's levels won
    public void Show(MissionStage stage, int wonBefore, int wonAfter, int total, bool newlyUnlocked)
    {
        Costume costume = CostumeRules.ForStage(stage);
        if (row != null)
        {
            row.SetActive(true);
            if (rowIcon != null) rowIcon.sprite = Pick(maskIcons, costume);
            if (rowTitle != null) rowTitle.text = CostumeRules.Name(costume);
            StopAllCoroutines();
            StartCoroutine(Fill(wonBefore, wonAfter, Mathf.Max(1, total)));
        }

        if (newlyUnlocked)
        {
            pending = costume;
            StartCoroutine(ShowNewAfter(1.3f, costume, stage));
        }
    }

    private IEnumerator Fill(int from, int to, int total)
    {
        SetBar(from, total);
        if (plusOne != null) plusOne.alpha = 0f;
        if (to <= from) yield break;

        yield return new WaitForSecondsRealtime(0.4f);
        float t = 0f;
        Vector2 plusStart = plusOne != null ? plusOne.rectTransform.anchoredPosition : Vector2.zero;
        while (t < 0.9f)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / 0.6f);
            float spring = 1f + Mathf.Sin(u * Mathf.PI) * 0.25f * (1f - u);  // overshoot
            float value = Mathf.Lerp(from, to, Mathf.Min(1f, u * spring));
            if (rowFill != null) rowFill.fillAmount = value / total;
            if (plusOne != null)
            {
                plusOne.alpha = Mathf.Clamp01(1.5f - t * 1.6f);
                plusOne.rectTransform.anchoredPosition = plusStart + new Vector2(0f, 30f * Mathf.Clamp01(t / 0.9f));
            }
            yield return null;
        }
        if (plusOne != null) { plusOne.alpha = 0f; plusOne.rectTransform.anchoredPosition = plusStart; }
        SetBar(to, total);
    }

    private void SetBar(int won, int total)
    {
        if (rowFill != null) rowFill.fillAmount = (float)won / total;
        if (rowCount != null) rowCount.text = won >= total ? "Unlocked!" : won + " of " + total + " levels";
    }

    private IEnumerator ShowNewAfter(float delay, Costume costume, MissionStage stage)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (newCard == null) yield break;
        if (newPicture != null) newPicture.sprite = Pick(costumePictures, costume);
        if (newWorld != null) newWorld.sprite = Pick(worldPictures, costume);
        if (newMessage != null)
        {
            newMessage.text = "You finished <b>" + MissionStages.Name(stage) + "</b>. You got the <b>" +
                              CostumeRules.ShortName(costume) + "</b>" + (costume == Costume.Golden ? " costume" : " mask") +
                              " and the <b>" + BackgroundRules.Name(costume) + "</b>!";
        }
        newCard.SetActive(true);
        AudioManager.Play(Sfx.Cheer);
    }

    private void Update()
    {
        if (rays != null && newCard != null && newCard.activeInHierarchy)
        {
            rays.Rotate(0f, 0f, -25f * Time.unscaledDeltaTime);
        }
    }

    public void WearNew()
    {
        if (pending != Costume.None)
        {
            var store = new PlayerPrefsProgressStore();
            CostumeRules.Select(store, pending);
            BackgroundRules.Select(store, pending);
            foreach (CostumeWearer wearer in FindObjectsByType<CostumeWearer>(FindObjectsSortMode.None))
            {
                wearer.Wear(pending);
            }
            AudioManager.Play(Sfx.Sparkle);
        }
        CloseNew();
    }

    public void CloseNew()
    {
        if (newCard != null) newCard.SetActive(false);
        pending = Costume.None;
    }

    private static Sprite Pick(Sprite[] list, Costume c)
    {
        return list != null && (int)c < list.Length ? list[(int)c] : null;
    }
}
