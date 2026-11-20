using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Customize, left half (Costume): a big 3D mTiny on a slow turntable and a card for
// each costume. Earned costumes can be tried on with a tap; locked ones
// show the stage that gives them and how far along the child is.
public class DressUpUI : MonoBehaviour
{
    [System.Serializable]
    public class CostumeCard
    {
        public Costume costume;
        public Button button;
        public Image picture;
        public GameObject lockIcon;
        public GameObject selected;
        public TMP_Text hint;
        public Image progressFill;
    }

    public MissionCatalog catalog;
    public GameObject panel;
    public CostumeCard[] cards;
    public TMP_Text costumeName;

    [Header("3D preview")]
    public RawImage previewImage;
    public GameObject robotModel;          // the mTiny model asset (mTinyModel)
    public Vector3 stagePosition = new Vector3(500f, 500f, 0f);
    public float turnDegrees = 22f;

    private Transform turntable;
    private CostumeWearer wearer;
    private CostumeStage previewStage;
    private RenderTexture texture;
    private MissionProgress progress;
    private IProgressStore store;

    private bool wired;

    // (This sits on the room's own panel, which the setup leaves hidden:
    // hiding it here would close the room the first time it opens.)
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
        foreach (CostumeCard card in cards)
        {
            Costume c = card.costume;
            if (card.button != null) card.button.onClick.AddListener(() => Pick(c));
        }
    }

    public void Open()
    {
        Ensure();
        if (panel != null) panel.SetActive(true);
        BuildPreview();
        Refresh();
        Show(CostumeRules.Selected(store, progress, catalog));
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        if (previewStage != null) previewStage.Live = false;

        // The title screen's mTiny changes into the new costume
        foreach (TitleMascot3D title in FindObjectsByType<TitleMascot3D>(FindObjectsSortMode.None)) title.Refresh();
    }

    private void Refresh()
    {
        Costume chosen = CostumeRules.Selected(store, progress, catalog);
        foreach (CostumeCard card in cards)
        {
            bool open = CostumeRules.IsUnlocked(progress, catalog, card.costume);
            if (card.lockIcon != null) card.lockIcon.SetActive(!open);
            if (card.picture != null) card.picture.color = open ? Color.white : new Color(0.75f, 0.75f, 0.8f, 0.55f);
            if (card.selected != null) card.selected.SetActive(card.costume == chosen);

            if (card.costume == Costume.None)
            {
                if (card.hint != null) card.hint.text = "Always yours";
                if (card.progressFill != null) card.progressFill.transform.parent.gameObject.SetActive(false);
                continue;
            }

            MissionStage stage = CostumeRules.StageFor(card.costume);
            int won = CostumeRules.LevelsWon(progress, catalog, stage);
            int total = Mathf.Max(1, CostumeRules.LevelsInStage(catalog, stage));
            if (card.hint != null) card.hint.text = open ? MissionStages.Name(stage) : "Finish Stage " + ((int)stage + 1);
            if (card.progressFill != null)
            {
                card.progressFill.transform.parent.gameObject.SetActive(!open);
                card.progressFill.fillAmount = (float)won / total;
            }
        }
    }

    private void Pick(Costume c)
    {
        if (!CostumeRules.IsUnlocked(progress, catalog, c))
        {
            AudioManager.Play(Sfx.Oops);
            return;
        }
        CostumeRules.Select(store, c);
        AudioManager.Play(Sfx.Sparkle);
        Refresh();
        Show(c);
    }

    private void Show(Costume c)
    {
        if (costumeName != null) costumeName.text = CostumeRules.ShortName(c);
        if (wearer != null) wearer.Wear(c);
    }

    // The live 3D mTiny (see CostumeStage)
    private void BuildPreview()
    {
        if (turntable != null)
        {
            previewStage.Live = true;
            return;
        }

        CostumeStage stage = CostumeStage.Build(robotModel, stagePosition, 768, 1.65f, new Vector3(0f, 1f, -0.35f));
        turntable = stage.Turntable;
        wearer = stage.Wearer;
        previewStage = stage;
        texture = stage.Texture;
        if (previewImage != null) previewImage.texture = texture;
    }

    private void Update()
    {
        if (turntable == null || panel == null || !panel.activeInHierarchy) return;
        float angle = Mathf.Sin(Time.unscaledTime * 0.9f) * turnDegrees;
        turntable.localRotation = Quaternion.AngleAxis(angle, Vector3.back);
    }

    private void OnDestroy()
    {
        if (texture != null) texture.Release();
    }
}
