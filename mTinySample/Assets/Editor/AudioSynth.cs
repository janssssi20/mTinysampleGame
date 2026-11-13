using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Builds the game's music and sound effects from code, so every clip is
// original and free to ship. Writes WAVs to Assets/Resources/Audio,
// where AudioManager loads them. Swap in your own clips with the same
// names any time.
public static class AudioSynth
{
    private const string Folder = "Assets/Resources/Audio";
    private const int Rate = 22050;

    [MenuItem("mTiny/Setup/Generate Audio")]
    public static void GenerateAll()
    {
        Directory.CreateDirectory(Folder);

        Write("music_theme", Music(), isMusic: true);

        Write("sfx_tap", Tap(), false);
        Write("sfx_start", Start(), false);
        Write("sfx_step", Step(), false);
        Write("sfx_turn", Turn(), false);
        Write("sfx_oops", Oops(), false);
        Write("sfx_win", Win(), false);
        Write("sfx_cheer", Cheer(), false);

        // Note blocks: Do Re Mi Fa Sol La Si HighDo = C5 D5 E5 F5 G5 A5 B5 C6
        int[] scale = { 72, 74, 76, 77, 79, 81, 83, 84 };
        string[] names = Enum.GetNames(typeof(NoteType));
        for (int i = 0; i < names.Length; i++)
        {
            Write("note_" + names[i].ToLowerInvariant(), Note(scale[i]), false);
        }

        GenerateScenarioSounds();

        Debug.Log("[mTiny] Music and sound effects written to " + Folder);
    }

    // Only the scenario story sounds, so hand-made clips elsewhere are kept
    [MenuItem("mTiny/Setup/Generate Scenario Sounds")]
    public static void GenerateScenarioSounds()
    {
        Directory.CreateDirectory(Folder);

        Write("sfx_policesiren", PoliceSiren(), false);
        Write("sfx_ambulancesiren", AmbulanceSiren(), false);
        Write("sfx_carwhoosh", CarWhoosh(), false);
        Write("sfx_signalwait", SignalWait(), false);
        Write("sfx_signalwalk", SignalWalk(), false);
        Write("sfx_doorchime", DoorChime(), false);
        Write("sfx_doorslide", DoorSlide(), false);
        Write("sfx_heartbeat", Heartbeat(), false);
        Write("sfx_bonk", Bonk(), false);
        Write("sfx_thud", Thud(), false);
        Write("sfx_dizzy", Dizzy(), false);
        Write("sfx_sparkle", Sparkle(), false);
        Write("sfx_sneak", Sneak(), false);
        Write("sfx_whistle", Whistle(), false);

        AssetDatabase.Refresh();
        Debug.Log("[mTiny] Scenario sounds written to " + Folder);
    }

    // =========================================================
    // SCENARIO SOUNDS
    // =========================================================
    // Toy-like and gentle: sirens are soft two-tone tunes, never
    // loud or scary, and every sound is short.
    // =========================================================

    // Police "wee-woo": two soft tones taking turns
    private static float[] PoliceSiren()
    {
        float[] buffer = Buffer(1.8f);
        for (int i = 0; i < 4; i++)
        {
            float freq = i % 2 == 0 ? 960f : 720f;
            AddNote(buffer, i * 0.45f, 0.43f, freq, 0.3f, Lead, 0.85f);
        }
        FadeOut(buffer, 0.1f);
        return Normalize(buffer, 0.6f);
    }

    // Ambulance wail: rises and falls twice
    private static float[] AmbulanceSiren()
    {
        float[] buffer = Buffer(1.6f);
        float phase = 0f;
        for (int i = 0; i < buffer.Length; i++)
        {
            float t = (float)i / Rate;
            float freq = 650f + 330f * (0.5f - 0.5f * Mathf.Cos(t / 0.8f * 2f * Mathf.PI));
            phase += freq / Rate;
            float env = Mathf.Clamp01(t / 0.05f) * Mathf.Clamp01((1.6f - t) / 0.15f);
            buffer[i] = Lead(phase) * 0.3f * env;
        }
        return Normalize(buffer, 0.6f);
    }

