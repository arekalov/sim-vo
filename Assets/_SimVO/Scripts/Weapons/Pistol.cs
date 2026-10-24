using System.Collections;
using SimVO.Audio;
using UnityEngine;

namespace SimVO.Weapons
{
    /// <summary>One-handed, semi-automatic, magazine fed.</summary>
    public class Pistol : Weapon
    {
        public MagazineSocket socket;
        public Magazine magazinePrefab;
        [Tooltip("Spawn a full magazine into the socket when the scene starts.")]
        public bool startLoaded = true;

        public Magazine Magazine => socket != null ? socket.CurrentMagazine : null;
        public override bool HasAmmo => Magazine != null && Magazine.Rounds > 0;
        public override int RoundsLoaded => Magazine != null ? Magazine.Rounds : 0;

        IEnumerator Start()
        {
            yield return null;
            if (startLoaded && Magazine == null && socket != null && magazinePrefab != null)
            {
                socket.InsertNew(magazinePrefab);
            }
        }

        protected override void ConsumeRound()
        {
            if (Magazine != null) Magazine.Rounds--;
        }

        public void EjectMagazine()
        {
            if (socket != null) socket.Eject();
        }

        public override void QuickReload()
        {
            if (socket == null) return;
            if (Magazine != null)
            {
                Magazine.Refill();
                Play(ProceduralSfx.MagIn);
            }
            else if (magazinePrefab != null)
            {
                socket.InsertNew(magazinePrefab);
            }
        }
    }
}
