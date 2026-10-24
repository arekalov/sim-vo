using System.Collections;
using System.Collections.Generic;
using SimVO.Core;
using UnityEngine;

namespace SimVO.Targets
{
    /// <summary>
    /// Runs the player's lane: hangs a target on the carrier, drives it to 5/10/15 m, and after each hit
    /// flips the frame to swap paper and moves on to the next distance. Outside a game a practice target waits at 5 m.
    /// </summary>
    public class TargetSpawner : MonoBehaviour
    {
        public TargetCarrier carrier;
        public Target ringTargetPrefab;
        public Target silhouetteTargetPrefab;
        public List<float> distances = new List<float> { 5f, 10f, 15f };
        public float practiceDistance = 5f;
        public float swapDelay = 0.35f;

        Target active;
        int distanceIndex;
        GameSession session;
        bool busy;

        void Start()
        {
            session = GameSession.Instance;
            if (session != null) session.StateChanged += OnStateChanged;
            Hang();
            carrier.DriveTo(practiceDistance);
        }

        void OnDestroy()
        {
            if (session != null) session.StateChanged -= OnStateChanged;
        }

        Target Prefab => session != null && session.settings.targetKind == TargetKind.Silhouette && silhouetteTargetPrefab != null
            ? silhouetteTargetPrefab
            : ringTargetPrefab;

        void OnStateChanged()
        {
            if (session.State == SessionState.Running)
            {
                distanceIndex = 0;
                StopAllCoroutines();
                busy = false;
                StartCoroutine(SwapAndDrive(distances[0]));
            }
            else if (session.State == SessionState.Finished)
            {
                StartCoroutine(ReturnHome());
            }
        }

        void Hang()
        {
            if (active != null) Destroy(active.gameObject);
            var mount = carrier.targetMount != null ? carrier.targetMount : carrier.transform;
            active = Instantiate(Prefab, mount.position, mount.rotation, mount);
            active.HitRegistered += OnHit;
        }

        void OnHit(Target target, int points)
        {
            if (target != active || busy) return;
            if (session == null || session.State != SessionState.Running) return;
            distanceIndex = (distanceIndex + 1) % distances.Count;
            StartCoroutine(SwapAndDrive(distances[distanceIndex]));
        }

        IEnumerator SwapAndDrive(float distance)
        {
            busy = true;
            yield return new WaitForSeconds(swapDelay);
            yield return carrier.Flip(Hang);
            var arrived = false;
            carrier.DriveTo(distance, () => arrived = true);
            while (!arrived) yield return null;
            busy = false;
        }

        IEnumerator ReturnHome()
        {
            busy = true;
            var arrived = false;
            carrier.DriveTo(carrier.homeDistance, () => arrived = true);
            while (!arrived) yield return null;
            yield return new WaitForSeconds(2.5f);
            yield return carrier.Flip(Hang);
            carrier.DriveTo(practiceDistance);
            busy = false;
        }
    }
}
