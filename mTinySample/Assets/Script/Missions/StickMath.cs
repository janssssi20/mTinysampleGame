using UnityEngine;

public enum StickDirection
{
    None,
    Up,
    Left,
    Right,
    Down
}

// Turns a joystick position (each axis -1 to 1) into one of four
// directions: small wobbles near the middle count as nothing, and the
// stronger axis wins on a diagonal.
public static class StickMath
{
    public static StickDirection DirectionOf(Vector2 stick, float deadZone)
    {
        if (stick.magnitude < deadZone)
        {
            return StickDirection.None;
        }

        if (Mathf.Abs(stick.y) >= Mathf.Abs(stick.x))
        {
            return stick.y > 0f ? StickDirection.Up : StickDirection.Down;
        }

        return stick.x > 0f ? StickDirection.Right : StickDirection.Left;
    }
}
