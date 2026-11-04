public enum SoundChannel
{
    Music,
    Effects
}

// Music and sound-effect switches from Settings. Both start on.
// Kept apart from mission progress, so Reset progress leaves them alone.
public sealed class SoundSettings
{
    public const string MusicKey = "mtiny.music";
    public const string EffectsKey = "mtiny.sfx";
    public const string MusicVolumeKey = "mtiny.musicVolume";

    // Music volume is stored as a whole percent
    public const int DefaultMusicVolume = 70;

    private readonly IProgressStore store;

    public SoundSettings(IProgressStore store)
    {
        this.store = store;
    }

    public bool IsOn(SoundChannel channel)
    {
        return store.GetInt(Key(channel), 1) != 0;
    }

    // Flips the switch and returns its new state
    public bool Toggle(SoundChannel channel)
    {
        bool on = !IsOn(channel);
        store.SetInt(Key(channel), on ? 1 : 0);
        store.Save();
        return on;
    }

    // 0 (silent) to 1 (full)
    public float MusicVolume
    {
        get
        {
            int percent = store.GetInt(MusicVolumeKey, DefaultMusicVolume);
            return System.Math.Max(0, System.Math.Min(100, percent)) / 100f;
        }
    }

    // Not saved to disk here: a slider calls this every frame while
    // dragging, then Save() once the finger lifts.
    public void SetMusicVolume(float volume)
    {
        int percent = (int)System.Math.Round(volume * 100f);
        store.SetInt(MusicVolumeKey, System.Math.Max(0, System.Math.Min(100, percent)));
    }

    public void Save()
    {
        store.Save();
    }

    private static string Key(SoundChannel channel)
    {
        return channel == SoundChannel.Music ? MusicKey : EffectsKey;
    }
}