    // A car going past: soft rushing air and a low hum that drops
    private static float[] CarWhoosh()
    {
        float[] buffer = Buffer(0.9f);
        System.Random random = new System.Random(11);
        float low = 0f, phase = 0f;
        for (int i = 0; i < buffer.Length; i++)
        {
            float k = (float)i / buffer.Length;
            float env = Mathf.Sin(k * Mathf.PI);
            env *= env;
            low += ((float)random.NextDouble() * 2f - 1f - low) * 0.08f;
            phase += Mathf.Lerp(170f, 110f, k) / Rate;
            buffer[i] = (low * 0.9f + Triangle(phase) * 0.25f) * env;
        }
        return Normalize(buffer, 0.55f);
    }

    // Crossing light, red: slow "wait" beeps
    private static float[] SignalWait()
    {
        float[] buffer = Buffer(1.2f);
        for (int i = 0; i < 3; i++) AddNote(buffer, i * 0.4f, 0.1f, 1000f, 0.3f, Lead, 0.8f);
        return Normalize(buffer, 0.5f);
    }

    // Crossing light, green: quick bird-like "walk" chirps
    private static float[] SignalWalk()
    {
        float[] buffer = Buffer(1.4f);
        for (int i = 0; i < 7; i++) Sweep(buffer, i * 0.2f, 0.07f, 1800f, 3000f, 0.3f, Sine);
        return Normalize(buffer, 0.5f);
    }

    // Door chime: "ding-dong"
    private static float[] DoorChime()
    {
        float[] buffer = Buffer(1.1f);
        AddNote(buffer, 0f, 0.6f, Freq(79), 0.4f, Sine, 0f);
        AddNote(buffer, 0.3f, 0.75f, Freq(76), 0.4f, Sine, 0f);
        return Normalize(buffer, 0.7f);
    }

    // Sliding doors: a soft "shhh"
    private static float[] DoorSlide()
    {
        float[] buffer = Buffer(0.5f);
        System.Random random = new System.Random(5);
        float low = 0f;
        for (int i = 0; i < buffer.Length; i++)
        {
            float k = (float)i / buffer.Length;
            low += ((float)random.NextDouble() * 2f - 1f - low) * 0.25f;
            buffer[i] = low * Mathf.Sin(k * Mathf.PI);
        }
        return Normalize(buffer, 0.4f);
    }

    // Heartbeat: two "lub-dub" beats
    private static float[] Heartbeat()
    {
        float[] buffer = Buffer(1.1f);
        foreach (float start in new[] { 0f, 0.18f, 0.6f, 0.78f })
        {
            Sweep(buffer, start, 0.14f, 95f, 50f, start % 0.6f < 0.1f ? 0.9f : 0.65f, Sine);
        }
        return Normalize(buffer, 0.85f);
    }

    // Cartoon "boing" when mTiny bonks into the blocks
    private static float[] Bonk()
    {
        float[] buffer = Buffer(0.75f);
        Sweep(buffer, 0f, 0.12f, 160f, 60f, 0.8f, Sine);
        float phase = 0f;
        int start = Mathf.RoundToInt(0.03f * Rate);
        for (int i = start; i < buffer.Length; i++)
        {
            float t = (float)(i - start) / Rate;
            float k = t / 0.7f;
            float freq = Mathf.Lerp(520f, 170f, k) * (1f + 0.08f * Mathf.Sin(t * 2f * Mathf.PI * 22f));
            phase += freq / Rate;
            buffer[i] += Triangle(phase) * 0.35f * Mathf.Exp(-t / 0.25f);
        }
        return Normalize(buffer, 0.75f);
    }

    // Soft landing thump (a falling block, a dropped bag)
    private static float[] Thud()
    {
        float[] buffer = Buffer(0.3f);
        Sweep(buffer, 0f, 0.22f, 170f, 60f, 0.8f, Sine);
        AddHat(buffer, 0f, 0.3f, new System.Random(2));
        return Normalize(buffer, 0.7f);
    }

