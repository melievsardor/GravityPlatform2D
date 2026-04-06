using UnityEngine;

namespace PlatformGravity.Camera
{
    /// <summary>
    /// Smooth-damped camera that tracks a target transform.
    /// Runs in LateUpdate so it always reads the final position for the current frame.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothTime = 0.2f;
        [SerializeField] private float zOffset = -10f;

        private Vector3 _dampVelocity;

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desired = new Vector3(target.position.x, target.position.y, zOffset);
            transform.position = Vector3.SmoothDamp(
                transform.position, desired, ref _dampVelocity, smoothTime);
        }
    }
}