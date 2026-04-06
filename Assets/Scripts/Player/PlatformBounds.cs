using UnityEngine;

namespace PlatformGravity.Player
{
    /// <summary>
    /// Defines the logical boundaries of the platform in world space.
    /// <see cref="PlayerController"/> reads <see cref="WorldBounds"/> every frame to decide
    /// when the player has reached a corner and needs to transition to an adjacent face.
    ///
    /// The <see cref="size"/> field should match the actual world-space extent of the
    /// platform collider (i.e. local collider size × transform scale).
    /// </summary>
    [ExecuteAlways]
    public class PlatformBounds : MonoBehaviour
    {
        [Tooltip("World-space width and height of the platform.")]
        [SerializeField] private Vector2 size = new Vector2(6f, 2.5f);

        /// <summary>
        /// Axis-aligned rect centred on this transform's position.
        /// </summary>
        public Rect WorldBounds
        {
            get
            {
                Vector2 centre = transform.position;
                Vector2 half = size * 0.5f;
                return new Rect(centre - half, size);
            }
        }

        public Vector2 Size => size;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Rect b = WorldBounds;
            var c = new Vector3(b.center.x, b.center.y, 0f);
            var ext = new Vector3(b.width, b.height, 0.01f);

            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.25f);
            Gizmos.DrawCube(c, ext);
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.9f);
            Gizmos.DrawWireCube(c, ext);
        }
#endif
    }
}