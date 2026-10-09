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
