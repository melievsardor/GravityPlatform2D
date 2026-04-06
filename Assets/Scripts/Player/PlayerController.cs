using UnityEngine;

namespace PlatformGravity.Player
{
    /// <summary>
    /// Core character controller that handles movement across all four faces of a rectangular platform.
    /// Each face acts as its own ground plane with gravity pulling the player toward its surface.
    /// When the player reaches a corner, they seamlessly transition to the adjacent face.
    ///
    /// Face layout (clockwise):
    ///   0 = Top, 1 = Right, 2 = Bottom, 3 = Left
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CapsuleCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float acceleration = 40f;
        [SerializeField] private float deceleration = 30f;
        [SerializeField] private float jumpForce = 12f;
        [SerializeField] private float gravityStrength = 30f;

        [Header("Ground Detection")]
        [SerializeField] private float groundCheckDist = 0.15f;

        [Header("References")]
        [SerializeField] private PlatformBounds platform;

        // Exposed for input handlers to write into each physics frame.
        public float MoveInput { get; set; }
        public bool JumpRequested { get; set; }

        // Current state, read by other systems (visuals, camera, etc.).
        public bool IsGrounded { get; private set; }
        public int CurrentFace { get; private set; }
        public Vector2 DownDir => FaceDown[CurrentFace];

        /// <summary>
        /// Unit vectors pointing from each face toward the platform centre (i.e. "gravity direction").
        /// </summary>
        public static readonly Vector2[] FaceDown =
        {
            Vector2.down,   // Top    – pull down
            Vector2.left,   // Right  – pull left
            Vector2.up,     // Bottom – pull up
            Vector2.right   // Left   – pull right
        };

        /// <summary>
        /// Unit vectors for positive (clockwise) walk direction on each face.
        /// </summary>
        public static readonly Vector2[] FaceRight =
        {
            Vector2.right,  // Top
            Vector2.down,   // Right
            Vector2.left,   // Bottom
            Vector2.up      // Left
        };

        private Rigidbody2D _rb;
        private CapsuleCollider2D _col;
        private float _halfHeight;
        private Vector2 _velocity;
        private float _currentLateralSpeed;

        // Pre-allocated buffer to avoid per-frame allocations in raycasts.
        private readonly RaycastHit2D[] _hitBuffer = new RaycastHit2D[8];
        private ContactFilter2D _groundFilter;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<CapsuleCollider2D>();

            // We handle physics manually, so disable the built-in gravity and rotation.
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.linearVelocity = Vector2.zero;

            _halfHeight = _col.size.y * 0.5f;

            _groundFilter = new ContactFilter2D();
            _groundFilter.useTriggers = false;
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            Vector2 down = FaceDown[CurrentFace];
            Vector2 right = FaceRight[CurrentFace];

            // -- Gravity --
            _velocity += down * (gravityStrength * dt);

            // -- Smooth horizontal movement --
            // Accelerate toward the target speed, decelerate when no input is given.
            float targetSpeed = MoveInput * moveSpeed;

            if (Mathf.Abs(MoveInput) > 0.01f)
                _currentLateralSpeed = Mathf.MoveTowards(_currentLateralSpeed, targetSpeed, acceleration * dt);
            else
                _currentLateralSpeed = Mathf.MoveTowards(_currentLateralSpeed, 0f, deceleration * dt);

            // Recompose: keep the gravity component, replace lateral with smoothed value.
            float gravComponent = Vector2.Dot(_velocity, down);
            _velocity = down * gravComponent + right * _currentLateralSpeed;

            // -- Jump --
            if (JumpRequested && IsGrounded)
            {
                // Strip any downward speed and launch outward from the surface.
                gravComponent = Vector2.Dot(_velocity, down);
                if (gravComponent > 0f) _velocity -= down * gravComponent;

                _velocity += (-down) * jumpForce;
                IsGrounded = false;
            }
            JumpRequested = false;

            // -- Integrate position --
            Vector2 nextPos = _rb.position + _velocity * dt;

            // -- Ground snap --
            nextPos = PerformGroundCheck(nextPos);

            // -- Commit --
            _rb.MovePosition(nextPos);
            _rb.linearVelocity = Vector2.zero;

