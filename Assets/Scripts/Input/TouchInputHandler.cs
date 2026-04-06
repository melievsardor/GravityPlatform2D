using UnityEngine;

namespace PlatformGravity.Input
{
    /// <summary>
    /// Converts on-screen button presses (forwarded by <see cref="MobileButton"/>)
    /// into movement and jump commands on <see cref="Player.PlayerController"/>.
    ///
    /// Runs in FixedUpdate to stay in sync with the physics loop.
    /// Keyboard input takes priority — if a horizontal key is held, touch is ignored.
    /// </summary>
    public class TouchInputHandler : MonoBehaviour
    {
        private Player.PlayerController _player;

        // Reference-counted so overlapping touches on the same button never desync.
        private int _leftHeld;
        private int _rightHeld;
        private bool _jumpPressed;

        private void Awake()
        {
            _player = GetComponent<Player.PlayerController>();
        }

        private void FixedUpdate()
        {
            // Let keyboard win when both sources are active at the same time.
            float keyAxis = UnityEngine.Input.GetAxisRaw("Horizontal");
            if (!Mathf.Approximately(keyAxis, 0f)) return;

            if (_rightHeld > 0 && _leftHeld == 0)
                _player.MoveInput = 1f;
            else if (_leftHeld > 0 && _rightHeld == 0)
                _player.MoveInput = -1f;
            else
                _player.MoveInput = 0f;

            if (_jumpPressed)
            {
                _player.JumpRequested = true;
                _jumpPressed = false;
            }
        }

        // Called by MobileButton through direct references.
        public void OnLeftDown() => _leftHeld++;
        public void OnLeftUp() => _leftHeld = Mathf.Max(0, _leftHeld - 1);
        public void OnRightDown() => _rightHeld++;
        public void OnRightUp() => _rightHeld = Mathf.Max(0, _rightHeld - 1);
        public void OnJumpDown() => _jumpPressed = true;
    }
}