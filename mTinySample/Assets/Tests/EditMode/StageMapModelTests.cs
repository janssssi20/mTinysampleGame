using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class StageMapModelTests
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
    public void GetNodeState_FreshSave_FirstIsNextSecondIsLocked()
    {
        Assert.AreEqual(NodeState.Next, StageMapModel.GetNodeState(progress, 0));
        Assert.AreEqual(NodeState.Locked, StageMapModel.GetNodeState(progress, 1));
    }

    [Test]
    public void GetNodeState_WonMission_IsDoneAndFollowingIsNext()
    {
        progress.RecordWin(0, 2);

        Assert.AreEqual(NodeState.Done, StageMapModel.GetNodeState(progress, 0));
        Assert.AreEqual(NodeState.Next, StageMapModel.GetNodeState(progress, 1));
    }

    [Test]
    public void GetNodeState_FirstOfLockedStage_IsLocked()
    {
        Assert.AreEqual(NodeState.Locked, StageMapModel.GetNodeState(progress, 5));
    }

    [Test]
    public void StarCountText_FormatsFraction()
    {
        Assert.AreEqual("9/15", StageMapModel.StarCountText(9, 15));
    }
}
