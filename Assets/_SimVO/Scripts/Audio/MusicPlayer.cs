using UnityEngine;

namespace SimVO.Audio
{
    /// <summary>Loops the background track; falls back to a generated ambient pad when no clip is assigned.</summary>
    [RequireComponent(typeof(AudioSource))]
    public class MusicPlayer : MonoBehaviour
    {
        public AudioClip track;
        public float baseVolume = 0.35f;

        AudioSource source;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.loop = true;
            source.spatialBlend = 0f;
            source.playOnAwake = false;
            source.clip = track != null ? track : ProceduralSfx.AmbientPad;
            Apply(Sfx.Music);
        }

        void OnEnable()
        {
            Sfx.MusicChanged += Apply;
            if (!source.isPlaying) source.Play();
        }

        void OnDisable() => Sfx.MusicChanged -= Apply;

        void Apply(float volume) => source.volume = volume * baseVolume;
    }
}
