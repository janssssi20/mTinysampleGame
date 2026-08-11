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
    // PROGRAM SETTINGS
    // =========================================================

    [Header("Program Settings")]
    public float commandDelay = 0.2f;

    private bool isExecuting = false;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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

        if (commandManager.commandList == null ||
            commandManager.commandList.Count == 0)
        {
            Debug.LogWarning(
                "Cannot run program: Command list is empty!"
            );

            return;
        }

        Debug.Log(
            $"Starting program with " +
            $"{commandManager.commandList.Count} commands."
        );

        StartCoroutine(ExecuteCommands());
    }


    // =========================================================
    // EXECUTE COMMANDS
    // =========================================================

    private IEnumerator ExecuteCommands()
    {
        isExecuting = true;

        Debug.Log(
            "========== PROGRAM STARTED =========="
        );

        foreach (Command cmd in commandManager.commandList)
        {
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
            for (int i = 0; i < repeatCount; i++)
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

        isExecuting = false;

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

            commandManager.emotionPanel.SetActive(true);
             
            commandManager.emotionAnimator.SetTrigger("Bumped");
            yield return new WaitForSeconds(3f);

            // Hide emotion panel
            commandManager.emotionPanel.SetActive(false);


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

        Debug.Log(
            "Forward movement completed."
        );
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

        Quaternion targetRot =
            startRot *
            Quaternion.Euler(
                0f,
                0f,
                angle
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

        isExecuting = false;

        Debug.Log(
            "Program stopped."
        );
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
}