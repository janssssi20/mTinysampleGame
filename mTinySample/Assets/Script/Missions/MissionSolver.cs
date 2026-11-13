using System.Collections.Generic;
using UnityEngine;

// Marked tiles move to new random road tiles each time a level starts.
// This picks those tiles and works out the shortest program for the
// layout (used for the stars and the goal card's "Best" count).
public static class MissionSolver
{
    private const int MaxRepeat = 5;

    // New random places for a mission's marked tiles: plain road only
    // (not the start, the finish, the first step or a city stop), one per
    // tile. Layouts whose best program needs no multiplier are skipped
    // when the mission requires one.
    public static List<MissionCue> RandomCues(MissionDefinition mission, System.Random rng)
    {
        List<MissionCue> fallback = Copy(mission.cues);
        if (mission.cues == null || mission.cues.Count == 0) return fallback;

        List<Vector2Int> cells = MissionRoute.Cells(mission);
        var eligible = new List<Vector2Int>();
        for (int i = 2; i < cells.Count - 1; i++)
        {
            if (!mission.cityTiles.Exists(t => t != null && t.cell == cells[i])) eligible.Add(cells[i]);
        }
        if (eligible.Count < mission.cues.Count) return fallback;

        for (int attempt = 0; attempt < 30; attempt++)
        {
            var pool = new List<Vector2Int>(eligible);
            var cues = new List<MissionCue>();
            foreach (MissionCue cue in mission.cues)
            {
                int pick = rng.Next(pool.Count);
                cues.Add(new MissionCue { cell = pool[pick], kind = cue.kind });
                pool.RemoveAt(pick);
            }

            if (!mission.requireMultiplier || Solve(mission, cues).Contains("x"))
            {
                return cues;
            }
        }

        return fallback;
    }

    // Shortest program along the road: straight runs use multipliers (up
    // to 5x), and a feeling or note is played on each marked tile
    public static string Solve(MissionDefinition mission, IList<MissionCue> cues)
    {
        List<Vector2Int> cells = MissionRoute.Cells(mission);
        var tokens = new List<string>();
        Vector2Int facing = Vector2Int.up;
        int run = 0;

        for (int i = 1; i < cells.Count; i++)
        {
            Vector2Int step = cells[i] - cells[i - 1];
            if (step != facing)
            {
                Flush(tokens, ref run);
                if (step == new Vector2Int(-facing.y, facing.x)) tokens.Add("L");
                else if (step == new Vector2Int(facing.y, -facing.x)) tokens.Add("R");
                else { tokens.Add("R"); tokens.Add("R"); }
                facing = step;
            }

            run++;

            if (i == cells.Count - 1 || cues == null) continue;

            foreach (MissionCue cue in cues)
            {
                if (cue == null || cue.cell != cells[i]) continue;
                Flush(tokens, ref run);
                tokens.Add(cue.kind == CueKind.Music ? "Do" : "Happy");
            }
        }

        Flush(tokens, ref run);
        return string.Join(" ", tokens);
    }

    private static void Flush(List<string> tokens, ref int run)
    {
        while (run > 0)
        {
            int k = Mathf.Min(run, MaxRepeat);
            tokens.Add("F");
            if (k > 1) tokens.Add(k + "x");
            run -= k;
        }
    }

    private static List<MissionCue> Copy(List<MissionCue> cues)
    {
        var copy = new List<MissionCue>();
        if (cues == null) return copy;
        foreach (MissionCue c in cues) if (c != null) copy.Add(new MissionCue { cell = c.cell, kind = c.kind });
        return copy;
    }
}
