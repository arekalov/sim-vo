using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace SimVO.Weapons
{
    /// <summary>
    /// Keeps one fresh item in its socket and spawns another when the player takes it. For magazines the box
    /// offers the magazine of whichever weapon is currently in a hand.
    /// </summary>
    public class MagazineDispenser : MonoBehaviour
    {
        public XRSocketInteractor socket;
        public GameObject itemPrefab;
        public float respawnDelay = 0.5f;
        public bool matchHeldWeapon = true;

        GameObject spawnedPrefab;

        IEnumerator Start()
        {
            if (socket is MagazineSocket magazineSocket) magazineSocket.lockWhenInserted = false;
            socket.selectExited.AddListener(OnTaken);
            yield return null;
            if (!socket.hasSelection) Spawn();
            while (true)
            {
                yield return new WaitForSeconds(0.4f);
                if (!matchHeldWeapon || !socket.hasSelection) continue;
                var wanted = CurrentPrefab();
                if (wanted != spawnedPrefab) Replace(wanted);
            }
        }

        void OnDestroy()
        {
            if (socket != null) socket.selectExited.RemoveListener(OnTaken);
        }

        GameObject CurrentPrefab()
        {
            if (!matchHeldWeapon || itemPrefab == null || itemPrefab.GetComponent<Magazine>() == null) return itemPrefab;
            foreach (var pistol in FindObjectsByType<Pistol>(FindObjectsSortMode.None))
            {
                if (pistol.Holder != null && pistol.magazinePrefab != null) return pistol.magazinePrefab.gameObject;
            }
            return itemPrefab;
        }

        void OnTaken(SelectExitEventArgs args) => StartCoroutine(Respawn());

        IEnumerator Respawn()
        {
            yield return new WaitForSeconds(respawnDelay);
            if (socket != null && !socket.hasSelection) Spawn();
        }

        void Replace(GameObject wanted)
        {
            var current = socket.firstInteractableSelected;
            if (current == null) return;
            socket.interactionManager.SelectExit((IXRSelectInteractor)socket, current);
            Destroy(current.transform.gameObject);
            Spawn(wanted);
        }

        void Spawn(GameObject prefab = null)
        {
            prefab = prefab != null ? prefab : CurrentPrefab();
            if (prefab == null) return;
            spawnedPrefab = prefab;
            var anchor = socket.attachTransform != null ? socket.attachTransform : socket.transform;
            var item = Instantiate(prefab, anchor.position, anchor.rotation);
            var magazine = item.GetComponent<Magazine>();
            if (magazine != null) magazine.Refill();
            StartCoroutine(SelectNextFrame(item.GetComponent<XRGrabInteractable>()));
        }

        IEnumerator SelectNextFrame(XRGrabInteractable grab)
        {
            var waited = 0f;
            while (grab != null && !socket.IsHovering(grab) && waited < 0.5f)
            {
                waited += Time.deltaTime;
                yield return null;
            }
            if (grab != null && socket.interactionManager != null && !grab.isSelected)
            {
                socket.interactionManager.SelectEnter((IXRSelectInteractor)socket, (IXRSelectInteractable)grab);
            }
        }
    }
}
