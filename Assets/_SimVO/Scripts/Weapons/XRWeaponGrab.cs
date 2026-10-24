using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace SimVO.Weapons
{
    /// <summary>
    /// Bridges XR Interaction Toolkit to the weapon: trigger (Activate) fires, the primary button of the
    /// holding hand ejects the magazine, recoil goes to that controller's haptics.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(Weapon))]
    public class XRWeaponGrab : MonoBehaviour, IWeaponHolder
    {
        Weapon weapon;
        XRGrabInteractable grab;
        HapticImpulsePlayer haptics;
        InputAction ejectAction;

        void Awake()
        {
            weapon = GetComponent<Weapon>();
            grab = GetComponent<XRGrabInteractable>();
        }

        void OnEnable()
        {
            grab.activated.AddListener(OnActivated);
            grab.deactivated.AddListener(OnDeactivated);
            grab.selectEntered.AddListener(OnSelectEntered);
            grab.selectExited.AddListener(OnSelectExited);
        }

        void OnDisable()
        {
            grab.activated.RemoveListener(OnActivated);
            grab.deactivated.RemoveListener(OnDeactivated);
            grab.selectEntered.RemoveListener(OnSelectEntered);
            grab.selectExited.RemoveListener(OnSelectExited);
            ReleaseEject();
        }

        void OnActivated(ActivateEventArgs args)
        {
            weapon.TriggerHeld = true;
            weapon.TryFire();
        }

        void OnDeactivated(DeactivateEventArgs args)
        {
            weapon.TriggerHeld = false;
        }

        void OnSelectEntered(SelectEnterEventArgs args)
        {
            Debug.Log($"SimVO: grab {weapon.displayName} by {args.interactorObject.transform.name} ({args.interactorObject.handedness})");
            if (args.interactorObject is XRSocketInteractor) return;
            haptics = args.interactorObject.transform.GetComponentInParent<HapticImpulsePlayer>();
            weapon.Holder = this;
            SetupEject(args.interactorObject.handedness);
        }

        void OnSelectExited(SelectExitEventArgs args)
        {
            Debug.Log($"SimVO: release {weapon.displayName} by {args.interactorObject.transform.name}");
            if (args.interactorObject is XRSocketInteractor) return;
            if (ReferenceEquals(weapon.Holder, this)) weapon.Holder = null;
            weapon.TriggerHeld = false;
            haptics = null;
            ReleaseEject();
        }

        void SetupEject(InteractorHandedness handedness)
        {
            ReleaseEject();
            var side = handedness == InteractorHandedness.Left ? "LeftHand" : "RightHand";
            ejectAction = new InputAction("EjectMagazine", InputActionType.Button, $"<XRController>{{{side}}}/primaryButton");
            ejectAction.Enable();
        }

        void ReleaseEject()
        {
            if (ejectAction == null) return;
            ejectAction.Disable();
            ejectAction.Dispose();
            ejectAction = null;
        }

        void Update()
        {
            if (ejectAction != null && ejectAction.WasPressedThisFrame() && weapon is Pistol pistol)
            {
                pistol.EjectMagazine();
            }
        }

        public void SendHaptic(float amplitude, float duration)
        {
            if (haptics != null) haptics.SendHapticImpulse(amplitude, duration);
        }
    }
}
