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
    public void MusicVolume_DefaultsTo70Percent()
    {
        Assert.AreEqual(0.7f, new SoundSettings(new FakeStore()).MusicVolume, 0.001f);
    }

    [Test]
    public void MusicVolume_IsRememberedAndClamped()
    {
        FakeStore store = new FakeStore();
        SoundSettings settings = new SoundSettings(store);

        settings.SetMusicVolume(0.25f);
        Assert.AreEqual(0.25f, new SoundSettings(store).MusicVolume, 0.001f);

        settings.SetMusicVolume(1.5f);
        Assert.AreEqual(1f, settings.MusicVolume, 0.001f);

        settings.SetMusicVolume(-1f);
        Assert.AreEqual(0f, settings.MusicVolume, 0.001f);
    }

    [Test]
    public void ResetProgress_KeepsSoundSwitches()
    {
        FakeStore store = new FakeStore();
        new SoundSettings(store).Toggle(SoundChannel.Effects);
        new SoundSettings(store).SetMusicVolume(0.4f);

        MissionCatalog catalog = UnityEngine.ScriptableObject.CreateInstance<MissionCatalog>();
        new MissionProgress(catalog, store).ResetAll();

        Assert.IsFalse(new SoundSettings(store).IsOn(SoundChannel.Effects));
        Assert.AreEqual(0.4f, new SoundSettings(store).MusicVolume, 0.001f);
    }
}
