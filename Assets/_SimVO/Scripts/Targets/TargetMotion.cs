using SimVO.Core;
using UnityEngine;

namespace SimVO.Targets
{
    /// <summary>Moves the hanging target: sideways along the crossbar or around a circle.</summary>
    public class TargetMotion : MonoBehaviour
    {
        public TargetMotionMode mode = TargetMotionMode.Static;
        public float amplitude = 0.55f;
        public float speed = 0.6f;
        public float circleRadius = 0.32f;
        public float circleSpeed = 0.35f;

        Vector3 rest;
        float phase;

        void Awake() => rest = transform.localPosition;

        void Update()
        {
            if (mode == TargetMotionMode.Static)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, rest, 4f * Time.deltaTime);
                return;
            }
            if (GameSession.Instance != null && GameSession.Instance.Paused) return;
            phase += Time.deltaTime;
            transform.localPosition = mode == TargetMotionMode.Lateral
                ? rest + new Vector3(Mathf.Sin(phase * speed * Mathf.PI) * amplitude, 0f, 0f)
                : rest + new Vector3(Mathf.Cos(phase * circleSpeed * 2f * Mathf.PI) * circleRadius, Mathf.Sin(phase * circleSpeed * 2f * Mathf.PI) * circleRadius - circleRadius * 0.5f, 0f);
        }
    }
}
