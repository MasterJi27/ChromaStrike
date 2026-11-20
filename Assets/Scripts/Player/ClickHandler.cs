using System;
using UnityEngine;
using ChromaStrike.Core;
using ChromaStrike.Spheres;

namespace ChromaStrike.Player
{
    /// <summary>
    /// Raycast shooting. On left click a ray leaves the exact centre of the camera; if the first
    /// thing it hits is a Sphere, that sphere is destroyed.
    /// Because walls and props are on the mask, a sphere hiding behind a crate cannot be hit.
    /// </summary>
    public sealed class ClickHandler : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private CursorController cursor;

        /// <summary>Gameplay switch. Off in the menu and after the timer ends.</summary>
        public bool IsActive { get; set; }

        /// <summary>Raised for every shot. The bool tells whether a sphere was hit.</summary>
        public event Action<bool> ShotFired;

        private void OnEnable() => input.FirePressed += HandleFire;
        private void OnDisable() => input.FirePressed -= HandleFire;

        private void HandleFire()
        {
            // The click that re-captures the cursor after Esc must not count as a shot.
            if (!IsActive || !cursor.IsCaptured) return;
            ShotFired?.Invoke(TryShoot());
        }

        /// <summary>Casts the centre ray. Public so tests can shoot without simulating the mouse.</summary>
        public bool TryShoot()
        {
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (!Physics.Raycast(ray, out RaycastHit hit, config.shootDistance, config.shootMask, QueryTriggerInteraction.Ignore))
                return false;

            if (!hit.collider.TryGetComponent(out Sphere sphere)) return false;
            sphere.Pop();
            return true;
        }
    }
}
