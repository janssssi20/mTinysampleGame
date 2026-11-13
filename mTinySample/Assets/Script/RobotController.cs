using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class RobotController : MonoBehaviour
{
    private CommandManager commandManager;
    private Rigidbody2D rb;

    // =========================================================
    // MOVEMENT
    // =========================================================

    [Header("Movement")]
    public float moveDistance = 1f;
    public float moveSpeed = 2f;

    // =========================================================
    // WALKABLE TILES
    // =========================================================

    [Header("Walkable Tiles")]
    public LayerMask walkableLayer;

    // =========================================================
    // WALL DETECTION
    // =========================================================

    [Header("Wall Detection")]
    public LayerMask wallLayer;

    // =========================================================
    // PROGRAM SETTINGS
    // =========================================================

    [Header("Program Settings")]
    public float commandDelay = 0.2f;

    private bool isExecuting = false;

    // Set when the robot reaches the goal so the program
    // stops after the current step finishes.
    private bool stopRequested = false;

    // Set when mTiny bumps a wall or leaves the road. The program stops
    // there and the block that caused it is shown to the child.
    private bool bumped = false;
    private int currentCommand = -1;

    // Size the robot returns to after a note bounce or a Reset
    private Vector3 baseScale;

    // Depth of the mat mTiny drives on. It spawns on the start tile, so
    // it keeps that depth (just above it) instead of floating at 0, which
    // only looked right from straight above.
    private float planeZ;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        baseScale = transform.localScale;
        planeZ = transform.position.z - 0.02f;

        if (rb != null)
        {
            // Collisions must never spin the robot off the grid
            rb.freezeRotation = true;
        }
    }


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        commandManager = CommandManager.Instance;

        if (commandManager == null)
        {
            commandManager =
                FindFirstObjectByType<CommandManager>();
        }

        if (commandManager == null)
        {
            Debug.LogError(
                "RobotController: CommandManager could not be found!"
            );
        }

        if (rb == null)
        {
            Debug.LogError(
                "RobotController: Rigidbody2D is missing!"
            );
        }
    }


// =========================================================
// RUN PROGRAM
// =========================================================

