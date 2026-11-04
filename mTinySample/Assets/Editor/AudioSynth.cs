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

        Debug.Log("[mTiny] Music and sound effects written to " + Folder);
    }


    // =========================================================
    // MUSIC
    // =========================================================
    // A cheerful 16-bar loop in C major at 112 BPM: a soft lead,
    // bouncing bass, a light arpeggio and gentle drums.
    // =========================================================

    private const float Bpm = 112f;

    // Each bar: chord root (MIDI) and melody as (note, eighths); -1 = rest
    private static readonly int[] ChordRoots = { 48, 43, 45, 41, 48, 41, 43, 48 };

    private static readonly int[][] ChordTones =
    {
        new[] { 60, 64, 67 }, new[] { 59, 62, 67 }, new[] { 60, 64, 69 }, new[] { 60, 65, 69 },
        new[] { 60, 64, 67 }, new[] { 60, 65, 69 }, new[] { 59, 62, 67 }, new[] { 60, 64, 67 }
    };

    private static readonly int[][] MelodyA =
    {
        new[] { 76, 2, 79, 2, 76, 1, 74, 1, 72, 2 },
        new[] { 74, 2, 67, 2, 71, 2, 74, 2 },
        new[] { 72, 2, 76, 2, 81, 3, 79, 1 },
        new[] { 77, 2, 76, 2, 74, 2, 72, 2 },
        new[] { 76, 1, 76, 1, 79, 2, 84, 2, 79, 2 },
        new[] { 81, 2, 79, 2, 77, 2, 81, 2 },
        new[] { 79, 2, 77, 2, 74, 2, 71, 2 },
        new[] { 72, 4, -1, 4 }
    };

    private static readonly int[][] MelodyB =
    {
        new[] { 72, 1, 74, 1, 76, 2, 79, 2, 76, 2 },
        new[] { 74, 3, 71, 1, 67, 2, 71, 2 },
        new[] { 69, 2, 72, 2, 76, 2, 72, 2 },
        new[] { 77, 3, 76, 1, 74, 2, 77, 2 },
        new[] { 79, 2, 76, 2, 72, 2, 76, 2 },
        new[] { 77, 2, 81, 2, 84, 2, 81, 2 },
        new[] { 79, 2, 74, 2, 71, 2, 74, 2 },
        new[] { 72, 2, 67, 2, 72, 4 }
    };

    private static float[] Music()
    {
        float beat = 60f / Bpm;
        float eighth = beat / 2f;
        int bars = 16;
        float[] buffer = new float[Mathf.RoundToInt(bars * 4 * beat * Rate)];
        System.Random noise = new System.Random(7);

        for (int bar = 0; bar < bars; bar++)
        {
            int chord = bar % 8;
            float barStart = bar * 4 * beat;
            int[] melody = bar < 8 ? MelodyA[chord] : MelodyB[chord];

            // Lead
            float t = barStart;
            for (int i = 0; i + 1 < melody.Length; i += 2)
            {
                float length = melody[i + 1] * eighth;
                if (melody[i] >= 0)
                {
                    AddNote(buffer, t, length * 0.95f, Freq(melody[i]), 0.22f, Lead, 0.25f);
                }
                t += length;
            }

            // Bass on beats 1-4, root then fifth
            for (int b = 0; b < 4; b++)
            {
                int note = ChordRoots[chord] + (b % 2 == 0 ? 0 : 7);
                AddNote(buffer, barStart + b * beat, beat * 0.8f, Freq(note), 0.22f, Triangle, 0.6f);
            }

            // Arpeggio in eighths
            for (int e = 0; e < 8; e++)
            {
                int[] tones = ChordTones[chord];
                AddNote(buffer, barStart + e * eighth, eighth * 0.9f, Freq(tones[e % 3]), 0.06f, Sine, 0.15f);
            }

            // Drums: kick on 1 and 3, hat on the off-beats
            for (int b = 0; b < 4; b++)
            {
                if (b % 2 == 0) AddKick(buffer, barStart + b * beat, 0.35f);
                AddHat(buffer, barStart + b * beat + eighth, 0.045f, noise);
            }
        }

        return Normalize(buffer, 0.85f);
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

    private static void AddKick(float[] buffer, float start, float volume)
    {
        int first = Mathf.RoundToInt(start * Rate);
        int count = Mathf.RoundToInt(0.12f * Rate);
        float phase = 0f;

        for (int i = 0; i < count && first + i < buffer.Length; i++)
        {
            float k = (float)i / count;
            phase += Mathf.Lerp(130f, 45f, k) / Rate;
            buffer[first + i] += Sine(phase) * volume * (1f - k) * (1f - k);
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
