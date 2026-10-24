using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace SimVO.Weapons
{
    /// <summary>Keeps one fresh magazine (or shell) in its socket; spawns another when the player takes it.</summary>
    public class MagazineDispenser : MonoBehaviour
    {
        public XRSocketInteractor socket;
        public GameObject itemPrefab;
        public float respawnDelay = 0.5f;

        IEnumerator Start()
        {
            if (socket is MagazineSocket magazineSocket) magazineSocket.lockWhenInserted = false;
            socket.selectExited.AddListener(OnTaken);
            yield return null;
            if (!socket.hasSelection) Spawn();
        }

        void OnDestroy()
        {
            if (socket != null) socket.selectExited.RemoveListener(OnTaken);
        }

        void OnTaken(SelectExitEventArgs args) => StartCoroutine(Respawn());

        IEnumerator Respawn()
        {
            yield return new WaitForSeconds(respawnDelay);
            if (socket != null && !socket.hasSelection) Spawn();
        }

        void Spawn()
        {
            if (itemPrefab == null) return;
            var anchor = socket.attachTransform != null ? socket.attachTransform : socket.transform;
            var item = Instantiate(itemPrefab, anchor.position, anchor.rotation);
            var magazine = item.GetComponent<Magazine>();
            if (magazine != null) magazine.Refill();
            StartCoroutine(SelectNextFrame(item.GetComponent<XRGrabInteractable>()));
        }

        IEnumerator SelectNextFrame(XRGrabInteractable grab)
        {
            yield return null;
            if (grab != null && socket.interactionManager != null)
            {
                socket.interactionManager.SelectEnter((IXRSelectInteractor)socket, (IXRSelectInteractable)grab);
            }
        }
    }
}
