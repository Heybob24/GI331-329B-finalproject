using UnityEngine;

namespace DreamDungeon
{
    public enum BattleCameraMode
    {
        FullDungeonView,
        FollowAdventurer,
        FollowBattleTarget
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class BattleCameraController : MonoBehaviour
    {
        [Header("Mode Configuration")]
        [SerializeField] private BattleCameraMode currentMode = BattleCameraMode.FullDungeonView;
        [SerializeField] private Camera targetCamera;

        [Header("Framing & Clearances")]
        [SerializeField, Range(0.1f, 2f)] private float paddingCells = 0.8f;
        [SerializeField, Range(0.05f, 0.40f)] private float hudTopClearance = 0.18f;
        [SerializeField, Range(3f, 10f)] private float minOrthographicSize = 4.5f;
        [SerializeField, Range(1f, 15f)] private float smoothFollowSpeed = 6f;

        private int gridWidth = 9;
        private int gridHeight = 9;
        private float cellSize = 0.62f;
        private Vector2 worldCenter = new Vector2(0f, 0.45f);

        private Transform followTarget;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private Rect lastSafeArea;
        private bool hasGridConfig;

        public BattleCameraMode CurrentMode => currentMode;
        public Camera Camera => targetCamera;
        public float OrthographicSize => targetCamera != null ? targetCamera.orthographicSize : 5.6f;

        private void Awake()
        {
            if (targetCamera == null) targetCamera = GetComponent<Camera>() ?? Camera.main;
            if (targetCamera != null) targetCamera.orthographic = true;
        }

        private void Start()
        {
            ApplyFitting();
        }

        private void LateUpdate()
        {
            if (targetCamera == null) return;

            // Detect runtime screen resolution or safe area changes (rotation, window resize, device notch)
            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight || Screen.safeArea != lastSafeArea)
            {
                ApplyFitting();
            }

            if (currentMode == BattleCameraMode.FullDungeonView || followTarget == null)
            {
                // Lock to dungeon center in FullDungeonView
                Vector3 desired = new Vector3(worldCenter.x, worldCenter.y, targetCamera.transform.position.z);
                targetCamera.transform.position = desired;
            }
            else
            {
                // Smooth tracking within dungeon bounds
                Vector3 targetPos = followTarget.position;
                float halfHeight = targetCamera.orthographicSize;
                float halfWidth = halfHeight * targetCamera.aspect;

                float dungeonHalfW = (gridWidth * cellSize) * 0.5f;
                float dungeonHalfH = (gridHeight * cellSize) * 0.5f;

                float clampedX = Mathf.Clamp(targetPos.x, worldCenter.x - Mathf.Max(0f, dungeonHalfW - halfWidth), worldCenter.x + Mathf.Max(0f, dungeonHalfW - halfWidth));
                float clampedY = Mathf.Clamp(targetPos.y, worldCenter.y - Mathf.Max(0f, dungeonHalfH - halfHeight), worldCenter.y + Mathf.Max(0f, dungeonHalfH - halfHeight));

                Vector3 current = targetCamera.transform.position;
                Vector3 targetClamped = new Vector3(clampedX, clampedY, current.z);
                targetCamera.transform.position = Vector3.Lerp(current, targetClamped, Time.unscaledDeltaTime * smoothFollowSpeed);
            }
        }

        public void Configure(int width, int height, float size, Vector2 center)
        {
            gridWidth = Mathf.Max(1, width);
            gridHeight = Mathf.Max(1, height);
            cellSize = Mathf.Max(0.1f, size);
            worldCenter = center;
            hasGridConfig = true;

            ApplyFitting();
        }

        public void SetMode(BattleCameraMode mode)
        {
            currentMode = mode;
            if (currentMode == BattleCameraMode.FullDungeonView)
            {
                ApplyFitting();
            }
        }

        public void SetFollowTarget(Transform target)
        {
            followTarget = target;
        }

        public void ApplyFitting()
        {
            if (targetCamera == null) targetCamera = GetComponent<Camera>() ?? Camera.main;
            if (targetCamera == null) return;

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            lastSafeArea = Screen.safeArea;

            float dungeonWidth = (hasGridConfig ? gridWidth : 9) * cellSize;
            float dungeonHeight = (hasGridConfig ? gridHeight : 9) * cellSize;

            // Extra padding on edges so outer cells and HP bars are not cropped
            float pad = paddingCells * cellSize;
            float totalWidth = dungeonWidth + pad * 2f;
            float totalHeight = dungeonHeight + pad * 2f;

            // Effective camera aspect ratio considering safe area
            float screenAspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            if (Screen.safeArea.width > 0 && Screen.safeArea.height > 0)
            {
                screenAspect = Screen.safeArea.width / Screen.safeArea.height;
            }

            // In portrait orientation, top HUD occupies the top clearance space.
            // Reserve extra vertical room so the top row of dungeon cells never collides with Top HUD.
            float usableVerticalRatio = Mathf.Clamp(1f - hudTopClearance, 0.60f, 0.95f);
            float effectiveHeight = totalHeight / usableVerticalRatio;

            float sizeByHeight = effectiveHeight * 0.5f;
            float sizeByWidth = totalWidth / (2f * screenAspect);

            float targetSize = Mathf.Max(sizeByHeight, sizeByWidth);
            targetSize = Mathf.Max(targetSize, minOrthographicSize);

            targetCamera.orthographic = true;
            targetCamera.orthographicSize = targetSize;
            targetCamera.transform.position = new Vector3(worldCenter.x, worldCenter.y, -10f);
        }
    }
}
