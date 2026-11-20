using UnityEngine;

namespace ChromaStrike.Player
{
    /// <summary>
    /// Locks the mouse while playing and frees it for menus.
    /// Esc releases the cursor without pausing the timer; the next click captures it again
    /// (that click is ignored by ClickHandler so it never fires by accident).
    /// </summary>
    public sealed class CursorController : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;

        private bool gameplayMode;

        public bool IsCaptured => Cursor.lockState == CursorLockMode.Locked;

        public void SetGameplayMode(bool enabled)
        {
            gameplayMode = enabled;
            Apply(enabled);
        }

        private void Update()
        {
            if (!gameplayMode) return;

            if (IsCaptured && input.ReleaseCursorPressedThisFrame) Apply(false);
            else if (!IsCaptured && input.FirePressedThisFrame) Apply(true);
        }

        private static void Apply(bool captured)
        {
            Cursor.lockState = captured ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !captured;
        }
    }
}
