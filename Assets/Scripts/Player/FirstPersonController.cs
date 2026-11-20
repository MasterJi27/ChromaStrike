using UnityEngine;
using ChromaStrike.Core;

namespace ChromaStrike.Player
{
    /// <summary>
    /// Classic FPS movement: WASD moves the CharacterController, the mouse turns the body (yaw)
    /// and tilts the camera (pitch). Shift sprints. Everything is tuned from GameConfig.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private CursorController cursor;
        [SerializeField, Tooltip("Child object that holds the camera. Only this part pitches up and down.")]
        private Transform cameraPivot;

        private CharacterController body;
        private Vector3 startPosition;
        private Quaternion startRotation;
        private float pitch;
        private float verticalVelocity;

        /// <summary>Menus and the end screen switch the controller off.</summary>
        public bool ControlEnabled { get; set; }

        private void Awake()
        {
            body = GetComponent<CharacterController>();
            startPosition = transform.position;
            startRotation = transform.rotation;
        }

        private void Update()
        {
            if (!ControlEnabled) return;
            if (cursor.IsCaptured) Look();
            Move();
        }

        private void Look()
        {
            Vector2 delta = input.Look * config.lookSensitivity;
            transform.Rotate(0f, delta.x, 0f);
            pitch = Mathf.Clamp(pitch - delta.y, -config.maxLookAngle, config.maxLookAngle);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void Move()
        {
            Vector2 axis = input.Move;
            float speed = input.SprintHeld ? config.sprintSpeed : config.walkSpeed;
            Vector3 planar = Vector3.ClampMagnitude(transform.right * axis.x + transform.forward * axis.y, 1f) * speed;

            if (body.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f; // keeps the body glued to the floor
            verticalVelocity += config.gravity * Time.deltaTime;

            body.Move((planar + Vector3.up * verticalVelocity) * Time.deltaTime);
        }

        /// <summary>Puts the player back at the spawn point (used when a round starts).</summary>
        public void ResetPose()
        {
            body.enabled = false; // a CharacterController must be off while it is teleported
            transform.SetPositionAndRotation(startPosition, startRotation);
            body.enabled = true;
            pitch = 0f;
            verticalVelocity = 0f;
            if (cameraPivot != null) cameraPivot.localRotation = Quaternion.identity;
        }
    }
}
