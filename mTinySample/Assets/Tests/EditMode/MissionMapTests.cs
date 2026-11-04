using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class MissionMapTests
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

    private void WinEasy(params int[] stars)
    {
        for (int i = 0; i < stars.Length; i++) progress.RecordWin(i, stars[i]);
    }

    // ---------------- TROPHIES ----------------

    [Test]
    public void Trophy_StageNotFinished_None()
    {
        WinEasy(3, 3, 3, 3);

        Assert.AreEqual(TrophyTier.None, StageMapModel.GetTrophy(progress, catalog, MissionStage.Easy));
    }

    [Test]
    public void Trophy_AllThreeStars_Gold()
    {
        WinEasy(3, 3, 3, 3, 3);

        Assert.AreEqual(TrophyTier.Gold, StageMapModel.GetTrophy(progress, catalog, MissionStage.Easy));
    }

    [Test]
    public void Trophy_TenToFourteenStars_Silver()
    {
        WinEasy(2, 2, 2, 2, 2);
        Assert.AreEqual(TrophyTier.Silver, StageMapModel.GetTrophy(progress, catalog, MissionStage.Easy));

        progress.RecordWin(0, 3);
        progress.RecordWin(1, 3);
        progress.RecordWin(2, 3);
        progress.RecordWin(3, 3);
        Assert.AreEqual(TrophyTier.Silver, StageMapModel.GetTrophy(progress, catalog, MissionStage.Easy), "14 stars");
    }

    [Test]
    public void Trophy_UnderTenStars_Bronze()
    {
        WinEasy(1, 2, 2, 2, 2);

        Assert.AreEqual(TrophyTier.Bronze, StageMapModel.GetTrophy(progress, catalog, MissionStage.Easy));
    }

    // ---------------- CONFETTI ----------------

    [Test]
    public void Confetti_FirstTrophy_PlaysOnce()
    {
        WinEasy(1, 1, 1, 1, 1);

        Assert.IsTrue(progress.TakeTrophyCelebration(MissionStage.Easy, TrophyTier.Bronze));
        Assert.IsFalse(progress.TakeTrophyCelebration(MissionStage.Easy, TrophyTier.Bronze));
    }

    [Test]
    public void Confetti_TrophyUpgrade_PlaysAgain()
    {
        progress.TakeTrophyCelebration(MissionStage.Easy, TrophyTier.Bronze);

        Assert.IsTrue(progress.TakeTrophyCelebration(MissionStage.Easy, TrophyTier.Silver));
    }

    [Test]
    public void Confetti_NoTrophy_NeverPlays()
    {
        Assert.IsFalse(progress.TakeTrophyCelebration(MissionStage.Easy, TrophyTier.None));
    }

    [Test]
    public void Confetti_ResetAll_PlaysAgainAfterReset()
    {
        progress.TakeTrophyCelebration(MissionStage.Easy, TrophyTier.Gold);

        progress.ResetAll();

        Assert.IsTrue(progress.TakeTrophyCelebration(MissionStage.Easy, TrophyTier.Bronze));
    }

    // ---------------- PATH ----------------

    [Test]
    public void NextMission_FreshSave_IsFirst()
    {
        Assert.AreEqual(0, StageMapModel.NextMissionIndex(progress, catalog));
    }

    [Test]
    public void NextMission_AfterWins_IsFirstUnfinished()
    {
        WinEasy(3, 2, 1);

        Assert.AreEqual(3, StageMapModel.NextMissionIndex(progress, catalog));
    }

    [Test]
    public void NextMission_AllFinished_IsLast()
    {
        for (int i = 0; i < 15; i++) progress.RecordWin(i, 1);

        Assert.AreEqual(14, StageMapModel.NextMissionIndex(progress, catalog));
    }

    [Test]
    public void PathSlot_TrophySitsAfterEachStage()
    {
        // Slots: E1..E5 = 0..4, Easy trophy = 5, M1 = 6 ...
        Assert.AreEqual(0, StageMapModel.MissionSlot(catalog, 0));
        Assert.AreEqual(4, StageMapModel.MissionSlot(catalog, 4));
        Assert.AreEqual(5, StageMapModel.TrophySlot(catalog, MissionStage.Easy));
        Assert.AreEqual(6, StageMapModel.MissionSlot(catalog, 5));
        Assert.AreEqual(17, StageMapModel.TrophySlot(catalog, MissionStage.Hard));
    }
}
