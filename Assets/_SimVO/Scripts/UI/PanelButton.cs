using System.Collections;
using SimVO.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SimVO.UI
{
    /// <summary>Physical button on the control panel with a text label.</summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class PanelButton : MonoBehaviour, IPressable
    {
        public TMP_Text label;
        public Transform cap;
        public float pressDepth = 0.008f;
        public AudioSource audioSource;
        public UnityEvent onPress = new UnityEvent();

        XRSimpleInteractable interactable;
        Vector3 capRest;
        Coroutine animation;

        void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
            if (cap != null) capRest = cap.localPosition;
        }

        void OnEnable() => interactable.selectEntered.AddListener(OnSelectEntered);
        void OnDisable() => interactable.selectEntered.RemoveListener(OnSelectEntered);

        void OnSelectEntered(SelectEnterEventArgs args) => Press();

        public void SetLabel(string text)
        {
            if (label != null) label.text = text;
        }

        public void Press()
        {
            if (audioSource != null) audioSource.PlayOneShot(ProceduralSfx.ButtonClick);
            if (cap != null)
            {
                if (animation != null) StopCoroutine(animation);
                animation = StartCoroutine(Animate());
            }
            onPress.Invoke();
        }

        IEnumerator Animate()
        {
            cap.localPosition = capRest - Vector3.up * pressDepth;
            yield return new WaitForSeconds(0.12f);
            cap.localPosition = capRest;
            animation = null;
        }
    }
}
