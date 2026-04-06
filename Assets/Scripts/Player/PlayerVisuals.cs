using UnityEngine;

namespace PlatformGravity.Player
{
    /// <summary>
    /// Handles sprite orientation so the character always appears upright relative to the
    /// current platform face, and flips the sprite when changing walk direction.
    /// Purely cosmetic — has no effect on gameplay or physics.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerVisuals : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private float rotationSpeed = 720f;

        private PlayerController _player;

        // Each entry is the z-rotation (degrees) that makes the sprite's feet point toward the face surface.
        private static readonly float[] FaceAngles = { 0f, 90f, 180f, -90f };

        private void Awake()
        {
            _player = GetComponent<PlayerController>();

            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void Update()
        {
            // Smoothly rotate toward the target angle so face transitions look fluid.
            float targetAngle = FaceAngles[_player.CurrentFace];
            Quaternion targetRot = Quaternion.Euler(0f, 0f, targetAngle);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRot, rotationSpeed * Time.deltaTime);

            // Flip the sprite based on walk direction; skip when idle to keep the last facing.
            if (spriteRenderer != null && !Mathf.Approximately(_player.MoveInput, 0f))
                spriteRenderer.flipX = _player.MoveInput < 0f;
        }
    }
}