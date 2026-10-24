using UnityEngine;

namespace SimVO.Audio
{
    /// <summary>Recorded CC0 clips assigned by the generator; falls back to procedural placeholders when a slot is empty.</summary>
    public class SfxLibrary : MonoBehaviour
    {
        public static SfxLibrary Instance { get; private set; }

        public AudioClip[] pistolShots;
        public AudioClip[] rifleShots;
        public AudioClip[] shotgunShots;
        public AudioClip[] paperHits;
        public AudioClip[] metalHits;
        public AudioClip[] bullseye;
        public AudioClip[] magIn;
        public AudioClip[] magOut;
        public AudioClip[] shellIn;
        public AudioClip[] dryClick;
        public AudioClip[] buttonClick;
        public AudioClip[] carrierMove;
        public AudioClip[] targetFlip;

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        static AudioClip Pick(AudioClip[] clips, AudioClip fallback)
        {
            if (clips == null || clips.Length == 0) return fallback;
            var clip = clips[Random.Range(0, clips.Length)];
            return clip != null ? clip : fallback;
        }

        public static AudioClip PistolShot => Pick(Instance?.pistolShots, ProceduralSfx.PistolShot);
        public static AudioClip RifleShot => Pick(Instance?.rifleShots, ProceduralSfx.RifleShot);
        public static AudioClip ShotgunShot => Pick(Instance?.shotgunShots, ProceduralSfx.ShotgunShot);
        public static AudioClip PaperHit => Pick(Instance?.paperHits, ProceduralSfx.PaperHit);
        public static AudioClip MetalHit => Pick(Instance?.metalHits, ProceduralSfx.MetalHit);
        public static AudioClip Bullseye => Pick(Instance?.bullseye, ProceduralSfx.Bullseye);
        public static AudioClip MagIn => Pick(Instance?.magIn, ProceduralSfx.MagIn);
        public static AudioClip MagOut => Pick(Instance?.magOut, ProceduralSfx.MagOut);
        public static AudioClip ShellIn => Pick(Instance?.shellIn, ProceduralSfx.MagIn);
        public static AudioClip DryClick => Pick(Instance?.dryClick, ProceduralSfx.DryClick);
        public static AudioClip ButtonClick => Pick(Instance?.buttonClick, ProceduralSfx.ButtonClick);
        public static AudioClip CarrierMove => Pick(Instance?.carrierMove, ProceduralSfx.MotorHum);
        public static AudioClip TargetFlip => Pick(Instance?.targetFlip, ProceduralSfx.MagOut);
    }
}
