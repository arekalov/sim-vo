using System.Collections.Generic;
using SimVO.Weapons;
using UnityEngine;

namespace SimVO.Environment
{
    /// <summary>Rack by the entrance. In VR weapons are simply grabbed; on desktop keys 1/2/3 take them.</summary>
    public class WeaponStand : MonoBehaviour
    {
        public List<Weapon> weapons = new List<Weapon>();

        public Weapon GetWeapon(int index)
        {
            return index >= 0 && index < weapons.Count ? weapons[index] : null;
        }
    }
}
