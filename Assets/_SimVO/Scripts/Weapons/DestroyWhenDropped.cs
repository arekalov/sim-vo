using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SimVO.Weapons
{
    /// <summary>Cleans up magazines left on the floor.</summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class DestroyWhenDropped : MonoBehaviour
    {
        public float delay = 6f;

        XRGrabInteractable grab;
        Coroutine pending;

        void Awake() => grab = GetComponent<XRGrabInteractable>();
        void OnEnable() => grab.selectExited.AddListener(OnDropped);
        void OnDisable() => grab.selectExited.RemoveListener(OnDropped);

        void OnDropped(SelectExitEventArgs args)
        {
            if (pending != null) StopCoroutine(pending);
            pending = StartCoroutine(DestroyLater());
        }

        IEnumerator DestroyLater()
        {
            yield return new WaitForSeconds(delay);
            if (!grab.isSelected) Destroy(gameObject);
        }
    }
}
