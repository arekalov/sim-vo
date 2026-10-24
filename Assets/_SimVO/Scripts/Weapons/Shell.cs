using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SimVO.Weapons
{
    /// <summary>A single shotgun shell the player grabs and pushes into the loading port.</summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class Shell : MonoBehaviour
    {
        public XRGrabInteractable Grab { get; private set; }
        void Awake() => Grab = GetComponent<XRGrabInteractable>();
    }
}