            // -- Corner transition --
            if (platform != null)
                TryWrapCorner();
        }

        /// <summary>
        /// Casts a short ray from the player's feet toward the current surface.
        /// If the platform is within snap range, corrects the position and zeroes inward speed.
        /// Skips the player's own collider when evaluating hits.
        /// </summary>
        private Vector2 PerformGroundCheck(Vector2 pos)
        {
            Vector2 down = FaceDown[CurrentFace];
            Vector2 feet = pos + down * _halfHeight;

            int hitCount = Physics2D.Raycast(feet, down, _groundFilter, _hitBuffer, groundCheckDist + 1f);

            float closestDist = float.MaxValue;
            bool hitFound = false;

            for (int i = 0; i < hitCount; i++)
            {
                if (_hitBuffer[i].collider.gameObject == gameObject) continue;
                if (_hitBuffer[i].distance < closestDist)
                {
                    closestDist = _hitBuffer[i].distance;
                    hitFound = true;
                }
            }

            if (hitFound && closestDist <= groundCheckDist)
            {
                IsGrounded = true;
                pos += down * (closestDist - 0.005f);

                float inwardSpeed = Vector2.Dot(_velocity, down);
                if (inwardSpeed > 0f) _velocity -= down * inwardSpeed;
            }
            else if (!hitFound)
            {
                IsGrounded = false;
            }

            return pos;
        }

        /// <summary>
        /// Checks whether the player has moved past a platform corner.
        /// If so, moves them onto the adjacent face and preserves their lateral momentum.
        /// Runs an immediate ground check on the new face to prevent a one-frame gap.
        /// </summary>
        private void TryWrapCorner()
        {
            Rect bounds = platform.WorldBounds;
            Vector2 pos = _rb.position;

            int nextFace = -1;

            switch (CurrentFace)
            {
                case 0:
                    if (pos.x > bounds.xMax) nextFace = 1;
                    else if (pos.x < bounds.xMin) nextFace = 3;
                    break;
                case 1:
                    if (pos.y < bounds.yMin) nextFace = 2;
                    else if (pos.y > bounds.yMax) nextFace = 0;
                    break;
                case 2:
                    if (pos.x < bounds.xMin) nextFace = 3;
                    else if (pos.x > bounds.xMax) nextFace = 1;
                    break;
                case 3:
                    if (pos.y > bounds.yMax) nextFace = 0;
                    else if (pos.y < bounds.yMin) nextFace = 2;
                    break;
            }

            if (nextFace < 0) return;

            PerformFaceTransition(nextFace, pos, bounds);
        }

        /// <summary>
        /// Places the player at the correct starting position on the new face,
        /// transfers lateral speed, and re-runs ground detection so they land immediately.
        /// </summary>
        private void PerformFaceTransition(int newFace, Vector2 pos, Rect b)
        {
            float surfaceOffset = _halfHeight + 0.05f;

            switch (newFace)
            {
                case 0: // Landing on Top
                    pos.y = b.yMax + surfaceOffset;
                    pos.x = (CurrentFace == 1) ? b.xMax : b.xMin;
                    break;
                case 1: // Landing on Right
                    pos.x = b.xMax + surfaceOffset;
                    pos.y = (CurrentFace == 0) ? b.yMax : b.yMin;
                    break;
                case 2: // Landing on Bottom
                    pos.y = b.yMin - surfaceOffset;
                    pos.x = (CurrentFace == 1) ? b.xMax : b.xMin;
                    break;
                case 3: // Landing on Left
                    pos.x = b.xMin - surfaceOffset;
                    pos.y = (CurrentFace == 0) ? b.yMax : b.yMin;
                    break;
            }

            // Preserve the sign and magnitude of lateral movement through the transition.
            float lateralSpeed = Vector2.Dot(_velocity, FaceRight[CurrentFace]);

            CurrentFace = newFace;
            IsGrounded = true;

            _velocity = FaceRight[newFace] * lateralSpeed;
            _currentLateralSpeed = lateralSpeed;

            _rb.MovePosition(pos);

            // Run ground check right away so there's no floating frame after the swap.
            pos = PerformGroundCheck(pos);
            _rb.MovePosition(pos);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, DownDir * 1.5f);
        }
#endif
    }
}