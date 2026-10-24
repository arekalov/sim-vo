using System.Collections;
using UnityEngine;

namespace SimVO.Targets
{
    /// <summary>Small steel duck on the gallery belt: falls back when hit, rises again after a moment.</summary>
    public class DuckTarget : Target
    {
        public Transform flipPivot;
        public float downSeconds = 2.5f;

        public override int MaxPoints => 5;

        public override int Evaluate(Vector3 worldPoint, out Vector2 offset)
        {
            offset = Vector2.zero;
            return 5;
        }

        protected override void OnHitVisual(Vector3 point, Vector3 normal, Vector3 direction, int points)
        {
            if (!Armed) return;
            StartCoroutine(Fall());
        }

        IEnumerator Fall()
        {
            Armed = false;
            yield return Turn(0f, 85f, 0.12f);
            yield return new WaitForSeconds(downSeconds);
            yield return Turn(85f, 0f, 0.25f);
            Armed = true;
        }

        IEnumerator Turn(float from, float to, float seconds)
        {
            var t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                if (flipPivot != null) flipPivot.localRotation = Quaternion.Euler(Mathf.Lerp(from, to, t / seconds), 0f, 0f);
                yield return null;
            }
            if (flipPivot != null) flipPivot.localRotation = Quaternion.Euler(to, 0f, 0f);
        }
    }
}
