using UnityEngine;

namespace DreamDungeon
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaController : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;
        private ScreenOrientation lastOrientation;

        private void Awake() => rectTransform = GetComponent<RectTransform>();

        private void Update()
        {
            Vector2Int size = new Vector2Int(Screen.width, Screen.height);
            Rect safeArea = Screen.safeArea;
            if (safeArea == lastSafeArea && size == lastScreenSize && Screen.orientation == lastOrientation) return;

            lastSafeArea = safeArea;
            lastScreenSize = size;
            lastOrientation = Screen.orientation;
            ApplySafeArea(safeArea, size);
        }

        private void ApplySafeArea(Rect safeArea, Vector2Int size)
        {
            if (size.x <= 0 || size.y <= 0) return;
            rectTransform.anchorMin = new Vector2(safeArea.xMin / size.x, safeArea.yMin / size.y);
            rectTransform.anchorMax = new Vector2(safeArea.xMax / size.x, safeArea.yMax / size.y);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
