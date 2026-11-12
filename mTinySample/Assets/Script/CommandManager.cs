using System.Collections.Generic;
using UnityEngine;
using TMPro;

// CommandType, EmotionType and Command live in Missions/ProgramTypes.cs

public class CommandManager : MonoBehaviour
{
    public static CommandManager Instance;

    // =========================================================
    // COMMAND LIST
    // =========================================================

    [Header("Command List")]
    public List<Command> commandList = new List<Command>();

    private SequenceUIManager uiManager;

    // =========================================================
    // COMMAND SEQUENCE UI
    // =========================================================

    [Header("Command Sequence UI")]
    [Tooltip("The GameObject containing the command sequence UI.")]
    public GameObject sequenceUI;

    // =========================================================
    // COMMAND ICONS
    // =========================================================

    [Header("Command Icons")]
    public Sprite forwardIcon;
    public Sprite leftIcon;
    public Sprite rightIcon;
    public Sprite StartFlagIcon;
    public Sprite RunFlagIcon;

    // =========================================================
    // MULTIPLIER ICONS
    // =========================================================

    [Header("Multiplier Icons")]
    public Sprite multiplier2xIcon;
    public Sprite multiplier3xIcon;
    public Sprite multiplier4xIcon;
    public Sprite multiplier5xIcon;

    // =========================================================
    // EMOTION ICONS
    // =========================================================

    [Header("Emotion Icons")]
    public Sprite happyIcon;
    public Sprite sadIcon;
    public Sprite angryIcon;
    public Sprite surpriseIcon;
    public Sprite dizzyIcon;
    public Sprite sleepyIcon;
    public Sprite playfulIcon;

    // =========================================================
    // NOTE ICONS
    // =========================================================

    [Header("Note Icons")]
    [Tooltip("One per note, in NoteType order: Do Re Mi Fa Sol La Si HighDo.")]
    public Sprite[] noteIcons = new Sprite[8];

    // =========================================================
    // EMOTION PANEL
    // =========================================================

    [Header("Emotion Panel")]
    public GameObject emotionPanel;
    public Animator emotionAnimator;
    [Tooltip("Plays emotions and special-tile stories on a card above the game.")]
    public EmotionStage emotionStage;

    // =========================================================
    // INSTRUCTION PANEL
    // =========================================================

    [Header("Instruction Panel")]
    public GameObject instructionPanel;

    // =========================================================
    // PLAY MODE
    // =========================================================

    [Header("Play Mode")]
    [Tooltip("Explore: no timer, no stars, no sad ending, never locks.")]
    public bool freePlay = false;

    [Tooltip("Shown after a run that didn't reach the flag.")]
    public TryAgainBanner tryAgainBanner;

    // Fewest blocks that solve this mission (from its intended
    // solution). When set, stars count blocks instead of time.
    private int bestBlocks = 0;

    // =========================================================
    // TIMER
    // =========================================================

    [Header("Game Timer")]
    public TMP_Text timerText;

    private float elapsedTime = 0f;

    private bool gameStarted = false;

    private bool gameFinished = false;

    // =========================================================
    // FINISH PANEL
    // =========================================================

    [Header("Finish Panel")]
    public GameObject finishPanel;

    [Tooltip("TMP text that displays the final completion time.")]
    public TMP_Text finishTimeText;

    // =========================================================
    // STAR RATING
    // =========================================================

    [Header("Star Rating")]
    [Tooltip("Prefab used to display one earned star.")]
    public GameObject starPrefab;

    [Tooltip("Parent Transform where generated stars will appear.")]
    public Transform starContainer;


    // Stores generated stars
    private List<GameObject> generatedStars =
        new List<GameObject>();

    // =========================================================
    // PROGRAM STATE
    // =========================================================

    // True while the robot is executing; blocks program edits
    private bool programRunning = false;

    // True when the last block in the sequence UI is a multiplier
    private bool lastBlockIsMultiplier = false;


    // =========================================================
    // CUSTOM CURSOR
    // =========================================================

    [Header("Custom Cursor")]
    public Texture2D cursorTexture;

    [Tooltip("Pixel position of the pointer inside the cursor image.")]
    public Vector2 cursorHotspot = new Vector2(25, 80);

