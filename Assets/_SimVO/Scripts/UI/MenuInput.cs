using UnityEngine;
using UnityEngine.InputSystem;

namespace SimVO.UI
{
    /// <summary>Esc on desktop or the left controller's menu button toggles the pause menu.</summary>
    public class MenuInput : MonoBehaviour
    {
        public GameMenu menu;

        InputAction action;

        void OnEnable()
        {
            action = new InputAction("Menu", InputActionType.Button);
            action.AddBinding("<Keyboard>/escape");
            action.AddBinding("<XRController>{LeftHand}/menuButton");
            action.AddBinding("<XRController>{LeftHand}/menu");
            action.performed += OnPerformed;
            action.Enable();
        }

        void OnDisable()
        {
            if (action == null) return;
            action.performed -= OnPerformed;
            action.Disable();
            action.Dispose();
            action = null;
        }

        void OnPerformed(InputAction.CallbackContext context)
        {
            if (menu != null) menu.Toggle();
        }
    }
}
