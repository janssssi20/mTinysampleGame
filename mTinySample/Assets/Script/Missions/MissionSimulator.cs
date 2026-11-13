using System;
using System.Collections.Generic;
using UnityEngine;

public enum SimResult
{
    ReachedGoal,
    LeftPath,
    EndedShort,
    MissingMovement,
    MissingMultiplier,
    MissingEmotion,
    MissingSound,
    MissedCue,
    BadToken
}

// Walks a written program across a mission's grid the same way the
// robot does in game, so mission data can be checked without play mode.
//
// Tokens: F, L, R, 2x-5x (repeats the previous token), emotion names and
// note names (Do Re Mi Fa Sol La Si HighDo).
// The robot starts facing up; L turns counter-clockwise.
public static class MissionSimulator
{
    private static readonly HashSet<string> EmotionTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Happy", "Sad", "Angry", "Surprise", "Dizzy", "Sleepy", "Playful"
    };

    private static readonly HashSet<string> NoteTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Do", "Re", "Mi", "Fa", "Sol", "La", "Si", "HighDo"
    };

    public static SimResult Run(MissionDefinition mission, string program)
    {
        string[] tokens = Split(program);

        bool movementUsed = false;
        bool multiplierUsed = false;
        bool emotionUsed = false;
        bool soundUsed = false;
        var played = new HashSet<(Vector2Int, CueKind)>();

        Vector2Int position = mission.start;
        Vector2Int facing = Vector2Int.up;

        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i];

            if (IsMultiplier(token))
            {
                // A multiplier is folded into the block before it
                return SimResult.BadToken;
            }

            if (!IsMove(token) && !EmotionTokens.Contains(token) && !NoteTokens.Contains(token))
            {
                return SimResult.BadToken;
            }

            int repeat = 1;

            if (i + 1 < tokens.Length && IsMultiplier(tokens[i + 1]))
            {
                repeat = tokens[i + 1][0] - '0';
                multiplierUsed = true;
                i++;
            }

            if (EmotionTokens.Contains(token))
            {
                emotionUsed = true;
                played.Add((position, CueKind.Feeling));
                continue;
            }

            if (NoteTokens.Contains(token))
            {
                soundUsed = true;
                played.Add((position, CueKind.Music));
                continue;
            }

            movementUsed = true;

            for (int r = 0; r < repeat; r++)
            {
                switch (char.ToUpperInvariant(token[0]))
                {
                    case 'L':
                        facing = new Vector2Int(-facing.y, facing.x);
                        break;

                    case 'R':
                        facing = new Vector2Int(facing.y, -facing.x);
                        break;

                    default:
                        position += facing;

                        if (!mission.IsWalkable(position))
                        {
                            return SimResult.LeftPath;
                        }

                        if (position == mission.goal)
                        {
                            SimResult result = CheckRequirements(mission, movementUsed, multiplierUsed, emotionUsed, soundUsed);

                            if (result == SimResult.ReachedGoal && mission.cues != null)
                            {
                                foreach (MissionCue cue in mission.cues)
                                {
                                    if (cue != null && !played.Contains((cue.cell, cue.kind)))
                                    {
                                        return SimResult.MissedCue;
                                    }
                                }
                            }

                            return result;
                        }

                        break;
                }
            }
        }

        return SimResult.EndedShort;
    }

    public static int CountBlocks(string program)
    {
        return Split(program).Length;
    }

    private static SimResult CheckRequirements(
        MissionDefinition mission, bool movementUsed, bool multiplierUsed, bool emotionUsed, bool soundUsed)
    {
        if (mission.requireMovement && !movementUsed)
        {
            return SimResult.MissingMovement;
        }

        if (mission.requireMultiplier && !multiplierUsed)
        {
            return SimResult.MissingMultiplier;
        }

        if (mission.requireEmotion && !emotionUsed)
        {
            return SimResult.MissingEmotion;
        }

        if (mission.requireSound && !soundUsed)
        {
            return SimResult.MissingSound;
        }

        return SimResult.ReachedGoal;
    }

    private static string[] Split(string program)
    {
        if (string.IsNullOrWhiteSpace(program))
        {
            return new string[0];
        }

        return program.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
    }

    private static bool IsMove(string token)
    {
        return token.Length == 1 && "FLRflr".IndexOf(token[0]) >= 0;
    }

    private static bool IsMultiplier(string token)
    {
        return token.Length == 2 &&
               token[0] >= '2' && token[0] <= '5' &&
               (token[1] == 'x' || token[1] == 'X');
    }
}
