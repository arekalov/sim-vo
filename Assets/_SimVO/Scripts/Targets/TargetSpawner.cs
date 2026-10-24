using System.Collections;
using System.Collections.Generic;
using SimVO.Core;
using UnityEngine;

namespace SimVO.Targets
{
    /// <summary>
    /// Keeps one active target on a lane at 5/10/15 m. Each hit retires the target and raises the next one.
    /// Outside a game a practice target stays on the first lane.
    /// </summary>
    public class TargetSpawner : MonoBehaviour
    {
        public Target ringTargetPrefab;
        public List<Transform> lanes = new List<Transform>();
        public float respawnDelay = 0.6f;
        public float retireDuration = 0.3f;

        Target active;
        int laneIndex;
        GameSession session;
        Coroutine cycle;

        void Start()
        {
            session = GameSession.Instance;
            if (session != null) session.StateChanged += OnStateChanged;
            SpawnAt(0);
        }

        void OnDestroy()
        {
            if (session != null) session.StateChanged -= OnStateChanged;
        }

        void OnStateChanged()
        {
            if (session.State == SessionState.Running)
            {
                laneIndex = 0;
                Replace(0);
            }
        }

        void Replace(int lane)
        {
            if (cycle != null) StopCoroutine(cycle);
            if (active != null) Destroy(active.gameObject);
            active = null;
            SpawnAt(lane);
        }

        void SpawnAt(int lane)
        {
            if (lanes.Count == 0 || ringTargetPrefab == null) return;
            var anchor = lanes[Mathf.Clamp(lane, 0, lanes.Count - 1)];
            active = Instantiate(ringTargetPrefab, anchor.position, anchor.rotation, transform);
            active.HitRegistered += OnHit;
            StartCoroutine(PopIn(active.transform));
        }

        void OnHit(Target target, int points)
        {
            if (target != active) return;
            target.HitRegistered -= OnHit;
            if (session == null || session.State != SessionState.Running) return;
            laneIndex = (laneIndex + 1) % lanes.Count;
            cycle = StartCoroutine(Retire(target, laneIndex));
        }

        IEnumerator Retire(Target target, int nextLane)
        {
            active = null;
            var t = 0f;
            var start = target.transform.localScale;
            while (t < retireDuration && target != null)
            {
                t += Time.deltaTime;
                target.transform.localScale = Vector3.Lerp(start, new Vector3(start.x, 0.01f, start.z), t / retireDuration);
                yield return null;
            }
            if (target != null) Destroy(target.gameObject);
            yield return new WaitForSeconds(respawnDelay);
            SpawnAt(nextLane);
            cycle = null;
        }

        IEnumerator PopIn(Transform target)
        {
            var t = 0f;
            var full = target.localScale;
            while (t < 0.2f && target != null)
            {
                t += Time.deltaTime;
                target.localScale = Vector3.Lerp(new Vector3(full.x, 0.01f, full.z), full, t / 0.2f);
                yield return null;
            }
            if (target != null) target.localScale = full;
        }
    }
}
