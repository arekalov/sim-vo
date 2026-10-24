using SimVO.Core;
using UnityEngine;

namespace SimVO.Platform
{
    /// <summary>Derives standing / kneeling / prone from headset height relative to the floor.</summary>
    public class BodyPositionTracker : MonoBehaviour
    {
        public Transform head;
        public float standingHeight = 1.6f;
        public float kneelingRatio = 0.78f;
        public float proneRatio = 0.45f;
        public Renderer floorMarker;
        public Color standingColor = new Color(0.2f, 0.9f, 0.3f);
        public Color kneelingColor = new Color(0.95f, 0.8f, 0.2f);
        public Color proneColor = new Color(0.9f, 0.3f, 0.2f);

        float observedMax;
        BodyPosition current = BodyPosition.Standing;
        MaterialPropertyBlock block;

        void Update()
        {
            if (head == null) return;
            var height = head.position.y - transform.position.y;
            observedMax = Mathf.Max(observedMax, height);
            var standing = Mathf.Max(standingHeight, observedMax);
            var next = height < standing * proneRatio ? BodyPosition.Prone
                : height < standing * kneelingRatio ? BodyPosition.Kneeling
                : BodyPosition.Standing;
            if (next == current) return;
            current = next;
            if (GameSession.Instance != null) GameSession.Instance.CurrentPosition = next;
            Tint(next);
        }

        void Tint(BodyPosition position)
        {
            if (floorMarker == null) return;
            block ??= new MaterialPropertyBlock();
            var color = position switch
            {
                BodyPosition.Kneeling => kneelingColor,
                BodyPosition.Prone => proneColor,
                _ => standingColor,
            };
            block.SetColor("_BaseColor", color);
            block.SetColor("_EmissionColor", color * 1.5f);
            floorMarker.SetPropertyBlock(block);
        }
    }
}
