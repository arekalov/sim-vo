using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace SimVO.Weapons
{
    /// <summary>Loading port on the shotgun: a shell dropped in is consumed and adds one round.</summary>
    public class ShellSocket : XRSocketInteractor
    {
        public Shotgun shotgun;

        public override bool CanHover(IXRHoverInteractable interactable)
            => base.CanHover(interactable) && interactable.transform.GetComponent<Shell>() != null;

        public override bool CanSelect(IXRSelectInteractable interactable)
            => base.CanSelect(interactable) && interactable.transform.GetComponent<Shell>() != null;

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            var shell = args.interactableObject.transform.GetComponent<Shell>();
            if (shell == null || shotgun == null) return;
            if (shotgun.LoadShell())
            {
                interactionManager.SelectExit((IXRSelectInteractor)this, (IXRSelectInteractable)shell.Grab);
                StartCoroutine(Consume(shell.gameObject));
            }
        }

        /// <summary>Let the interaction manager finish this frame before the shell disappears.</summary>
        System.Collections.IEnumerator Consume(GameObject shell)
        {
            yield return null;
            if (shell != null) Destroy(shell);
        }
    }
}
