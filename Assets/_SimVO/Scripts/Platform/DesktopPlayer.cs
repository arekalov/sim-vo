using SimVO.Core;
using SimVO.Environment;
using SimVO.UI;
using SimVO.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SimVO.Platform
{
    /// <summary>
    /// Mouse and keyboard stand-in for the VR body: WASD move, mouse look, LMB fire, R reload,
    /// 1/2/3 take a weapon from the rack, C kneel, Z prone, E press what you look at, Esc free the cursor.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class DesktopPlayer : MonoBehaviour, IWeaponHolder
    {
        public Camera cam;
        public Transform handAnchor;
        public WeaponStand stand;
        public GameMenu menu;
        public float moveSpeed = 2.2f;
        public float lookSensitivity = 0.08f;
        public float standingEyeHeight = 1.65f;
        public float kneelingEyeHeight = 1.05f;
        public float proneEyeHeight = 0.4f;
        public float interactDistance = 3f;

        CharacterController controller;
        float yaw;
        float pitch;
        Weapon weapon;
        BodyPosition position = BodyPosition.Standing;
        float recoilKick;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            yaw = transform.eulerAngles.y;
        }

        void OnEnable()
        {
            LockCursor(true);
            ApplyPosition(BodyPosition.Standing);
        }

        void OnDisable() => LockCursor(false);

        void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null) return;

            var locked = Cursor.lockState == CursorLockMode.Locked;
            if (!locked && !Application.isEditor)
            {
                if (mouse.leftButton.wasPressedThisFrame) LockCursor(true);
                return;
            }

            if (locked)
            {
                var delta = mouse.delta.ReadValue() * lookSensitivity;
                yaw += delta.x;
                pitch = Mathf.Clamp(pitch - delta.y, -80f, 80f);
            }
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            recoilKick = Mathf.MoveTowards(recoilKick, 0f, 20f * Time.deltaTime);
            cam.transform.localRotation = Quaternion.Euler(pitch - recoilKick, 0f, 0f);

            var move = Vector2.zero;
            if (keyboard.wKey.isPressed) move.y += 1f;
            if (keyboard.sKey.isPressed) move.y -= 1f;
            if (keyboard.dKey.isPressed) move.x += 1f;
            if (keyboard.aKey.isPressed) move.x -= 1f;
            var speed = position == BodyPosition.Standing ? moveSpeed : moveSpeed * 0.4f;
            controller.SimpleMove((transform.forward * move.y + transform.right * move.x).normalized * speed);

            if (keyboard.cKey.wasPressedThisFrame) ApplyPosition(position == BodyPosition.Kneeling ? BodyPosition.Standing : BodyPosition.Kneeling);
            if (keyboard.zKey.wasPressedThisFrame) ApplyPosition(position == BodyPosition.Prone ? BodyPosition.Standing : BodyPosition.Prone);

            if (keyboard.digit1Key.wasPressedThisFrame) TakeWeapon(0);
            if (keyboard.digit2Key.wasPressedThisFrame) TakeWeapon(1);
            if (keyboard.digit3Key.wasPressedThisFrame) TakeWeapon(2);
            if (keyboard.digit4Key.wasPressedThisFrame) TakeWeapon(3);
            if (keyboard.digit5Key.wasPressedThisFrame) TakeWeapon(4);
            if (keyboard.digit6Key.wasPressedThisFrame) TakeWeapon(5);
            if (keyboard.digit7Key.wasPressedThisFrame) TakeWeapon(6);
            if (keyboard.gKey.wasPressedThisFrame) DropWeapon();

            if (weapon != null) ConvergeOnCrosshair();
            var menuOpen = menu != null && menu.IsOpen;
            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (menuOpen) Interact();
                else if (weapon != null) weapon.TryFire();
            }
            if (weapon != null) weapon.TriggerHeld = !menuOpen && mouse.leftButton.isPressed;
            if (keyboard.rKey.wasPressedThisFrame && weapon != null && !menuOpen) weapon.QuickReload();
            if (keyboard.fKey.wasPressedThisFrame && weapon != null && !menuOpen) weapon.Cock();
            if (keyboard.eKey.wasPressedThisFrame) Interact();
        }

        public void SetPosition(BodyPosition next) => ApplyPosition(next);

        void ApplyPosition(BodyPosition next)
        {
            position = next;
            var eye = next switch
            {
                BodyPosition.Kneeling => kneelingEyeHeight,
                BodyPosition.Prone => proneEyeHeight,
                _ => standingEyeHeight,
            };
            controller.height = Mathf.Max(eye + 0.1f, 0.5f);
            controller.center = new Vector3(0f, controller.height * 0.5f, 0f);
            cam.transform.localPosition = new Vector3(0f, eye, 0f);
            if (GameSession.Instance != null) GameSession.Instance.CurrentPosition = next;
        }

        public Weapon CurrentWeapon => weapon;

        public void TakeWeapon(int index)
        {
            if (stand == null) return;
            var next = stand.GetWeapon(index);
            if (next == null || next == weapon) return;
            DropWeapon();
            weapon = next;
            var grab = weapon.GetComponent<XRGrabInteractable>();
            if (grab != null) grab.enabled = false;
            var body = weapon.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.interpolation = RigidbodyInterpolation.None;
                body.isKinematic = true;
            }
            weapon.transform.SetParent(handAnchor, false);
            weapon.transform.localPosition = weapon.twoHanded ? new Vector3(-0.08f, -0.02f, -0.1f) : Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;
            weapon.Holder = this;
        }

        public void DropWeapon()
        {
            if (weapon == null) return;
            weapon.Holder = null;
            weapon.TriggerHeld = false;
            var grab = weapon.GetComponent<XRGrabInteractable>();
            if (grab != null) grab.enabled = true;
            var body = weapon.GetComponent<Rigidbody>();
            if (body != null) body.isKinematic = false;
            var returner = weapon.GetComponent<ReturnToSpawnOnDrop>();
            if (returner != null) returner.ReturnNow();
            weapon = null;
        }

        /// <summary>Points the barrel at whatever the crosshair is on, so desktop shots land where the player looks.</summary>
        void ConvergeOnCrosshair()
        {
            var ray = new Ray(cam.transform.position, cam.transform.forward);
            var aimPoint = Physics.Raycast(ray, out var hit, 100f, SimVOLayers.ProjectileHitMask, QueryTriggerInteraction.Ignore)
                ? hit.point
                : ray.GetPoint(100f);
            for (var i = 0; i < 2; i++)
            {
                var muzzle = weapon.muzzle != null ? weapon.muzzle.position : weapon.transform.position;
                var direction = aimPoint - muzzle;
                if (direction.sqrMagnitude < 0.0001f) return;
                weapon.transform.rotation = Quaternion.LookRotation(direction, cam.transform.up);
            }
        }

        public void Interact()
        {
            var ray = new Ray(cam.transform.position, cam.transform.forward);
            if (!Physics.Raycast(ray, out var hit, interactDistance, ~0, QueryTriggerInteraction.Collide)) return;
            var pressable = hit.collider.GetComponentInParent<IPressable>();
            pressable?.Press();
        }

        public void SendHaptic(float amplitude, float duration)
        {
            recoilKick = amplitude * 2.5f;
        }

        /// <summary>Turns the body and camera towards a world point. Used by editor tooling and tests.</summary>
        public void AimAt(Vector3 worldPoint)
        {
            var direction = worldPoint - cam.transform.position;
            var flat = new Vector3(direction.x, 0f, direction.z);
            yaw = Quaternion.LookRotation(flat).eulerAngles.y;
            pitch = -Mathf.Atan2(direction.y, flat.magnitude) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            cam.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
