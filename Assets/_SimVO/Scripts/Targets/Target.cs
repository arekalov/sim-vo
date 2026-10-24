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

        public event Action<Target, int> HitRegistered;

        /// <summary>Points for a hit at this world point; offset is the hit position relative to the centre, metres.</summary>
        public abstract int Evaluate(Vector3 worldPoint, out Vector2 offset);
        public abstract int MaxPoints { get; }

        public void RegisterHit(Vector3 point, Vector3 normal, Vector3 direction)
        {
            var points = Evaluate(point, out var offset);
            FxLibrary.Instance?.SpawnHole(point, normal, transform, maxHoles);
            FxLibrary.Instance?.ShowPoints(point, points);
            PlayHitSound(points);
            GameSession.Instance?.RegisterHit(points, offset, point);
            HitRegistered?.Invoke(this, points);
        }

        void PlayHitSound(int points)
        {
            if (audioSource == null) return;
            if (points >= MaxPoints)
            {
                audioSource.PlayOneShot(ProceduralSfx.Bullseye);
                return;
            }
            audioSource.PlayOneShot(surface == TargetSurface.Metal ? ProceduralSfx.MetalHit : ProceduralSfx.PaperHit);
        }
    }
}
