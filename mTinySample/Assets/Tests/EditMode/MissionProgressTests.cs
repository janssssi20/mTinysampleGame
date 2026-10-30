using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class MissionProgressTests
{
    private class FakeStore : IProgressStore
    {
        public readonly Dictionary<string, int> values = new Dictionary<string, int>();

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
    private FakeStore store;
    private MissionProgress progress;

    [SetUp]
    public void SetUp()
    {
        catalog = ScriptableObject.CreateInstance<MissionCatalog>();

        string[] prefixes = { "E", "M", "H" };

        for (int s = 0; s < 3; s++)
        {
            for (int n = 1; n <= 5; n++)
            {
                catalog.missions.Add(new MissionDefinition { id = prefixes[s] + n, stage = (MissionStage)s });
            }
        }

        store = new FakeStore();
        progress = new MissionProgress(catalog, store);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(catalog);
    }

    private void WinRange(int fromIndex, int toIndex)
    {
        for (int i = fromIndex; i <= toIndex; i++)
        {
            progress.RecordWin(i, 3);
        }
    }

    [Test]
    public void FirstMission_IsUnlocked_SecondIsLocked()
    {
        Assert.IsTrue(progress.IsMissionUnlocked(0));
        Assert.IsFalse(progress.IsMissionUnlocked(1));
    }

    [Test]
    public void RecordWin_UnlocksNextMission()
    {
        progress.RecordWin(0, 1);

        Assert.IsTrue(progress.IsMissionUnlocked(1));
    }

    [Test]
    public void RecordWin_LowerScore_KeepsBest()
    {
        progress.RecordWin(0, 3);
        progress.RecordWin(0, 1);

        Assert.AreEqual(3, progress.GetBestStars(0));
    }

    [Test]
    public void MediumStage_LockedUntilE5Won()
    {
        WinRange(0, 3);
        Assert.IsFalse(progress.IsStageUnlocked(MissionStage.Medium));

        progress.RecordWin(4, 2);
        Assert.IsTrue(progress.IsStageUnlocked(MissionStage.Medium));
    }

    [Test]
    public void HardStage_LockedUntilM5Won()
    {
        WinRange(0, 8);
        Assert.IsFalse(progress.IsStageUnlocked(MissionStage.Hard));

        progress.RecordWin(9, 1);
        Assert.IsTrue(progress.IsStageUnlocked(MissionStage.Hard));
    }

    [Test]
    public void MediumFirstMission_UnlockedByE5()
    {
        WinRange(0, 4);

        Assert.IsTrue(progress.IsMissionUnlocked(5));
    }

    [Test]
    public void GetStageStars_SumsBestStars()
    {
        progress.RecordWin(0, 3);
        progress.RecordWin(1, 2);

        Assert.AreEqual(5, progress.GetStageStars(MissionStage.Easy));
        Assert.AreEqual(5, progress.GetTotalStars());
    }

    [Test]
    public void RecordWin_FinalMissionTwice_CelebratesOnce()
    {
        WinRange(0, 13);

        Assert.IsTrue(progress.RecordWin(14, 2));
        Assert.IsFalse(progress.RecordWin(14, 3));
    }

    [Test]
    public void RecordWin_NotFinalMission_NeverCelebrates()
    {
        Assert.IsFalse(progress.RecordWin(0, 3));
    }

    [Test]
    public void ResetAll_ClearsStarsAndCelebration()
    {
        WinRange(0, 13);
        progress.RecordWin(14, 3);

        progress.ResetAll();

        Assert.IsFalse(progress.IsMissionUnlocked(1));
        Assert.AreEqual(0, progress.GetTotalStars());
        Assert.IsTrue(progress.RecordWin(14, 1));
    }

    [Test]
    public void RecordWin_ClampsStarsTo1To3()
    {
        progress.RecordWin(0, 7);
        Assert.AreEqual(3, progress.GetBestStars(0));

        progress.RecordWin(1, 0);
        Assert.AreEqual(0, progress.GetBestStars(1));
        Assert.IsFalse(progress.IsMissionUnlocked(2));
    }
}
