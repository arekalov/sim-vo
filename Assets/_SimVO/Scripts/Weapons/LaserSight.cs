using SimVO.Audio;
using SimVO.Core;
using UnityEngine;

namespace SimVO.Weapons
{
    /// <summary>Red aiming beam from the muzzle to the first surface, for novices. Toggled in settings.</summary>
    [RequireComponent(typeof(LineRenderer))]
    public class LaserSight : MonoBehaviour
    {
        public Weapon weapon;
        public float maxDistance = 40f;
        public Transform dot;

        LineRenderer line;

        void Awake()
        {
            line = GetComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
        }

        void LateUpdate()
        {
            var muzzle = weapon != null ? weapon.muzzle : null;
            var on = Sfx.LaserSight && muzzle != null && weapon.Holder != null;
            line.enabled = on;
            if (dot != null) dot.gameObject.SetActive(on);
            if (!on) return;
            var origin = muzzle.position + muzzle.forward * 0.02f;
            var end = origin + muzzle.forward * maxDistance;
            if (Physics.Raycast(origin, muzzle.forward, out var hit, maxDistance, SimVOLayers.ProjectileHitMask, QueryTriggerInteraction.Ignore))
            {
                end = hit.point;
                if (dot != null)
                {
                    dot.position = hit.point + hit.normal * 0.003f;
                    dot.rotation = Quaternion.LookRotation(-hit.normal);
                }
            }
            else if (dot != null) dot.position = end;
            line.SetPosition(0, origin);
            line.SetPosition(1, end);
        }
    }
}
