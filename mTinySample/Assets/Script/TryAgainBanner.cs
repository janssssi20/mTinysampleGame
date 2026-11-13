using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum TryAgainReason
{
    Bumped,
    TooShort,
    MissingMovement,
    MissingMultiplier,
    MissingEmotion,
    MissingSound,
    MissedFeelingTile,
    MissedMusicTile
}

// Friendly card after a run that didn't finish the mission. It says what
// happened in a few words, and Try again puts mTiny back
// on the start tile with the program kept, so the child fixes one block
// instead of starting over.
public class TryAgainBanner : MonoBehaviour
{
    public TMP_Text message;
    public Button tryAgainButton;

    private void Awake()
    {
        if (tryAgainButton != null)
        {
            tryAgainButton.onClick.AddListener(TryAgain);
        }
    }

    public static string MessageFor(TryAgainReason reason)
    {
        switch (reason)
        {
            case TryAgainReason.Bumped: return "Oops, mTiny bumped! Fix the red block and try again.";
            case TryAgainReason.MissingMovement: return "Add a block that moves mTiny, then try again.";
            case TryAgainReason.MissingMultiplier: return "You made it! Now use a x2 block and try again.";
            case TryAgainReason.MissingEmotion: return "You made it! Now add a feeling block and try again.";
            case TryAgainReason.MissingSound: return "You made it! Now add a music block and try again.";
            case TryAgainReason.MissedFeelingTile: return "Almost! Stop on the smiley tile and show a feeling there.";
            case TryAgainReason.MissedMusicTile: return "Almost! Stop on the music tile and play music there.";
            default: return "Almost! Add more blocks to reach the flag.";
        }
    }

    public void Show(TryAgainReason reason)
    {
        if (message != null)
        {
            message.text = MessageFor(reason);
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void TryAgain()
    {
        CommandButtonManager buttons = FindFirstObjectByType<CommandButtonManager>();

        if (buttons != null)
        {
            buttons.ReturnRobotToStart();
        }

        Hide();
    }
}