    [Tooltip("Cursor rendering mode.")]
    public CursorMode cursorMode = CursorMode.Auto;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        // Scene-scoped: the newest scene's manager takes over. Never destroy
        // the GameObject here; it carries other managers too.

        Instance = this;
    }


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {

        SetCustomCursor();

        uiManager =
            FindFirstObjectByType<SequenceUIManager>();

        if (uiManager == null)
        {
            Debug.LogError(
                "CommandManager: SequenceUIManager not found in scene!"
            );
        }

        // ---------------------------------------------------------
        // FINISH PANEL
        // ---------------------------------------------------------

        if (finishPanel != null)
        {
            finishPanel.SetActive(false);
        }

        // ---------------------------------------------------------
        // EMOTION PANEL
        // ---------------------------------------------------------

        if (emotionPanel != null)
        {
            emotionPanel.SetActive(false);
        }

        // ---------------------------------------------------------
        // INSTRUCTION PANEL
        // ---------------------------------------------------------

        if (instructionPanel != null)
        {
            instructionPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning(
                "CommandManager: Instruction Panel is not assigned!"
            );
        }

        // ---------------------------------------------------------
        // SEQUENCE UI
        // ---------------------------------------------------------

        if (sequenceUI != null)
        {
            sequenceUI.SetActive(true);
        }
        else
        {
            Debug.LogWarning(
                "CommandManager: Sequence UI is not assigned!"
            );
        }

        // ---------------------------------------------------------
        // TIMER
        // ---------------------------------------------------------

        elapsedTime = 0f;

        gameStarted = false;

        gameFinished = false;

        UpdateTimerUI();

        // ---------------------------------------------------------
        // CLEAR STARS
        // ---------------------------------------------------------

        ClearGeneratedStars();

        HideTryAgain();
    }


    // =========================================================
    // SET CUSTOM CURSOR
    // =========================================================

    private void SetCustomCursor()
    {
        if (cursorTexture == null)
        {
            Debug.LogWarning(
                "CommandManager: Cursor Texture is not assigned!"
            );

            return;
        }

        Cursor.SetCursor(
            cursorTexture,
            cursorHotspot,
            cursorMode
        );

        Debug.Log(
            $"Custom cursor applied. " +
            $"Hotspot: {cursorHotspot}"
        );
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // Timer only runs after StartGame()
        if (gameStarted && !gameFinished)
        {
            elapsedTime += Time.unscaledDeltaTime;

            UpdateTimerUI();
        }
    }


    // =========================================================
    // START GAME
    // =========================================================

    public void StartGame()
    {
        // Prevent multiple starts
        if (gameStarted)
        {
            return;
        }

        // Close instruction panel
        if (instructionPanel != null)
        {
            instructionPanel.SetActive(false);
        }

        // Make sure finish panel is hidden
        if (finishPanel != null)
        {
            finishPanel.SetActive(false);
        }

        // Make sure sequence UI is visible
        if (sequenceUI != null)
        {
            sequenceUI.SetActive(true);
        }

        // Clear previous stars
        ClearGeneratedStars();

        // Reset timer
        elapsedTime = 0f;

        // Start game
        gameStarted = true;

        gameFinished = false;

        // Immediately update timer
        UpdateTimerUI();

        Debug.Log(
            "Game started. Timer is now running."
        );
    }


    // =========================================================
    // HIDE SEQUENCE UI
    // =========================================================
    // Call this when command execution begins.
    // =========================================================

    public void HideSequenceUI()
    {
        if (sequenceUI != null)
        {
            sequenceUI.SetActive(false);

            Debug.Log(
                "Command Sequence UI hidden."
            );
        }
        else
        {
            Debug.LogWarning(
                "CommandManager: Sequence UI is not assigned!"
            );
        }
    }


    // =========================================================
    // SHOW SEQUENCE UI
    // =========================================================
    // Called when the Reset button is pressed.
    // =========================================================

    public void ShowSequenceUI()
    {
        if (sequenceUI != null)
        {
            sequenceUI.SetActive(true);

            Debug.Log(
                "Command Sequence UI shown."
            );
        }
        else
        {
            Debug.LogWarning(
                "CommandManager: Sequence UI is not assigned!"
            );
        }
    }


    // =========================================================
    // UPDATE TIMER UI
    // =========================================================

    private void UpdateTimerUI()
    {
        if (timerText == null)
        {
            return;
        }

        // No clock for Explore or block-starred missions: young
        // children shouldn't feel rushed
        bool showTimer = !freePlay && bestBlocks <= 0;

        if (timerText.gameObject.activeSelf != showTimer)
        {
            timerText.gameObject.SetActive(showTimer);
        }

        timerText.text =
            FormatTime(elapsedTime);
    }


    // =========================================================
    // FORMAT TIME
    // =========================================================

    private string FormatTime(float time)
    {
        int minutes =
            Mathf.FloorToInt(time / 60f);

        int seconds =
            Mathf.FloorToInt(time % 60f);

        return string.Format(
            "{0:00}:{1:00}",
            minutes,
            seconds
        );
    }


    // =========================================================
    // GET ELAPSED TIME
    // =========================================================

    public float GetElapsedTime()
    {
        return elapsedTime;
    }


 // =========================================================
