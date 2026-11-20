using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class BackgroundRulesTests
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
        catalog = ScriptableObject.CreateInstance<MissionCatalog>();
        for (int s = 0; s < 5; s++)
            for (int n = 0; n < 3; n++)
                catalog.missions.Add(new MissionDefinition { id = "S" + s + "-" + n, stage = (MissionStage)s });
        store = new FakeStore();
        progress = new MissionProgress(catalog, store);
    }

    [TearDown]
    public void TearDown() { Object.DestroyImmediate(catalog); }

    private void WinStage(int stage)
    {
        for (int n = 0; n < 3; n++) progress.RecordWin(stage * 3 + n, 1);
    }

    [Test]
    public void EachMask_HasItsOwnWorld()
    {
        Assert.AreEqual("Sunny Park", BackgroundRules.Name(Costume.None));
        Assert.AreEqual("Bamboo Forest", BackgroundRules.Name(Costume.Panda));
        Assert.AreEqual("Happy Farm", BackgroundRules.Name(Costume.Chicken));
        Assert.AreEqual("Dog Town", BackgroundRules.Name(Costume.Dog));
        Assert.AreEqual("Cosy Cat Room", BackgroundRules.Name(Costume.Cat));
        Assert.AreEqual("Champion Stage", BackgroundRules.Name(Costume.Golden));
        Assert.IsTrue(BackgroundRules.IsDark(Costume.Golden));
        Assert.IsFalse(BackgroundRules.IsDark(Costume.None));
    }

    [Test]
    public void World_UnlocksWithItsMask()
    {
        Assert.IsTrue(BackgroundRules.IsUnlocked(progress, catalog, Costume.None), "Sunny Park is always yours");
        Assert.IsFalse(BackgroundRules.IsUnlocked(progress, catalog, Costume.Chicken));
        WinStage(1);
        Assert.IsTrue(BackgroundRules.IsUnlocked(progress, catalog, Costume.Chicken));
        Assert.IsTrue(CostumeRules.IsUnlocked(progress, catalog, Costume.Chicken));
        Assert.IsFalse(BackgroundRules.IsUnlocked(progress, catalog, Costume.Dog));
    }

    [Test]
    public void Selected_FallsBackToSunnyParkWhenLocked()
    {
        BackgroundRules.Select(store, Costume.Cat);
        Assert.AreEqual(Costume.None, BackgroundRules.Selected(store, progress, catalog));
        WinStage(3);
        Assert.AreEqual(Costume.Cat, BackgroundRules.Selected(store, progress, catalog));
    }

    [Test]
    public void World_AndCostume_AreChosenSeparately()
    {
        WinStage(0);
        WinStage(1);
        CostumeRules.Select(store, Costume.Panda);
        BackgroundRules.Select(store, Costume.Chicken);
        Assert.AreEqual(Costume.Panda, CostumeRules.Selected(store, progress, catalog));
        Assert.AreEqual(Costume.Chicken, BackgroundRules.Selected(store, progress, catalog));
    }

    [Test]
    public void UnlockAllSwitch_OpensEveryWorld()
    {
        store.SetInt(MissionProgress.UnlockAllKey, 1);
        foreach (Costume c in System.Enum.GetValues(typeof(Costume)))
        {
            Assert.IsTrue(BackgroundRules.IsUnlocked(progress, catalog, c), c + " should be open");
        }
        BackgroundRules.Select(store, Costume.Golden);
        Assert.AreEqual(Costume.Golden, BackgroundRules.Selected(store, progress, catalog));

        store.SetInt(MissionProgress.UnlockAllKey, 0);
        Assert.AreEqual(Costume.None, BackgroundRules.Selected(store, progress, catalog));
    }

    [Test]
    public void TeacherReset_GoesBackToSunnyPark()
    {
        WinStage(0);
        BackgroundRules.Select(store, Costume.Panda);
        progress.ResetAll();
        Assert.AreEqual(Costume.None, BackgroundRules.SelectedRaw(store));
    }
}