    // Stars spinning round mTiny's head: a twinkly up-and-down warble
    private static float[] Dizzy()
    {
        float[] buffer = Buffer(1.6f);
        int[] notes = { 88, 91, 95, 91 };
        for (int i = 0; i < 15; i++) AddNote(buffer, i * 0.1f, 0.12f, Freq(notes[i % 4]), 0.2f, Sine, 0.1f);
        return Normalize(buffer, 0.5f);
    }

    // Quick sparkle for stars and hearts
    private static float[] Sparkle()
    {
        float[] buffer = Buffer(0.7f);
        int[] notes = { 84, 88, 91, 96 };
        for (int i = 0; i < notes.Length; i++) AddNote(buffer, i * 0.06f, 0.4f, Freq(notes[i]), 0.2f, Sine, 0.05f);
        return Normalize(buffer, 0.55f);
    }

    // Raccoon tiptoeing: soft, sneaky low plucks
    private static float[] Sneak()
    {
        float[] buffer = Buffer(1.3f);
        int[] notes = { 55, 57, 55, 59, 57, 60 };
        float[] times = { 0f, 0.15f, 0.4f, 0.55f, 0.8f, 0.95f };
        for (int i = 0; i < notes.Length; i++) AddNote(buffer, times[i], 0.09f, Freq(notes[i]), 0.35f, Triangle, 0f);
        return Normalize(buffer, 0.6f);
    }

    // Police whistle: a short, bright trill
    private static float[] Whistle()
    {
        float[] buffer = Buffer(0.7f);
        float phase = 0f;
        for (int i = 0; i < buffer.Length; i++)
        {
            float t = (float)i / Rate;
            phase += (2300f + 160f * Mathf.Sin(t * 2f * Mathf.PI * 28f)) / Rate;
            float env = Mathf.Clamp01(t / 0.02f) * Mathf.Clamp01((0.7f - t) / 0.1f);
            buffer[i] = Sine(phase) * 0.3f * env;
        }
        return Normalize(buffer, 0.45f);
    }


    // =========================================================
    // MUSIC
    // =========================================================
    // A calm music-box lullaby for K-G2: 16 bars in C major at
    // 76 BPM. Bell-like melody, soft rocking accompaniment, a warm
    // bass and a quiet pad; no drums, so children can think while
    // it plays. Notes ring past the loop point and wrap to the start,
    // so the loop has no seam.
    // =========================================================

    private const float Bpm = 76f;

    // Chord tones per bar (bass root first), 16 bars:
    // C Am F G  C Em F G | C Am F G  C Em F C
    private static readonly int[][] Chords =
    {
        new[] { 48, 60, 64, 67 }, new[] { 45, 57, 60, 64 }, new[] { 41, 57, 60, 65 }, new[] { 43, 55, 59, 62 },
        new[] { 48, 60, 64, 67 }, new[] { 40, 55, 59, 64 }, new[] { 41, 57, 60, 65 }, new[] { 43, 55, 59, 62 },
        new[] { 48, 60, 64, 67 }, new[] { 45, 57, 60, 64 }, new[] { 41, 57, 60, 65 }, new[] { 43, 55, 59, 62 },
        new[] { 48, 60, 64, 67 }, new[] { 40, 55, 59, 64 }, new[] { 41, 57, 60, 65 }, new[] { 48, 60, 64, 67 }
    };

    // Melody per bar as (note, eighths); -1 = rest. Small, singable steps.
    private static readonly int[][] Melody =
    {
        new[] { 76, 4, 79, 2, 76, 2 },
        new[] { 72, 2, 74, 2, 76, 4 },
        new[] { 77, 4, 76, 2, 74, 2 },
        new[] { 74, 6, -1, 2 },
        new[] { 76, 2, 79, 2, 81, 4 },
        new[] { 79, 4, 76, 4 },
        new[] { 77, 2, 76, 2, 74, 2, 72, 2 },
        new[] { 74, 8 },

        new[] { 72, 4, 76, 4 },
        new[] { 76, 2, 74, 2, 72, 4 },
        new[] { 69, 4, 72, 4 },
        new[] { 71, 2, 74, 2, 79, 4 },
        new[] { 79, 4, 76, 4 },
        new[] { 76, 2, 74, 2, 71, 4 },
        new[] { 72, 4, 74, 4 },
        new[] { 72, 8 }
    };

