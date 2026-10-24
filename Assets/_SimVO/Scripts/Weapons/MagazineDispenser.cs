using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace SimVO.Weapons
{
    /// <summary>Keeps one fresh magazine in its socket; spawns another when the player takes it.</summary>
    public class MagazineDispenser : MonoBehaviour
    {
        public MagazineSocket socket;
        public Magazine magazinePrefab;
        public float respawnDelay = 0.5f;

        IEnumerator Start()
        {
            socket.lockWhenInserted = false;
            socket.selectExited.AddListener(OnTaken);
            yield return null;
            if (!socket.hasSelection) socket.InsertNew(magazinePrefab);
        }

        void OnDestroy()
        {
            if (socket != null) socket.selectExited.RemoveListener(OnTaken);
        }

        void OnTaken(SelectExitEventArgs args)
        {
            StartCoroutine(Respawn());
        }

        IEnumerator Respawn()
        {
            yield return new WaitForSeconds(respawnDelay);
            if (socket != null && !socket.hasSelection) socket.InsertNew(magazinePrefab);
        }
    }
}
