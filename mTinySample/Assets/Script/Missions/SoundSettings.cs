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

    private static string Key(SoundChannel channel)
    {
        return channel == SoundChannel.Music ? MusicKey : EffectsKey;
    }
}
