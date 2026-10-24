using UnityEngine;

namespace SimVO.Core
{
    /// <summary>Physics layer indices configured by the editor setup (SimVO/Setup).</summary>
    public static class SimVOLayers
    {
        public const int IgnoreRaycast = 2;
        public const int Weapon = 6;
        public const int Target = 7;

        public static int ProjectileHitMask => ~((1 << Weapon) | (1 << IgnoreRaycast));
    }
}
