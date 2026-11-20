using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChromaStrike.Player
{
    /// <summary>
    /// The only place that talks to the Input System. Other scripts read plain values or
    /// subscribe to events, so swapping keyboard for a gamepad later means editing this file only.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        private InputAction move;
        private InputAction look;
        private InputAction fire;
        private InputAction sprint;
        private InputAction release;

        /// <summary>WASD as a vector (x = strafe, y = forward).</summary>
        public Vector2 Move => move.ReadValue<Vector2>();
        /// <summary>Mouse movement in pixels since the last frame.</summary>
        public Vector2 Look => look.ReadValue<Vector2>();
        public bool SprintHeld => sprint.IsPressed();
        public bool ReleaseCursorPressedThisFrame => release.WasPressedThisFrame();
        public bool FirePressedThisFrame => fire.WasPressedThisFrame();

        /// <summary>Raised the moment the left mouse button goes down.</summary>
        public event Action FirePressed;

        private void Awake()
        {
            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");

            look = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");
            fire = new InputAction("Fire", InputActionType.Button, "<Mouse>/leftButton");
            sprint = new InputAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
            release = new InputAction("Release Cursor", InputActionType.Button, "<Keyboard>/escape");

            fire.performed += OnFirePerformed;
        }

        private void OnEnable()
        {
            move.Enable(); look.Enable(); fire.Enable(); sprint.Enable(); release.Enable();
        }

        private void OnDisable()
        {
            move.Disable(); look.Disable(); fire.Disable(); sprint.Disable(); release.Disable();
        }

        private void OnDestroy()
        {
            fire.performed -= OnFirePerformed;
            move.Dispose(); look.Dispose(); fire.Dispose(); sprint.Dispose(); release.Dispose();
        }

        private void OnFirePerformed(InputAction.CallbackContext _) => FirePressed?.Invoke();
    }
}
