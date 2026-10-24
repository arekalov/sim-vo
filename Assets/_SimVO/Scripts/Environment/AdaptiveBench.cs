using SimVO.Core;
using UnityEngine;

namespace SimVO.Environment
{
    /// <summary>Shooting bench that lowers itself for kneeling and prone, carrying the console and ammo boxes with it.</summary>
    public class AdaptiveBench : MonoBehaviour
    {
        public float standingOffset = 0f;
        public float kneelingOffset = -0.38f;
        public float proneOffset = -0.66f;
        public float speed = 1.2f;

        Vector3 rest;

        void Awake() => rest = transform.localPosition;

        void Update()
        {
            var position = GameSession.Instance != null ? GameSession.Instance.CurrentPosition : BodyPosition.Standing;
            var offset = position switch
            {
                BodyPosition.Kneeling => kneelingOffset,
                BodyPosition.Prone => proneOffset,
                _ => standingOffset,
            };
            var target = rest + Vector3.up * offset;
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, target, speed * Time.deltaTime);
        }
    }
}
