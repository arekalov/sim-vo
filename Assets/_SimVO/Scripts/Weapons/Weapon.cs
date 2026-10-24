using System;
using SimVO.Audio;
using SimVO.Core;
using UnityEngine;

namespace SimVO.Weapons
{
    /// <summary>Whoever holds the weapon: a VR hand or the desktop player. Receives recoil feedback.</summary>
    public interface IWeaponHolder
    {
        void SendHaptic(float amplitude, float duration);
    }

    public abstract class Weapon : MonoBehaviour
    {
        public string displayName = "Оружие";
        public Transform muzzle;
        public float muzzleVelocity = 350f;
        public float minShotInterval = 0.12f;
        public float recoilHaptic = 0.8f;
        public float recoilHapticDuration = 0.08f;
        public AudioSource audioSource;
        public Light muzzleFlashLight;
        [Tooltip("Random cone half-angle in degrees when standing; kneeling and prone shrink it.")]
        public float swayStanding = 0.45f;
        public float swayKneeling = 0.25f;
        public float swayProne = 0.12f;
        [Tooltip("Projectiles per trigger pull (pellets for a shotgun).")]
        public int projectilesPerShot = 1;
        public float pelletSpread = 0f;
        public bool twoHanded;

        public IWeaponHolder Holder { get; set; }
        public abstract bool HasAmmo { get; }
        public abstract int RoundsLoaded { get; }
        /// <summary>Desktop one-key reload.</summary>
        public abstract void QuickReload();
        protected abstract void ConsumeRound();
        protected virtual AudioClip ShotClip => SfxLibrary.PistolShot;

        public event Action Fired;
        public event Action DryFired;

        float lastShotTime = -10f;
        float flashUntil;

        public bool TryFire()
        {
            if (Time.time - lastShotTime < minShotInterval) return false;
            if (!HasAmmo)
            {
                Play(SfxLibrary.DryClick);
                DryFired?.Invoke();
                return false;
            }

            lastShotTime = Time.time;
            ConsumeRound();
            var origin = muzzle != null ? muzzle.position : transform.position;
            var aim = muzzle != null ? muzzle.forward : transform.forward;
            var count = Mathf.Max(1, projectilesPerShot);
            for (var i = 0; i < count; i++)
            {
                var cone = CurrentSway + (count > 1 ? pelletSpread : 0f);
                ProjectilePool.Instance?.Launch(origin, Scatter(aim, cone), muzzleVelocity, this);
            }
            Play(ShotClip);
            Holder?.SendHaptic(recoilHaptic, recoilHapticDuration);
            if (muzzleFlashLight != null)
            {
                muzzleFlashLight.enabled = true;
                flashUntil = Time.time + 0.04f;
            }
            GameSession.Instance?.RegisterShot();
            Fired?.Invoke();
            return true;
        }

        float CurrentSway
        {
            get
            {
                var position = GameSession.Instance != null ? GameSession.Instance.CurrentPosition : BodyPosition.Standing;
                return position switch
                {
                    BodyPosition.Prone => swayProne,
                    BodyPosition.Kneeling => swayKneeling,
                    _ => swayStanding,
                };
            }
        }

        static Vector3 Scatter(Vector3 direction, float halfAngleDegrees)
        {
            if (halfAngleDegrees <= 0f) return direction;
            var offset = UnityEngine.Random.insideUnitCircle * halfAngleDegrees;
            return Quaternion.AngleAxis(offset.x, Vector3.up) * Quaternion.AngleAxis(offset.y, Vector3.Cross(Vector3.up, direction).normalized) * direction;
        }

        protected virtual void Update()
        {
            if (muzzleFlashLight != null && muzzleFlashLight.enabled && Time.time > flashUntil)
            {
                muzzleFlashLight.enabled = false;
            }
        }

        protected void Play(AudioClip clip)
        {
            if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
        }
    }
}
