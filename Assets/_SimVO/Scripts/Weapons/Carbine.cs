using SimVO.Audio;
using UnityEngine;

namespace SimVO.Weapons
{
    /// <summary>Two-handed magazine-fed rifle. Same loading as the pistol, different ballistics and sound.</summary>
    public class Carbine : Pistol
    {
        protected override AudioClip ShotClip => OwnClip ?? SfxLibrary.RifleShot;

        void Reset()
        {
            displayName = "Карабин";
            muzzleVelocity = 800f;
            minShotInterval = 0.5f;
            recoilHaptic = 1f;
            swayStanding = 0.35f;
            swayKneeling = 0.18f;
            swayProne = 0.06f;
            twoHanded = true;
        }
    }
}
