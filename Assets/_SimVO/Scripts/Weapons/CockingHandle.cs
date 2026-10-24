using SimVO.Audio;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SimVO.Weapons
{
    /// <summary>
    /// Slide, bolt, pump or charging handle: grab it with the free hand, pull it back along the weapon, let go.
    /// Pulling past the travel distance chambers a round when the handle is released.
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class CockingHandle : MonoBehaviour
    {
        public Weapon weapon;
        public Transform moving;
        public float travel = 0.04f;
        public AudioSource audioSource;

        XRSimpleInteractable interactable;
        Transform hand;
        Vector3 handStart;
        Vector3 restLocal;
        Vector3 directionLocal;
        bool pulled;

        void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
            if (moving != null) restLocal = moving.localPosition;
        }

        void OnEnable()
        {
            interactable.selectEntered.AddListener(OnSelectEntered);
            interactable.selectExited.AddListener(OnSelectExited);
        }

        void OnDisable()
        {
            interactable.selectEntered.RemoveListener(OnSelectEntered);
            interactable.selectExited.RemoveListener(OnSelectExited);
            hand = null;
        }

        void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (weapon == null || weapon.Holder == null) return;
            hand = args.interactorObject.transform;
            handStart = hand.position;
            pulled = false;
            if (moving != null)
            {
                restLocal = moving.localPosition;
                directionLocal = moving.parent != null ? moving.parent.InverseTransformDirection(-weapon.transform.forward) : -Vector3.forward;
            }
        }

        void Update()
        {
            if (hand == null || moving == null) return;
            var along = Vector3.Dot(hand.position - handStart, -weapon.transform.forward);
            var k = Mathf.Clamp01(along / travel);
            moving.localPosition = restLocal + directionLocal * travel * k;
            if (k >= 0.95f && !pulled)
            {
                pulled = true;
                weapon.Holder?.SendHaptic(0.5f, 0.05f);
                Sfx.Play(audioSource, SfxLibrary.CockBack);
            }
        }

        void OnSelectExited(SelectExitEventArgs args)
        {
            hand = null;
            if (moving != null) moving.localPosition = restLocal;
            if (pulled) weapon.Cock();
            pulled = false;
        }
    }
}
