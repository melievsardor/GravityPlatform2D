using UnityEngine;

namespace PlatformGravity.Input
{
    /// <summary>
    /// Translates keyboard input (A/D or arrow keys for movement, Space for jump)
    /// into commands on <see cref="Player.PlayerController"/>.
    ///
    /// Jump is buffered between Update and FixedUpdate so that single-frame button
    /// presses are never lost due to the timing mismatch between rendering and physics.
    /// </summary>
    [RequireComponent(typeof(Player.PlayerController))]
    public class KeyboardInputHandler : MonoBehaviour
    {
        private Player.PlayerController _player;
        private bool _jumpBuffered;

        private void Awake()
        {
            _player = GetComponent<Player.PlayerController>();
        }

        private void Update()
        {
            // GetButtonDown only fires for one render frame, so latch it here
            // and let FixedUpdate consume it on the next physics step.
            if (UnityEngine.Input.GetButtonDown("Jump"))
                _jumpBuffered = true;
        }

        private void FixedUpdate()
        {
            float horizontal = UnityEngine.Input.GetAxisRaw("Horizontal");

            if (!Mathf.Approximately(horizontal, 0f))
                _player.MoveInput = horizontal;

            if (_jumpBuffered)
            {
                _player.JumpRequested = true;
                _jumpBuffered = false;
            }
        }
    }
}