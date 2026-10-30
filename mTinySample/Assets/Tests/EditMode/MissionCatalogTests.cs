using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;

public class MissionCatalogTests
{
    private const string CatalogPath = "Assets/Data/MissionCatalog.asset";

    private MissionCatalog catalog;

    [SetUp]
    public void SetUp()
    {
        catalog = AssetDatabase.LoadAssetAtPath<MissionCatalog>(CatalogPath);
        Assert.IsNotNull(catalog, "Mission catalog asset is missing at " + CatalogPath);
    }

    [Test]
    public void Catalog_Has15Missions_InPlayOrder()
    {
        string[] expected =
        {
            "E1", "E2", "E3", "E4", "E5",
            "M1", "M2", "M3", "M4", "M5",
            "H1", "H2", "H3", "H4", "H5"
        };

        List<string> ids = new List<string>();
        foreach (MissionDefinition mission in catalog.missions) ids.Add(mission.id);

        CollectionAssert.AreEqual(expected, ids);
    }

    [Test]
    public void Catalog_FivePerStage()
    {
        Assert.AreEqual(5, catalog.IndicesInStage(MissionStage.Easy).Count);
        Assert.AreEqual(5, catalog.IndicesInStage(MissionStage.Medium).Count);
        Assert.AreEqual(5, catalog.IndicesInStage(MissionStage.Hard).Count);
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
    public void NewMissions_RespectBlockLimits()
    {
        foreach (MissionDefinition mission in catalog.missions)
        {
            // E1-E3 are the owner's existing missions, kept as built
            if (mission.id == "E1" || mission.id == "E2" || mission.id == "E3") continue;

            int limit = mission.stage == MissionStage.Easy ? 5 : mission.stage == MissionStage.Medium ? 7 : 8;
            int blocks = MissionSimulator.CountBlocks(mission.intendedSolution);

            Assert.LessOrEqual(blocks, limit, mission.id + " needs " + blocks + " blocks");
        }
    }

    [Test]
    public void RequirementsMatchSpec()
    {
        foreach (int i in catalog.IndicesInStage(MissionStage.Medium))
        {
            Assert.IsTrue(catalog.Get(i).requireMultiplier, catalog.Get(i).id + " should require a multiplier");
        }

        foreach (int i in catalog.IndicesInStage(MissionStage.Hard))
        {
            Assert.IsTrue(catalog.Get(i).requireEmotion, catalog.Get(i).id + " should require an emotion");
        }
    }

    [Test]
    public void VisibleBlocks_CoverRequiredBlocks()
    {
        foreach (MissionDefinition mission in catalog.missions)
        {
            if (mission.requireMultiplier)
                Assert.IsTrue((mission.visibleBlocks & BlockGroups.Multiplier) != 0, mission.id + " hides a required multiplier");

            if (mission.requireEmotion)
                Assert.IsTrue((mission.visibleBlocks & BlockGroups.Emotion) != 0, mission.id + " hides a required emotion");
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
    public void EveryInstruction_IsNonEmpty()
    {
        foreach (MissionDefinition mission in catalog.missions)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(mission.instruction), mission.id + " has no instruction");
            Assert.IsFalse(string.IsNullOrWhiteSpace(mission.title), mission.id + " has no title");
        }
    }
}
