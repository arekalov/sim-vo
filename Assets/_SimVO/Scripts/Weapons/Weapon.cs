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

        public IWeaponHolder Holder { get; set; }
        public abstract bool HasAmmo { get; }
        public abstract int RoundsLoaded { get; }
        /// <summary>Desktop one-key reload.</summary>
        public abstract void QuickReload();
        protected abstract void ConsumeRound();
        protected virtual AudioClip ShotClip => ProceduralSfx.PistolShot;

        public event Action Fired;
        public event Action DryFired;

        float lastShotTime = -10f;
        float flashUntil;

        public bool TryFire()
        {
            if (Time.time - lastShotTime < minShotInterval) return false;
            if (!HasAmmo)
            {
                Play(ProceduralSfx.DryClick);
                DryFired?.Invoke();
                return false;
            }

            lastShotTime = Time.time;
            ConsumeRound();
            var origin = muzzle != null ? muzzle.position : transform.position;
            var direction = muzzle != null ? muzzle.forward : transform.forward;
            ProjectilePool.Instance?.Launch(origin, direction, muzzleVelocity, this);
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
