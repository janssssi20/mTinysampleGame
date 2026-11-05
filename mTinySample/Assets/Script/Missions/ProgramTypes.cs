using System.Collections.Generic;
using UnityEngine;

public enum CommandType
{
    Forward,
    TurnLeft,
    TurnRight,
    StartFlag,
    RunFlag,
    Emotion,

    // Added last: scenes store these as numbers
    Note
}

public enum EmotionType
{
    Happy,
    Sad,
    Angry,
    Surprise,
    Dizzy,
    Sleepy,
    Playful,
    Emergency,
    Polices,
    Crossing,
    Theft
}

// The seven solfège notes of a C major scale, Do = C
public enum NoteType
{
    Do,
    Re,
    Mi,
    Fa,
    Sol,
    La,
    Si
}

[System.Serializable]
public class Command
{
    public CommandType commandType;
    public EmotionType emotion;
    public NoteType note;
    public int repeatCount;

    // Constructor for normal commands
    public Command(CommandType type, int repeat = 1)
    {
        commandType = type;
        emotion = EmotionType.Happy;
        repeatCount = Mathf.Max(1, repeat);
    }

    // Constructor for emotion commands
    public Command(
        CommandType type,
        EmotionType emotionType,
        int repeat = 1)
    {
        commandType = type;
        emotion = emotionType;
        repeatCount = Mathf.Max(1, repeat);
    }

    // Constructor for note commands
    public Command(NoteType noteType, int repeat = 1)
    {
        commandType = CommandType.Note;
        emotion = EmotionType.Happy;
        note = noteType;
        repeatCount = Mathf.Max(1, repeat);
    }
}

// Program edits shared by the sequence bar and its tests.
public static class ProgramEdits
{
    // Every sequence begins with the start flag: the player presses it
    // first, and only then can other blocks be added.
    public static bool CanAddBlock(List<Command> program, CommandType type)
    {
        bool hasStartFlag =
            program != null &&
            program.Count > 0 &&
            program[0].commandType == CommandType.StartFlag;

        return type == CommandType.StartFlag ? !hasStartFlag : hasStartFlag;
    }

    // Takes back the child's last block. A multiplier is always the last
    // icon after the block it repeats, so it is removed first.
    // Returns how many sequence-bar icons to remove (0 or 1).
    public static int Undo(List<Command> program)
    {
        if (program == null || program.Count == 0)
        {
            return 0;
        }

        Command last = program[program.Count - 1];

        if (last.repeatCount > 1)
        {
            last.repeatCount = 1;
            return 1;
        }

        // The start flag is placed by the game, not the child
        if (last.commandType == CommandType.StartFlag)
        {
            return 0;
        }

        program.RemoveAt(program.Count - 1);

        return 1;
    }
}