    private static float[] Music()
    {
        float beat = 60f / Bpm;
        float eighth = beat / 2f;
        int bars = Melody.Length;
        float[] buffer = new float[Mathf.RoundToInt(bars * 4 * beat * Rate)];

        for (int bar = 0; bar < bars; bar++)
        {
            int[] chord = Chords[bar];
            float barStart = bar * 4 * beat;

            // Music-box melody, an octave above the chords
            float t = barStart;
            int[] melody = Melody[bar];
            for (int i = 0; i + 1 < melody.Length; i += 2)
            {
                if (melody[i] >= 0)
                {
                    AddBell(buffer, t, Freq(melody[i]), 0.26f, 0.9f);
                }
                t += melody[i + 1] * eighth;
            }

            // Rocking accompaniment: low-high-mid-high in quarter notes
            int[] rock = { chord[1], chord[3], chord[2], chord[3] };
            for (int b = 0; b < 4; b++)
            {
                AddBell(buffer, barStart + b * beat, Freq(rock[b]), 0.07f, 0.6f);
            }

            // Warm bass on beats 1 and 3
            AddBell(buffer, barStart, Freq(chord[0]), 0.16f, 1.2f, harmonics: false);
            AddBell(buffer, barStart + 2 * beat, Freq(chord[0] + 12), 0.08f, 0.9f, harmonics: false);

            // Quiet pad holding the chord under everything
            for (int c = 1; c < chord.Length; c++)
            {
                AddPad(buffer, barStart, 4 * beat, Freq(chord[c]), 0.025f);
            }
        }

        return Normalize(buffer, 0.8f);
    }

    // Struck bell: sharp start, long gentle ring. Writes past the end
    // wrap to the start, which keeps the music loop seamless.
    private static void AddBell(float[] buffer, float start, float freq, float volume, float ring,
        bool harmonics = true)
    {
        int first = Mathf.RoundToInt(start * Rate);
        int count = Mathf.RoundToInt(ring * 4f * Rate);

        for (int i = 0; i < count; i++)
        {
            float time = (float)i / Rate;
            float tone = Sine(freq * time);
            if (harmonics)
            {
                // Overtones fade faster than the note, like a music box tine
                tone += 0.35f * Sine(2f * freq * time) * Mathf.Exp(-time / (ring * 0.3f));
                tone += 0.12f * Sine(3f * freq * time) * Mathf.Exp(-time / (ring * 0.15f));
            }

            float envelope = Mathf.Clamp01(time / 0.004f) * Mathf.Exp(-time / ring);
            buffer[(first + i) % buffer.Length] += tone * volume * envelope;
        }
    }

    // Soft held tone that swells in and out
    private static void AddPad(float[] buffer, float start, float length, float freq, float volume)
    {
        int first = Mathf.RoundToInt(start * Rate);
        int count = Mathf.RoundToInt(length * Rate);

        for (int i = 0; i < count; i++)
        {
            float time = (float)i / Rate;
            float envelope = Mathf.Sin(Mathf.PI * i / count);
            buffer[(first + i) % buffer.Length] += Sine(freq * time) * volume * envelope;
        }
    }


    // =========================================================
    // SOUND EFFECTS
    // =========================================================

    // Soft "pop" for every button press
    private static float[] Tap()
    {
        float[] buffer = Buffer(0.09f);
        Sweep(buffer, 0f, 0.08f, 950f, 600f, 0.5f, Sine);
        return Normalize(buffer, 0.7f);
    }

