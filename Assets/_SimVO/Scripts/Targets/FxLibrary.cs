using System.Collections.Generic;
using UnityEngine;

namespace SimVO.Targets
{
    /// <summary>Scene-level factory for bullet holes and floating score labels.</summary>
    public class FxLibrary : MonoBehaviour
    {
        public static FxLibrary Instance { get; private set; }

        public GameObject bulletHolePrefab;
        public HitPopup popupPrefab;

        readonly Dictionary<Transform, Queue<GameObject>> holesByParent = new Dictionary<Transform, Queue<GameObject>>();

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        public void SpawnHole(Vector3 point, Vector3 normal, Transform parent, int maxPerParent = 40)
        {
            if (bulletHolePrefab == null) return;
            var hole = Instantiate(bulletHolePrefab, point + normal * 0.002f, Quaternion.LookRotation(-normal), parent);
            if (parent == null) return;
            if (!holesByParent.TryGetValue(parent, out var queue))
            {
                queue = new Queue<GameObject>();
                holesByParent[parent] = queue;
            }
            queue.Enqueue(hole);
            while (queue.Count > maxPerParent)
            {
                var oldest = queue.Dequeue();
                if (oldest != null) Destroy(oldest);
            }
        }

        public void ClearHoles(Transform parent)
        {
            if (!holesByParent.TryGetValue(parent, out var queue)) return;
            while (queue.Count > 0)
            {
                var hole = queue.Dequeue();
                if (hole != null) Destroy(hole);
            }
        }

        public void ShowPoints(Vector3 point, int points)
        {
            if (popupPrefab == null) return;
            var popup = Instantiate(popupPrefab, point, Quaternion.identity);
            popup.Show(points);
        }
    }
}
