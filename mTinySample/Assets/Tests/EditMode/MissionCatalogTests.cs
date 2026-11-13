using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class MissionCatalogTests
{
    private const string CatalogPath = "Assets/Data/MissionCatalog.asset";

    // Board tiles (start + path + goal) per stage (owner's request)
    private static readonly int[] MinTiles = { 6, 8, 10, 12, 15 };
    private static readonly int[] MaxTiles = { 8, 10, 12, 15, 20 };

    // Most blocks a stage's best answer may use
    private static readonly int[] BlockLimit = { 11, 9, 12, 17, 22 };

    // Boards stay small enough for the fixed Top view
    private const int MaxColumns = 7;
    private const int MaxRows = 4;

    private MissionCatalog catalog;

    [SetUp]
    public void SetUp()
    {
        catalog = AssetDatabase.LoadAssetAtPath<MissionCatalog>(CatalogPath);
        Assert.IsNotNull(catalog, "Mission catalog asset is missing at " + CatalogPath);
    }

    [Test]
    public void Catalog_Has50Missions_InPlayOrder()
    {
        List<string> expected = new List<string>();
        for (int s = 1; s <= 5; s++)
        {
            for (int n = 1; n <= 10; n++) expected.Add($"S{s}-{n:00}");
        }

        List<string> ids = new List<string>();
        foreach (MissionDefinition mission in catalog.missions) ids.Add(mission.id);

        CollectionAssert.AreEqual(expected, ids);
    }

    [Test]
    public void Catalog_TenPerStage_InStageOrder()
    {
        for (int s = 0; s < MissionStages.Count; s++)
        {
            List<int> indices = catalog.IndicesInStage((MissionStage)s);
            Assert.AreEqual(10, indices.Count, "Stage " + (s + 1));
            Assert.AreEqual(s * 10, indices[0], "Stage " + (s + 1) + " starts at the wrong place");
        }
    }

    [Test]
    public void EveryIntendedSolution_ReachesGoal()
    {
        List<string> failures = new List<string>();

        foreach (MissionDefinition mission in catalog.missions)
        {
            SimResult result = MissionSimulator.Run(mission, mission.intendedSolution);

            if (result != SimResult.ReachedGoal)
            {
                failures.Add(mission.id + ": " + result);
            }
        }

        CollectionAssert.IsEmpty(failures);
    }

    [Test]
    public void EveryMission_RespectsBlockLimits()
    {
        foreach (MissionDefinition mission in catalog.missions)
        {
            int blocks = MissionSimulator.CountBlocks(mission.intendedSolution);

            Assert.LessOrEqual(blocks, BlockLimit[(int)mission.stage], mission.id + " needs " + blocks + " blocks");
        }
    }

    [Test]
    public void BoardTiles_FollowStageRanges()
    {
        foreach (MissionDefinition mission in catalog.missions)
        {
            int tiles = MissionRoute.Cells(mission).Count;
            int s = (int)mission.stage;

            Assert.That(tiles, Is.InRange(MinTiles[s], MaxTiles[s]), mission.id + " has " + tiles + " tiles");
        }
    }

    [Test]
    public void Boards_FitTheTopView()
    {
        foreach (MissionDefinition mission in catalog.missions)
        {
            List<Vector2Int> cells = MissionRoute.Cells(mission);
            Vector2Int min = cells[0], max = cells[0];
            foreach (Vector2Int c in cells)
            {
                min = Vector2Int.Min(min, c);
                max = Vector2Int.Max(max, c);
            }

            Assert.LessOrEqual(max.x - min.x + 1, MaxColumns, mission.id + " is too wide");
            Assert.LessOrEqual(max.y - min.y + 1, MaxRows, mission.id + " is too tall");
        }
    }

    [Test]
    public void Roads_NeverTouchExceptInOrder()
    {
        // Side-by-side roads would let mTiny cut across; keep them apart
        foreach (MissionDefinition mission in catalog.missions)
        {
            List<Vector2Int> cells = MissionRoute.Cells(mission);
            for (int a = 0; a < cells.Count; a++)
            {
                for (int b = a + 2; b < cells.Count; b++)
                {
                    Vector2Int d = cells[a] - cells[b];
                    Assert.AreNotEqual(1, Mathf.Abs(d.x) + Mathf.Abs(d.y),
                        mission.id + " road touches itself at " + cells[a] + " and " + cells[b]);
                }
            }
        }
    }

    [Test]
    public void RequirementsMatchSpec()
    {
        // Stage 1 movement; 2 + multipliers; 3 + feelings; 4 and 5 + sounds
        foreach (MissionDefinition mission in catalog.missions)
        {
            int s = (int)mission.stage;

            Assert.IsTrue(mission.requireMovement, mission.id + " should require movement");
            Assert.AreEqual(s >= 1, mission.requireMultiplier, mission.id + " multiplier");
            Assert.AreEqual(s >= 2, mission.requireEmotion, mission.id + " emotion");
            Assert.AreEqual(s >= 3, mission.requireSound, mission.id + " sound");
        }
    }

    [Test]
    public void VisibleBlocks_MatchTheStage()
    {
        foreach (MissionDefinition mission in catalog.missions)
        {
            int s = (int)mission.stage;
            BlockGroups v = mission.visibleBlocks;

            Assert.IsTrue((v & BlockGroups.Movement) != 0, mission.id + " hides movement");
            Assert.AreEqual(s >= 1, (v & BlockGroups.Multiplier) != 0, mission.id + " multiplier blocks");
            Assert.AreEqual(s >= 2, (v & BlockGroups.Emotion) != 0, mission.id + " feeling blocks");
            Assert.AreEqual(s >= 3, (v & BlockGroups.Sound) != 0, mission.id + " music blocks");
        }
    }

    [Test]
    public void MarkedTiles_MatchTheStage()
    {
        // Stage 3: a smiley tile. Stages 4-5: a smiley tile and a music tile.
        foreach (MissionDefinition mission in catalog.missions)
        {
            int s = (int)mission.stage;
            int feel = mission.cues.FindAll(c => c.kind == CueKind.Feeling).Count;
            int music = mission.cues.FindAll(c => c.kind == CueKind.Music).Count;

            Assert.AreEqual(s >= 2 ? 1 : 0, feel, mission.id + " smiley tiles");
            Assert.AreEqual(s >= 3 ? 1 : 0, music, mission.id + " music tiles");
        }
    }

    [Test]
    public void MarkedTiles_AreOnPlainRoad()
    {
        // On the road, not the start, finish or a city stop, and never stacked
        foreach (MissionDefinition mission in catalog.missions)
        {
            var seen = new HashSet<Vector2Int>();
            foreach (MissionCue cue in mission.cues)
            {
                Assert.IsTrue(mission.path.Contains(cue.cell), mission.id + " marked tile off the road at " + cue.cell);
                Assert.IsFalse(mission.cityTiles.Exists(t => t.cell == cue.cell), mission.id + " marked tile on a city stop");
                Assert.IsTrue(seen.Add(cue.cell), mission.id + " two marks on one tile");
            }
        }
    }

    [Test]
    public void MarkedTiles_NeedTheirBlock()
    {
        // Each level's best answer fails if the feeling or note is played
        // anywhere but on its marked tile
        foreach (MissionDefinition mission in catalog.missions)
        {
            if (mission.cues.Count == 0) continue;

            string withoutStop = mission.intendedSolution;
            foreach (string t in new[] { "Happy", "Sad", "Angry", "Surprise", "Dizzy", "Sleepy", "Playful" })
            {
                withoutStop = withoutStop.Replace(" " + t + " ", " ");
            }
            withoutStop = "Happy Do " + withoutStop;

            SimResult result = MissionSimulator.Run(mission, withoutStop);
            Assert.AreNotEqual(SimResult.ReachedGoal, result, mission.id + " can be won without stopping on the smiley tile");
        }
    }

    [Test]
    public void StageFive_UsesTheCity()
    {
        foreach (int i in catalog.IndicesInStage(MissionStage.Stage5))
        {
            Assert.Greater(catalog.Get(i).cityTiles.Count, 0, catalog.Get(i).id + " has no city stop");
        }
    }

    [Test]
    public void CityTiles_AreOnThePath()
    {
        foreach (MissionDefinition mission in catalog.missions)
        {
            foreach (CityTilePlacement tile in mission.cityTiles)
            {
                Assert.IsTrue(mission.path.Contains(tile.cell), mission.id + " city tile off path at " + tile.cell);
            }
        }
    }

    [Test]
    public void EveryRoute_IsConnectedInOrder()
    {
        foreach (MissionDefinition mission in catalog.missions)
        {
            Assert.IsTrue(MissionRoute.IsConnected(mission), mission.id + " route has a gap or is out of order");
        }
    }

    [Test]
    public void CityTiles_SitOnEastWestRoad()
    {
        // The hospital, police and crossing art all have their road running
        // east-west, so they only join the road network there.
        foreach (MissionDefinition mission in catalog.missions)
        {
            foreach (CityTilePlacement tile in mission.cityTiles)
            {
                Assert.AreEqual(Side.E | Side.W, MissionRoute.Openings(mission, tile.cell),
                    mission.id + " " + tile.kind + " at " + tile.cell + " is not on an east-west stretch");
            }
        }
    }

    [Test]
    public void EveryInstruction_IsNonEmpty()
    {
        foreach (MissionDefinition mission in catalog.missions)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(mission.instruction), mission.id + " has no instruction");
            Assert.IsFalse(string.IsNullOrWhiteSpace(mission.title), mission.id + " has no title");
        }
    }

    [Test]
    public void StageNames_MatchTheOwnersList()
    {
        Assert.AreEqual("Let's Start!", MissionStages.Name(MissionStage.Stage1));
        Assert.AreEqual("Little Learner", MissionStages.Name(MissionStage.Stage2));
        Assert.AreEqual("Rising Star", MissionStages.Name(MissionStage.Stage3));
        Assert.AreEqual("Super Learner", MissionStages.Name(MissionStage.Stage4));
        Assert.AreEqual("Champion!", MissionStages.Name(MissionStage.Stage5));
        Assert.AreEqual("The ultimate challenge", MissionStages.Tagline(MissionStage.Stage5));
    }
}
