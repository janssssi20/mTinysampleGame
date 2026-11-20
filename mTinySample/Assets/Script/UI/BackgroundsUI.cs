using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Customize, right half (Backgrounds): a card for each world. Open worlds switch the
// title screen's background with a tap; locked ones show the mask that
// opens them and the stage to finish.
public class BackgroundsUI : MonoBehaviour
{
    [System.Serializable]
    public class WorldCard
    {
        public Costume world;
        public Button button;
        public Image picture;
        public GameObject lockCover;
        public TMP_Text lockText;
        public GameObject selected;
    }

    public MissionCatalog catalog;
    public GameObject panel;
    public Image backdrop;            // the chosen world, behind the cards
    public WorldCard[] cards;

    private MissionProgress progress;
    private IProgressStore store;
    private bool wired;

    private void Awake()
    {
        Ensure();
    }

    private void Ensure()
    {
        if (store == null) store = new PlayerPrefsProgressStore();
        if (progress == null) progress = new MissionProgress(catalog, store);
        if (wired) return;
        wired = true;
        foreach (WorldCard card in cards)
        {
            Costume c = card.world;
            if (card.button != null) card.button.onClick.AddListener(() => Pick(c));
        }
    }

    public void Open()
    {
        Ensure();
        if (panel != null) panel.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        TitleBackground.RefreshAll();
    }

    private void Refresh()
    {
        Costume chosen = BackgroundRules.Selected(store, progress, catalog);
        foreach (WorldCard card in cards)
        {
            bool open = BackgroundRules.IsUnlocked(progress, catalog, card.world);
            if (card.lockCover != null) card.lockCover.SetActive(!open);
            if (card.selected != null) card.selected.SetActive(card.world == chosen);
            if (card.lockText != null && card.world != Costume.None)
            {
                card.lockText.text = "Finish Stage " + ((int)CostumeRules.StageFor(card.world) + 1);
            }
            if (card.world == chosen && backdrop != null && card.picture != null) backdrop.sprite = card.picture.sprite;
        }
    }

    private void Pick(Costume c)
    {
        if (!BackgroundRules.IsUnlocked(progress, catalog, c))
        {
            AudioManager.Play(Sfx.Oops);
            return;
        }
        BackgroundRules.Select(store, c);
        AudioManager.Play(Sfx.Sparkle);
        Refresh();
        TitleBackground.RefreshAll();
    }
}