// ADD NORMAL COMMAND (Defaults to 1x initially)
// =========================================================

    public void AddCommand(CommandType type)
    {
        if (!CanEditProgram())
        {
            return;
        }

        if (!ProgramEdits.CanAddBlock(commandList, type))
        {
            Debug.LogWarning(
                $"Cannot add {type}: press the start flag first."
            );
            return;
        }

        if (type == CommandType.Emotion)
        {
            Debug.LogWarning(
                "AddCommand(CommandType.Emotion) was called. " +
                "Use AddEmotion(EmotionType) instead."
            );
            return;
        }

        // Add command with a default repeat count of 1
        Command newCommand = new Command(type, 1);
        commandList.Add(newCommand);

        // Add command visual block
        AddCommandVisual(type);

        lastBlockIsMultiplier = false;

        Debug.Log($"Added command: {type} x1");
    }


    // =========================================================
    // ADD EMOTION COMMAND (Defaults to 1x initially)
    // =========================================================

    public void AddEmotion(EmotionType emotionType)
    {
        if (!CanEditProgram())
        {
            return;
        }

        if (!ProgramEdits.CanAddBlock(commandList, CommandType.Emotion))
        {
            Debug.LogWarning(
                "Cannot add an emotion: press the start flag first."
            );
            return;
        }

        // Store emotion command with a default repeat count of 1
        Command newCommand = new Command(
            CommandType.Emotion,
            emotionType,
            1
        );

        commandList.Add(newCommand);

        // Add emotion visual block
        AddEmotionVisual(emotionType);

        lastBlockIsMultiplier = false;

        Debug.Log($"Added emotion: {emotionType} x1");
    }


    // =========================================================
    // ADD NOTE COMMAND (Defaults to 1x initially)
    // =========================================================

    public void AddNote(NoteType note)
    {
        if (!CanEditProgram())
        {
            return;
        }

        if (!ProgramEdits.CanAddBlock(commandList, CommandType.Note))
        {
            Debug.LogWarning(
                "Cannot add a note: press the start flag first."
            );
            return;
        }

        commandList.Add(new Command(note, 1));

        // Let the child hear the note they picked
        AudioManager.PlayNote(note);

        int index = (int)note;
        Sprite icon = noteIcons != null && index < noteIcons.Length ? noteIcons[index] : null;

        if (uiManager != null && icon != null)
        {
            uiManager.AddVisualBlock(icon);
        }
        else if (icon == null)
        {
            Debug.LogWarning($"No icon assigned for note: {note}");
        }

        lastBlockIsMultiplier = false;

        Debug.Log($"Added note: {note} x1");
    }


    // =========================================================
    // SET MULTIPLIER (Modifies the PREVIOUS command)
    // =========================================================

    public void SetMultiplier(int multiplier)
    {
        if (!CanEditProgram())
        {
            return;
        }

        // Make sure there is a command to modify
        if (commandList.Count == 0)
        {
            Debug.LogWarning(
                "Cannot apply multiplier: No previous command found in the list!"
            );
            return;
        }

        int clampedMultiplier = Mathf.Clamp(multiplier, 1, 5);

        // Get the last command added to the list
        Command lastCommand = commandList[commandList.Count - 1];

        // Start and Run flags cannot be multiplied
        if (lastCommand.commandType == CommandType.StartFlag ||
            lastCommand.commandType == CommandType.RunFlag)
        {
            Debug.LogWarning("Start and Run flags cannot be multiplied!");
            return;
        }

        // Update the repeat count of the previous command
        lastCommand.repeatCount = clampedMultiplier;

        // Display the multiplier visual block in the UI sequence
        if (uiManager != null)
        {
            // Pressing another multiplier replaces the previous
            // multiplier block instead of stacking a second one
            if (lastBlockIsMultiplier)
            {
                uiManager.RemoveLastBlock();

                lastBlockIsMultiplier = false;
            }

            Sprite icon = GetMultiplierIcon(clampedMultiplier);

            if (icon != null)
            {
                uiManager.AddVisualBlock(icon);

                lastBlockIsMultiplier = true;
            }
        }

        Debug.Log(
            $"Multiplier {clampedMultiplier}x applied to previous command: {lastCommand.commandType}"
        );
    }


    // =========================================================
    // UI - COMMAND
    // =========================================================

    private void AddCommandVisual(
        CommandType type)
    {
        if (uiManager == null)
        {
            return;
        }

        Sprite icon =
            GetCommandIcon(type);

        if (icon != null)
        {
            uiManager.AddVisualBlock(icon);
        }
        else
        {
            Debug.LogWarning(
                $"No icon assigned for command type: {type}"
            );
        }
    }


    // =========================================================
    // UI - EMOTION
    // =========================================================

    private void AddEmotionVisual(
        EmotionType emotionType)
    {
        if (uiManager == null)
        {
            return;
        }

        Sprite icon =
            GetEmotionIcon(
                emotionType
            );

        if (icon != null)
        {
            uiManager.AddVisualBlock(icon);
        }
        else
        {
            Debug.LogWarning(
                $"No icon assigned for emotion: {emotionType}"
            );
        }
    }


    // =========================================================
    // GET COMMAND ICON
    // =========================================================

    private Sprite GetCommandIcon(
        CommandType type)
    {
        switch (type)
        {
            case CommandType.StartFlag:
                return StartFlagIcon;

            case CommandType.Forward:
                return forwardIcon;

            case CommandType.TurnLeft:
                return leftIcon;

            case CommandType.TurnRight:
                return rightIcon;

            case CommandType.RunFlag:
                return RunFlagIcon;

            default:
                return null;
        }
    }


    // =========================================================
    // GET MULTIPLIER ICON
    // =========================================================

    private Sprite GetMultiplierIcon(
        int multiplier)
    {
        switch (multiplier)
        {
            case 2:
                return multiplier2xIcon;

            case 3:
                return multiplier3xIcon;

            case 4:
                return multiplier4xIcon;

            case 5:
                return multiplier5xIcon;

            default:
                return null;
        }
    }


    // =========================================================
    // GET EMOTION ICON
    // =========================================================

    private Sprite GetEmotionIcon(
        EmotionType emotionType)
    {
        switch (emotionType)
        {
            case EmotionType.Happy:
                return happyIcon;

            case EmotionType.Sad:
                return sadIcon;

            case EmotionType.Angry:
                return angryIcon;

            case EmotionType.Surprise:
                return surpriseIcon;

            case EmotionType.Dizzy:
                return dizzyIcon;

            case EmotionType.Sleepy:
                return sleepyIcon;

            case EmotionType.Playful:
                return playfulIcon;

            default:
                return null;
        }
    }


    // =========================================================
    // UNDO LAST BLOCK
    // =========================================================
    // Called by the Undo button. Takes back the last block (or its
    // multiplier) without clearing the whole program.
    // =========================================================

    public void RemoveLastBlock()
    {
        if (!CanEditProgram())
        {
            return;
        }

        int iconsToRemove = ProgramEdits.Undo(commandList);

        if (iconsToRemove > 0 && uiManager != null)
        {
            uiManager.RemoveLastBlock();
        }

        lastBlockIsMultiplier =
            commandList.Count > 0 &&
            commandList[commandList.Count - 1].repeatCount > 1;
    }


    // =========================================================
    // SHOW INSTRUCTION PANEL
    // =========================================================

    public void ShowInstructionPanel()
    {
        if (instructionPanel != null)
        {
            instructionPanel.SetActive(true);
        }
    }


    // =========================================================
    // CLEAR COMMANDS
    // =========================================================

    public void ClearCommands()
    {
        commandList.Clear();

        lastBlockIsMultiplier = false;

        if (uiManager != null)
        {
            uiManager.ClearUI();
        }

        Debug.Log(
            "All commands cleared."
        );
    }


    // =========================================================
    // GET COMMANDS
    // =========================================================

    public List<Command> GetCommands()
    {
        return commandList;
    }


    // =========================================================
    // GET COMMAND COUNT
    // =========================================================

    public int GetCommandCount()
    {
        return commandList.Count;
    }


    // =========================================================
    // HAS RUNNABLE COMMANDS
    // =========================================================
    // Flags alone do not make a program.
    // =========================================================

    public bool HasRunnableCommands()
    {
        foreach (Command command in commandList)
        {
            if (command != null &&
                command.commandType != CommandType.StartFlag &&
                command.commandType != CommandType.RunFlag)
            {
                return true;
            }
        }

        return false;
    }


    // =========================================================
    // PROGRAM RUNNING STATE
    // =========================================================

    public void SetProgramRunning(bool running)
    {
        programRunning = running;
    }


    public bool IsProgramRunning()
    {
        return programRunning;
    }


    // =========================================================
    // CAN EDIT PROGRAM
    // =========================================================

    private bool CanEditProgram()
    {
        if (!gameStarted || gameFinished)
        {
            Debug.LogWarning(
                "Cannot edit program: press Start first."
            );

            return false;
        }

        if (programRunning)
        {
            Debug.LogWarning(
                "Cannot edit program while the robot is running."
            );

            return false;
        }

        // The child is fixing their program: the red block and the
        // Try again card from the last run no longer apply
        ClearHighlight();
        HideTryAgain();

        return true;
    }


    // =========================================================
    // EMOTION PANEL
    // =========================================================

    public void OpenEmotionPanel()
    {
        if (emotionPanel != null)
        {
            emotionPanel.SetActive(true);
        }
    }


    public void CloseEmotionPanel()
    {
        if (emotionPanel != null)
        {
            emotionPanel.SetActive(false);
        }
    }


    // =========================================================
    // FINISH GAME
    // =========================================================
    // Returns the stars awarded, or 0 if the level was already
    // finished.
    // =========================================================

    public int ShowFinishPanel()
    {
        // Prevent duplicate finish calls
        if (gameFinished)
        {
            return 0;
        }

        // Stop timer
        gameFinished = true;

        gameStarted = false;

        // Calculate stars: by blocks used when the mission knows its
        // best answer, otherwise by time (the old single-mission scenes)
        int blocksUsed = ProgramEdits.CountBlocks(commandList);

        int stars = bestBlocks > 0
            ? StarRules.ForBlocks(blocksUsed, bestBlocks)
            : CalculateStarRating(elapsedTime);

        if (finishTimeText != null)
        {
            finishTimeText.text = bestBlocks > 0
                ? "Blocks: " + blocksUsed
                : "Time: " + FormatTime(elapsedTime);
        }

        // Generate star prefabs
        GenerateStars(stars);

        // Show finish panel
        if (finishPanel != null)
        {
            finishPanel.SetActive(true);
        }

        Debug.Log(
            $"Game finished! " +
            $"Time: {FormatTime(elapsedTime)} | " +
            $"Stars: {stars}"
        );

        return stars;
    }


    // =========================================================
    // CALCULATE STAR RATING
    // =========================================================

    private int CalculateStarRating(
        float time)
    {
        // Under 1:30
        // 3 stars
        if (time < 90f)
        {
            return 3;
        }

        // 1:30 to 2:00
        // 2 stars
        if (time <= 120f)
        {
            return 2;
        }

        // Over 2:00
        // 1 star
        return 1;
    }


    // =========================================================
    // GENERATE STAR PREFABS
    // =========================================================

    private void GenerateStars(
        int starCount)
    {
        // Remove existing stars first
        ClearGeneratedStars();

        // Make sure prefab exists
        if (starPrefab == null)
        {
            Debug.LogError(
                "CommandManager: " +
                "Star Prefab is not assigned!"
            );

            return;
        }

        // Make sure container exists
        if (starContainer == null)
        {
            Debug.LogError(
                "CommandManager: " +
                "Star Container is not assigned!"
            );

            return;
        }

        // Generate earned stars
        for (int i = 0;
             i < starCount;
             i++)
        {
            GameObject newStar =
                Instantiate(
                    starPrefab,
                    starContainer
                );

            // Reset UI transform
            RectTransform rect =
                newStar.GetComponent<
                    RectTransform>();

            if (rect != null)
            {
                rect.localScale =
                    Vector3.one;

                rect.localRotation =
                    Quaternion.identity;
            }

            generatedStars.Add(
                newStar
            );
        }

        Debug.Log(
            $"Generated {starCount} star prefab(s)."
        );
    }


    // =========================================================
    // CLEAR GENERATED STARS
    // =========================================================

    private void ClearGeneratedStars()
    {
        // Destroy stars created by this script
        for (int i =
             generatedStars.Count - 1;
             i >= 0;
             i--)
        {
            if (generatedStars[i] != null)
            {
                Destroy(
                    generatedStars[i]
                );
            }
        }

        generatedStars.Clear();

        // Clear existing children
        if (starContainer != null)
        {
            for (int i =
                 starContainer.childCount - 1;
                 i >= 0;
                 i--)
            {
                Destroy(
                    starContainer
                        .GetChild(i)
                        .gameObject
                );
            }
        }
    }


    // =========================================================
    // HIDE FINISH PANEL
    // =========================================================

    public void HideFinishPanel()
    {
        if (finishPanel != null)
        {
            finishPanel.SetActive(false);
        }
    }


    // =========================================================
    // RESET AFTER FINISH
    // =========================================================
    // Called by Reset. A run in progress keeps its timer, but a
    // finished level is cleared so it can be played again.
    // =========================================================

    public void ResetAfterFinish()
    {
        if (!gameFinished)
        {
            return;
        }

        elapsedTime = 0f;

        gameStarted = false;

        gameFinished = false;

        ClearGeneratedStars();

        HideFinishPanel();

        UpdateTimerUI();
    }


    // =========================================================
    // RESET GAME
    // =========================================================

    public void ResetGameTimer()
    {
        elapsedTime = 0f;

        gameStarted = false;

        gameFinished = false;

        // Clear stars
        ClearGeneratedStars();

        // Hide finish panel
        if (finishPanel != null)
        {
            finishPanel.SetActive(false);
        }

        // Show instruction panel
        if (instructionPanel != null)
        {
            instructionPanel.SetActive(true);
        }

        // Show command sequence UI again
        ShowSequenceUI();

        // Reset timer display
        UpdateTimerUI();

        Debug.Log(
            "Game timer reset."
        );
    }


    // =========================================================
    // BEST BLOCKS
    // =========================================================
    // Set by MissionBoardBuilder for the mission being played.
    // =========================================================

    public void SetBestBlocks(int blocks)
    {
        bestBlocks = blocks;
        UpdateTimerUI();
    }


    // =========================================================
    // RUN HIGHLIGHT
    // =========================================================

    public void HighlightCommand(int commandIndex, bool problem = false)
    {
        if (uiManager == null || commandIndex < 0 || commandIndex >= commandList.Count)
        {
            return;
        }

        int first, count;
        ProgramEdits.IconSpan(commandList, commandIndex, out first, out count);
        uiManager.Highlight(first, count, problem);
    }

    public void ClearHighlight()
    {
        if (uiManager != null)
        {
            uiManager.ClearHighlight();
        }
    }


    // =========================================================
    // TRY AGAIN
    // =========================================================

    public void ShowTryAgain(TryAgainReason reason)
    {
        if (tryAgainBanner != null && !freePlay)
        {
            tryAgainBanner.Show(reason);
        }
    }

    public void HideTryAgain()
    {
        if (tryAgainBanner != null)
        {
            tryAgainBanner.Hide();
        }
    }


    // =========================================================
    // CHECK GAME STARTED
    // =========================================================

    public bool IsGameStarted()
    {
        return gameStarted;
    }


    // =========================================================
    // CHECK GAME FINISHED
    // =========================================================

    public bool IsGameFinished()
    {
        return gameFinished;
    }
}