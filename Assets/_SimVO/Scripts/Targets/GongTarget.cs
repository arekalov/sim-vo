using System.Collections;
using UnityEngine;

namespace SimVO.Targets
{
    /// <summary>Steel plate on chains: rings and swings when hit. Centre 10, anywhere else 5.</summary>
    public class GongTarget : Target
    {
        public float radius = 0.2f;
        public Transform swingPivot;
        public float swingDegrees = 18f;

        Coroutine swing;

        public override int MaxPoints => 10;

        public override int Evaluate(Vector3 worldPoint, out Vector2 offset)
        {
            var local = transform.InverseTransformPoint(worldPoint);
            offset = new Vector2(local.x, local.y);
            var d = offset.magnitude;
            if (d > radius) return 0;
            return d <= radius * 0.35f ? 10 : 5;
        }

        protected override void OnHitVisual(Vector3 point, Vector3 normal, Vector3 direction, int points)
        {
            if (swingPivot == null) return;
            if (swing != null) StopCoroutine(swing);
            swing = StartCoroutine(Swing());
        }

        IEnumerator Swing()
        {
            var t = 0f;
            const float duration = 1.6f;
            while (t < duration)
            {
                t += Time.deltaTime;
                var decay = 1f - t / duration;
                swingPivot.localRotation = Quaternion.Euler(Mathf.Sin(t * 14f) * swingDegrees * decay, 0f, 0f);
                yield return null;
            }
            swingPivot.localRotation = Quaternion.identity;
            swing = null;
        }
    }
}
