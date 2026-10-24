using UnityEngine;

namespace SimVO.Targets
{
    /// <summary>Round target with concentric scoring rings: 10 in the centre down to 1 at the edge.</summary>
    public class RingTarget : Target
    {
        public float radius = 0.25f;
        public int rings = 10;

        public override int MaxPoints => rings;

        public override int Evaluate(Vector3 worldPoint, out Vector2 offset)
        {
            var local = transform.InverseTransformPoint(worldPoint);
            offset = new Vector2(local.x, local.y);
            var ring = Mathf.FloorToInt(offset.magnitude / (radius / rings));
            return Mathf.Clamp(rings - ring, 0, rings);
        }
    }
}
