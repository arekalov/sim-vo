using System.Collections;
using UnityEngine;
using UnityEngine.XR.Management;

namespace SimVO.Platform
{
    /// <summary>Enables the VR rig when an XR runtime is active, otherwise the desktop rig.</summary>
    public class PlatformBootstrap : MonoBehaviour
    {
        public GameObject xrRig;
        public GameObject desktopRig;
        [Tooltip("Editor only: run the desktop controls even if a headset is connected.")]
        public bool forceDesktopInEditor;

        public static bool IsXR { get; private set; }

        IEnumerator Start()
        {
            yield return null;
            var manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
            var xr = manager != null && manager.activeLoader != null;
#if UNITY_EDITOR
            if (forceDesktopInEditor) xr = false;
#endif
            IsXR = xr;
            if (xrRig != null) xrRig.SetActive(xr);
            if (desktopRig != null) desktopRig.SetActive(!xr);
            Debug.Log(xr ? "SimVO: XR rig active" : "SimVO: desktop rig active");
        }
    }
}
