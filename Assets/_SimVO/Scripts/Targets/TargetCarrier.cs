using System;
using System.Collections;
using SimVO.Audio;
using UnityEngine;

namespace SimVO.Targets
{
    /// <summary>
    /// Motorised trolley on a ceiling rail. Drives the hanging target frame out to a distance and back,
    /// like a real gallery carrier, and can flip the frame edge-on to swap the paper.
    /// </summary>
    public class TargetCarrier : MonoBehaviour
    {
        public Transform trolley;
        public Transform frame;
        public Transform targetMount;
        public float homeDistance = 1.2f;
        public float speed = 9f;
        public float flipDuration = 0.18f;
        public AudioSource audioSource;

        public float CurrentDistance => trolley != null ? trolley.localPosition.z : 0f;
        public bool IsMoving { get; private set; }
        public event Action<float> Arrived;

        Coroutine drive;

        void Awake()
        {
            if (trolley != null) trolley.localPosition = new Vector3(0f, 0f, homeDistance);
        }

        public void DriveTo(float distance, Action onArrived = null)
        {
            if (drive != null) StopCoroutine(drive);
            drive = StartCoroutine(Drive(distance, onArrived));
        }

        IEnumerator Drive(float distance, Action onArrived)
        {
            IsMoving = true;
            var start = trolley.localPosition.z;
            var length = Mathf.Abs(distance - start);
            var duration = Mathf.Max(0.25f, length / speed);
            var clip = SfxLibrary.CarrierMove;
            if (audioSource != null && clip != null && length > 0.5f)
            {
                audioSource.clip = clip;
                audioSource.loop = true;
                audioSource.volume = 0.35f * Sfx.Effects;
                audioSource.Play();
            }
            var t = 0f;
            while (t < duration)
            {
                while (Core.GameSession.Instance != null && Core.GameSession.Instance.Paused) yield return null;
                t += Time.deltaTime;
                var k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
                trolley.localPosition = new Vector3(0f, 0f, Mathf.Lerp(start, distance, k));
                yield return null;
            }
            trolley.localPosition = new Vector3(0f, 0f, distance);
            if (audioSource != null && audioSource.loop) audioSource.Stop();
            IsMoving = false;
            drive = null;
            onArrived?.Invoke();
            Arrived?.Invoke(distance);
        }

        /// <summary>Turns the frame edge-on, runs the swap callback, turns it back.</summary>
        public IEnumerator Flip(Action whileEdgeOn)
        {
            if (frame == null)
            {
                whileEdgeOn?.Invoke();
                yield break;
            }
            Sfx.Play(audioSource, SfxLibrary.TargetFlip);
            yield return Rotate(0f, 90f);
            whileEdgeOn?.Invoke();
            yield return Rotate(90f, 0f);
        }

        /// <summary>Turns the frame to an angle: 0 faces the shooter, 90 is edge-on.</summary>
        public IEnumerator TurnTo(float angle)
        {
            if (frame == null) yield break;
            var from = frame.localEulerAngles.y;
            if (from > 180f) from -= 360f;
            if (Mathf.Abs(from - angle) < 0.5f) yield break;
            Sfx.Play(audioSource, SfxLibrary.TargetFlip, 0.6f);
            yield return Rotate(from, angle);
        }

        IEnumerator Rotate(float from, float to)
        {
            var t = 0f;
            while (t < flipDuration)
            {
                while (Core.GameSession.Instance != null && Core.GameSession.Instance.Paused) yield return null;
                t += Time.deltaTime;
                frame.localRotation = Quaternion.Euler(0f, Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / flipDuration)), 0f);
                yield return null;
            }
            frame.localRotation = Quaternion.Euler(0f, to, 0f);
        }
    }
}
