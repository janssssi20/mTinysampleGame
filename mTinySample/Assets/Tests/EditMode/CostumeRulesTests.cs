using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class CostumeRulesTests
{
    private class FakeStore : IProgressStore
    {
        public readonly Dictionary<string, int> values = new Dictionary<string, int>();
        public int GetInt(string key, int fallback) { int v; return values.TryGetValue(key, out v) ? v : fallback; }
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
        // Five stages of three levels
        catalog = ScriptableObject.CreateInstance<MissionCatalog>();
        for (int s = 0; s < 5; s++)
            for (int n = 0; n < 3; n++)
                catalog.missions.Add(new MissionDefinition { id = "S" + s + "-" + n, stage = (MissionStage)s });
        store = new FakeStore();
        progress = new MissionProgress(catalog, store);
    }

    [TearDown]
    public void TearDown() { Object.DestroyImmediate(catalog); }

    private void WinStage(int stage, int levels)
    {
        for (int n = 0; n < levels; n++) progress.RecordWin(stage * 3 + n, 1);
    }

    [Test]
    public void EachStage_GivesItsCostume()
    {
        Assert.AreEqual(Costume.Panda, CostumeRules.ForStage(MissionStage.Stage1));
        Assert.AreEqual(Costume.Chicken, CostumeRules.ForStage(MissionStage.Stage2));
        Assert.AreEqual(Costume.Dog, CostumeRules.ForStage(MissionStage.Stage3));
        Assert.AreEqual(Costume.Cat, CostumeRules.ForStage(MissionStage.Stage4));
        Assert.AreEqual(Costume.Golden, CostumeRules.ForStage(MissionStage.Stage5));
        Assert.AreEqual(MissionStage.Stage2, CostumeRules.StageFor(Costume.Chicken));
    }

    [Test]
    public void Progress_CountsLevelsWon()
    {
        WinStage(1, 2);
        Assert.AreEqual(2, CostumeRules.LevelsWon(progress, catalog, MissionStage.Stage2));
        Assert.AreEqual(0, CostumeRules.LevelsWon(progress, catalog, MissionStage.Stage1));
    }

    [Test]
    public void Costume_UnlocksOnlyWhenEveryLevelIsWon()
    {
        Assert.IsTrue(CostumeRules.IsUnlocked(progress, catalog, Costume.None), "plain mTiny is always yours");
        WinStage(0, 2);
        Assert.IsFalse(CostumeRules.IsUnlocked(progress, catalog, Costume.Panda));
        WinStage(0, 3);
        Assert.IsTrue(CostumeRules.IsUnlocked(progress, catalog, Costume.Panda));
        Assert.IsFalse(CostumeRules.IsUnlocked(progress, catalog, Costume.Chicken));
    }

    [Test]
    public void NewUnlock_IsAnnouncedOnce()
    {
        Assert.IsFalse(CostumeRules.TakeNewUnlock(progress, catalog, store, Costume.Panda), "not earned yet");
        WinStage(0, 3);
        Assert.IsTrue(CostumeRules.TakeNewUnlock(progress, catalog, store, Costume.Panda));
        Assert.IsFalse(CostumeRules.TakeNewUnlock(progress, catalog, store, Costume.Panda));
    }

    [Test]
    public void Selected_FallsBackToPlainWhenLocked()
    {
        CostumeRules.Select(store, Costume.Cat);
        Assert.AreEqual(Costume.None, CostumeRules.Selected(store, progress, catalog));
        WinStage(3, 3);
        Assert.AreEqual(Costume.Cat, CostumeRules.Selected(store, progress, catalog));
    }

    [Test]
    public void UnlockAllSwitch_OpensEveryCostume_WithoutAnnouncingThem()
    {
        store.SetInt(MissionProgress.UnlockAllKey, 1);

        foreach (Costume c in System.Enum.GetValues(typeof(Costume)))
        {
            Assert.IsTrue(CostumeRules.IsUnlocked(progress, catalog, c), c + " should be open");
        }
        Assert.IsFalse(CostumeRules.IsEarned(progress, catalog, Costume.Cat), "not earned by playing");
        Assert.IsFalse(CostumeRules.TakeNewUnlock(progress, catalog, store, Costume.Cat), "no New mask! card");

        CostumeRules.Select(store, Costume.Cat);
        Assert.AreEqual(Costume.Cat, CostumeRules.Selected(store, progress, catalog));

        // Switch off: a costume not earned goes back to plain mTiny
        store.SetInt(MissionProgress.UnlockAllKey, 0);
        Assert.AreEqual(Costume.None, CostumeRules.Selected(store, progress, catalog));
    }

    [Test]
    public void TeacherReset_ClearsCostumes()
    {
        WinStage(0, 3);
        CostumeRules.Select(store, Costume.Panda);
        CostumeRules.TakeNewUnlock(progress, catalog, store, Costume.Panda);
        progress.ResetAll();

        Assert.AreEqual(Costume.None, CostumeRules.SelectedRaw(store));
        Assert.IsFalse(CostumeRules.IsUnlocked(progress, catalog, Costume.Panda));
        WinStage(0, 3);
        Assert.IsTrue(CostumeRules.TakeNewUnlock(progress, catalog, store, Costume.Panda), "announced again after a reset");
    }
}
