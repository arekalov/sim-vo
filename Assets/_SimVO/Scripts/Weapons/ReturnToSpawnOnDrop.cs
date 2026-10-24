using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SimVO.Weapons
{
    /// <summary>Puts a dropped weapon back on its rack after a delay, so a novice never loses it on the floor.</summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class ReturnToSpawnOnDrop : MonoBehaviour
    {
        public float delay = 4f;

        Vector3 spawnPosition;
        Quaternion spawnRotation;
        Transform spawnParent;
        XRGrabInteractable grab;
        Coroutine pending;

        void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            spawnParent = transform.parent;
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
        }

        void OnEnable() => grab.selectExited.AddListener(OnDropped);
        void OnDisable() => grab.selectExited.RemoveListener(OnDropped);

        void OnDropped(SelectExitEventArgs args)
        {
            var body = GetComponent<Rigidbody>();
            if (body != null) body.isKinematic = false;
            if (pending != null) StopCoroutine(pending);
            pending = StartCoroutine(ReturnLater());
        }

        IEnumerator ReturnLater()
        {
            yield return new WaitForSeconds(delay);
            if (!grab.isSelected) ReturnNow();
            pending = null;
        }

        public void ReturnNow()
        {
            transform.SetParent(spawnParent, true);
            transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            var body = GetComponent<Rigidbody>();
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
            }
        }
    }
}
