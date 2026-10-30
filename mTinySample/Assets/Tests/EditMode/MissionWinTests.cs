using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class MissionWinTests
{
    private class FakeStore : IProgressStore
    {
        private readonly Dictionary<string, int> values = new Dictionary<string, int>();

        public int GetInt(string key, int fallback)
        {
            int value;
            return values.TryGetValue(key, out value) ? value : fallback;
        }

        public void SetInt(string key, int value) { values[key] = value; }
        public void DeleteKey(string key) { values.Remove(key); }
        public void Save() { }
    }

    private MissionCatalog catalog;
    private MissionProgress progress;

    [SetUp]
    public void SetUp()
    {
        catalog = ScriptableObject.CreateInstance<MissionCatalog>();

        for (int i = 0; i < 15; i++)
        {
            catalog.missions.Add(new MissionDefinition { id = "X" + i, stage = (MissionStage)(i / 5) });
        }

        progress = new MissionProgress(catalog, new FakeStore());
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(catalog);
    }

    [Test]
    public void NextIndex_CrossesStageBoundary()
    {
        Assert.AreEqual(5, catalog.NextIndex(4));
        Assert.AreEqual(-1, catalog.NextIndex(14));
    }

    [Test]
    public void Decide_MiddleMission_ShowsNextWithoutCelebration()
    {
        MissionWinOutcome outcome = MissionWinOutcome.Decide(progress, catalog, 4, 3);

        Assert.IsTrue(outcome.showNext);
        Assert.IsFalse(outcome.celebrate);
        Assert.AreEqual(5, outcome.nextIndex);
    }

    [Test]
    public void Decide_FinalMission_FirstWin_CelebratesWithoutNext()
    {
        MissionWinOutcome outcome = MissionWinOutcome.Decide(progress, catalog, 14, 2);

        Assert.IsTrue(outcome.celebrate);
        Assert.IsFalse(outcome.showNext);
        Assert.AreEqual(2, outcome.totalStars);
    }

    [Test]
    public void Decide_FinalMission_SecondWin_NoCelebration()
    {
        MissionWinOutcome.Decide(progress, catalog, 14, 2);

        MissionWinOutcome outcome = MissionWinOutcome.Decide(progress, catalog, 14, 3);

        Assert.IsFalse(outcome.celebrate);
        Assert.AreEqual(3, outcome.totalStars);
    }

    [Test]
    public void Decide_ZeroStars_RecordsNothing()
    {
        MissionWinOutcome.Decide(progress, catalog, 0, 0);

        Assert.AreEqual(0, progress.GetBestStars(0));
        Assert.IsFalse(progress.IsMissionUnlocked(1));
    }
}
