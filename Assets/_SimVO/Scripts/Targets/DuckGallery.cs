using System.Collections.Generic;
using SimVO.Core;
using UnityEngine;

namespace SimVO.Targets
{
    /// <summary>Carnival row: ducks ride a belt across the lane and wrap around.</summary>
    public class DuckGallery : MonoBehaviour
    {
        public List<DuckTarget> ducks = new List<DuckTarget>();
        public float beltLength = 1.6f;
        public float speed = 0.45f;
        public bool reverseEveryOther = true;

        void Update()
        {
            if (GameSession.Instance != null && GameSession.Instance.Paused) return;
            for (var i = 0; i < ducks.Count; i++)
            {
                var duck = ducks[i];
                if (duck == null) continue;
                var dir = reverseEveryOther && i % 2 == 1 ? -1f : 1f;
                var p = duck.transform.localPosition;
                p.x += dir * speed * Time.deltaTime;
                var half = beltLength * 0.5f;
                if (p.x > half) p.x -= beltLength;
                if (p.x < -half) p.x += beltLength;
                duck.transform.localPosition = p;
            }
        }
    }
}
