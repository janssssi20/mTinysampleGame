using UnityEngine;

public enum Sfx
{
    Tap,
    Start,
    Step,
    Turn,
    Oops,
    Win,
    Cheer
}

// Background music and sound effects for every scene.
// Creates itself on first load and survives scene changes, so the music
// keeps playing through the loading screen. Clips live in
// Resources/Audio (made by mTiny/Setup/Generate Audio).
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private const string Folder = "Audio/";

    [Range(0f, 1f)] public float musicVolume = 0.35f;
    [Range(0f, 1f)] public float effectsVolume = 0.8f;

    private AudioSource music;
    private AudioSource effects;
    private AudioClip[] clips;
    private SoundSettings settings;


    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Create()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject host = new GameObject("AudioManager");
        DontDestroyOnLoad(host);
        host.AddComponent<AudioManager>();
    }

    private void Awake()
    {
        Instance = this;
        settings = new SoundSettings(new PlayerPrefsProgressStore());

        music = gameObject.AddComponent<AudioSource>();
        music.loop = true;
        music.playOnAwake = false;
        music.volume = musicVolume;
        music.clip = Resources.Load<AudioClip>(Folder + "music_theme");

        effects = gameObject.AddComponent<AudioSource>();
        effects.playOnAwake = false;

        string[] names = System.Enum.GetNames(typeof(Sfx));
        clips = new AudioClip[names.Length];

        for (int i = 0; i < names.Length; i++)
        {
            clips[i] = Resources.Load<AudioClip>(Folder + "sfx_" + names[i].ToLowerInvariant());
        }

        ApplyMusic();
    }


    // =========================================================
    // PLAYING
    // =========================================================

    // Safe to call from anywhere, even before the manager exists
    public static void Play(Sfx sound)
    {
        if (Instance != null)
        {
            Instance.PlayEffect(sound);
        }
    }

    private void PlayEffect(Sfx sound)
    {
        AudioClip clip = clips[(int)sound];

        if (clip != null && settings.IsOn(SoundChannel.Effects))
        {
            effects.PlayOneShot(clip, effectsVolume);
        }
    }


    // =========================================================
    // SETTINGS
    // =========================================================

    public bool IsOn(SoundChannel channel)
    {
        return settings.IsOn(channel);
    }

    public bool Toggle(SoundChannel channel)
    {
        bool on = settings.Toggle(channel);

        if (channel == SoundChannel.Music)
        {
            ApplyMusic();
        }

        return on;
    }

    private void ApplyMusic()
    {
        if (music.clip == null)
        {
            return;
        }

        if (settings.IsOn(SoundChannel.Music))
        {
            if (!music.isPlaying) music.Play();
        }
        else
        {
            music.Stop();
        }
    }
}