public void RunProgram()
{
    if (isExecuting)
    {
        Debug.LogWarning(
            "Robot is already executing the program."
        );

        return;
    }

    if (commandManager == null)
    {
        Debug.LogError(
            "Cannot run program: CommandManager is missing!"
        );

        return;
    }

    if (!commandManager.HasRunnableCommands())
    {
        Debug.LogWarning(
            "Cannot run program: Command list is empty!"
        );

        return;
    }

    if (commandManager.IsGameFinished())
    {
        Debug.LogWarning(
            "Cannot run program: Level is already finished. Press Reset."
        );

        return;
    }

    stopRequested = false;

    Debug.Log(
        $"Starting program with " +
        $"{commandManager.commandList.Count} commands."
    );

    // The sequence bar stays up so the running block can glow;
    // editing is already locked while the program runs.
    commandManager.ClearHighlight();

    // =========================================================
    // START COMMAND EXECUTION
    // =========================================================

    StartCoroutine(ExecuteCommands());
}


    // =========================================================
    // EXECUTE COMMANDS
    // =========================================================

    private IEnumerator ExecuteCommands()
    {
        isExecuting = true;

        commandManager.SetProgramRunning(true);

        // Each run starts with every marked tile waiting again
        if (MissionBoardBuilder.Instance != null)
        {
            MissionBoardBuilder.Instance.ResetCues();
        }

        Debug.Log(
            "========== PROGRAM STARTED =========="
        );

        // Iterate over a copy so UI clicks during execution
        // can never modify the list being executed.
        Command[] program = commandManager.commandList.ToArray();

        bumped = false;

        for (int index = 0; index < program.Length; index++)
        {
            Command cmd = program[index];

            if (stopRequested || bumped)
            {
                break;
            }

            currentCommand = index;
            commandManager.HighlightCommand(index);

            if (cmd == null)
            {
                Debug.LogWarning(
                    "Found a null command. Skipping."
                );

                continue;
            }


            int repeatCount = Mathf.Max(
                1,
                cmd.repeatCount
            );

            Debug.Log(
                $"Executing: {cmd.commandType} " +
                $"x{repeatCount}"
            );

            // Execute command according to multiplier
            for (int i = 0; i < repeatCount && !stopRequested && !bumped; i++)
            {
                Debug.Log(
                    $"Execution {i + 1}/{repeatCount}: " +
                    $"{cmd.commandType}"
                );

                yield return RunStep(cmd);

                // =================================================
                // DELAY BETWEEN REPEATED COMMANDS
                // =================================================

                if (commandDelay > 0f)
                {
                    yield return new WaitForSeconds(
                        commandDelay
                    );
                }
            }
        }

        if (stopRequested)
        {
            // Goal was reached; GoalZone handles the result
            stopRequested = false;

            commandManager.ClearHighlight();

            FinishExecution();

            yield break;
        }

        // =====================================================
        // FREE PLAY (EXPLORE): A PROGRAM JUST ENDS
        // =====================================================
        // There is nothing to fail in Explore, so a song or a drive
        // never ends with a sad face.

        if (commandManager.freePlay)
        {
            commandManager.ClearHighlight();

            FinishExecution();

            yield break;
        }

        // =====================================================
        // PROGRAM ENDED WITHOUT REACHING THE GOAL
        // =====================================================

        Debug.Log(
            bumped
                ? "Program stopped at a bump."
                : "Program finished without reaching the goal."
        );

        // A bump already played its own "uh-oh" and bump face
        if (!bumped)
        {
            AudioManager.Play(Sfx.Oops);

            yield return ShowEmotion(EmotionType.Sad);
        }

        FinishExecution();

        // Keep the block that caused the bump lit, then offer Try again
        if (bumped)
        {
            commandManager.HighlightCommand(currentCommand, problem: true);
        }
        else
        {
            commandManager.ClearHighlight();
        }

        commandManager.ShowTryAgain(bumped ? TryAgainReason.Bumped : TryAgainReason.TooShort);
    }


    // =========================================================
    // RUN ONE STEP
    // =========================================================
    // One action of a block. Programs repeat it for multipliers;
    // the remote runs it straight away.
    // =========================================================

    private IEnumerator RunStep(Command cmd)
    {
        switch (cmd.commandType)
        {
            // =========================================
            // FORWARD
            // =========================================

            case CommandType.Forward:

                Debug.Log(
                    "Executing Forward"
                );

                yield return MoveForward();

                break;


            // =========================================
            // TURN LEFT
            // =========================================

            case CommandType.TurnLeft:

                Debug.Log(
                    "Executing Turn Left"
                );

                yield return TurnLeft();

                break;


            // =========================================
            // TURN RIGHT
            // =========================================

            case CommandType.TurnRight:

                Debug.Log(
                    "Executing Turn Right"
                );

                yield return TurnRight();

                break;


            // =========================================
            // EMOTION
            // =========================================

            case CommandType.Emotion:

                Debug.Log(
                    $"Executing Emotion: " +
                    $"{cmd.emotion}"
                );

                // A smiley tile under mTiny counts this feeling
                if (MissionBoardBuilder.Instance != null)
                {
                    MissionBoardBuilder.Instance.CuePlayed(CueKind.Feeling, transform.position);
                }



                yield return ShowEmotion(
                    cmd.emotion
                );

                break;


            // =========================================
            // NOTE
            // =========================================

            case CommandType.Note:

                Debug.Log(
                    $"Executing Note: " +
                    $"{cmd.note}"
                );

                // A music tile under mTiny counts this note
                if (MissionBoardBuilder.Instance != null)
                {
                    MissionBoardBuilder.Instance.CuePlayed(CueKind.Music, transform.position);
                }

                yield return PlayNote(
                    cmd.note
                );

                break;


            // =========================================
            // START FLAG
            // =========================================

            case CommandType.StartFlag:

                Debug.Log(
                    "Start Flag"
                );

                break;


            // =========================================
            // RUN FLAG
            // =========================================

            case CommandType.RunFlag:

                Debug.Log(
                    "Run Flag"
                );

                break;


            // =========================================
            // UNKNOWN
            // =========================================

            default:

                Debug.LogWarning(
                    $"Unknown command type: " +
                    $"{cmd.commandType}"
                );

                break;
        }
    }


    // =========================================================
    // REMOTE CONTROL
    // =========================================================
    // Explore's remote drives mTiny one action at a time, like the
    // real mTiny pen: no start flag, no program, no Run button.
    // =========================================================

    public bool RunSingle(Command cmd)
    {
        if (isExecuting || cmd == null)
        {
            return false;
        }

        StartCoroutine(ExecuteSingle(cmd));
        return true;
    }

    private IEnumerator ExecuteSingle(Command cmd)
    {
        isExecuting = true;
        bumped = false;

        yield return RunStep(cmd);

        isExecuting = false;
        bumped = false;
    }


    // =========================================================
    // FINISH EXECUTION
    // =========================================================

    private void FinishExecution()
    {
        isExecuting = false;

        if (commandManager != null)
        {
            commandManager.SetProgramRunning(false);
        }
    }


    // =========================================================
    // STOP AT GOAL
    // =========================================================
    // Called by GoalZone. The current step finishes, then the
    // remaining commands are skipped.
    // =========================================================

    public void StopAtGoal()
    {
        stopRequested = true;
    }


    // =========================================================
    // EMOTION
    // =========================================================

    public IEnumerator ShowEmotion(
        EmotionType emotion)
    {
        if (commandManager == null)
        {
            Debug.LogError(
                "Cannot show emotion: " +
                "CommandManager is missing!"
            );

            yield break;
        }

        // The emotion card above the game (the board can't cover it)
        if (commandManager.emotionStage != null)
        {
            yield return commandManager.emotionStage.Play(emotion);
            yield break;
        }

        if (commandManager.emotionPanel == null)
        {
            Debug.LogError(
                "Emotion Panel is not assigned " +
                "in CommandManager!"
            );

            yield break;
        }

        if (commandManager.emotionAnimator == null)
        {
            Debug.LogError(
                "Emotion Animator is not assigned " +
                "in CommandManager!"
            );

            yield break;
        }

        // Show emotion panel
        commandManager.emotionPanel.SetActive(true);

        // Trigger correct animation
        switch (emotion)
        {
            case EmotionType.Happy:

                commandManager.emotionAnimator.SetTrigger(
                    "Happy"
                );

                break;


            case EmotionType.Sad:

                commandManager.emotionAnimator.SetTrigger(
                    "Sad"
                );

                break;


            case EmotionType.Angry:

                commandManager.emotionAnimator.SetTrigger(
                    "Angry"
                );

                break;


            case EmotionType.Surprise:

                commandManager.emotionAnimator.SetTrigger(
                    "Surprise"
                );

                break;


            case EmotionType.Dizzy:

                commandManager.emotionAnimator.SetTrigger(
                    "Dizzy"
                );

                break;


            case EmotionType.Sleepy:

                commandManager.emotionAnimator.SetTrigger(
                    "Sleepy"
                );

                break;


            case EmotionType.Playful:

                commandManager.emotionAnimator.SetTrigger(
                    "Playful"
                );

                break;
                        
            
            case EmotionType.Emergency:

                commandManager.emotionAnimator.SetTrigger(
                    "Emergency"
                );

                break;

            case EmotionType.Polices:

                commandManager.emotionAnimator.SetTrigger(
                    "Police"
                );

                break;

            case EmotionType.Crossing:

                commandManager.emotionAnimator.SetTrigger(
                    "Crossing"
                );

                break;

            case EmotionType.Theft:

                commandManager.emotionAnimator.SetTrigger(
                    "Theft"
                );

                break;


            default:

                Debug.LogWarning(
                    $"Unknown emotion: {emotion}"
                );

                break;
        }

        // Keep emotion visible
        yield return new WaitForSeconds(3f);

        // Hide emotion panel
        commandManager.emotionPanel.SetActive(false);
    }


    // =========================================================
    // MOVE FORWARD
    // =========================================================

    private IEnumerator MoveForward()
    {
        if (rb == null)
        {
            Debug.LogError(
                "Cannot move: Rigidbody2D is missing!"
            );

            yield break;
        }

        Vector2 startPos = rb.position;

        // Robot's UP direction is forward
        Vector2 direction = transform.up;

        Vector2 targetPos =
            startPos +
            direction * moveDistance;

        Debug.Log(
            $"Moving from {startPos} " +
            $"to {targetPos}"
        );


        // =========================================================
        // WALL CHECK
        // =========================================================

        float detectionRadius = 0.2f;

        RaycastHit2D wallHit = Physics2D.CircleCast(
            startPos,
            detectionRadius,
            direction,
            moveDistance,
            wallLayer
        );

        if (wallHit.collider != null)
        {
            Debug.Log(
                $"WALL DETECTED: {wallHit.collider.name}"
            );

            bumped = true;

            yield return PlayBumped();

            // Robot stays where it is
            yield break;
        }

        // =====================================================
        // WALKABLE TILE CHECK
        // =====================================================

        Collider2D tile = Physics2D.OverlapPoint(
            targetPos,
            walkableLayer
        );

        if (tile == null)
        {
            Debug.LogWarning(
                $"Blocked! No walkable tile found " +
                $"at {targetPos}"
            );

            bumped = true;

            yield return PlayBumped();

            yield break;
        }

        // =====================================================
        // SMOOTH MOVEMENT
        // =====================================================

        AudioManager.Play(Sfx.Step);

        float t = 0f;

        while (t < 1f)
        {
            t +=
                Time.fixedDeltaTime *
                moveSpeed;

            Vector2 newPosition =
                Vector2.Lerp(
                    startPos,
                    targetPos,
                    t
                );

            rb.MovePosition(newPosition);

            yield return new WaitForFixedUpdate();
        }

        // Make sure robot reaches exact target
        rb.MovePosition(targetPos);

        yield return new WaitForFixedUpdate();

        ClearVelocity();

        Debug.Log(
            "Forward movement completed."
        );


        // =====================================================
        // CHECK SPECIAL TILE
        // =====================================================

        yield return CheckSpecialTile();
    }



    private IEnumerator CheckSpecialTile()
    {
        Collider2D[] tiles = Physics2D.OverlapCircleAll(
            rb.position,
            0.2f
        );

        Debug.Log(
            $"Checking special tile at {rb.position}. " +
            $"Found {tiles.Length} collider(s)."
        );

        foreach (Collider2D tileCollider in tiles)
        {
            if (tileCollider == null)
                continue;

            SpecialTile specialTile =
                tileCollider.GetComponent<SpecialTile>();

            if (specialTile == null)
            {
                specialTile =
                    tileCollider.GetComponentInParent<SpecialTile>();
            }

            if (specialTile == null)
                continue;

            Debug.Log(
                $"SPECIAL TILE FOUND: " +
                $"{specialTile.tileType}"
            );

            switch (specialTile.tileType)
            {
                case TileType.Hospital:

                    Debug.Log("Hospital detected!");

                    yield return ShowEmotion(
                        EmotionType.Emergency
                    );

                    break;


                case TileType.ChildrenCrossing:

                    Debug.Log(
                        "School detected!"
                    );
 
                    yield return ShowEmotion(
                        EmotionType.Crossing
                    );
                    break;



                case TileType.PoliceStation:

                    Debug.Log(
                        "Police Station detected!"
                    );
                    yield return ShowEmotion(
                        EmotionType.Polices
                    );


                    break;

                case TileType.TheftIncident:

                    Debug.Log(
                        "Theft detected!"
                    );

                    yield return ShowEmotion(
                        EmotionType.Theft
                    );

                    break;

                case TileType.BumpedIncident:

                    Debug.Log(
                        "bumped detected!"
                    );

                    yield return PlayBumped();

                    break;

                case TileType.Construction:

                    Debug.Log(
                        "construction detected!"
                    );

                    yield return PlayBumped();

                    break;
            }

            break;
        }
    }






    // =========================================================
    // TURN LEFT
    // =========================================================

    private IEnumerator TurnLeft()
    {
        yield return RotateRobot(90f);
    }


    // =========================================================
    // TURN RIGHT
    // =========================================================

    private IEnumerator TurnRight()
    {
        yield return RotateRobot(-90f);
    }


    // =========================================================
    // ROTATE ROBOT
    // =========================================================

    private IEnumerator RotateRobot(
        float angle)
    {
        Quaternion startRot =
            transform.rotation;

        AudioManager.Play(Sfx.Turn);

        // Snap to a multiple of 90 so small errors never accumulate
        float targetZ =
            Mathf.Round(
                (startRot.eulerAngles.z + angle) / 90f
            ) * 90f;

        Quaternion targetRot =
            Quaternion.Euler(
                0f,
                0f,
                targetZ
            );

        float t = 0f;

        while (t < 1f)
        {
            t +=
                Time.deltaTime *
                moveSpeed;

            transform.rotation =
                Quaternion.Lerp(
                    startRot,
                    targetRot,
                    t
                );

            yield return null;
        }

        transform.rotation = targetRot;

        Debug.Log(
            $"Robot rotated {angle} degrees."
        );
    }


    // =========================================================
    // STOP PROGRAM
    // =========================================================

    public void StopProgram()
    {
        StopAllCoroutines();

        stopRequested = false;

        bumped = false;

        // A note bounce cut short must not leave mTiny bigger
        transform.localScale = baseScale;

        FinishExecution();

        ClearVelocity();

        Debug.Log(
            "Program stopped."
        );
    }


    // =========================================================
    // RESET POSE
    // =========================================================

    public void ResetPose(
        Vector3 position,
        Quaternion rotation)
    {
        position.z = planeZ;

        transform.SetPositionAndRotation(
            position,
            rotation
        );

        if (rb != null)
        {
            rb.position = position;
            rb.rotation = rotation.eulerAngles.z;
        }

        ClearVelocity();
    }


    // =========================================================
    // CLEAR VELOCITY
    // =========================================================

    private void ClearVelocity()
    {
        if (rb == null)
        {
            return;
        }

        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }


    // =========================================================
    // CHECK EXECUTION STATE
    // =========================================================

    public bool IsExecuting()
    {
        return isExecuting;
    }


    // =========================================================
    // KEEP ROBOT ON 2D PLANE
    // =========================================================

    private void LateUpdate()
    {
        Vector3 p = transform.position;

        if (p.z != planeZ)
        {
            p.z = planeZ;

            transform.position = p;
        }
    }

    // =========================================================
    // BUMPED EMOTION
    // =========================================================

    // =========================================================
    // PLAY NOTE
    // =========================================================
    // mTiny sings the note and does a little bounce in time.
    // =========================================================

    [Header("Notes")]
    [Tooltip("Seconds each note block lasts.")]
    public float noteDuration = 0.5f;

    private IEnumerator PlayNote(NoteType note)
    {
        AudioManager.PlayNote(note);

        Vector3 restScale = baseScale;
        float t = 0f;

        while (t < noteDuration)
        {
            t += Time.deltaTime;

            // One quick bounce, then settle
            float bounce = Mathf.Sin(Mathf.Clamp01(t / (noteDuration * 0.6f)) * Mathf.PI);
            transform.localScale = restScale * (1f + 0.12f * bounce);

            yield return null;
        }

        transform.localScale = restScale;
    }


    private IEnumerator PlayBumped()
    {
        // The bumped story has its own bonk; the plain panel gets the uh-oh
        if (commandManager == null || commandManager.emotionStage == null)
        {
            AudioManager.Play(Sfx.Oops);
        }

        if (commandManager == null)
        {
            Debug.LogError(
                "Cannot play Bumped: CommandManager is missing!"
            );

            yield break;
        }

        if (commandManager.emotionStage != null)
        {
            yield return commandManager.emotionStage.PlayBumped();
            yield break;
        }

        if (commandManager.emotionPanel == null)
        {
            Debug.LogError(
                "Cannot play Bumped: Emotion Panel is not assigned!"
            );

            yield break;
        }

        if (commandManager.emotionAnimator == null)
        {
            Debug.LogError(
                "Cannot play Bumped: Emotion Animator is not assigned!"
            );

            yield break;
        }

        Debug.Log(
            "ROBOT BUMPED INTO A WALL!"
        );

        // Show emotion panel
        commandManager.emotionPanel.SetActive(true);

        // Play Bumped animation
        commandManager.emotionAnimator.SetTrigger(
            "Bumped"
        );

        // Keep animation visible
        yield return new WaitForSeconds(3f);

        // Hide emotion panel
        commandManager.emotionPanel.SetActive(false);
    }
}