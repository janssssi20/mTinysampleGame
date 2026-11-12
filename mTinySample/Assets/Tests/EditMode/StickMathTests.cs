using NUnit.Framework;
using UnityEngine;

public class StickMathTests
{
    [Test]
    public void SmallWobble_IsNothing()
    {
        Assert.AreEqual(StickDirection.None, StickMath.DirectionOf(new Vector2(0.2f, 0.2f), 0.45f));
    }

    [Test]
    public void EachWay_PointsThatWay()
    {
        Assert.AreEqual(StickDirection.Up, StickMath.DirectionOf(new Vector2(0f, 1f), 0.45f));
        Assert.AreEqual(StickDirection.Down, StickMath.DirectionOf(new Vector2(0f, -1f), 0.45f));
        Assert.AreEqual(StickDirection.Left, StickMath.DirectionOf(new Vector2(-1f, 0f), 0.45f));
        Assert.AreEqual(StickDirection.Right, StickMath.DirectionOf(new Vector2(1f, 0f), 0.45f));
    }

    [Test]
    public void Diagonal_StrongerAxisWins()
    {
        Assert.AreEqual(StickDirection.Up, StickMath.DirectionOf(new Vector2(0.5f, 0.8f), 0.45f));
        Assert.AreEqual(StickDirection.Right, StickMath.DirectionOf(new Vector2(0.8f, -0.5f), 0.45f));
    }
}
