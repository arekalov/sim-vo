using UnityEngine;

namespace SimVO.Audio
{
    /// <summary>Volume settings persisted in PlayerPrefs and applied to every one-shot and the music player.</summary>
    public static class Sfx
    {
        const string MusicKey = "simvo.volume.music";
        const string EffectsKey = "simvo.volume.sfx";

        static float? music;
        static float? effects;

        public static float Music
        {
            get => music ??= PlayerPrefs.GetFloat(MusicKey, 0.5f);
            set
            {
                music = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(MusicKey, music.Value);
                PlayerPrefs.Save();
                MusicChanged?.Invoke(music.Value);
            }
        }

        public static float Effects
        {
            get => effects ??= PlayerPrefs.GetFloat(EffectsKey, 0.8f);
            set
            {
                effects = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(EffectsKey, effects.Value);
                PlayerPrefs.Save();
            }
        }

        public static event System.Action<float> MusicChanged;

        const string AssistKey = "simvo.assist";
        static bool? assist;

        /// <summary>Simplified mode for novices: laser on every gun, no cocking needed, half the sway.</summary>
        public static bool Assist
        {
            get => assist ??= PlayerPrefs.GetInt(AssistKey, 1) == 1;
            set
            {
                assist = value;
                PlayerPrefs.SetInt(AssistKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool LaserSight => Assist;

        public static void Play(AudioSource source, AudioClip clip, float scale = 1f)
        {
            if (source == null || clip == null) return;
            source.PlayOneShot(clip, Effects * scale);
        }
    }
}
