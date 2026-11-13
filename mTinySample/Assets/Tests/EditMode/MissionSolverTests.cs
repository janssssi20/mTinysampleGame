using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// Marked tiles move each play: every random layout must be winnable,
// and the solver's best program must follow the rules.
public class MissionSolverTests
{
    private MissionCatalog catalog;

    [SetUp]
    public void SetUp()
    {
        catalog = AssetDatabase.LoadAssetAtPath<MissionCatalog>("Assets/Data/MissionCatalog.asset");
        Assert.IsNotNull(catalog);
    }

    private static MissionDefinition WithCues(MissionDefinition m, List<MissionCue> cues)
    {
        return new MissionDefinition
        {
            id = m.id, stage = m.stage, requireMovement = m.requireMovement, requireMultiplier = m.requireMultiplier,
            requireEmotion = m.requireEmotion, requireSound = m.requireSound, start = m.start, path = m.path,
            goal = m.goal, cityTiles = m.cityTiles, cues = cues
        };
    }

    [Test]
    public void EveryRandomLayout_IsWinnable()
    {
        var failures = new List<string>();
        var rng = new System.Random(1234);

        foreach (MissionDefinition mission in catalog.missions)
        {
            if (mission.cues.Count == 0) continue;

            for (int n = 0; n < 200; n++)
            {
                List<MissionCue> cues = MissionSolver.RandomCues(mission, rng);
                MissionDefinition layout = WithCues(mission, cues);
                string program = MissionSolver.Solve(mission, cues);
                SimResult result = MissionSimulator.Run(layout, program);

                if (result != SimResult.ReachedGoal)
                {
                    failures.Add(mission.id + " [" + program + "]: " + result);
                    break;
                }
            }
        }

        CollectionAssert.IsEmpty(failures);
    }

    [Test]
    public void RandomLayouts_UsePlainRoadOnly()
    {
        var rng = new System.Random(99);

        foreach (MissionDefinition mission in catalog.missions)
        {
            if (mission.cues.Count == 0) continue;
            List<Vector2Int> cells = MissionRoute.Cells(mission);

            for (int n = 0; n < 100; n++)
            {
                List<MissionCue> cues = MissionSolver.RandomCues(mission, rng);
                Assert.AreEqual(mission.cues.Count, cues.Count, mission.id);

                var seen = new HashSet<Vector2Int>();
                for (int c = 0; c < cues.Count; c++)
                {
                    int index = cells.IndexOf(cues[c].cell);
                    Assert.That(index, Is.InRange(2, cells.Count - 2), mission.id + " mark at the ends of the road");
                    Assert.IsFalse(mission.cityTiles.Exists(t => t.cell == cues[c].cell), mission.id + " mark on a city stop");
                    Assert.IsTrue(seen.Add(cues[c].cell), mission.id + " two marks on one tile");
                    Assert.AreEqual(mission.cues[c].kind, cues[c].kind, mission.id + " mark kind changed");
                }
            }
        }
    }

    [Test]
    public void RandomLayouts_Change()
    {
        // Over a few plays the marks really move
        MissionDefinition mission = catalog.missions.Find(m => m.cues.Count > 0);
        var rng = new System.Random(7);
        var places = new HashSet<Vector2Int>();
        for (int n = 0; n < 20; n++) places.Add(MissionSolver.RandomCues(mission, rng)[0].cell);

        Assert.Greater(places.Count, 1);
    }

    [Test]
    public void Solver_MatchesTheCatalogsBest_ForItsOwnLayout()
    {
        // With the catalog's own marks, the solver finds an answer as short as the written one
        foreach (MissionDefinition mission in catalog.missions)
        {
            if (mission.cues.Count == 0) continue;
            int solver = MissionSimulator.CountBlocks(MissionSolver.Solve(mission, mission.cues));
            int written = MissionSimulator.CountBlocks(mission.intendedSolution);
            Assert.AreEqual(written, solver, mission.id);
        }
    }
}
