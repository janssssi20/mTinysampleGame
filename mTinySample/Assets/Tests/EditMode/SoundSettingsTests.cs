using System.Collections.Generic;
using NUnit.Framework;

public class SoundSettingsTests
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

    [Test]
    public void BothChannels_StartOn()
    {
        SoundSettings settings = new SoundSettings(new FakeStore());

        Assert.IsTrue(settings.IsOn(SoundChannel.Music));
        Assert.IsTrue(settings.IsOn(SoundChannel.Effects));
    }

    [Test]
    public void Toggle_FlipsOnlyThatChannel_AndRemembers()
    {
        FakeStore store = new FakeStore();
        SoundSettings settings = new SoundSettings(store);

        Assert.IsFalse(settings.Toggle(SoundChannel.Music));
        Assert.IsTrue(settings.IsOn(SoundChannel.Effects));

        // A fresh reader over the same store sees the saved switch
        Assert.IsFalse(new SoundSettings(store).IsOn(SoundChannel.Music));

        Assert.IsTrue(settings.Toggle(SoundChannel.Music));
    }

    [Test]
    public void ResetProgress_KeepsSoundSwitches()
    {
        FakeStore store = new FakeStore();
        new SoundSettings(store).Toggle(SoundChannel.Effects);

        MissionCatalog catalog = UnityEngine.ScriptableObject.CreateInstance<MissionCatalog>();
        new MissionProgress(catalog, store).ResetAll();

        Assert.IsFalse(new SoundSettings(store).IsOn(SoundChannel.Effects));
    }
}
