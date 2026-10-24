using UnityEngine;

namespace SimVO.Weapons
{
    /// <summary>
    /// Working rifle scope: a narrow-FOV camera at the objective renders into the eyepiece while the rifle is held
    /// and the eyepiece is close to the shooter's head.
    /// </summary>
    public class ScopeView : MonoBehaviour
    {
        public Weapon weapon;
        public Camera scopeCamera;
        public Renderer lens;
        public Renderer reticle;
        public float activeDistance = 0.7f;

        void Update()
        {
            var held = weapon != null && weapon.Holder != null;
            var main = Camera.main;
            var near = held && main != null && (main.transform.position - transform.position).sqrMagnitude < activeDistance * activeDistance;
            if (scopeCamera != null) scopeCamera.enabled = near;
            if (lens != null) lens.enabled = near;
            if (reticle != null) reticle.enabled = near;
        }
    }
}