    // Rising arpeggio when the start flag is pressed
    private static float[] Start()
    {
        float[] buffer = Buffer(0.5f);
        int[] notes = { 72, 76, 79, 84 };
        for (int i = 0; i < notes.Length; i++)
        {
            AddNote(buffer, i * 0.07f, i == 3 ? 0.3f : 0.09f, Freq(notes[i]), 0.3f, Lead, 0.3f);
        }
        return Normalize(buffer, 0.8f);
    }

    // Little blip for every step forward
    private static float[] Step()
    {
        float[] buffer = Buffer(0.12f);
        AddNote(buffer, 0f, 0.1f, Freq(76), 0.4f, Triangle, 0.1f);
        return Normalize(buffer, 0.6f);
    }

    // Upward swoop for a turn
    private static float[] Turn()
    {
        float[] buffer = Buffer(0.18f);
        Sweep(buffer, 0f, 0.16f, 420f, 760f, 0.4f, Triangle);
        return Normalize(buffer, 0.6f);
    }

    // Friendly "uh-oh": two falling notes, never scary
    private static float[] Oops()
    {
        float[] buffer = Buffer(0.6f);
        Sweep(buffer, 0f, 0.22f, Freq(67), Freq(64), 0.35f, Lead);
        Sweep(buffer, 0.25f, 0.32f, Freq(64), Freq(60), 0.35f, Lead);
        return Normalize(buffer, 0.75f);
    }

    // Short fanfare for reaching the flag
    private static float[] Win()
    {
        float[] buffer = Buffer(1.3f);
        int[] notes = { 67, 72, 76 };
        for (int i = 0; i < notes.Length; i++)
        {
            AddNote(buffer, i * 0.12f, 0.11f, Freq(notes[i]), 0.3f, Lead, 0.3f);
        }
        foreach (int note in new[] { 72, 76, 79, 84 })
        {
            AddNote(buffer, 0.38f, 0.8f, Freq(note), 0.18f, Lead, 0.5f);
        }
        return Normalize(buffer, 0.85f);
    }

    // Sparkly bells and a shaker for confetti and the big celebration
    private static float[] Cheer()
    {
        float[] buffer = Buffer(1.6f);
        int[] scale = { 84, 86, 88, 91, 93, 96 };
        System.Random random = new System.Random(3);

        for (int i = 0; i < 16; i++)
        {
            int note = scale[random.Next(scale.Length)];
            AddNote(buffer, i * 0.06f, 0.4f, Freq(note), 0.16f, Sine, 0.08f);
        }
        for (int i = 0; i < 10; i++)
        {
            AddHat(buffer, i * 0.09f, 0.08f, random);
        }
        return Normalize(buffer, 0.8f);
    }


    // A clear xylophone-like note for the note blocks. The same bell
    // as the music, so mTiny sings in the game's own voice.
    private static float[] Note(int midi)
    {
        float[] buffer = Buffer(1.2f);
        AddBell(buffer, 0f, Freq(midi), 0.5f, 0.28f);
        FadeOut(buffer, 0.15f);
        return Normalize(buffer, 0.8f);
    }

    // Short fade at the very end. AddBell wraps its tail to the start,
    // which suits loops but must never click in a one-shot.
    private static void FadeOut(float[] buffer, float seconds)
    {
        int count = Mathf.Min(buffer.Length, Mathf.RoundToInt(seconds * Rate));
        for (int i = 0; i < count; i++)
        {
            buffer[buffer.Length - 1 - i] *= (float)i / count;
        }
    }


    // =========================================================
    // TONE BUILDING
    // =========================================================

    private static float Sine(float phase) { return Mathf.Sin(phase * 2f * Mathf.PI); }

    private static float Triangle(float phase) { return 4f * Mathf.Abs(phase - Mathf.Floor(phase + 0.5f)) - 1f; }

    // Mostly triangle with a little square for a toy-like brightness
    private static float Lead(float phase)
    {
        float square = phase % 1f < 0.5f ? 1f : -1f;
        return Triangle(phase) * 0.7f + square * 0.3f;
    }

