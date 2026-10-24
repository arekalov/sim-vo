using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimVO.Audio
{
    /// <summary>
    /// Placeholder sound effects synthesised at runtime, so the prototype has no licensed audio.
    /// Replace with CC0 recordings later; keep the property names.
    /// </summary>
    public static class ProceduralSfx
    {
        const int SampleRate = 44100;
        static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

        public static AudioClip PistolShot => Get("pistol", 0.35f, (t, r) =>
            Noise(r) * Decay(t, 22f) * 0.9f + Mathf.Sin(2f * Mathf.PI * 95f * t) * Decay(t, 14f) * 0.6f);

        public static AudioClip RifleShot => Get("rifle", 0.5f, (t, r) =>
            Noise(r) * Decay(t, 14f) * 0.9f + Mathf.Sin(2f * Mathf.PI * 70f * t) * Decay(t, 9f) * 0.7f);

        public static AudioClip ShotgunShot => Get("shotgun", 0.6f, (t, r) =>
            Noise(r) * Decay(t, 10f) * 0.95f + Mathf.Sin(2f * Mathf.PI * 60f * t) * Decay(t, 7f) * 0.8f);

        public static AudioClip DryClick => Get("dry", 0.06f, (t, r) => Noise(r) * Decay(t, 120f) * 0.5f);

        public static AudioClip MagIn => Get("magin", 0.12f, (t, r) =>
            Noise(r) * Decay(t, 70f) * 0.5f + Mathf.Sin(2f * Mathf.PI * 180f * t) * Decay(t, 40f) * 0.5f);

        public static AudioClip MagOut => Get("magout", 0.1f, (t, r) => Noise(r) * Decay(t, 90f) * 0.4f);

        public static AudioClip PaperHit => Get("paper", 0.09f, (t, r) => Noise(r) * Decay(t, 60f) * 0.6f);

        public static AudioClip MetalHit => Get("metal", 0.5f, (t, r) =>
            (Mathf.Sin(2f * Mathf.PI * 1850f * t) + 0.6f * Mathf.Sin(2f * Mathf.PI * 2760f * t)) * Decay(t, 7f) * 0.5f);

        public static AudioClip Bullseye => Get("bullseye", 0.6f, (t, r) =>
            (Mathf.Sin(2f * Mathf.PI * 1320f * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * 2640f * t)) * Decay(t, 5f) * 0.5f);

        public static AudioClip Beep => Get("beep", 0.1f, (t, r) => Mathf.Sin(2f * Mathf.PI * 880f * t) * Envelope(t, 0.1f) * 0.5f);

        public static AudioClip FinalBeep => Get("finalbeep", 0.5f, (t, r) => Mathf.Sin(2f * Mathf.PI * 1320f * t) * Envelope(t, 0.5f) * 0.5f);

        public static AudioClip Record => Get("record", 0.9f, (t, r) =>
        {
            var note = t < 0.3f ? 523f : t < 0.6f ? 659f : 784f;
            var local = t % 0.3f;
            return Mathf.Sin(2f * Mathf.PI * note * t) * Envelope(local, 0.3f) * 0.5f;
        });

        public static AudioClip MotorHum => Get("motor", 1.0f, (t, r) =>
            (Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.25f + Noise(r) * 0.05f) * 0.35f);

        /// <summary>Twelve-second ambient pad: slow A-minor chord swell with a soft noise wash.</summary>
        public static AudioClip AmbientPad => Get("ambient", 12f, (t, r) =>
        {
            var swell = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * t / 12f);
            var chord = Mathf.Sin(2f * Mathf.PI * 110f * t) + 0.8f * Mathf.Sin(2f * Mathf.PI * 130.8f * t) + 0.7f * Mathf.Sin(2f * Mathf.PI * 164.8f * t) + 0.4f * Mathf.Sin(2f * Mathf.PI * 220f * t);
            var shimmer = 0.15f * Mathf.Sin(2f * Mathf.PI * 329.6f * t) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * t / 4f));
            return (chord * 0.12f + shimmer) * (0.35f + 0.65f * swell) + Noise(r) * 0.008f;
        });

        public static AudioClip ButtonClick => Get("button", 0.05f, (t, r) =>
            Mathf.Sin(2f * Mathf.PI * 1200f * t) * Decay(t, 80f) * 0.4f);

        static float Noise(System.Random r) => (float)(r.NextDouble() * 2.0 - 1.0);
        static float Decay(float t, float k) => Mathf.Exp(-t * k);
        static float Envelope(float t, float length)
        {
            var attack = Mathf.Clamp01(t / 0.01f);
            var release = Mathf.Clamp01((length - t) / 0.03f);
            return Mathf.Min(attack, release);
        }

        static AudioClip Get(string key, float seconds, Func<float, System.Random, float> generator)
        {
            if (cache.TryGetValue(key, out var clip) && clip != null) return clip;
            var count = Mathf.CeilToInt(seconds * SampleRate);
            var data = new float[count];
            var random = new System.Random(key.GetHashCode());
            for (var i = 0; i < count; i++)
            {
                data[i] = Mathf.Clamp(generator(i / (float)SampleRate, random), -1f, 1f);
            }
            clip = AudioClip.Create($"sfx_{key}", count, 1, SampleRate, false);
            clip.SetData(data, 0);
            cache[key] = clip;
            return clip;
        }
    }
}
