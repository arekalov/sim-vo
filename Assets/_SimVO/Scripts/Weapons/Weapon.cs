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
        public float recoilHaptic = 1f;
        public float recoilHapticDuration = 0.1f;
        [Tooltip("Child that kicks back on a shot, usually the model.")]
        public Transform recoilPivot;
        public float recoilKickDistance = 0.03f;
        public float recoilKickAngle = 7f;
        public float recoilRecoverSeconds = 0.14f;
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
        [Tooltip("Keeps firing at minShotInterval while the trigger is held.")]
        public bool automatic;
        [Tooltip("Recorded shots for this weapon; empty falls back to the class default.")]
        public AudioClip[] shotClips;

        public bool TriggerHeld { get; set; }
        [Tooltip("Bolt action or pump: the handle must be worked after every shot.")]
        public bool requiresCockingAfterShot;
        /// <summary>A round sits in the chamber. Drops when the gun runs dry or after a shot on bolt and pump guns.</summary>
        public bool Chambered { get; protected set; } = true;
        public event Action Cocked;

        public IWeaponHolder Holder { get; set; }
        public abstract bool HasAmmo { get; }
        public abstract int RoundsLoaded { get; }
        /// <summary>Desktop one-key reload.</summary>
        public abstract void QuickReload();
        protected abstract void ConsumeRound();
        protected AudioClip OwnClip => shotClips != null && shotClips.Length > 0 ? shotClips[UnityEngine.Random.Range(0, shotClips.Length)] : null;
        protected virtual AudioClip ShotClip => OwnClip ?? SfxLibrary.PistolShot;

        public event Action Fired;
        public event Action DryFired;

        float lastShotTime = -10f;
        float flashUntil;
        Coroutine recoil;
        Vector3 pivotRestPosition;
        Quaternion pivotRestRotation;
        bool pivotRestCaptured;

        System.Collections.IEnumerator Recoil()
        {
            if (!pivotRestCaptured)
            {
                pivotRestPosition = recoilPivot.localPosition;
                pivotRestRotation = recoilPivot.localRotation;
                pivotRestCaptured = true;
            }
            var t = 0f;
            while (t < recoilRecoverSeconds)
            {
                t += Time.deltaTime;
                var k = 1f - Mathf.Clamp01(t / recoilRecoverSeconds);
                var kick = Mathf.Sin(k * Mathf.PI * 0.5f);
                recoilPivot.localPosition = pivotRestPosition + Vector3.back * recoilKickDistance * kick;
                recoilPivot.localRotation = pivotRestRotation * Quaternion.Euler(-recoilKickAngle * kick, 0f, 0f);
                yield return null;
            }
            recoilPivot.localPosition = pivotRestPosition;
            recoilPivot.localRotation = pivotRestRotation;
            recoil = null;
        }

        public bool TryFire()
        {
            if (GameSession.Instance != null && GameSession.Instance.Paused) return false;
            if (Time.time - lastShotTime < minShotInterval) return false;
            if (Sfx.Assist && HasAmmo) Chambered = true;
            if (!HasAmmo || !Chambered)
            {
                Play(SfxLibrary.DryClick);
                DryFired?.Invoke();
                return false;
            }

            lastShotTime = Time.time;
            ConsumeRound();
            Chambered = !requiresCockingAfterShot && HasAmmo;
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
            if (recoilPivot != null && isActiveAndEnabled)
            {
                if (recoil != null) StopCoroutine(recoil);
                recoil = StartCoroutine(Recoil());
            }
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
                var sway = position switch
                {
                    BodyPosition.Prone => swayProne,
                    BodyPosition.Kneeling => swayKneeling,
                    _ => swayStanding,
                };
                return Sfx.Assist ? sway * 0.5f : sway;
            }
        }

        static Vector3 Scatter(Vector3 direction, float halfAngleDegrees)
        {
            if (halfAngleDegrees <= 0f) return direction;
            var offset = UnityEngine.Random.insideUnitCircle * halfAngleDegrees;
            return Quaternion.AngleAxis(offset.x, Vector3.up) * Quaternion.AngleAxis(offset.y, Vector3.Cross(Vector3.up, direction).normalized) * direction;
        }

        /// <summary>Work the slide, bolt or pump: chambers a round if there is one to chamber.</summary>
        public virtual void Cock()
        {
            Chambered = HasAmmo;
            Play(SfxLibrary.CockForward);
            Cocked?.Invoke();
        }

        protected virtual void Update()
        {
            if (automatic && TriggerHeld && HasAmmo) TryFire();
            if (muzzleFlashLight != null && muzzleFlashLight.enabled && Time.time > flashUntil)
            {
                muzzleFlashLight.enabled = false;
            }
        }

        protected void Play(AudioClip clip)
        {
            Sfx.Play(audioSource, clip);
        }
    }
}
