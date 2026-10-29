using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SomeGame.EditorTools
{
    /// <summary>
    /// Synthesizes the game's sound effects into Assets/Audio/Sfx as 16-bit mono WAV files: engine and
    /// tyre-screech loops, boost, jump, landing, splash, oil spin, crash, countdown beeps, UI click, lap
    /// chime, star ding and the win / lose jingles. Everything is built from oscillators, filtered noise
    /// and envelopes, so it can be tweaked here and regenerated.
    /// </summary>
    public static class AudioGenerator
    {
        public const string Folder = "Assets/Audio/Sfx";
        const int Rate = 44100;

        [MenuItem("SomeGame/Generate Sounds")]
        public static void GenerateAll()
        {
            Directory.CreateDirectory(Folder);
            Write("EngineLoop", EngineLoop());
            Write("ScreechLoop", ScreechLoop());
            Write("Boost", Boost());
            Write("Jump", Jump());
            Write("Land", Land());
            Write("Splash", Splash());
            Write("OilSpin", OilSpin());
            Write("Crash", Crash());
            Write("BeepLow", Beep(659.25f, 0.16f));
            Write("BeepHigh", Beep(1318.5f, 0.45f));
            Write("Click", Click());
            Write("Lap", Chime(new[] { 1046.5f, 1568f }, 0.11f, 0.5f));
            Write("Star", Chime(new[] { 1760f }, 0f, 0.6f, bright: true));
            Write("Win", Jingle(new[] { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f }, 0.11f, 1.1f));
            Write("Lose", Jingle(new[] { 659.25f, 587.33f, 523.25f, 392f }, 0.16f, 0.9f));
            Write("IceSlide", IceSlide());
            Write("RoofLand", RoofLand());
            Write("WallWhoosh", WallWhoosh());
            Write("Penguin", Penguin());
            Write("Moo", Moo());
            Write("DashPanel", DashPanel());
            Write("SnowPoof", SnowPoof());
            Write("IceCrack", IceCrack());
            Write("ShipHorn", ShipHorn());
            Write("Unlock", Unlock());
            Write("Fanfare", Fanfare());
            Write("Trick", Chime(new[] { 1318.5f, 1975.5f }, 0.06f, 0.35f, bright: true));
            AssetDatabase.Refresh();
            foreach (var path in Directory.GetFiles(Folder, "*.wav"))
            {
                var importer = (AudioImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.ADPCM;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
            Debug.Log("Sounds generated in " + Folder);
        }

        // ------------------------------------------------------------------ sounds

        // One second, seamless: a four-cylinder buzz at 60 Hz (an exact number of cycles per second) with
        // firing pulses and a little rumble. The car scripts change pitch and volume.
        static float[] EngineLoop()
        {
            var s = new float[Rate];
            var noise = new Noise(1);
            float lp = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                double t = (double)i / Rate;
                double phase = t * 60.0;
                float saw = 0f;
                for (int h = 1; h <= 9; h++) saw += (float)Math.Sin(2 * Math.PI * h * phase) / h * (h % 2 == 0 ? 0.7f : 1f);
                float firing = 0.6f + 0.4f * (float)Math.Pow(0.5 + 0.5 * Math.Sin(2 * Math.PI * 120.0 * t), 3);
                lp += (noise.Next() - lp) * 0.05f;
                s[i] = (saw * 0.45f * firing + lp * 0.5f) * 0.8f;
            }
            return Normalize(s, 0.7f);
        }

        // Tyre squeal: a wavering tone around 1.1 kHz over band-passed noise; crossfaded to loop.
        static float[] ScreechLoop()
        {
            var s = new float[Rate];
            var noise = new Noise(2);
            var band = new BandPass(1400f, 2.5f);
            double phase = 0;
            for (int i = 0; i < s.Length; i++)
            {
                double t = (double)i / Rate;
                double f = 1100 + 60 * Math.Sin(2 * Math.PI * 7 * t) + 30 * Math.Sin(2 * Math.PI * 13 * t);
                phase += f / Rate;
                float tone = (float)Math.Sin(2 * Math.PI * phase) * 0.35f + (float)Math.Sin(4 * Math.PI * phase) * 0.12f;
                s[i] = tone + band.Process(noise.Next()) * 0.9f;
            }
            return Normalize(Loopable(s, 0.08f), 0.6f);
        }

        // Rising whoosh: noise through a band-pass sweeping up, with a rising tone.
        static float[] Boost()
        {
            int n = (int)(0.9f * Rate);
            var s = new float[n];
            var noise = new Noise(3);
            var band = new BandPass(300f, 1.2f);
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                band.SetFrequency(Mathf.Lerp(300f, 3200f, t * t));
                phase += Mathf.Lerp(180f, 700f, t) / Rate;
                float env = Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.6f));
                s[i] = (band.Process(noise.Next()) * 1.4f + (float)Math.Sin(2 * Math.PI * phase) * 0.25f) * env;
            }
            return Normalize(s, 0.8f);
        }

        // Skidding onto ice: a bright, glassy hiss with little crackles.
        static float[] IceSlide()
        {
            int n = (int)(0.8f * Rate);
            var s = new float[n];
            var noise = new Noise(11);
            var band = new BandPass(5200f, 3f);
            var crackle = new Noise(12);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float env = Mathf.Min(1f, t * 20f) * (1f - t);
                float crack = crackle.Next() > 0.995f ? 1f : 0f;
                s[i] = band.Process(noise.Next()) * 1.2f * env + crack * 0.6f * (1f - t);
            }
            return Normalize(s, 0.55f);
        }

        // Landing on a snowy roof: a deep wooden thud with a soft crunch of snow.
        static float[] RoofLand()
        {
            int n = (int)(0.45f * Rate);
            var s = new float[n];
            var noise = new Noise(13);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float thud = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(110f, 60f, t / 0.45f) * t) * Mathf.Exp(-t * 14f);
                float knock = Mathf.Sin(2f * Mathf.PI * 420f * t) * Mathf.Exp(-t * 60f) * 0.4f;
                lp += (noise.Next() - lp) * 0.15f;
                s[i] = thud + knock + lp * 0.8f * Mathf.Exp(-t * 9f);
            }
            return Normalize(s, 0.8f);
        }

        // Riding up the ice wall: an airy swoosh that rises and falls.
        static float[] WallWhoosh()
        {
            int n = (int)(0.7f * Rate);
            var s = new float[n];
            var noise = new Noise(14);
            var band = new BandPass(600f, 1.5f);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                band.SetFrequency(Mathf.Lerp(600f, 2400f, Mathf.Sin(Mathf.PI * t)));
                s[i] = band.Process(noise.Next()) * Mathf.Sin(Mathf.PI * t);
            }
            return Normalize(s, 0.6f);
        }

        // A startled penguin: two quick nasal honks.
        static float[] Penguin()
        {
            int n = (int)(0.42f * Rate);
            var s = new float[n];
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                bool second = t > 0.2f;
                float local = second ? t - 0.2f : t;
                float env = local < 0.17f ? Mathf.Sin(Mathf.PI * local / 0.17f) : 0f;
                float f = (second ? 640f : 560f) + 40f * Mathf.Sin(2f * Mathf.PI * 30f * t) - 180f * local;
                phase += f / Rate;
                float p = (float)(phase % 1.0);
                float nasal = (p < 0.3f ? 1f : -0.4f) * 0.5f + Mathf.Sin(2f * Mathf.PI * p * 3f) * 0.3f;
                s[i] = nasal * env;
            }
            return Normalize(s, 0.6f);
        }

        // A grumpy cow: a low, nasal "mmmoooh" sliding down, the mouth opening from hum to vowel.
        static float[] Moo()
        {
            int n = (int)(1.0f * Rate);
            var s = new float[n];
            var low = new BandPass(400f, 1.2f);
            var high = new BandPass(900f, 2f);
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float env = Mathf.Clamp01(t / 0.08f) * Mathf.Clamp01((1f - t) / 0.3f);
                float f = 150f - 45f * t + 4f * Mathf.Sin(2f * Mathf.PI * 5.5f * t);
                phase += f / Rate;
                float saw = (float)(phase % 1.0) * 2f - 1f;
                float open = Mathf.Clamp01((t - 0.12f) / 0.35f); // the hum opens into "ooh"
                low.SetFrequency(Mathf.Lerp(300f, 520f, open));
                high.SetFrequency(Mathf.Lerp(700f, 1050f, open));
                s[i] = (low.Process(saw) * 1.2f + high.Process(saw) * 0.5f * open) * env;
            }
            return Normalize(s, 0.7f);
        }

        // Over a dash panel: an electric zap chirping up, with a shimmer.
        static float[] DashPanel()
        {
            int n = (int)(0.5f * Rate);
            var s = new float[n];
            double phase = 0, phase2 = 0;
            var noise = new Noise(15);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                phase += Mathf.Lerp(400f, 2200f, t * t) / Rate;
                phase2 += Mathf.Lerp(800f, 4400f, t) / Rate;
                float square = Math.Sin(2 * Math.PI * phase) > 0 ? 0.5f : -0.5f;
                float env = Mathf.Min(1f, t * 25f) * (1f - t);
                s[i] = (square * 0.6f + (float)Math.Sin(2 * Math.PI * phase2) * 0.3f + noise.Next() * 0.1f) * env;
            }
            return Normalize(s, 0.6f);
        }

        // Bumping a snowball: a soft, muffled poof.
        static float[] SnowPoof()
        {
            int n = (int)(0.35f * Rate);
            var s = new float[n];
            var noise = new Noise(16);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                lp += (noise.Next() - lp) * 0.08f;
                s[i] = lp * Mathf.Exp(-t * 12f) * Mathf.Min(1f, t * 200f) + Mathf.Sin(2f * Mathf.PI * 80f * t) * Mathf.Exp(-t * 20f) * 0.5f;
            }
            return Normalize(s, 0.7f);
        }

        /// <summary>Only the fjord's sounds (cracking ice, the icebreaker's horn), hooked into the audio library.</summary>
        public static void GenerateFjordSounds()
        {
            Directory.CreateDirectory(Folder);
            Write("IceCrack", IceCrack());
            Write("ShipHorn", ShipHorn());
            AssetDatabase.Refresh();
            foreach (var name in new[] { "IceCrack", "ShipHorn" })
            {
                var importer = (AudioImporter)AssetImporter.GetAtPath($"{Folder}/{name}.wav");
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.ADPCM;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
            var library = AssetDatabase.LoadAssetAtPath<SomeGame.Audio.AudioLibrary>("Assets/Resources/AudioLibrary.asset");
            if (library == null) return;
            library.iceCrack = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Folder}/IceCrack.wav");
            library.shipHorn = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Folder}/ShipHorn.wav");
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Only the freestyle park's bumper, hooked into the audio library.</summary>
        public static void GenerateParkSounds()
        {
            Directory.CreateDirectory(Folder);
            Write("Bumper", Bumper());
            AssetDatabase.Refresh();
            var importer = (AudioImporter)AssetImporter.GetAtPath($"{Folder}/Bumper.wav");
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.ADPCM;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
            var library = AssetDatabase.LoadAssetAtPath<SomeGame.Audio.AudioLibrary>("Assets/Resources/AudioLibrary.asset");
            if (library == null) return;
            library.bumper = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Folder}/Bumper.wav");
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Only the express's whistle, hooked into the audio library.</summary>
        public static void GenerateExpressSounds()
        {
            Directory.CreateDirectory(Folder);
            Write("TrainWhistle", TrainWhistle());
            AssetDatabase.Refresh();
            var importer = (AudioImporter)AssetImporter.GetAtPath($"{Folder}/TrainWhistle.wav");
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.ADPCM;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
            var library = AssetDatabase.LoadAssetAtPath<SomeGame.Audio.AudioLibrary>("Assets/Resources/AudioLibrary.asset");
            if (library == null) return;
            library.trainWhistle = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Folder}/TrainWhistle.wav");
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Only the avalanche's sounds (its rumble, the thunderclap), hooked into the audio library.</summary>
        public static void GenerateAvalancheSounds()
        {
            Directory.CreateDirectory(Folder);
            Write("AvalancheRumble", AvalancheRumble());
            Write("Thunder", Thunder());
            AssetDatabase.Refresh();
            foreach (var name in new[] { "AvalancheRumble", "Thunder" })
            {
                var importer = (AudioImporter)AssetImporter.GetAtPath($"{Folder}/{name}.wav");
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.ADPCM;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
            var library = AssetDatabase.LoadAssetAtPath<SomeGame.Audio.AudioLibrary>("Assets/Resources/AudioLibrary.asset");
            if (library == null) return;
            library.avalancheRumble = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Folder}/AvalancheRumble.wav");
            library.thunder = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Folder}/Thunder.wav");
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
        }

        // Two seconds of deep roaring rumble that loops seamlessly: low-passed noise, slowly swelling, with a
        // hiss of snow on top (the ends cross-faded into each other).
        static float[] AvalancheRumble()
        {
            int n = 2 * Rate, fade = Rate / 4;
            var raw = new float[n + fade];
            var noise = new Noise(41);
            float lp1 = 0f, lp2 = 0f, hp = 0f;
            for (int i = 0; i < raw.Length; i++)
            {
                float white = noise.Next();
                lp1 += (white - lp1) * 0.02f;
                lp2 += (lp1 - lp2) * 0.05f;
                hp += (white - hp) * 0.5f;
                float t = (float)i / Rate;
                raw[i] = lp2 * 6f * (0.8f + 0.2f * Mathf.Sin(t * Mathf.PI * 2f * 0.5f)) + (white - hp) * 0.08f;
            }
            var s = new float[n];
            for (int i = 0; i < n; i++)
            {
                s[i] = raw[i];
                if (i < fade) { float k = (float)i / fade; s[i] = raw[i] * k + raw[n + i] * (1f - k); }
            }
            return Normalize(s, 0.7f);
        }

        // A thunderclap: a sharp crack, then a long rolling rumble dying away.
        static float[] Thunder()
        {
            int n = (int)(2.4f * Rate);
            var s = new float[n];
            var noise = new Noise(53);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate, white = noise.Next();
                lp += (white - lp) * 0.03f;
                float crack = white * Mathf.Exp(-t * 18f);
                float roll = lp * 5f * Mathf.Exp(-t * 1.3f) * (0.7f + 0.3f * Mathf.Sin(t * 9f + Mathf.Sin(t * 3f)));
                s[i] = crack * 0.8f + roll;
            }
            return Normalize(s, 0.85f);
        }

        // A steam whistle: a breathy chord (three notes) rising into place, with the hiss of steam.
        static float[] TrainWhistle()
        {
            int n = (int)(1.3f * Rate);
            var s = new float[n];
            var noise = new Noise(31);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float env = Mathf.Min(1f, t / 0.08f) * Mathf.Clamp01((1.3f - t) / 0.3f);
                float bend = 1f - 0.06f * Mathf.Exp(-t * 10f);
                float v = 0f;
                foreach (float f in new[] { 587.33f, 739.99f, 880f }) v += Mathf.Sin(2f * Mathf.PI * f * bend * t) + 0.3f * Mathf.Sin(4f * Mathf.PI * f * bend * t);
                float white = noise.Next();
                lp += (white - lp) * 0.3f;
                s[i] = (v * 0.3f + (white - lp) * 0.35f) * env;
            }
            return Normalize(s, 0.6f);
        }

        // A pinball bumper: a springy "boing" (a pitch that drops fast) with a bright ding on top.
        static float[] Bumper()
        {
            int n = (int)(0.45f * Rate);
            var s = new float[n];
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float f = 260f + 520f * Mathf.Exp(-t * 18f) + 20f * Mathf.Sin(t * 90f);
                phase += 2f * Mathf.PI * f / Rate;
                float boing = Mathf.Sin(phase) * Mathf.Exp(-t * 7f);
                float ding = (Mathf.Sin(2f * Mathf.PI * 1568f * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * 2349f * t)) * Mathf.Exp(-t * 14f) * 0.35f;
                s[i] = boing * 0.8f + ding;
            }
            return Normalize(s, 0.75f);
        }

        // Ice cracking: sharp snaps (clicks with a bright ring) running into a low groan.
        static float[] IceCrack()
        {
            int n = (int)(0.9f * Rate);
            var s = new float[n];
            var noise = new Noise(23);
            float[] snaps = { 0f, 0.07f, 0.11f, 0.19f, 0.26f, 0.38f };
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate, v = 0f;
                float white = noise.Next();
                foreach (float at in snaps)
                {
                    if (t < at) continue;
                    float u = t - at;
                    v += white * Mathf.Exp(-u * 90f) * 0.9f + Mathf.Sin(2f * Mathf.PI * (2600f - at * 2000f) * u) * Mathf.Exp(-u * 60f) * 0.35f;
                }
                lp += (white - lp) * 0.01f;
                float groan = Mathf.Sin(2f * Mathf.PI * (70f + 25f * Mathf.Sin(t * 9f)) * t) * Mathf.Exp(-t * 3f) * Mathf.Min(1f, t * 8f) * 0.5f;
                s[i] = v + groan + lp * 2f * Mathf.Exp(-t * 2f);
            }
            return Normalize(s, 0.75f);
        }

        // The icebreaker's horn: two deep tones together, swelling in, holding, dying away.
        static float[] ShipHorn()
        {
            int n = (int)(1.6f * Rate);
            var s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float env = Mathf.Min(1f, t / 0.12f) * Mathf.Clamp01((1.6f - t) / 0.35f);
                float v = 0f;
                foreach (float f in new[] { 110f, 138.6f })
                    for (int h = 1; h <= 6; h++) v += Mathf.Sin(2f * Mathf.PI * f * h * t + h) / (h * 1.3f);
                s[i] = v * env * (1f + 0.05f * Mathf.Sin(t * 30f));
            }
            return Normalize(s, 0.7f);
        }

        // A race unlocks: a metallic clink of the lock, then a bright rising arpeggio.
        static float[] Unlock()
        {
            int n = (int)(0.9f * Rate);
            var s = new float[n];
            float[] notes = { 783.99f, 987.77f, 1174.66f, 1567.98f };
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float clink = (Mathf.Sin(2f * Mathf.PI * 2350f * t) + Mathf.Sin(2f * Mathf.PI * 3170f * t) * 0.6f) * Mathf.Exp(-t * 35f) * 0.6f;
                float tune = 0f;
                for (int k = 0; k < notes.Length; k++)
                {
                    float start = 0.12f + k * 0.08f;
                    if (t < start) continue;
                    float u = t - start;
                    tune += (Mathf.Sin(2f * Mathf.PI * notes[k] * u) + Mathf.Sin(4f * Mathf.PI * notes[k] * u) * 0.25f) * Mathf.Exp(-u * 4f) * 0.35f;
                }
                s[i] = clink + tune;
            }
            return Normalize(s, 0.75f);
        }

        // A new cup: a short brassy fanfare, then a held major chord with a shimmer.
        static float[] Fanfare()
        {
            int n = (int)(2.4f * Rate);
            var s = new float[n];
            (float start, float length, float[] chord)[] parts =
            {
                (0f, 0.16f, new[] { 523.25f }), (0.17f, 0.16f, new[] { 523.25f }), (0.34f, 0.16f, new[] { 659.25f }),
                (0.51f, 1.8f, new[] { 523.25f, 659.25f, 783.99f, 1046.5f }),
            };
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float v = 0f;
                foreach (var (start, length, chord) in parts)
                {
                    if (t < start || t > start + length + 0.3f) continue;
                    float u = t - start;
                    float env = Mathf.Min(1f, u * 40f) * (u < length ? 1f - 0.3f * u / length : Mathf.Exp(-(u - length) * 10f) * 0.7f);
                    foreach (float f in chord)
                        for (int h = 1; h <= 4; h++) v += Mathf.Sin(2f * Mathf.PI * f * h * u) / (h * h) * env * 0.25f; // brassy
                }
                v += Mathf.Sin(2f * Mathf.PI * 2093f * t) * 0.05f * Mathf.Sin(t * 40f) * (t > 0.5f ? Mathf.Exp(-(t - 0.5f) * 1.5f) : 0f);
                s[i] = v;
            }
            return Normalize(s, 0.8f);
        }

        static float[] Jump()
        {
            int n = (int)(0.35f * Rate);
            var s = new float[n];
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                phase += Mathf.Lerp(220f, 620f, Mathf.Sqrt(t)) / Rate;
                s[i] = (float)Math.Sin(2 * Math.PI * phase) * (1f - t) * Mathf.Min(1f, t * 30f);
            }
            return Normalize(s, 0.55f);
        }

        // Thump: a falling low sine with a short noise crunch.
        static float[] Land()
        {
            int n = (int)(0.35f * Rate);
            var s = new float[n];
            var noise = new Noise(4);
            float lp = 0f;
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                phase += Mathf.Lerp(110f, 38f, t) / Rate;
                lp += (noise.Next() - lp) * 0.2f;
                s[i] = (float)Math.Sin(2 * Math.PI * phase) * Mathf.Exp(-t * 6f) + lp * Mathf.Exp(-t * 25f) * 0.8f;
            }
            return Normalize(s, 0.85f);
        }

        // Splash: a burst of filtered noise that fades, with bubbly droplet blips on top.
        static float[] Splash()
        {
            int n = (int)(1.0f * Rate);
            var s = new float[n];
            var noise = new Noise(5);
            var band = new BandPass(900f, 0.8f);
            var rng = new System.Random(5);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                band.SetFrequency(Mathf.Lerp(1800f, 500f, t));
                s[i] = band.Process(noise.Next()) * Mathf.Exp(-t * 4f) * Mathf.Min(1f, t * 80f) * 1.6f;
            }
            for (int k = 0; k < 14; k++)
            {
                int start = (int)(rng.NextDouble() * 0.7 * n);
                float f = 700f + (float)rng.NextDouble() * 1400f;
                int len = (int)(0.05f * Rate);
                double phase = 0;
                for (int j = 0; j < len && start + j < n; j++)
                {
                    float u = (float)j / len;
                    phase += f * (1f + u * 0.8f) / Rate;
                    s[start + j] += (float)Math.Sin(2 * Math.PI * phase) * (1f - u) * 0.25f;
                }
            }
            return Normalize(s, 0.75f);
        }

        // Oil: a squealing tone that wobbles and falls as the car spins round.
        static float[] OilSpin()
        {
            int n = (int)(0.9f * Rate);
            var s = new float[n];
            var noise = new Noise(6);
            var band = new BandPass(1200f, 3f);
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                double f = Mathf.Lerp(1300f, 700f, t) + 120 * Math.Sin(2 * Math.PI * 9 * t);
                phase += f / Rate;
                band.SetFrequency((float)f);
                float env = Mathf.Min(1f, t * 20f) * (1f - t);
                s[i] = ((float)Math.Sin(2 * Math.PI * phase) * 0.4f + band.Process(noise.Next()) * 0.8f) * env;
            }
            return Normalize(s, 0.6f);
        }

        // Crash: a short crunch of noise, a low thud and a metallic ring.
        static float[] Crash()
        {
            int n = (int)(0.4f * Rate);
            var s = new float[n];
            var noise = new Noise(7);
            float[] partials = { 523f, 1187f, 1931f, 2779f };
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                double tt = (double)i / Rate;
                float ring = 0f;
                foreach (var f in partials) ring += (float)Math.Sin(2 * Math.PI * f * tt) * 0.15f;
                s[i] = noise.Next() * Mathf.Exp(-t * 18f) * 0.9f
                     + (float)Math.Sin(2 * Math.PI * 70 * tt) * Mathf.Exp(-t * 10f) * 0.8f
                     + ring * Mathf.Exp(-t * 7f);
            }
            return Normalize(s, 0.8f);
        }

        static float[] Beep(float f, float seconds)
        {
            int n = (int)(seconds * Rate);
            var s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                double tt = (double)i / Rate;
                float tone = (float)(Math.Sin(2 * Math.PI * f * tt) + 0.3 * Math.Sin(2 * Math.PI * 2 * f * tt) + 0.15 * Math.Sin(2 * Math.PI * 3 * f * tt));
                s[i] = tone * Mathf.Min(1f, t * 60f) * Mathf.Min(1f, (1f - t) * 8f);
            }
            return Normalize(s, 0.6f);
        }

        static float[] Click()
        {
            int n = (int)(0.06f * Rate);
            var s = new float[n];
            var noise = new Noise(8);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                double tt = (double)i / Rate;
                s[i] = ((float)Math.Sin(2 * Math.PI * 1800 * tt) * 0.6f + noise.Next() * 0.3f) * Mathf.Exp(-t * 14f);
            }
            return Normalize(s, 0.5f);
        }

        // Bell-like notes (sine plus a bright partial) played one after another.
        static float[] Chime(float[] notes, float gap, float decay, bool bright = false)
        {
            int n = (int)((gap * notes.Length + decay) * Rate);
            var s = new float[n];
            for (int k = 0; k < notes.Length; k++)
            {
                int start = (int)(k * gap * Rate);
                for (int i = start; i < n; i++)
                {
                    double tt = (double)(i - start) / Rate;
                    float env = Mathf.Exp(-(float)tt * 5f / decay) * Mathf.Min(1f, (float)tt * 400f);
                    float f = notes[k];
                    s[i] += (float)(Math.Sin(2 * Math.PI * f * tt) + (bright ? 0.5 : 0.25) * Math.Sin(2 * Math.PI * f * 2.76 * tt)) * env * 0.5f;
                }
            }
            return Normalize(s, 0.6f);
        }

        // A short tune: plucky square-ish notes over a soft pad of the first and last note.
        static float[] Jingle(float[] notes, float step, float tail)
        {
            int n = (int)((step * notes.Length + tail) * Rate);
            var s = new float[n];
            for (int k = 0; k < notes.Length; k++)
            {
                int start = (int)(k * step * Rate);
                bool last = k == notes.Length - 1;
                float decay = last ? tail : step * 2.5f;
                for (int i = start; i < n; i++)
                {
                    double tt = (double)(i - start) / Rate;
                    float env = Mathf.Exp(-(float)tt * 3f / decay) * Mathf.Min(1f, (float)tt * 300f);
                    if (env < 0.001f) break;
                    double ph = notes[k] * tt;
                    float pluck = (float)(Math.Sin(2 * Math.PI * ph) + 0.33 * Math.Sin(6 * Math.PI * ph) + 0.2 * Math.Sin(10 * Math.PI * ph));
                    s[i] += pluck * env * 0.45f;
                }
            }
            return Normalize(s, 0.65f);
        }

        // ------------------------------------------------------------------ helpers

        sealed class Noise
        {
            uint _state;
            public Noise(uint seed) => _state = seed * 2654435761u + 1;
            public float Next()
            {
                _state ^= _state << 13; _state ^= _state >> 17; _state ^= _state << 5;
                return (_state & 0xFFFFFF) / (float)0xFFFFFF * 2f - 1f;
            }
        }

        /// <summary>RBJ biquad band-pass (constant peak gain).</summary>
        sealed class BandPass
        {
            readonly float _q;
            float _b0, _b2, _a1, _a2, _x1, _x2, _y1, _y2;
            public BandPass(float frequency, float q) { _q = q; SetFrequency(frequency); }
            public void SetFrequency(float f)
            {
                double w = 2 * Math.PI * f / Rate, alpha = Math.Sin(w) / (2 * _q), a0 = 1 + alpha;
                _b0 = (float)(alpha / a0); _b2 = -_b0;
                _a1 = (float)(-2 * Math.Cos(w) / a0); _a2 = (float)((1 - alpha) / a0);
            }
            public float Process(float x)
            {
                float y = _b0 * x + _b2 * _x2 - _a1 * _y1 - _a2 * _y2;
                _x2 = _x1; _x1 = x; _y2 = _y1; _y1 = y;
                return y;
            }
        }

        // Crossfades the end into the start so the clip loops without a click.
        static float[] Loopable(float[] s, float fadeSeconds)
        {
            int fade = (int)(fadeSeconds * Rate);
            var result = new float[s.Length - fade];
            Array.Copy(s, result, result.Length);
            for (int i = 0; i < fade; i++)
            {
                float t = (float)i / fade;
                result[i] = s[i] * t + s[s.Length - fade + i] * (1f - t);
            }
            return result;
        }

        static float[] Normalize(float[] s, float peak)
        {
            float max = 0.0001f;
            foreach (var v in s) max = Mathf.Max(max, Mathf.Abs(v));
            for (int i = 0; i < s.Length; i++) s[i] = s[i] / max * peak;
            return s;
        }

        static void Write(string name, float[] samples)
        {
            using var stream = new FileStream($"{Folder}/{name}.wav", FileMode.Create);
            using var w = new BinaryWriter(stream);
            int bytes = samples.Length * 2;
            w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + bytes);
            w.Write(new[] { 'W', 'A', 'V', 'E' }); w.Write(new[] { 'f', 'm', 't', ' ' });
            w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(Rate); w.Write(Rate * 2); w.Write((short)2); w.Write((short)16);
            w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(bytes);
            foreach (var v in samples) w.Write((short)Mathf.Clamp(v * 32767f, -32768f, 32767f));
        }
    }
}
