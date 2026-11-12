using UnityEngine;

// Explore's remote: a joystick and buttons that drive mTiny straight away,
// like a real remote. Hold the stick and mTiny keeps going: up drives
// forward, left and right turn, down turns all the way around.
public class RemoteControl : MonoBehaviour
{
    public RobotController robot;
    public RemoteJoystick joystick;

    private int nextNote;

    // The second half of a turn-around, sent once the first turn ends
    private bool secondTurnPending;

    private RobotController Robot
    {
        get
        {
            if (robot == null) robot = FindFirstObjectByType<RobotController>();
            return robot;
        }
    }

    private void OnDisable()
    {
        secondTurnPending = false;
    }

    private void Update()
    {
        if (Robot == null || Robot.IsExecuting())
        {
            return;
        }

        if (secondTurnPending)
        {
            secondTurnPending = !Send(new Command(CommandType.TurnRight));
            return;
        }

        switch (joystick != null ? joystick.Direction : StickDirection.None)
        {
            case StickDirection.Up:
                Send(new Command(CommandType.Forward));
                break;

            case StickDirection.Left:
                Send(new Command(CommandType.TurnLeft));
                break;

            case StickDirection.Right:
                Send(new Command(CommandType.TurnRight));
                break;

            case StickDirection.Down:
                secondTurnPending = Send(new Command(CommandType.TurnRight));
                break;
        }
    }

    // ---------------- BUTTONS ----------------

    public void Happy() { Send(new Command(CommandType.Emotion, EmotionType.Happy)); }

    // Toot toot: just a sound, mTiny keeps doing what it was doing
    public void Beep() { AudioManager.Play(Sfx.Start); }

    // Each press sings the next note of the scale, Do up to high Do
    public void Sing()
    {
        int count = System.Enum.GetValues(typeof(NoteType)).Length;

        if (Send(new Command((NoteType)nextNote)))
        {
            nextNote = (nextNote + 1) % count;
        }
    }

    private bool Send(Command command)
    {
        return Robot != null && Robot.RunSingle(command);
    }
}
