using System.Collections;
using SimVO.Audio;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace SimVO.Weapons
{
    /// <summary>Socket that only accepts magazines. Used inside weapons and in the magazine dispenser.</summary>
    public class MagazineSocket : XRSocketInteractor
    {
        /// <summary>Interaction layer bit that hands do not use, so an inserted magazine is not grabbed by accident.</summary>
        public const int InsertedLayer = 1 << 1;
        public const int DefaultLayer = 1 << 0;

        public bool lockWhenInserted = true;
        public AudioSource audioSource;
        public float ejectImpulse = 1.2f;

        public Magazine CurrentMagazine { get; private set; }

        public override bool CanHover(IXRHoverInteractable interactable)
            => base.CanHover(interactable) && IsMagazine(interactable);

        public override bool CanSelect(IXRSelectInteractable interactable)
            => base.CanSelect(interactable) && IsMagazine(interactable);

        static bool IsMagazine(IXRInteractable interactable)
            => interactable.transform.GetComponent<Magazine>() != null;

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            var magazine = args.interactableObject.transform.GetComponent<Magazine>();
            if (magazine == null) return;
            CurrentMagazine = magazine;
            if (lockWhenInserted) magazine.Grab.interactionLayers = InsertedLayer;
            Play(SfxLibrary.MagIn);
        }

        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);
            if (CurrentMagazine == null || args.interactableObject.transform != CurrentMagazine.transform) return;
            CurrentMagazine.Grab.interactionLayers = DefaultLayer;
            CurrentMagazine = null;
        }

        Coroutine disable;

        public void Eject()
        {
            var magazine = CurrentMagazine;
            if (magazine == null) return;
            interactionManager.SelectExit((IXRSelectInteractor)this, (IXRSelectInteractable)magazine.Grab);
            if (disable != null) StopCoroutine(disable);
            disable = StartCoroutine(DisableBriefly(0.8f));
            var body = magazine.GetComponent<Rigidbody>();
            if (body != null) body.AddForce(-transform.up * ejectImpulse, ForceMode.Impulse);
            Play(SfxLibrary.MagOut);
        }

        public Magazine InsertNew(Magazine prefab)
        {
            if (disable != null) StopCoroutine(disable);
            disable = null;
            socketActive = true;
            var anchor = attachTransform != null ? attachTransform : transform;
            var magazine = Instantiate(prefab, anchor.position, anchor.rotation);
            magazine.Refill();
            StartCoroutine(SelectNextFrame(magazine));
            return magazine;
        }

        IEnumerator SelectNextFrame(Magazine magazine)
        {
            var waited = 0f;
            while (magazine != null && !IsHovering(magazine.Grab) && waited < 0.5f)
            {
                waited += Time.deltaTime;
                yield return null;
            }
            if (magazine != null && interactionManager != null && !magazine.Grab.isSelected)
            {
                interactionManager.SelectEnter((IXRSelectInteractor)this, (IXRSelectInteractable)magazine.Grab);
            }
        }

        IEnumerator DisableBriefly(float seconds)
        {
            socketActive = false;
            yield return new WaitForSeconds(seconds);
            socketActive = true;
            disable = null;
        }

        void Play(AudioClip clip)
        {
            Sfx.Play(audioSource, clip);
        }
    }
}
