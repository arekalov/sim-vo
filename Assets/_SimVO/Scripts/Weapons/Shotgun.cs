using System.Collections;
using SimVO.Audio;
using UnityEngine;

namespace SimVO.Weapons
{
    /// <summary>Two-handed, tube-fed one shell at a time, fires a pellet cone.</summary>
    public class Shotgun : Weapon
    {
        public int tubeCapacity = 5;
        [SerializeField] int shellsLoaded = 5;
        public ShellSocket loadingPort;
        public bool startLoaded = true;

        public int ShellsLoaded => shellsLoaded;
        public override bool HasAmmo => shellsLoaded > 0;
        public override int RoundsLoaded => shellsLoaded;
        protected override AudioClip ShotClip => OwnClip ?? SfxLibrary.ShotgunShot;

        void Reset()
        {
            requiresCockingAfterShot = true;
            displayName = "Дробовик";
            muzzleVelocity = 400f;
            minShotInterval = 0.7f;
            recoilHaptic = 1f;
            recoilHapticDuration = 0.15f;
            projectilesPerShot = 8;
            pelletSpread = 2.5f;
            twoHanded = true;
        }

        IEnumerator Start()
        {
            yield return null;
            if (!startLoaded) shellsLoaded = 0;
        }

        protected override void ConsumeRound()
        {
            shellsLoaded = Mathf.Max(0, shellsLoaded - 1);
        }

        /// <summary>Called by the loading port when a shell is pushed in.</summary>
        public bool LoadShell()
        {
            if (shellsLoaded >= tubeCapacity) return false;
            shellsLoaded++;
            Play(SfxLibrary.ShellIn);
            return true;
        }

        public override void QuickReload()
        {
            if (shellsLoaded < tubeCapacity)
            {
                shellsLoaded = tubeCapacity;
                Play(SfxLibrary.ShellIn);
            }
            Cock();
        }
    }
}
