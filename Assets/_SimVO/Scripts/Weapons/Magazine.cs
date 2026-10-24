using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SimVO.Weapons
{
    [RequireComponent(typeof(XRGrabInteractable))]
    public class Magazine : MonoBehaviour
    {
        public int capacity = 10;
        [SerializeField] int rounds = 10;

        public int Rounds
        {
            get => rounds;
            set
            {
                var clamped = Mathf.Clamp(value, 0, capacity);
                if (clamped == rounds) return;
                rounds = clamped;
                Changed?.Invoke();
            }
        }

        public event Action Changed;
        public XRGrabInteractable Grab { get; private set; }

        void Awake()
        {
            Grab = GetComponent<XRGrabInteractable>();
        }

        public void Refill() => Rounds = capacity;
    }
}
