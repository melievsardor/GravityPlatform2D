using UnityEngine;
using UnityEngine.EventSystems;

namespace PlatformGravity.Input
{
    /// <summary>
    /// UI component for a single on-screen control button (Left, Right, or Jump).
    /// Listens for pointer down/up/exit events and routes them to
    /// <see cref="TouchInputHandler"/> on the player.
    ///
    /// Tracks its own pressed state to prevent duplicate down/up calls
    /// when the pointer slides off the button without a proper up event.
    /// </summary>
    public class MobileButton : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public enum ButtonAction { Left, Right, Jump }

        [SerializeField] private ButtonAction action;
        [SerializeField] private TouchInputHandler target;

        private bool _pressed;

        private void Awake()
        {
            if (target == null)
                target = FindFirstObjectByType<TouchInputHandler>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_pressed) return;
            _pressed = true;

            switch (action)
            {
                case ButtonAction.Left: target.OnLeftDown(); break;
                case ButtonAction.Right: target.OnRightDown(); break;
                case ButtonAction.Jump: target.OnJumpDown(); break;
            }
        }

        public void OnPointerUp(PointerEventData eventData) => Release();
        public void OnPointerExit(PointerEventData eventData) => Release();

        private void Release()
        {
            if (!_pressed) return;
            _pressed = false;

            // Jump is a one-shot impulse, so only directional buttons need a release callback.
            switch (action)
            {
                case ButtonAction.Left: target.OnLeftUp(); break;
                case ButtonAction.Right: target.OnRightUp(); break;
            }
        }
    }
}