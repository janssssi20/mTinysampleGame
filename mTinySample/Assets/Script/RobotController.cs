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


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

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

    // =========================================================
    // HIDE COMMAND SEQUENCE UI
    // =========================================================

    commandManager.HideSequenceUI();

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

        Debug.Log(
            "========== PROGRAM STARTED =========="
        );

        // Iterate over a copy so UI clicks during execution
        // can never modify the list being executed.
        Command[] program = commandManager.commandList.ToArray();

        foreach (Command cmd in program)
        {
            if (stopRequested)
            {
                break;
            }

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
            for (int i = 0; i < repeatCount && !stopRequested; i++)
            {
                Debug.Log(
                    $"Execution {i + 1}/{repeatCount}: " +
                    $"{cmd.commandType}"
                );

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



                        yield return ShowEmotion(
                            cmd.emotion
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

            FinishExecution();

            yield break;
        }

        // =====================================================
        // PROGRAM ENDED WITHOUT REACHING THE GOAL
        // =====================================================

        Debug.Log(
            "Program finished without reaching the goal."
        );

        yield return ShowEmotion(EmotionType.Sad);

        FinishExecution();

        // Let the player edit the program or press Reset
        commandManager.ShowSequenceUI();
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

            yield return PlayBumped();

            yield break;
        }

        // =====================================================
        // SMOOTH MOVEMENT
        // =====================================================

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
        position.z = 0f;

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

        if (p.z != 0f)
        {
            p.z = 0f;

            transform.position = p;
        }
    }

    // =========================================================
    // BUMPED EMOTION
    // =========================================================

    private IEnumerator PlayBumped()
    {
        if (commandManager == null)
        {
            Debug.LogError(
                "Cannot play Bumped: CommandManager is missing!"
            );

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