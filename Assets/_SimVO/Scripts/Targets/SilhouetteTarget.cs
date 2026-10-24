using UnityEngine;

namespace SimVO.Targets
{
    /// <summary>Humanoid silhouette with three scoring zones: centre and head 5, inner torso 3, rest of the figure 1.</summary>
    public class SilhouetteTarget : Target
    {
        public override int MaxPoints => 5;

        public override int Evaluate(Vector3 worldPoint, out Vector2 offset)
        {
            var local = transform.InverseTransformPoint(worldPoint);
            offset = new Vector2(local.x, local.y);
            return Score(offset);
        }

        /// <summary>Zones in metres for the 0.5 x 0.8 m paper; shared with the texture generator.</summary>
        public static int Score(Vector2 p)
        {
            var head = (p - new Vector2(0f, 0.27f)).magnitude <= 0.075f;
            if (head) return 5;
            if (Mathf.Abs(p.x) <= 0.07f && p.y >= -0.17f && p.y <= 0.15f) return 5;
            if (Mathf.Abs(p.x) <= 0.15f && p.y >= -0.3f && p.y <= 0.19f) return 3;
            if (Mathf.Abs(p.x) <= 0.22f && p.y >= -0.38f && p.y <= 0.2f) return 1;
            return 0;
        }
    }
}
