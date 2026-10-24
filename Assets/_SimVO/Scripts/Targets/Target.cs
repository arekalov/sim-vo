using System;
using SimVO.Audio;
using SimVO.Core;
using UnityEngine;

namespace SimVO.Targets
{
    public abstract class Target : MonoBehaviour
    {
        public TargetSurface surface = TargetSurface.Paper;
        public AudioSource audioSource;
        public int maxHoles = 40;
        public bool showHoles = true;
        /// <summary>While false (edge-on in cover mode, wrong body position) hits leave marks but do not score.</summary>
        public bool Armed { get; set; } = true;

        public event Action<Target, int> HitRegistered;

        /// <summary>Points for a hit at this world point; offset is the hit position relative to the centre, metres.</summary>
        public abstract int Evaluate(Vector3 worldPoint, out Vector2 offset);
        public abstract int MaxPoints { get; }

        public void RegisterHit(Vector3 point, Vector3 normal, Vector3 direction)
        {
            var points = Evaluate(point, out var offset);
            if (showHoles) FxLibrary.Instance?.SpawnHole(point, normal, transform, maxHoles);
            OnHitVisual(point, normal, direction, points);
            PlayHitSound(points);
            if (!Armed) return;
            var counted = GameSession.Instance != null && GameSession.Instance.State == SessionState.Running;
            if (counted) FxLibrary.Instance?.ShowPoints(point, points);
            GameSession.Instance?.RegisterHit(points, offset, point);
            HitRegistered?.Invoke(this, points);
        }

        protected virtual void OnHitVisual(Vector3 point, Vector3 normal, Vector3 direction, int points) { }

        protected virtual void PlayHitSound(int points)
        {
            if (audioSource == null) return;
            if (points >= MaxPoints && Armed)
            {
                Sfx.Play(audioSource, SfxLibrary.Bullseye);
                return;
            }
            var clip = surface switch
            {
                TargetSurface.Metal => SfxLibrary.MetalHit,
                TargetSurface.Hologram => ProceduralSfx.Beep,
                _ => SfxLibrary.PaperHit,
            };
            Sfx.Play(audioSource, clip);
        }
    }
}
