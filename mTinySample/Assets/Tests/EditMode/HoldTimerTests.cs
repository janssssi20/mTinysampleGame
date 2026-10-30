using NUnit.Framework;

public class HoldTimerTests
{
    [Test]
    public void Tick_Before3Seconds_DoesNotTrigger()
    {
        HoldTimer timer = new HoldTimer(3f);
        timer.Begin();

        Assert.IsFalse(timer.Tick(2.9f));
    }

    [Test]
    public void Tick_Reaching3Seconds_TriggersOnce()
    {
        HoldTimer timer = new HoldTimer(3f);
        timer.Begin();

        Assert.IsFalse(timer.Tick(2f));
        Assert.IsTrue(timer.Tick(1f));
        Assert.IsFalse(timer.Tick(5f));
    }

    [Test]
    public void Cancel_ReleasedEarly_StartsOverOnNextHold()
    {
        HoldTimer timer = new HoldTimer(3f);
        timer.Begin();
        timer.Tick(2f);
        timer.Cancel();

        timer.Begin();

        Assert.IsFalse(timer.Tick(2f));
    }

    [Test]
    public void Tick_WithoutBegin_NeverTriggers()
    {
        HoldTimer timer = new HoldTimer(3f);

        Assert.IsFalse(timer.Tick(10f));
    }
}