    private static float Freq(int midi)
    {
        return 440f * Mathf.Pow(2f, (midi - 69) / 12f);
    }

    // Plucked note: quick attack, decays toward `sustain`, short release
    private static void AddNote(float[] buffer, float start, float length, float freq, float volume,
        Func<float, float> wave, float sustain)
    {
        int first = Mathf.RoundToInt(start * Rate);
        int count = Mathf.RoundToInt(length * Rate);
        int release = Mathf.Min(count / 3, Mathf.RoundToInt(0.03f * Rate));

        for (int i = 0; i < count && first + i < buffer.Length; i++)
        {
            float time = (float)i / Rate;
            float attack = Mathf.Clamp01(time / 0.005f);
            float decay = sustain + (1f - sustain) * Mathf.Exp(-time / 0.12f);
            float tail = i > count - release ? (count - i) / (float)release : 1f;

            buffer[first + i] += wave(freq * time) * volume * attack * decay * tail;
        }
    }

    // A note that glides from one pitch to another
    private static void Sweep(float[] buffer, float start, float length, float fromFreq, float toFreq,
        float volume, Func<float, float> wave)
    {
        int first = Mathf.RoundToInt(start * Rate);
        int count = Mathf.RoundToInt(length * Rate);
        float phase = 0f;

        for (int i = 0; i < count && first + i < buffer.Length; i++)
        {
            float k = (float)i / count;
            phase += Mathf.Lerp(fromFreq, toFreq, k) / Rate;
            float envelope = Mathf.Clamp01(i / (0.005f * Rate)) * (1f - k * k);

            buffer[first + i] += wave(phase) * volume * envelope;
        }
    }

    // High, short noise tick (each sample minus the last keeps it bright)
    private static void AddHat(float[] buffer, float start, float volume, System.Random random)
    {
        int first = Mathf.RoundToInt(start * Rate);
        int count = Mathf.RoundToInt(0.03f * Rate);
        float last = 0f;

        for (int i = 0; i < count && first + i < buffer.Length; i++)
        {
            float white = (float)random.NextDouble() * 2f - 1f;
            buffer[first + i] += (white - last) * 0.5f * volume * (1f - (float)i / count);
            last = white;
        }
    }

    private static float[] Buffer(float seconds)
    {
        return new float[Mathf.RoundToInt(seconds * Rate)];
    }

    private static float[] Normalize(float[] buffer, float peak)
    {
        float max = 0.0001f;
        foreach (float sample in buffer) max = Mathf.Max(max, Mathf.Abs(sample));
        for (int i = 0; i < buffer.Length; i++) buffer[i] *= peak / max;
        return buffer;
    }


    // =========================================================
    // WAV OUTPUT
    // =========================================================

    private static void Write(string name, float[] samples, bool isMusic)
    {
        string path = Folder + "/" + name + ".wav";

        using (BinaryWriter writer = new BinaryWriter(File.Create(path)))
        {
            int dataBytes = samples.Length * 2;

            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + dataBytes);
            writer.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
            writer.Write(16);
            writer.Write((short)1);      // PCM
            writer.Write((short)1);      // mono
            writer.Write(Rate);
            writer.Write(Rate * 2);      // bytes per second
            writer.Write((short)2);      // block align
            writer.Write((short)16);     // bits per sample
            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(dataBytes);

            foreach (float sample in samples)
            {
                writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue));
            }
        }

        AssetDatabase.ImportAsset(path);

        // Music streams compressed; effects load ready to play instantly
        AudioImporter importer = (AudioImporter)AssetImporter.GetAtPath(path);
        AudioImporterSampleSettings sampleSettings = importer.defaultSampleSettings;
        sampleSettings.loadType = isMusic ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
        sampleSettings.compressionFormat = isMusic ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
        sampleSettings.quality = 0.6f;
        importer.defaultSampleSettings = sampleSettings;
        importer.forceToMono = true;
        importer.SaveAndReimport();
    }
}
