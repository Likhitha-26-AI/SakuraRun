using System.Collections.Generic;
using UnityEngine;

namespace SakuraRunner
{
    public enum Sfx { Step, Jump, Slide, Land, Swish, Flower, PowerUp, ShieldBreak, Hit, Smash, Click }

    /// <summary>Fully procedural audio: a relaxing pentatonic (Japanese "in/yo"-style) loop plus synthesized SFX.</summary>
    public class AudioManager : MonoBehaviour
    {
        static AudioManager inst;
        const int Rate = 44100;

        AudioSource music;
        AudioSource[] voices;
        int nextVoice;
        readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();

        public static float MusicVolume = 0.5f;

        public static AudioManager Create(Transform parent)
        {
            var g = new GameObject("Audio");
            g.transform.SetParent(parent, false);
            g.AddComponent<AudioListener>();
            return g.AddComponent<AudioManager>();
        }

        public static void Play(Sfx s, float pitch = 1f, float vol = 1f)
        {
            if (inst == null) return;
            inst.PlayInternal(s, pitch, vol);
        }

        void Awake()
        {
            inst = this;
            voices = new AudioSource[10];
            for (int i = 0; i < voices.Length; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
            }
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            music.volume = MusicVolume;
            music.clip = BuildMusic();
            music.Play();
            BuildSfx();
        }

        void Update()
        {
            var gm = GameManager.I;
            float target = MusicVolume;
            if (gm != null && gm.State == GameState.Paused) target *= 0.35f;
            music.volume = Mathf.MoveTowards(music.volume, target, Time.unscaledDeltaTime * 0.8f);
        }

        void PlayInternal(Sfx s, float pitch, float vol)
        {
            if (!clips.TryGetValue(s, out var c)) return;
            var v = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            v.pitch = pitch;
            v.volume = vol * 0.8f;
            v.clip = c;
            v.Play();
        }

        // ------------------------------------------------------------------ synthesis helpers
        static float Pluck(float t, float freq, float decay)
        {
            // Koto/shamisen-ish: a few harmonics with fast-decaying upper partials
            float e = Mathf.Exp(-t * decay);
            float s = Mathf.Sin(2 * Mathf.PI * freq * t)
                    + 0.45f * Mathf.Sin(2 * Mathf.PI * freq * 2f * t) * Mathf.Exp(-t * decay * 0.6f)
                    + 0.18f * Mathf.Sin(2 * Mathf.PI * freq * 3f * t) * Mathf.Exp(-t * decay * 1.4f);
            float atk = Mathf.Clamp01(t * 400f);
            return s * e * atk;
        }

        static float Pad(float t, float freq)
        {
            return Mathf.Sin(2 * Mathf.PI * freq * t) * 0.6f + Mathf.Sin(2 * Mathf.PI * freq * 1.004f * t) * 0.4f;
        }

        static float Hz(int semitonesFromA4) { return 440f * Mathf.Pow(2f, semitonesFromA4 / 12f); }

        AudioClip BuildMusic()
        {
            // 8 bars at 84 BPM, eighth-note grid. Scale: D minor pentatonic-ish with "miyako-bushi" colour (D Eb G A Bb)
            const float bpm = 84f;
            float beat = 60f / bpm;
            float eighth = beat / 2f;
            int bars = 8;
            float total = bars * 4 * beat;
            int n = Mathf.CeilToInt(total * Rate);
            var data = new float[n];

            // semitones relative to A4
            int[] scale = { -7, -6, -2, 0, 1, 5, 7, 8, 12, 13, 17 }; // D4 Eb4 G4 A4 Bb4 D5 E5 F5? -> picked for gentle colour
            int[] melody =
            {
                3, -1, 5, -1, 4, 3, -1, 2,   -1, 3, 4, -1, 5, 6, 5, -1,
                4, -1, 3, 2, -1, 3, -1, 1,   0, -1, 1, 2, 3, -1, -1, -1,
                3, -1, 5, -1, 6, 5, -1, 4,   -1, 5, 6, -1, 7, 6, 5, -1,
                4, 3, -1, 2, 3, -1, 1, -1,   0, -1, -1, -1, 1, -1, -1, -1
            };
            int[] bassRoots = { -19, -19, -14, -17 }; // D2, D2, G2, A2 (rough)

            // melody
            for (int i = 0; i < melody.Length; i++)
            {
                int idx = melody[i];
                if (idx < 0) continue;
                float f = Hz(scale[Mathf.Clamp(idx, 0, scale.Length - 1)]);
                float start = i * eighth;
                Mix(data, start, 1.6f, t => Pluck(t, f, 3.2f) * 0.32f);
            }

            // soft pad chords, one per bar-pair
            int[][] chords =
            {
                new[] { -7, -2, 0 }, new[] { -7, -2, 1 }, new[] { -2, 0, 5 }, new[] { -5, 0, 5 }
            };
            for (int b = 0; b < bars; b++)
            {
                var ch = chords[(b / 2) % chords.Length];
                float start = b * 4 * beat;
                float dur = 4 * beat + 0.5f;
                foreach (int st in ch)
                {
                    float f = Hz(st - 12);
                    Mix(data, start, dur, t =>
                    {
                        float env = Mathf.Clamp01(t / 0.8f) * Mathf.Clamp01((dur - t) / 1.2f);
                        return Pad(t, f) * env * 0.05f;
                    });
                }
                // bass pluck on beats 1 and 3
                float bf = Hz(bassRoots[(b / 2) % bassRoots.Length]);
                Mix(data, start, 2.5f, t => Pluck(t, bf, 2.2f) * 0.22f);
                Mix(data, start + 2 * beat, 2.5f, t => Pluck(t, bf * 1.5f, 2.6f) * 0.14f);
            }

            // soft wood-block tick on every beat
            for (int b = 0; b < bars * 4; b++)
            {
                float start = b * beat;
                float amp = (b % 4 == 0) ? 0.1f : 0.05f;
                Mix(data, start, 0.12f, t => Mathf.Sin(2 * Mathf.PI * 900f * t) * Mathf.Exp(-t * 60f) * amp);
            }

            // simple feedback echo for space, wrapping so the loop is seamless
            int delay = (int)(eighth * 3f * Rate);
            var wet = new float[n];
            for (int i = 0; i < n; i++)
            {
                float d1 = data[(i - delay + n) % n];
                float d2 = data[(i - delay * 2 + n * 2) % n];
                wet[i] = data[i] + d1 * 0.3f + d2 * 0.15f;
            }
            Normalize(wet, 0.7f);
            var clip = AudioClip.Create("SakuraTheme", n, 1, Rate, false);
            clip.SetData(wet, 0);
            return clip;
        }

