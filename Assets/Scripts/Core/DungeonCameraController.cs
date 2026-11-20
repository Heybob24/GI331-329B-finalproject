using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace DreamDungeon
{
    public sealed class DungeonCameraController : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private DungeonGrid grid;
        [SerializeField] private float minZoom = 3.5f;
        [SerializeField] private float maxZoom = 6.2f;
        [SerializeField] private float mouseZoomSpeed = .12f;
        [SerializeField] private float dragThreshold = 4f;

        private bool dragging;
        private Vector2 lastPointer;
        private float lastPinchDistance;
        private bool pinching;

        public void Configure(Camera cameraToUse, DungeonGrid dungeonGrid)
        {
            targetCamera = cameraToUse;
            grid = dungeonGrid;
        }

        private void Awake()
        {
            if (grid == null) grid = GetComponent<DungeonGrid>();
            if (targetCamera == null) targetCamera = grid != null ? grid.WorldCamera : Camera.main;
            if (targetCamera != null && grid != null)
            {
                targetCamera.orthographic = true;
                targetCamera.transform.position = new Vector3(grid.WorldCenter.x, grid.WorldCenter.y, targetCamera.transform.position.z);
            }
        }

        private void LateUpdate()
        {
            if (targetCamera == null) return;
            HandleMouse();
            HandleTouch();
            ClampToDungeon();
        }

        private void HandleMouse()
        {
            if (Mouse.current == null) return;
            Mouse mouse = Mouse.current;
            Vector2 position = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame)
            {
                dragging = !PointerOverUi(position);
                lastPointer = position;
            }

            if (dragging && mouse.leftButton.isPressed)
            {
                Vector2 delta = position - lastPointer;
                if (delta.sqrMagnitude > dragThreshold * dragThreshold) Pan(lastPointer, position);
                lastPointer = position;
            }
            if (mouse.leftButton.wasReleasedThisFrame) dragging = false;

            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > .01f && !PointerOverUi(position))
            {
                targetCamera.orthographicSize = Mathf.Clamp(targetCamera.orthographicSize - scroll * mouseZoomSpeed, minZoom, maxZoom);
            }
        }

        private void HandleTouch()
        {
            Touchscreen screen = Touchscreen.current;
            if (screen == null || screen.touches.Count == 0) return;
            if (screen.touches.Count >= 2 && screen.touches[0].press.isPressed && screen.touches[1].press.isPressed)
            {
                TouchControl first = screen.touches[0];
                TouchControl second = screen.touches[1];
                if (PointerOverUi(first) || PointerOverUi(second)) { pinching = false; dragging = false; return; }
                Vector2 a = first.position.ReadValue();
                Vector2 b = second.position.ReadValue();
                float distance = Vector2.Distance(a, b);
                if (pinching && lastPinchDistance > 0f && distance > 0f)
                    targetCamera.orthographicSize = Mathf.Clamp(targetCamera.orthographicSize * lastPinchDistance / distance, minZoom, maxZoom);
                lastPinchDistance = distance;
                pinching = true;
                dragging = false;
                return;
            }

            pinching = false;
            TouchControl touch = screen.primaryTouch;
            Vector2 position = touch.position.ReadValue();
            if (touch.press.wasPressedThisFrame)
            {
                dragging = !PointerOverUi(touch);
                lastPointer = position;
            }
            if (dragging && touch.press.isPressed)
            {
                Vector2 delta = position - lastPointer;
                if (delta.sqrMagnitude > dragThreshold * dragThreshold) Pan(lastPointer, position);
                lastPointer = position;
            }
            if (touch.press.wasReleasedThisFrame) dragging = false;
        }

        private void Pan(Vector2 from, Vector2 to)
        {
            float depth = -targetCamera.transform.position.z;
            Vector3 before = targetCamera.ScreenToWorldPoint(new Vector3(from.x, from.y, depth));
            Vector3 after = targetCamera.ScreenToWorldPoint(new Vector3(to.x, to.y, depth));
            targetCamera.transform.position += before - after;
        }

        private void ClampToDungeon()
        {
            if (grid == null || !targetCamera.orthographic) return;
            float halfWidth = grid.Width * grid.CellSize * .5f;
            float halfHeight = grid.Height * grid.CellSize * .5f;
            float viewHalfHeight = targetCamera.orthographicSize;
            float viewHalfWidth = viewHalfHeight * targetCamera.aspect;
            Vector3 position = targetCamera.transform.position;
            position.x = viewHalfWidth >= halfWidth ? grid.WorldCenter.x : Mathf.Clamp(position.x, grid.WorldCenter.x - halfWidth + viewHalfWidth, grid.WorldCenter.x + halfWidth - viewHalfWidth);
            position.y = viewHalfHeight >= halfHeight ? grid.WorldCenter.y : Mathf.Clamp(position.y, grid.WorldCenter.y - halfHeight + viewHalfHeight, grid.WorldCenter.y + halfHeight - viewHalfHeight);
            targetCamera.transform.position = position;
        }

        private static bool PointerOverUi(Vector2 screenPosition)
        {
            if (EventSystem.current == null) return false;
            if (Mouse.current != null && Mouse.current.position.ReadValue() == screenPosition)
                return EventSystem.current.IsPointerOverGameObject();
            if (Touchscreen.current != null)
            {
                foreach (TouchControl touch in Touchscreen.current.touches)
                    if (touch.press.isPressed && touch.position.ReadValue() == screenPosition)
                        return EventSystem.current.IsPointerOverGameObject(touch.touchId.ReadValue());
            }
            return false;
        }

        private static bool PointerOverUi(TouchControl touch) =>
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.touchId.ReadValue());
    }
}
