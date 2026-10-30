// Fires once after a press is held for the full duration.
// Releasing early cancels; the next press starts from zero.
public class HoldTimer
{
    private readonly float duration;
    private float held;
    private bool holding;

    public HoldTimer(float duration)
    {
        this.duration = duration;
    }

    public void Begin()
    {
        held = 0f;
        holding = true;
    }

    public void Cancel()
    {
        holding = false;
        held = 0f;
    }

    // Returns true on the tick the hold completes.
    public bool Tick(float deltaTime)
    {
        if (!holding)
        {
            return false;
        }

        held += deltaTime;

        if (held < duration)
        {
            return false;
        }

        holding = false;

        return true;
    }
}
