using System.Collections.Generic;
using NUnit.Framework;

public class ProgramEditsTests
{
    private static List<Command> Program(params Command[] commands)
    {
        return new List<Command>(commands);
    }

    [Test]
    public void Undo_EmptyProgram_DoesNothing()
    {
        List<Command> program = Program();

        Assert.AreEqual(0, ProgramEdits.Undo(program));
        Assert.AreEqual(0, program.Count);
    }

    [Test]
    public void Undo_OnlyStartFlag_KeepsStartFlag()
    {
        List<Command> program = Program(new Command(CommandType.StartFlag));

        Assert.AreEqual(0, ProgramEdits.Undo(program));
        Assert.AreEqual(1, program.Count);
    }

    [Test]
    public void Undo_LastBlock_RemovesItAndOneIcon()
    {
        List<Command> program = Program(new Command(CommandType.StartFlag), new Command(CommandType.Forward));

        Assert.AreEqual(1, ProgramEdits.Undo(program));
        Assert.AreEqual(1, program.Count);
        Assert.AreEqual(CommandType.StartFlag, program[0].commandType);
    }

    [Test]
    public void Undo_LastHasMultiplier_RemovesOnlyTheMultiplier()
    {
        List<Command> program = Program(new Command(CommandType.StartFlag), new Command(CommandType.Forward, 3));

        Assert.AreEqual(1, ProgramEdits.Undo(program));
        Assert.AreEqual(2, program.Count);
        Assert.AreEqual(1, program[1].repeatCount);
    }

    [Test]
    public void Undo_Twice_RemovesMultiplierThenBlock()
    {
        List<Command> program = Program(
            new Command(CommandType.StartFlag),
            new Command(CommandType.Forward, 2),
            new Command(CommandType.Emotion, EmotionType.Happy));

        ProgramEdits.Undo(program);
        Assert.AreEqual(2, program.Count);
        Assert.AreEqual(2, program[1].repeatCount, "multiplier on the earlier block stays");

        ProgramEdits.Undo(program);
        Assert.AreEqual(1, program[1].repeatCount);
    }
}