        static void Mix(float[] buf, float startSec, float dur, System.Func<float, float> gen)
        {
            int s0 = (int)(startSec * Rate);
            int len = (int)(dur * Rate);
            for (int i = 0; i < len; i++)
            {
                int idx = s0 + i;
                if (idx >= buf.Length) idx -= buf.Length; // wrap tails for seamless loop
                if (idx < 0 || idx >= buf.Length) break;
                buf[idx] += gen(i / (float)Rate);
            }
        }

        static void Normalize(float[] d, float peak)
        {
            float m = 0.0001f;
            for (int i = 0; i < d.Length; i++) m = Mathf.Max(m, Mathf.Abs(d[i]));
            float g = peak / m;
            for (int i = 0; i < d.Length; i++) d[i] *= g;
        }

        // ------------------------------------------------------------------ SFX
        void BuildSfx()
        {
            var rng = new System.Random(7);

            clips[Sfx.Step] = Make("Step", 0.07f, (t, d) =>
                (float)(rng.NextDouble() * 2 - 1) * Mathf.Exp(-t * 70f) * 0.25f + Mathf.Sin(2 * Mathf.PI * 140f * t) * Mathf.Exp(-t * 50f) * 0.2f);

            clips[Sfx.Jump] = Make("Jump", 0.28f, (t, d) =>
            {
                float f = Mathf.Lerp(380f, 820f, t / d);
                return Mathf.Sin(2 * Mathf.PI * f * t) * Mathf.Exp(-t * 7f) * 0.4f;
            });

            clips[Sfx.Slide] = Make("Slide", 0.4f, (t, d) =>
            {
                float env = Mathf.Sin(Mathf.PI * t / d);
                return (float)(rng.NextDouble() * 2 - 1) * env * 0.12f * Mathf.Lerp(1f, 0.3f, t / d);
            });

            clips[Sfx.Land] = Make("Land", 0.16f, (t, d) =>
                Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(180f, 70f, t / d) * t) * Mathf.Exp(-t * 25f) * 0.5f);

            clips[Sfx.Swish] = Make("Swish", 0.18f, (t, d) =>
            {
                float env = Mathf.Sin(Mathf.PI * t / d);
                float f = Mathf.Lerp(500f, 1200f, t / d);
                return ((float)(rng.NextDouble() * 2 - 1) * 0.1f + Mathf.Sin(2 * Mathf.PI * f * t) * 0.05f) * env;
            });

            clips[Sfx.Flower] = Make("Flower", 0.45f, (t, d) =>
                (Pluck(t, 1318f, 7f) + 0.6f * Pluck(t, 1760f, 9f)) * 0.3f);

            clips[Sfx.PowerUp] = Make("PowerUp", 0.7f, (t, d) =>
            {
                float[] notes = { 523f, 659f, 784f, 1047f };
                float sum = 0f;
                for (int i = 0; i < notes.Length; i++)
                {
                    float ts = t - i * 0.08f;
                    if (ts > 0f) sum += Pluck(ts, notes[i], 5f);
                }
                return sum * 0.22f;
            });

            clips[Sfx.ShieldBreak] = Make("ShieldBreak", 0.5f, (t, d) =>
            {
                float chime = Mathf.Sin(2 * Mathf.PI * 1600f * t) * Mathf.Exp(-t * 9f) * 0.25f;
                float noise = (float)(rng.NextDouble() * 2 - 1) * Mathf.Exp(-t * 18f) * 0.3f;
                return chime + noise;
            });

            clips[Sfx.Hit] = Make("Hit", 0.6f, (t, d) =>
            {
                float thud = Mathf.Sin(2 * Mathf.PI * Mathf.Lerp(150f, 45f, t / d) * t) * Mathf.Exp(-t * 7f) * 0.7f;
                float noise = (float)(rng.NextDouble() * 2 - 1) * Mathf.Exp(-t * 22f) * 0.3f;
                return thud + noise;
            });

            clips[Sfx.Smash] = Make("Smash", 0.3f, (t, d) =>
                (float)(rng.NextDouble() * 2 - 1) * Mathf.Exp(-t * 16f) * 0.35f + Mathf.Sin(2 * Mathf.PI * 220f * t) * Mathf.Exp(-t * 20f) * 0.3f);

            clips[Sfx.Click] = Make("Click", 0.12f, (t, d) =>
                Pluck(t, 880f, 24f) * 0.35f);
        }

        static AudioClip Make(string name, float dur, System.Func<float, float, float> gen)
        {
            int n = (int)(dur * Rate);
            var d = new float[n];
            for (int i = 0; i < n; i++) d[i] = Mathf.Clamp(gen(i / (float)Rate, dur), -1f, 1f);
            var c = AudioClip.Create(name, n, 1, Rate, false);
            c.SetData(d, 0);
            return c;
        }
    }
}
